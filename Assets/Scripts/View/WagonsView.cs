using System;
using System.Collections.Generic;
using UnityEngine;
using ChugHaul.Core;

namespace ChugHaul.View
{
    // The wagons behind a train: one per unit of cargo, created when the train departs and
    // removed when it arrives. Each wagon sits on the exact track the locomotive has driven
    // (TrainView.Trail), a fixed distance behind it, so wagons follow it round corners.
    // A wagon appears once the locomotive has pulled far enough out of the station for it.
    [RequireComponent(typeof(TrainView))]
    public class WagonsView : MonoBehaviour
    {
        [Serializable]
        struct CargoWagon
        {
            public string cargo;      // CargoType.Name, e.g. "Logs"
            public GameObject prefab;
        }

        [SerializeField] CargoWagon[] wagonPrefabs;
        [SerializeField] GameObject fallbackPrefab;    // for cargo that has no wagon of its own yet
        [SerializeField] float locomotiveRear = 0f;    // from the train's pivot back to the locomotive's rear coupler
        [SerializeField] float wagonLength = 0.6f;     // the prefabs' length (the "L0.6" in their names)
        [SerializeField] float gap = 0.05f;            // between two vehicles

        Economy economy;
        Train train;
        TrainView locomotive;
        readonly List<GameObject> wagons = new List<GameObject>();

        public void Init(Economy economy, Train train)
        {
            this.economy = economy;
            this.train = train;
            locomotive = GetComponent<TrainView>();

            economy.TrainDeparted += OnTrainDeparted;
            economy.TrainArrived += OnTrainArrived;
        }

        void OnDestroy()
        {
            if (economy != null)
            {
                economy.TrainDeparted -= OnTrainDeparted;
                economy.TrainArrived -= OnTrainArrived;
            }
            RemoveWagons();
        }

        void OnTrainDeparted(Train departed)
        {
            if (departed != train) return;

            RemoveWagons();
            foreach (CargoType cargo in train.Cargo)
            {
                GameObject prefab = PrefabFor(cargo);
                if (prefab == null)
                {
                    Debug.LogWarning($"No wagon prefab for {cargo.Name} and no fallback set on {name}.", this);
                    continue;
                }

                // Not parented to the locomotive: wagons move along the track, not with its transform.
                var wagon = Instantiate(prefab, transform.parent);
                wagon.name = $"{name}_Wagon_{cargo.Name}";
                wagon.SetActive(false); // until there is track behind the locomotive for it
                wagons.Add(wagon);
            }
        }

        void OnTrainArrived(Train arrived, DeliveryResult result)
        {
            if (arrived == train) RemoveWagons();
        }

        GameObject PrefabFor(CargoType cargo)
        {
            if (wagonPrefabs != null)
            {
                foreach (CargoWagon entry in wagonPrefabs)
                {
                    if (entry.cargo == cargo.Name && entry.prefab != null)
                        return entry.prefab;
                }
            }
            return fallbackPrefab;
        }

        // After TrainView has moved the locomotive this frame.
        void LateUpdate()
        {
            if (wagons.Count == 0) return;

            IReadOnlyList<Vector3> trail = locomotive.Trail;
            Vector3 head = transform.position;
            float front = locomotiveRear + gap;

            foreach (GameObject wagon in wagons)
            {
                float rear = front + wagonLength;

                if (TryPointBehind(head, trail, front, out Vector3 f) &&
                    TryPointBehind(head, trail, rear, out Vector3 r) &&
                    (f - r).sqrMagnitude > 0.000001f)
                {
                    // Centre pivot between the two couplers, facing from the rear one to the front one,
                    // so the wagon turns smoothly as it rounds a corner.
                    wagon.transform.SetPositionAndRotation((f + r) * 0.5f, Quaternion.LookRotation(f - r));
                    if (!wagon.activeSelf) wagon.SetActive(true);
                }
                else if (wagon.activeSelf)
                {
                    wagon.SetActive(false);
                }

                front = rear + gap;
            }
        }

        // The point `distance` behind `head`, measured back along the driven track.
        // False if the train hasn't driven that far yet on this trip.
        static bool TryPointBehind(Vector3 head, IReadOnlyList<Vector3> trail, float distance, out Vector3 point)
        {
            Vector3 ahead = head;

            for (int i = trail.Count - 1; i >= 0; i--)
            {
                Vector3 behind = trail[i];
                float length = Vector3.Distance(ahead, behind);

                if (distance <= length)
                {
                    point = length > 0f ? Vector3.Lerp(ahead, behind, distance / length) : ahead;
                    return true;
                }

                distance -= length;
                ahead = behind;
            }

            point = default;
            return false;
        }

        void RemoveWagons()
        {
            foreach (GameObject wagon in wagons)
                if (wagon != null) Destroy(wagon);
            wagons.Clear();
        }
    }
}
