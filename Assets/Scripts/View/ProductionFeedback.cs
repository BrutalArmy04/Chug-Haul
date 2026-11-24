using System.Collections;
using UnityEngine;
using ChugHaul.Core;

namespace ChugHaul.View
{
    // Shows whether an industry, or a city's factory, is working: a short pulse for every unit
    // it makes, its animations running while it works, and everything stopped with a "!" marker
    // while it is stalled. Driven only by Core events.
    //
    // Contract with the art: while working, every ParticleSystem in the prefab plays and every
    // Animator with a bool parameter "Working" gets it set to true. Stalled: false, particles stop.
    public class ProductionFeedback : MonoBehaviour
    {
        public enum Source { Industry, Factory }

        [SerializeField] float pulseScale = 1.15f;
        [SerializeField] float pulseDuration = 0.25f;

        static readonly int WorkingParam = Animator.StringToHash("Working");

        Economy economy;
        Node node;
        Source source;
        GameObject stalledMarker;
        Vector3 restScale;
        Coroutine pulse;

        // Before the first Tick, so no stall event is missed. Everything starts working;
        // Core raises a stall event if it isn't.
        public void Init(Economy economy, Node node, Source source, GameObject stalledMarker)
        {
            this.economy = economy;
            this.node = node;
            this.source = source;
            this.stalledMarker = stalledMarker;
            restScale = transform.localScale;

            if (source == Source.Industry)
            {
                economy.Production.Produced += OnProduced;
                economy.Production.ProductionStalled += OnStalled;
                economy.Production.ProductionResumed += OnResumed;
            }
            else
            {
                economy.Factories.FactoryProduced += OnFactoryProduced;
                economy.Factories.FactoryStalled += OnStalled;
                economy.Factories.FactoryResumed += OnResumed;
            }

            SetWorking(true);
        }

        void OnDestroy()
        {
            if (stalledMarker != null) Destroy(stalledMarker); // it lives outside this object, but belongs to it
            if (economy == null) return;

            if (source == Source.Industry)
            {
                economy.Production.Produced -= OnProduced;
                economy.Production.ProductionStalled -= OnStalled;
                economy.Production.ProductionResumed -= OnResumed;
            }
            else
            {
                economy.Factories.FactoryProduced -= OnFactoryProduced;
                economy.Factories.FactoryStalled -= OnStalled;
                economy.Factories.FactoryResumed -= OnResumed;
            }
        }

        void OnProduced(Node n, CargoType cargo) { if (n == node) Pulse(); }
        void OnFactoryProduced(Node n) { if (n == node) Pulse(); }
        void OnStalled(Node n) { if (n == node) SetWorking(false); }
        void OnResumed(Node n) { if (n == node) SetWorking(true); }

        void SetWorking(bool working)
        {
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>())
            {
                if (working) particles.Play();
                else particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            foreach (Animator animator in GetComponentsInChildren<Animator>())
            {
                if (HasBool(animator, WorkingParam)) animator.SetBool(WorkingParam, working);
            }

            if (stalledMarker != null) stalledMarker.SetActive(!working);
        }

        static bool HasBool(Animator animator, int hash)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
                if (p.nameHash == hash && p.type == AnimatorControllerParameterType.Bool) return true;
            return false;
        }

        void Pulse()
        {
            if (!isActiveAndEnabled) return;
            if (pulse != null) StopCoroutine(pulse);
            pulse = StartCoroutine(PulseRoutine());
        }

        IEnumerator PulseRoutine()
        {
            for (float t = 0f; t < pulseDuration; t += Time.deltaTime)
            {
                float s = 1f + (pulseScale - 1f) * Mathf.Sin(t / pulseDuration * Mathf.PI); // up and back down
                transform.localScale = restScale * s;
                yield return null;
            }
            transform.localScale = restScale;
            pulse = null;
        }
    }
}
