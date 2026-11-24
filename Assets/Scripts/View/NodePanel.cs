using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using ChugHaul.Core;

namespace ChugHaul.View
{
    // What a city or industry makes, what it wants and how badly, kept up to date from Core
    // events (no polling). Opened by clicking a node; clicking another node switches it,
    // clicking anything else closes it. Every number shown comes from Core or EconomyConfig.
    public class NodePanel : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;   // rich text: sections are built in code
        [SerializeField] Color stalledColor = new Color(1f, 0.6f, 0.25f);

        Economy economy;
        EconomyConfig config;
        Node node;

        // Nodes stalled right now. Core reports only changes (one Stalled, one Resumed) and
        // everything starts running, so listening from the start of the game, also while the
        // panel is closed, gives the exact current state.
        readonly HashSet<Node> productionStalled = new HashSet<Node>();
        readonly HashSet<Node> factoryStalled = new HashSet<Node>();

        // Before the first Tick, so no stall event is missed.
        public void Init(Economy economy, EconomyConfig config)
        {
            this.economy = economy;
            this.config = config;

            economy.Production.ProductionStalled += OnProductionStalled;
            economy.Production.ProductionResumed += OnProductionResumed;
            economy.Factories.FactoryStalled += OnFactoryStalled;
            economy.Factories.FactoryResumed += OnFactoryResumed;

            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (economy == null) return;
            economy.Production.ProductionStalled -= OnProductionStalled;
            economy.Production.ProductionResumed -= OnProductionResumed;
            economy.Factories.FactoryStalled -= OnFactoryStalled;
            economy.Factories.FactoryResumed -= OnFactoryResumed;
        }

        public void Open(Node node)
        {
            this.node = node;
            gameObject.SetActive(true); // OnEnable subscribes to stock and demand changes
            Refresh();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            node = null;
        }

        // Stock and demand only matter while the panel shows something.
        void OnEnable()
        {
            if (economy == null) return;
            economy.Depot.DepotChanged += OnDepotChanged;
            economy.Demand.DemandChanged += OnDemandChanged;
        }

        void OnDisable()
        {
            if (economy == null) return;
            economy.Depot.DepotChanged -= OnDepotChanged;
            economy.Demand.DemandChanged -= OnDemandChanged;
        }

        void OnDepotChanged(Node changed, CargoType cargo, int count) => RefreshIfShown(changed);
        void OnDemandChanged(Node changed, CargoType cargo, int freeSlots) => RefreshIfShown(changed);

        void OnProductionStalled(Node n) { productionStalled.Add(n); RefreshIfShown(n); }
        void OnProductionResumed(Node n) { productionStalled.Remove(n); RefreshIfShown(n); }
        void OnFactoryStalled(Node n) { factoryStalled.Add(n); RefreshIfShown(n); }
        void OnFactoryResumed(Node n) { factoryStalled.Remove(n); RefreshIfShown(n); }

        void RefreshIfShown(Node changed)
        {
            if (isActiveAndEnabled && node != null && changed == node) Refresh();
        }

        void Refresh()
        {
            title.text = node.Name;

            var text = new StringBuilder();
            text.AppendLine(KindText(node));
            AppendProduces(text);
            AppendStored(text);
            AppendWants(text);
            body.text = text.ToString().TrimEnd();
        }

        // "Industry", "City", or "City · Food plant: Grain → Food".
        static string KindText(Node node)
        {
            if (node.Kind == NodeKind.Industry) return "Industry";

            Factory f = node.Factory;
            return f == null
                ? "City"
                : $"City · {f.Output.Name} plant: {f.Input.Name} → {f.Output.Name}";
        }

        // What the node makes (raw cargo, and a factory's product) and how full its depot is.
        void AppendProduces(StringBuilder text)
        {
            var made = new List<(CargoType cargo, bool stalled)>();
            foreach (CargoType cargo in node.Produces)
                made.Add((cargo, productionStalled.Contains(node)));
            if (node.Factory != null)
                made.Add((node.Factory.Output, factoryStalled.Contains(node)));

            if (made.Count == 0) return;

            string orange = ColorUtility.ToHtmlStringRGB(stalledColor);
            text.AppendLine().AppendLine("<b>Produces</b>");
            foreach (var (cargo, isStalled) in made)
            {
                text.Append($"{cargo.Name}  {economy.Depot.Count(node, cargo)}/{config.DepotCapacity}");
                if (isStalled) text.Append($"  <color=#{orange}>stalled</color>");
                text.AppendLine();
            }
        }

        // Cargo dropped here that this node doesn't make: unwanted, or delivered when its demand
        // was full. It waits in the depot for another train to pick it up (a transfer between lines).
        void AppendStored(StringBuilder text)
        {
            var stored = new List<OfferItem>();
            foreach (OfferItem item in economy.GetOffer(node))
                if (!Makes(item.Cargo)) stored.Add(item);

            if (stored.Count == 0) return;

            text.AppendLine().AppendLine("<b>Stored</b>");
            foreach (OfferItem item in stored)
                text.AppendLine($"{item.Cargo.Name}  {item.Count}/{config.DepotCapacity}");
        }

        // By id: the map and the economy each build their own CargoType objects.
        bool Makes(CargoType cargo)
        {
            foreach (CargoType made in node.Produces)
                if (made.Id == cargo.Id) return true;
            return node.Factory != null && node.Factory.Output.Id == cargo.Id;
        }

        // What the node wants: one pip per free slot, and the price the next wagon would get.
        void AppendWants(StringBuilder text)
        {
            if (node.Demands.Count == 0) return;

            text.AppendLine().AppendLine("<b>Wants</b>");
            foreach (CargoType cargo in node.Demands)
            {
                int free = economy.Demand.FreeSlots(node, cargo);
                string pips = new string('●', free) + new string('○', config.DemandSlots - free);
                string price = free > 0 ? $"{config.PriceBySlots[free] * 100f:0}%" : "full";
                text.AppendLine($"{cargo.Name}  {pips}  {price}");
            }
        }
    }
}
