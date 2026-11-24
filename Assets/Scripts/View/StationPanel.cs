using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ChugHaul.Core;

namespace ChugHaul.View
{
    // Orders for a train waiting at a stop: pick a destination on its line and the wagons to
    // take from what the stop has in stock, see what the trip would earn, then Go.
    // Stock, capacity, prices and the dispatch itself all come from Core; the panel only shows
    // them and passes the player's choice on. Doing nothing leaves the train waiting.
    public class StationPanel : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] Button closeButton;

        [Header("Destination")]
        [SerializeField] Transform destinationsParent;
        [SerializeField] Button destinationTemplate;   // inactive child; its TMP_Text shows the stop's name
        [SerializeField] Color chosenColor = new Color(0.55f, 0.85f, 1f);

        [Header("Wagons")]
        [SerializeField] Transform rowsParent;
        [SerializeField] WagonRow rowTemplate;          // inactive child

        [Header("Summary")]
        [SerializeField] TMP_Text summaryText;
        [SerializeField] Button goButton;
        [SerializeField] Color unpaidColor = new Color(0.6f, 0.6f, 0.6f); // wagons that would earn nothing

        // Raised after every Go, so the HUD can explain a refusal.
        public event Action<DispatchResult> DispatchAttempted;

        Economy economy;
        Train train;
        Node destination;
        IReadOnlyList<OfferItem> offer = Array.Empty<OfferItem>();
        readonly Dictionary<CargoType, int> chosen = new Dictionary<CargoType, int>();
        readonly List<(Node stop, Button button)> destinationButtons = new List<(Node, Button)>();
        readonly List<WagonRow> rows = new List<WagonRow>();

        public void Init(Economy economy)
        {
            this.economy = economy;
            closeButton.onClick.AddListener(Close);
            goButton.onClick.AddListener(Go);
            gameObject.SetActive(false);
        }

        public void Open(Train train)
        {
            this.train = train;
            destination = null;
            chosen.Clear();

            title.text = $"Line {train.Line.Id}: at {train.CurrentStop.Name}";
            BuildDestinations();
            gameObject.SetActive(true); // OnEnable subscribes to Core
            RefreshOffer();
        }

        public void Close()
        {
            gameObject.SetActive(false); // OnDisable unsubscribes
            train = null;
        }

        void OnEnable()
        {
            if (economy == null) return;
            economy.Depot.DepotChanged += OnDepotChanged;
            economy.Demand.DemandChanged += OnDemandChanged;
            economy.TrainDeparted += OnTrainDeparted;
        }

        void OnDisable()
        {
            if (economy == null) return;
            economy.Depot.DepotChanged -= OnDepotChanged;
            economy.Demand.DemandChanged -= OnDemandChanged;
            economy.TrainDeparted -= OnTrainDeparted;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Close();
        }

        // ---- Core events while open ----

        // The stock at this stop changed (production, or another train): rebuild the rows.
        void OnDepotChanged(Node node, CargoType cargo, int count)
        {
            if (train != null && node == train.CurrentStop) RefreshOffer();
        }

        // The destination's demand changed (consumption): prices may have moved.
        void OnDemandChanged(Node node, CargoType cargo, int freeSlots)
        {
            if (train != null && node == destination) RefreshPreview();
        }

        void OnTrainDeparted(Train departed)
        {
            if (departed == train) Close();
        }

        // ---- Destination ----

        void BuildDestinations()
        {
            foreach (var (_, button) in destinationButtons) Destroy(button.gameObject);
            destinationButtons.Clear();

            foreach (Node stop in train.Line.Stops) // route order
            {
                if (stop == train.CurrentStop) continue;

                Button button = Instantiate(destinationTemplate, destinationsParent);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TMP_Text>().text = stop.Name;

                Node target = stop;
                button.onClick.AddListener(() => ChooseDestination(target));
                destinationButtons.Add((stop, button));
            }
        }

        void ChooseDestination(Node stop)
        {
            destination = stop;
            foreach (var (s, button) in destinationButtons)
                button.image.color = s == destination ? chosenColor : Color.white;
            RefreshPreview();
        }

        // ---- Wagons ----

        void RefreshOffer()
        {
            offer = economy.GetOffer(train.CurrentStop);

            // Keep the player's choices, but never more than is in stock now.
            foreach (CargoType cargo in chosen.Keys.ToList())
            {
                int stock = StockOf(cargo);
                if (stock == 0) chosen.Remove(cargo);
                else chosen[cargo] = Math.Min(chosen[cargo], stock);
            }

            foreach (WagonRow row in rows) Destroy(row.gameObject);
            rows.Clear();

            foreach (OfferItem item in offer)
            {
                WagonRow row = Instantiate(rowTemplate, rowsParent);
                row.gameObject.SetActive(true);
                CargoType cargo = item.Cargo;
                row.Bind(cargo.Name, () => Change(cargo, -1), () => Change(cargo, +1));
                rows.Add(row);
            }

            RefreshPreview();
        }

        void Change(CargoType cargo, int delta)
        {
            chosen.TryGetValue(cargo, out int n);
            if (delta > 0 && !CanAdd(cargo, n)) return;
            if (delta < 0 && n == 0) return;

            n += delta;
            if (n == 0) chosen.Remove(cargo);
            else chosen[cargo] = n;

            RefreshPreview();
        }

        // Within the stock this stop offers and the train's capacity (both from Core).
        bool CanAdd(CargoType cargo, int n) => n < StockOf(cargo) && ChosenCount() < train.Capacity;

        int StockOf(CargoType cargo)
        {
            foreach (OfferItem item in offer)
                if (item.Cargo == cargo) return item.Count;
            return 0;
        }

        int ChosenCount() => chosen.Values.Sum();

        // The chosen wagons, in the order the stop offers them.
        Cargo BuildLoad()
        {
            var wagons = new List<CargoType>();
            foreach (OfferItem item in offer)
            {
                if (chosen.TryGetValue(item.Cargo, out int n))
                    for (int i = 0; i < n; i++) wagons.Add(item.Cargo);
            }
            return new Cargo(wagons);
        }

        // ---- Preview and Go ----

        void RefreshPreview()
        {
            Cargo load = BuildLoad();
            DeliveryResult preview = destination != null
                ? economy.PreviewDelivery(train, destination, load)
                : null;

            for (int i = 0; i < offer.Count; i++)
            {
                CargoType cargo = offer[i].Cargo;
                chosen.TryGetValue(cargo, out int n);
                rows[i].Show(
                    count: $"{n} / {offer[i].Count}",
                    earnings: preview != null ? EarningsOf(preview, cargo) : "",
                    canRemove: n > 0,
                    canAdd: CanAdd(cargo, n));
            }

            string stock = offer.Count == 0 ? "Nothing in stock here. " : "";
            string money = preview != null ? $"Earns +{preview.Total}" : "Pick a destination";
            summaryText.text = $"{stock}{load.Count} / {train.Capacity} wagons   {money}";

            goButton.interactable = destination != null;
        }

        // What each wagon of this cargo would earn, in load order: "+9 +8 depot".
        string EarningsOf(DeliveryResult preview, CargoType cargo)
        {
            string grey = ColorUtility.ToHtmlStringRGB(unpaidColor);
            return string.Join(" ", preview.Wagons
                .Where(w => w.Cargo == cargo)
                .Select(w => w.Outcome switch
                {
                    WagonOutcome.Sold => $"+{w.Coins}",
                    WagonOutcome.StoredInDepot => $"<color=#{grey}>depot</color>",
                    _ => $"<color=#{grey}>lost</color>",
                }));
        }

        void Go()
        {
            if (train == null || destination == null) return;

            DispatchResult result = economy.TryDispatch(train.Line.Id, destination, BuildLoad());
            DispatchAttempted?.Invoke(result);
            // On Ok, Core raises TrainDeparted, which closes the panel.
        }
    }
}
