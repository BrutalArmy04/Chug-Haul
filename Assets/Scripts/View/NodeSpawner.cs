using System;
using System.Collections.Generic;
using UnityEngine;
using ChugHaul.Core;

namespace ChugHaul.View
{
    // Builds the 3D object for each node: the prefab for its kind (an industry's by the cargo it
    // makes), a factory building beside cities that have one, and the production feedback.
    // Every node gets a plain root that owns its NodeView, with the art as children, so clicking
    // works whatever the art: its colliders go on the Selectable layer, and art without a
    // collider gets one around its renderers.
    public class NodeSpawner : MonoBehaviour
    {
        [Serializable]
        struct IndustryPrefab
        {
            public string cargo;      // what the industry makes (CargoType.Name): Grain, Logs, Ore
            public GameObject prefab; // farm, forest, mine
        }

        const string SelectableLayer = "Selectable"; // the layer SelectionController raycasts against

        [SerializeField] GameObject cityPrefab;
        [SerializeField] IndustryPrefab[] industryPrefabs;
        [SerializeField] GameObject fallbackIndustryPrefab; // for an industry without its own prefab yet

        [Header("Factories")]
        [SerializeField] GameObject factoryPrefab;          // placed beside cities that have a factory
        [SerializeField] float factoryDistance = 2.5f;      // from the city's centre, on the side with no track

        [Header("Stalled marker")]
        [SerializeField] GameObject stalledMarkerPrefab;    // optional "!" while production is stalled
        [SerializeField] float stalledMarkerHeight = 2.5f;

        public NodeView Spawn(Node node, Network network, Economy economy)
        {
            var root = new GameObject($"Node_{node.Id}_{node.Name}");
            root.transform.SetPositionAndRotation(new Vector3(node.X, 0f, node.Z), Quaternion.identity);
            root.transform.SetParent(transform, true);

            GameObject art = Place(ArtFor(node), root.transform, Vector3.zero);

            GameObject factory = null;
            if (node.Factory != null && factoryPrefab != null)
                factory = Place(factoryPrefab, root.transform, FreeDirection(node, network) * factoryDistance);

            MakeClickable(root);
            var view = root.AddComponent<NodeView>();
            view.Init(node.Id);

            // After MakeClickable, so the "!" markers never catch clicks.
            if (node.Kind == NodeKind.Industry && art != null)
                AddFeedback(art, economy, node, ProductionFeedback.Source.Industry);
            if (factory != null)
                AddFeedback(factory, economy, node, ProductionFeedback.Source.Factory);

            return view;
        }

        GameObject ArtFor(Node node)
        {
            if (node.Kind == NodeKind.City || node.Produces.Count == 0) return cityPrefab;

            string made = node.Produces[0].Name;
            if (industryPrefabs != null)
            {
                foreach (IndustryPrefab entry in industryPrefabs)
                    if (entry.cargo == made && entry.prefab != null) return entry.prefab;
            }
            return fallbackIndustryPrefab != null ? fallbackIndustryPrefab : cityPrefab;
        }

        // An art prefab as a child of the node. Any SelectableView it brings is removed, so a
        // click on it finds the node's own NodeView on the root.
        static GameObject Place(GameObject prefab, Transform parent, Vector3 localOffset)
        {
            if (prefab == null) return null;

            GameObject art = Instantiate(prefab, parent);
            art.transform.localPosition = localOffset;
            foreach (SelectableView stray in art.GetComponentsInChildren<SelectableView>(true))
                Destroy(stray);
            return art;
        }

        static void MakeClickable(GameObject root)
        {
            int layer = LayerMask.NameToLayer(SelectableLayer);
            if (layer < 0)
            {
                Debug.LogWarning($"No '{SelectableLayer}' layer: {root.name} can't be clicked.");
                return;
            }

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;

            if (root.GetComponentInChildren<Collider>(true) != null) return;

            // Art without a collider: one box around everything it draws.
            var box = root.AddComponent<BoxCollider>();
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            box.center = root.transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size; // the root is never rotated or scaled
        }

        void AddFeedback(GameObject target, Economy economy, Node node, ProductionFeedback.Source source)
        {
            GameObject marker = null;
            if (stalledMarkerPrefab != null)
            {
                // Not inside the node: the node's hover/selection tint colours everything under it,
                // and the marker shouldn't pulse with the building either.
                Vector3 above = target.transform.position + Vector3.up * stalledMarkerHeight;
                marker = Instantiate(stalledMarkerPrefab, above, Quaternion.identity, transform);
                marker.name = $"{target.transform.parent.name}_StalledMarker";
                if (!marker.TryGetComponent(out MarkerBob _)) marker.AddComponent<MarkerBob>();
            }

            target.AddComponent<ProductionFeedback>().Init(economy, node, source, marker);
        }

        // Of 16 directions round the node, the one furthest from every track leaving it,
        // so a factory building never sits on a line.
        static Vector3 FreeDirection(Node node, Network network)
        {
            var tracks = new List<Vector3>();
            foreach (Line line in network.LinesAt(node))
            {
                int i = line.IndexOf(node);
                if (i > 0) tracks.Add(Direction(node, line.Stops[i - 1]));
                if (i < line.Stops.Count - 1) tracks.Add(Direction(node, line.Stops[i + 1]));
            }

            Vector3 best = Vector3.back;
            float bestGap = -1f;
            for (int k = 0; k < 16; k++)
            {
                float angle = k * Mathf.PI / 8f;
                var candidate = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));

                float gap = 180f;
                foreach (Vector3 track in tracks)
                    gap = Mathf.Min(gap, Vector3.Angle(candidate, track));

                if (gap > bestGap)
                {
                    bestGap = gap;
                    best = candidate;
                }
            }
            return best;
        }

        static Vector3 Direction(Node from, Node to) =>
            new Vector3(to.X - from.X, 0f, to.Z - from.Z).normalized;
    }
}
