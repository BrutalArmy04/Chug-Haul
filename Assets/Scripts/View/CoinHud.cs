using System.Collections;
using System.Collections.Generic;
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

        int lastCoins;
        bool hasLast; // false until the first value is shown
        Coroutine punchRoutine;
        readonly List<GameObject> activePopups = new List<GameObject>();

        GameState state;
        Coroutine messageRoutine;

        public void Init(GameState state)
        {
            Unsubscribe();
            this.state = state;
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
            hasLast = false; // reset the last value so we don't spawn a popup on re-enable
            OnMoneyChanged(state.Coins); // show the current value right away
        }

        void Unsubscribe()
        {
            if (state != null) state.MoneyChanged -= OnMoneyChanged;
            hasLast = false;
        }

        void OnMoneyChanged(int coins)
        {
            coinText.text = $"Coins: {coins}";

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