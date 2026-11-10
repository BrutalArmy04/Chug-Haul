using UnityEngine;
using ChugHaul.Core;

namespace ChugHaul.View
{
    [RequireComponent(typeof(BoxCollider))]
    public class LineView : SelectableView
    {
        [SerializeField] TrackBuilder track;
        Vector3 a, b;
        public void Init(Line line, float width = 1f, float height = 0.3f, float endPadding = 1f)
        {
            Id = line.Id;
            a = new Vector3(line.NodeA.X, 0f, line.NodeA.Z);
            b = new Vector3(line.NodeB.X, 0f, line.NodeB.Z);
            Vector3 dir = (b - a).normalized;

            transform.position = (a + b) * 0.5f;
            transform.rotation = Quaternion.LookRotation(dir);

            var box = GetComponent<BoxCollider>();
            box.center = new Vector3(0f, height * 0.5f, 0f);
            // Length along local Z. Shortened at both ends so it doesn't overlap the city colliders.
            box.size = new Vector3(width, height, Mathf.Max(0.1f, line.Length - 2f * endPadding));

            track = GetComponent<TrackBuilder>();
            track.ShowGhost(a, b);
            if (line.IsActive) track.ShowSolid(a, b);   // handles lines already active at start
        }

        public void OnActivated() { track.ShowSolid(a, b); }
    }
}