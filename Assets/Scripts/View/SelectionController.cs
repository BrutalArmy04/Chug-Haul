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

        Economy rules;                              // replace with the real Core class
        SelectableView hovered, selected;

        public event Action<SelectableView> SelectionChanged;
        public event Action<int, BuyResult> PurchaseAttempted;   // HUD will use this later

        public void Init(Economy rules) { this.rules = rules; }

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

        SelectableView Pick(Vector2 screenPos)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, maxDistance, selectableMask, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<SelectableView>();
            return null;
        }

        void HandleClick(SelectableView target)
        {
            if (selected != null) selected.SetSelected(false);
            selected = target;
            if (selected != null) selected.SetSelected(true);
            SelectionChanged?.Invoke(selected);

            if (target is LineView line && rules != null)
            {
                BuyResult result = rules.TryBuyLine(line.Id);   // View asks, Core decides
                Debug.Log($"Line {line.Id}: {result}");
                PurchaseAttempted?.Invoke(line.Id, result);
            }
            else if (target is NodeView node)
            {
                Debug.Log($"Selected city {node.Id}");
            }
        }
    }
}