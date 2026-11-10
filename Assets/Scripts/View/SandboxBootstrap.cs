using System.Collections.Generic;
using UnityEngine;
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
        [SerializeField] float endPadding = 1f; // LineView's padding

        readonly Dictionary<int, LineView> views = new Dictionary<int, LineView>();
        Economy economy; // field so OnDestroy can see it
        System.Action<Line> onLineActivated; // field so we can unsubscribe

        void Start()
        {
            var state = new GameState(startCoins);
            Network network = TestMap.Create();
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

            onLineActivated = activatedLine =>
            {
                views[activatedLine.Id].OnActivated();
                SpawnTrain(activatedLine);
            };
            economy.LineActivated += onLineActivated;

            selection.Init(economy);
        }

        void SpawnTrain(Line line)
        {
            var a = new Vector3(line.NodeA.X, 0f, line.NodeA.Z);
            var b = new Vector3(line.NodeB.X, 0f, line.NodeB.Z);
            var go = Instantiate(trainPrefab, a, Quaternion.identity, trainsParent);
            var train = go.GetComponent<TrainView>();
            train.Init(line.Id, a, b, endPadding);
            train.Arrived += id => economy.OnTrainArrived(id);
        }

        void OnDestroy()
        {
            if (economy != null) economy.LineActivated -= onLineActivated;
        }
    }
}