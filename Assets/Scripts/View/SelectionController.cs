using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ChugHaul.Core;

namespace ChugHaul.View
{
    public class SelectionController : MonoBehaviour
    {
        [SerializeField] Camera cam;                 // falls back to Camera.main
        [SerializeField] LayerMask selectableMask;
        [SerializeField] float maxDistance = 500f;

        Economy economy;
        SelectableView hovered, selected;

        public event Action<SelectableView> SelectionChanged;
        public event Action<int, BuyResult> PurchaseAttempted;

        public void Init(Economy economy) { this.economy = economy; }

        void Awake() { if (cam == null) cam = Camera.main; }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || cam == null) return;

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            var hit = overUi ? null : Pick(mouse.position.ReadValue());

            if (hit != hovered)
            {
                if (hovered != null) hovered.SetHovered(false);
                hovered = hit;
                if (hovered != null) hovered.SetHovered(true);
            }

            if (mouse.leftButton.wasPressedThisFrame && !overUi)
                HandleClick(hit);
        }

        readonly RaycastHit[] hits = new RaycastHit[16];

        // Trains win: they are small and wait right beside buildings whose bigger, taller colliders
        // would otherwise hide them. For anything else, the nearest hit.
        SelectableView Pick(Vector2 screenPos)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            int count = Physics.RaycastNonAlloc(ray, hits, maxDistance, selectableMask, QueryTriggerInteraction.Ignore);

            SelectableView nearest = null, train = null;
            float nearestDistance = float.MaxValue, trainDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var view = hits[i].collider.GetComponentInParent<SelectableView>();
                if (view == null) continue;

                float distance = hits[i].distance;
                if (view is TrainView && distance < trainDistance) { train = view; trainDistance = distance; }
                if (distance < nearestDistance) { nearest = view; nearestDistance = distance; }
            }

            return train != null ? train : nearest;
        }

        void HandleClick(SelectableView target)
        {
            if (selected != null) selected.SetSelected(false);
            selected = target;
            if (selected != null) selected.SetSelected(true);
            SelectionChanged?.Invoke(selected);

            if (target is LineView line && economy != null)
            {
                BuyResult result = economy.TryBuyLine(line.Id);   // View asks, Core decides
                // Debug.Log($"Line {line.Id}: {result}");
                PurchaseAttempted?.Invoke(line.Id, result);
            }
            //else if (target is NodeView node)
            //{
            //    Debug.Log($"Selected city {node.Id}");
            //}
        }
    }
}