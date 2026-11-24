using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ChugHaul.View
{
    // One cargo line in the station panel: its name, how many are chosen out of the stock,
    // -/+ buttons, and what those wagons would earn. Shows what StationPanel tells it, nothing more.
    public class WagonRow : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text countText;
        [SerializeField] TMP_Text earningsText;
        [SerializeField] Button minusButton;
        [SerializeField] Button plusButton;

        public void Bind(string cargoName, UnityAction onMinus, UnityAction onPlus)
        {
            nameText.text = cargoName;
            minusButton.onClick.RemoveAllListeners();
            plusButton.onClick.RemoveAllListeners();
            minusButton.onClick.AddListener(onMinus);
            plusButton.onClick.AddListener(onPlus);
        }

        public void Show(string count, string earnings, bool canRemove, bool canAdd)
        {
            countText.text = count;
            earningsText.text = earnings;
            minusButton.interactable = canRemove;
            plusButton.interactable = canAdd;
        }
    }
}
