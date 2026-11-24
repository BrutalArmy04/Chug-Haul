using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ChugHaul.Core;

namespace ChugHaul.View
{
    // TEMPORARY: sandbox only. Stefan's GameBootstrap replaces this in the main scene.
    public class SandboxBootstrap : MonoBehaviour
    {
        [SerializeField] GameObject cityPrefab;
        [SerializeField] GameObject lineClickablePrefab;
        [SerializeField] GameObject trainPrefab;
        [SerializeField] Transform trainsParent; // empty "Trains" object in the scene
        [SerializeField] SelectionController selection;
        [SerializeField] int startCoins = 100;
        [SerializeField] float endPadding = 2f; // how far before a stop's centre trains halt
        [SerializeField] CoinHud hud;
        GameState state;
        Network network;

        readonly Dictionary<int, LineView> views = new Dictionary<int, LineView>();
        Economy economy; // field so OnDestroy can see it
        System.Action<Line> onLineActivated; // field so we can unsubscribe

        void Start()
        {
            state = new GameState(startCoins);
            network = TestMap.Create();
            economy = new Economy(state, network);

            foreach (var node in network.Nodes)
            {
                var go = Instantiate(cityPrefab, new Vector3(node.X, 0f, node.Z), Quaternion.identity);
                go.name = $"City_{node.Id}";
                go.GetComponent<NodeView>().Init(node.Id);
            }

            foreach (var line in network.Lines)
            {
                var go = Instantiate(lineClickablePrefab);
                go.name = $"Line_{line.Id}";
                var view = go.GetComponent<LineView>();
                view.Init(line);
                views[line.Id] = view;
            }

            onLineActivated = activatedLine => views[activatedLine.Id].OnActivated();
            economy.LineActivated += onLineActivated;
            economy.TrainBought += SpawnTrain;
            economy.TrainDeparted += OnTestDeparted;  // TEMPORARY test
            economy.TrainArrived += OnTestArrived;    // TEMPORARY test

            selection.Init(economy);

            hud.Init(state, economy);
            selection.PurchaseAttempted += hud.ShowPurchaseResult;

            // The only Tick caller. Added last, so the first Tick runs on a fully set-up scene.
            gameObject.AddComponent<SimulationDriver>().Init(economy);
        }

        void SpawnTrain(Train train)
        {
            var go = Instantiate(trainPrefab, trainsParent);
            go.name = $"Train_{train.Line.Id}";
            go.GetComponent<TrainView>().Init(economy, train, endPadding);
            if (go.TryGetComponent(out WagonsView wagons)) wagons.Init(economy, train);
        }

        // ---- TEMPORARY test keys. Remove before the PR: the buy prompt and station panel replace them. ----
        // M: +500 coins.   B: buy every line (some can't be clicked on the current test map).
        // T: buy a train on every bought line that has none.
        // 1-9: send every waiting train to that stop of its line (1 = first stop), loaded with
        //      whatever its current stop has in stock, up to the train's capacity.
        // F: cap the frame rate at 30 / 144 FPS. Every trip logs how long it took.
        // X: game speed 1x / 5x (production takes 20 s a unit).
        readonly Dictionary<Train, float> testDepartures = new Dictionary<Train, float>();

        void OnTestDeparted(Train train) => testDepartures[train] = Time.time;

        void OnTestArrived(Train train, DeliveryResult result)
        {
            if (testDepartures.TryGetValue(train, out float start))
                Debug.Log($"[test] Line {train.Line.Id}: trip to {train.CurrentStop.Name} took {Time.time - start:F2} s (FPS cap {Application.targetFrameRate})");
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || economy == null) return;

            if (keyboard.mKey.wasPressedThisFrame)
                state.Earn(500);

            if (keyboard.bKey.wasPressedThisFrame)
            {
                foreach (Line line in network.Lines)
                {
                    if (!line.IsActive)
                        Debug.Log($"[test] Line {line.Id}: buy line -> {economy.TryBuyLine(line.Id)}");
                }
            }

            if (keyboard.fKey.wasPressedThisFrame)
                Application.targetFrameRate = Application.targetFrameRate == 30 ? 144 : 30;

            if (keyboard.xKey.wasPressedThisFrame)
                Time.timeScale = Time.timeScale > 1f ? 1f : 5f;

            if (keyboard.tKey.wasPressedThisFrame)
            {
                foreach (Line line in network.Lines)
                {
                    if (line.IsActive && economy.GetTrain(line.Id) == null)
                        Debug.Log($"[test] Line {line.Id}: buy train -> {economy.TryBuyTrain(line.Id)}");
                }
            }

            for (int k = 0; k < 9; k++)
            {
                if (!keyboard[Key.Digit1 + k].wasPressedThisFrame) continue;

                foreach (Line line in network.Lines)
                {
                    Train train = economy.GetTrain(line.Id);
                    if (train == null || train.State != TrainState.AtStop || k >= line.Stops.Count) continue;

                    Cargo load = TestLoadFromDepot(train);
                    Debug.Log($"[test] Line {line.Id}: go to {line.Stops[k].Name} with {load.Count} wagons -> {economy.TryDispatch(line.Id, line.Stops[k], load)}");
                }
            }
        }

        Cargo TestLoadFromDepot(Train train)
        {
            var wagons = new List<CargoType>();
            foreach (OfferItem item in economy.GetOffer(train.CurrentStop))
                for (int i = 0; i < item.Count && wagons.Count < train.Capacity; i++)
                    wagons.Add(item.Cargo);
            return new Cargo(wagons);
        }
        // ---- end of TEMPORARY test keys ----

        void OnDestroy()
        {
            if (economy != null)
            {
                economy.LineActivated -= onLineActivated;
                economy.TrainBought -= SpawnTrain;
                economy.TrainDeparted -= OnTestDeparted;  // TEMPORARY test
                economy.TrainArrived -= OnTestArrived;    // TEMPORARY test
            }
            Time.timeScale = 1f;                          // TEMPORARY test (X key)
            if (selection != null) selection.PurchaseAttempted -= hud.ShowPurchaseResult;
        }
    }
}
