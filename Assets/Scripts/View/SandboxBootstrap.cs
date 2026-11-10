using ChugHaul.Core;
using System.Collections.Generic;
using UnityEngine;
using static PlasticGui.Help.GuiHelp;

namespace ChugHaul.View
{
    // TEMPORARY: sandbox only. Stefan's GameBootstrap replaces this in the main scene.
    public class SandboxBootstrap : MonoBehaviour
    {
        [SerializeField] GameObject cityPrefab;
        [SerializeField] GameObject lineClickablePrefab;
        [SerializeField] SelectionController selection;
        [SerializeField] int startCoins = 50;

        void Start()
        {
            var state = new GameState(startCoins);
            Network network = TestMap.Create();
            var economy = new Economy(state, network);
            var views = new Dictionary<int, LineView>();

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
            economy.LineActivated += activatedLine =>
            {
                Debug.Log($"LineActivated event fired for line {activatedLine.Id}");
                if (views.ContainsKey(activatedLine.Id))
                {
                    views[activatedLine.Id].OnActivated();
                    Debug.Log($"OnActivated called for line {activatedLine.Id}");
                }
                else
                {
                    Debug.LogError($"Line {activatedLine.Id} not found in views dictionary!");
                }
            };

            selection.Init(economy);
        }
    }
}