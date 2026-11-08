using System.Collections;
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

        GameState state;
        Coroutine messageRoutine;

        public void Init(GameState state)
        {
            Unsubscribe();
            this.state = state;
            if (isActiveAndEnabled) Subscribe();
        }

        void OnEnable() { if (state != null) Subscribe(); }
        void OnDisable() { Unsubscribe(); }

        void Subscribe()
        {
            state.MoneyChanged -= OnMoneyChanged; // guard against double subscription
            state.MoneyChanged += OnMoneyChanged;
            OnMoneyChanged(state.Coins); // show the current value right away
        }

        void Unsubscribe()
        {
            if (state != null) state.MoneyChanged -= OnMoneyChanged;
        }

        void OnMoneyChanged(int coins) { coinText.text = $"Coins: {coins}"; }

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
    }
}