using UnityEngine;
using ChugHaul.Core;

namespace ChugHaul.View
{
    // Advances the Core simulation once per frame. The ONLY place that calls Economy.Tick:
    // Core never reads Unity time, it only gets the frame's dt from here.
    public class SimulationDriver : MonoBehaviour
    {
        Economy economy;

        public void Init(Economy economy) { this.economy = economy; }

        void Update()
        {
            economy?.Tick(Time.deltaTime);
        }
    }
}
