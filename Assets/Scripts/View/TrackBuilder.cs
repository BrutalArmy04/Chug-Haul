using System.Collections.Generic;
using UnityEngine;

namespace ChugHaul.View
{
    public class TrackBuilder : MonoBehaviour
    {
        [SerializeField] GameObject trackPrefab;
        [SerializeField] float pieceLength = 0.8f; // exact length of Track prefab
        [SerializeField] bool pivotAtCenter = false; // Track prefab's pivot is at the start of the piece, not the center
        [SerializeField] float endPadding = 1f; // = LineView's endPadding

        [SerializeField] Material ghostMaterial; // transparent URP material, faint white or blue

        readonly List<GameObject> pieces = new List<GameObject>();
        public bool IsBuilt => pieces.Count > 0;

        public void Build(Vector3 a, Vector3 b)
        {
            Clear();   // rebuilding must never duplicate track

            Vector3 flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float total = flat.magnitude;
            if (total < 0.01f || pieceLength <= 0.001f) return;

            Vector3 dir = flat / total;
            Quaternion rot = Quaternion.LookRotation(dir);

            float usable = total - 2f * endPadding;
            if (usable <= 0.01f) return;

            int count = Mathf.Max(1, Mathf.CeilToInt(usable / pieceLength));
            float scaleZ = usable / (count * pieceLength);
            float step = usable / count; // actual spacing after scaling
            Vector3 start = a + dir * endPadding;

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

        public void ShowGhost(Vector3 a, Vector3 b) { Build(a, b); ApplyMaterial(ghostMaterial); }
        public void ShowSolid(Vector3 a, Vector3 b) { Build(a, b); }   // prefab's own materials

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