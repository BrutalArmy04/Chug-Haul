using UnityEngine;

namespace ChugHaul.View
{
    // Bobs a marker up and down around where it was placed, like the train's waiting marker.
    // Add it once the marker is at its resting position (Awake remembers it).
    public class MarkerBob : MonoBehaviour
    {
        [SerializeField] float height = 0.15f; // same as TrainView's waiting marker
        [SerializeField] float speed = 3f;

        Vector3 rest;

        void Awake() { rest = transform.position; }

        void Update()
        {
            transform.position = rest + Vector3.up * (Mathf.Sin(Time.time * speed) * height);
        }
    }
}
