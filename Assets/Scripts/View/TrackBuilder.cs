using System.Collections.Generic;
using UnityEngine;

namespace ChugHaul.View
{
    public class TrackBuilder : MonoBehaviour
    {
        [SerializeField] GameObject trackPrefab;
        [SerializeField] float pieceLength = 0.8f; // exact length of Track prefab
        [SerializeField] bool pivotAtCenter = false; // Track prefab's pivot is at the start of the piece, not the center
        [SerializeField] float endPadding = 1f; // = LineView's endPadding. Only at the line's first and last stop.

        [SerializeField] Material ghostMaterial; // transparent URP material, faint white or blue

        readonly List<GameObject> pieces = new List<GameObject>();
        public bool IsBuilt => pieces.Count > 0;

        // Track through every stop in order. Padding only at the two ends; at middle stops the
        // segments meet at the node's centre (under its building), so there is no gap.
        public void Build(IReadOnlyList<Vector3> stops)
        {
            Clear();   // rebuilding must never duplicate track
            if (stops == null || stops.Count < 2 || pieceLength <= 0.001f) return;

            int last = stops.Count - 2; // index of the last segment
            for (int i = 0; i <= last; i++)
            {
                float padStart = i == 0 ? endPadding : 0f;
                float padEnd = i == last ? endPadding : 0f;
                BuildSegment(stops[i], stops[i + 1], padStart, padEnd);
            }
        }

        void BuildSegment(Vector3 a, Vector3 b, float padStart, float padEnd)
        {
            Vector3 flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float total = flat.magnitude;
            if (total < 0.01f) return;

            Vector3 dir = flat / total;
            Quaternion rot = Quaternion.LookRotation(dir);

            float usable = total - padStart - padEnd;
            if (usable <= 0.01f) return;

            int count = Mathf.Max(1, Mathf.CeilToInt(usable / pieceLength));
            float scaleZ = usable / (count * pieceLength);
            float step = usable / count; // actual spacing after scaling
            Vector3 start = a + dir * padStart;

            for (int i = 0; i < count; i++)
            {
                float offset = i * step + (pivotAtCenter ? step * 0.5f : 0f);
                var piece = Instantiate(trackPrefab, start + dir * offset, rot, transform);
                piece.transform.localScale = new Vector3(1f, 1f, scaleZ);
                pieces.Add(piece);
            }
        }

        public void Clear()
        {
            foreach (var p in pieces) if (p != null) Destroy(p);
            pieces.Clear();
        }

        public void ShowGhost(IReadOnlyList<Vector3> stops) { Build(stops); ApplyMaterial(ghostMaterial); }
        public void ShowSolid(IReadOnlyList<Vector3> stops) { Build(stops); }   // prefab's own materials

        void ApplyMaterial(Material m)
        {
            foreach (var p in pieces)
                foreach (var r in p.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = m;
                    r.sharedMaterials = mats;
                }
        }
    }
}