using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using ChugHaul.Core;

namespace ChugHaul.View
{
    public class CoinHud : MonoBehaviour
    {
        [SerializeField] TMP_Text coinText;
        [SerializeField] TMP_Text messageText;
        [SerializeField] CanvasGroup messageGroup; // on MessageText, drives the fade
        [SerializeField] float messageHold = 1.2f;
        [SerializeField] float messageFade = 0.6f;

        [Header("Feedback")]
        [SerializeField] TMP_Text popupTemplate;
        [SerializeField] Color gainColor = new Color(0.4f, 1f, 0.5f);
        [SerializeField] Color spendColor = new Color(1f, 0.4f, 0.4f);
        [SerializeField] float popupRise = 60f; // UI pixels at 1920x1080 reference
        [SerializeField] float popupDuration = 0.9f;
        [SerializeField] float punchScale = 1.2f;
        [SerializeField] float punchDuration = 0.2f;

        [Header("Deliveries")]
        [SerializeField] float deliveryPopupHeight = 2.5f;   // world units above the station
        [SerializeField] float deliveryPopupDuration = 2f;
        [SerializeField] Color unpaidColor = new Color(0.8f, 0.8f, 0.8f); // a trip that earned nothing

        int lastCoins;
        bool hasLast; // false until the first value is shown
        Coroutine punchRoutine;
        readonly List<GameObject> activePopups = new List<GameObject>();

        GameState state;
        Economy economy;
        Camera cam;
        Color coinColor; // the counter's normal colour, from the scene
        Coroutine messageRoutine;

        void Awake() { coinColor = coinText.color; }

        public void Init(GameState state, Economy economy)
        {
            Unsubscribe();
            this.state = state;
            this.economy = economy;
            if (isActiveAndEnabled) Subscribe();
        }

        void OnEnable() { if (state != null) Subscribe(); }
        void OnDisable()
        { 
            Unsubscribe();
            foreach (var p in activePopups) if (p != null) Destroy(p);   // coroutines stop when disabled, so clean up
            activePopups.Clear();
            coinText.rectTransform.localScale = Vector3.one;
            punchRoutine = null;
        }

        void Subscribe()
        {
            state.MoneyChanged -= OnMoneyChanged; // guard against double subscription
            state.MoneyChanged += OnMoneyChanged;
            if (economy != null)
            {
                economy.TrainArrived -= OnTrainArrived;
                economy.TrainArrived += OnTrainArrived;
            }
            hasLast = false; // reset the last value so we don't spawn a popup on re-enable
            OnMoneyChanged(state.Coins); // show the current value right away
        }

        void Unsubscribe()
        {
            if (state != null) state.MoneyChanged -= OnMoneyChanged;
            if (economy != null) economy.TrainArrived -= OnTrainArrived;
            hasLast = false;
        }

        void OnMoneyChanged(int coins)
        {
            coinText.text = $"Coins: {coins}";
            coinText.color = coins < 0 ? spendColor : coinColor; // red while in debt

            if (hasLast && coins != lastCoins)
            {
                SpawnPopup(coins - lastCoins);
                Punch();
            }
            lastCoins = coins;
            hasLast = true;
        }

        // Called from the bootstrap with SelectionController.PurchaseAttempted
        public void ShowPurchaseResult(int lineId, BuyResult result)
        {
            string msg = result switch
            {
                BuyResult.Ok => "Line bought!",
                BuyResult.NotEnoughCoins => "Not enough coins",
                BuyResult.AlreadyActive => "Already active",
                _ => result.ToString()
            };
            ShowMessage(msg);
        }

        void ShowMessage(string msg)
        {
            messageText.text = msg;
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(FadeRoutine());
        }

        IEnumerator FadeRoutine()
        {
            messageGroup.alpha = 1f;
            yield return new WaitForSeconds(messageHold);
            for (float t = 0f; t < messageFade; t += Time.deltaTime)
            {
                messageGroup.alpha = 1f - t / messageFade;
                yield return null;
            }
            messageGroup.alpha = 0f;
            messageRoutine = null;
        }

        void SpawnPopup(int delta)
        {
            if (popupTemplate == null) return;

            var popup = Instantiate(popupTemplate, popupTemplate.transform.parent);
            popup.gameObject.SetActive(true);
            popup.text = delta > 0 ? $"+{delta}" : $"-{-delta}";
            popup.color = delta > 0 ? gainColor : spendColor;
            activePopups.Add(popup.gameObject);
            StartCoroutine(PopupRoutine(popup));
        }

        IEnumerator PopupRoutine(TMP_Text popup)
        {
            var rt = popup.rectTransform;
            Vector2 start = rt.anchoredPosition;
            Color c = popup.color;

            for (float t = 0f; t < popupDuration; t += Time.deltaTime)
            {
                float k = t / popupDuration;
                rt.anchoredPosition = start + Vector2.up * (popupRise * k);
                popup.color = new Color(c.r, c.g, c.b, 1f - k);
                yield return null;
            }
            activePopups.Remove(popup.gameObject);
            Destroy(popup.gameObject);
        }

        // Above the station the train arrived at: what the trip earned and what happened to each
        // wagon. Everything comes from Core's DeliveryResult; nothing is computed here.
        void OnTrainArrived(Train train, DeliveryResult result)
        {
            Node stop = train.CurrentStop; // Core has already moved the train to its destination
            var anchor = new Vector3(stop.X, deliveryPopupHeight, stop.Z);
            SpawnWorldPopup(anchor, DeliveryText(result), result.Total > 0 ? gainColor : unpaidColor);
        }

        // "+34  3× Food", then a line each for unpaid wagons: "1× Grain → depot", "1× Ore lost".
        static string DeliveryText(DeliveryResult result)
        {
            string sold = Wagons(result, WagonOutcome.Sold);
            string text = $"+{result.Total}  " + (result.Wagons.Count == 0 ? "(empty)" : sold);

            string stored = Wagons(result, WagonOutcome.StoredInDepot);
            if (stored.Length > 0) text += $"\n{stored} → depot";

            string lost = Wagons(result, WagonOutcome.Lost);
            if (lost.Length > 0) text += $"\n{lost} lost";

            return text.TrimEnd();
        }

        // "3× Food, 1× Grain" for the wagons with this outcome, in load order.
        static string Wagons(DeliveryResult result, WagonOutcome outcome)
        {
            return string.Join(", ", result.Wagons
                .Where(w => w.Outcome == outcome)
                .GroupBy(w => w.Cargo.Name)
                .Select(g => $"{g.Count()}× {g.Key}"));
        }

        void SpawnWorldPopup(Vector3 world, string text, Color color)
        {
            if (popupTemplate == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            var popup = Instantiate(popupTemplate, popupTemplate.transform.parent);
            popup.gameObject.SetActive(true);
            popup.text = text;
            popup.color = color;
            popup.alignment = TextAlignmentOptions.Bottom;          // centred above its point, extra lines grow up
            popup.textWrappingMode = TextWrappingModes.NoWrap;
            activePopups.Add(popup.gameObject);
            StartCoroutine(WorldPopupRoutine(popup, world));
        }

        // Stays on its point of the map while the camera moves, rising and fading out.
        IEnumerator WorldPopupRoutine(TMP_Text popup, Vector3 world)
        {
            var rt = popup.rectTransform;
            Color c = popup.color;
            float scale = popup.canvas != null ? popup.canvas.scaleFactor : 1f; // overlay canvas: position is in screen pixels

            for (float t = 0f; t < deliveryPopupDuration; t += Time.deltaTime)
            {
                float k = t / deliveryPopupDuration;
                Vector3 screen = cam.WorldToScreenPoint(world);
                popup.enabled = screen.z > 0f; // hidden while its point is behind the camera
                rt.position = new Vector3(screen.x, screen.y + popupRise * scale * k, 0f);
                popup.color = new Color(c.r, c.g, c.b, 1f - k);
                yield return null;
            }
            activePopups.Remove(popup.gameObject);
            Destroy(popup.gameObject);
        }

        void Punch()
        {
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(PunchRoutine());
        }

        IEnumerator PunchRoutine()
        {
            var tr = coinText.rectTransform;
            for (float t = 0f; t < punchDuration; t += Time.deltaTime)
            {
                float s = 1f + (punchScale - 1f) * Mathf.Sin(t / punchDuration * Mathf.PI);   // up and back down
                tr.localScale = Vector3.one * s;
                yield return null;
            }
            tr.localScale = Vector3.one;
            punchRoutine = null;
        }
    }
}