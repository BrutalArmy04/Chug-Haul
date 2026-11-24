using System.Collections.Generic;
using UnityEngine;
using ChugHaul.Core;

namespace ChugHaul.View
{
    // Track and click area for one line, through all of its stops.
    // Colliders live on one child per segment, so clicking any part of the line selects it.
    public class LineView : SelectableView
    {
        [SerializeField] TrackBuilder track;
        readonly List<Vector3> stops = new List<Vector3>();

        public void Init(Line line, float width = 1f, float height = 0.3f, float endPadding = 1f)
        {
            Id = line.Id;

            stops.Clear();
            foreach (Node stop in line.Stops)
                stops.Add(new Vector3(stop.X, 0f, stop.Z));

            // Children are placed in world space; keep the root neutral.
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            BuildColliders(width, height, endPadding);

            track = GetComponent<TrackBuilder>();
            track.ShowGhost(stops);
            if (line.IsActive) track.ShowSolid(stops);   // handles lines already active at start
        }

        public void OnActivated() { track.ShowSolid(stops); }

        // One thin box per segment. Shortened only at the line's two ends, so it doesn't overlap
        // those cities' colliders. At middle stops the boxes meet (the city's own collider still
        // wins there, because it's hit first).
        void BuildColliders(float width, float height, float endPadding)
        {
            int last = stops.Count - 2; // index of the last segment
            for (int i = 0; i <= last; i++)
            {
                Vector3 a = stops[i];
                Vector3 b = stops[i + 1];
                float padStart = i == 0 ? endPadding : 0f;
                float padEnd = i == last ? endPadding : 0f;

                Vector3 dir = (b - a).normalized;
                float length = Mathf.Max(0.1f, Vector3.Distance(a, b) - padStart - padEnd);

                var child = new GameObject($"SegmentCollider_{i}");
                child.layer = gameObject.layer;   // must be on the selectable layer to be raycast
                child.transform.SetParent(transform, false);
                child.transform.SetPositionAndRotation(
                    a + dir * (padStart + length * 0.5f),
                    Quaternion.LookRotation(dir));

                var box = child.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, height * 0.5f, 0f);
                box.size = new Vector3(width, height, length); // length along local Z
            }
        }
    }
}
