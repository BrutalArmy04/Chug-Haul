using UnityEngine;
using ChugHaul.Core;

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

                // Temporary visible bar so hover and selected tints can be seen
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(bar.GetComponent<Collider>());   // the line's own BoxCollider handles clicks
                bar.transform.SetParent(go.transform, false);
                bar.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                bar.transform.localScale = new Vector3(0.2f, 0.05f, line.Length - 2f);
            }

            selection.Init(economy);
        }
    }
}