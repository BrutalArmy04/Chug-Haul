using UnityEngine;

namespace ChugHaul.View
{
    public abstract class SelectableView : MonoBehaviour
    {
        [SerializeField] Color hoverColor = new Color(1f, 0.95f, 0.5f);
        [SerializeField] Color selectedColor = new Color(0.5f, 1f, 0.6f);
        [SerializeField, Range(0f, 1f)] float tintAmount = 0.3f;

        public int Id { get; protected set; }
        bool hovered, selected;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;

        public void SetHovered(bool value) { hovered = value; Refresh(); }
        public void SetSelected(bool value) { selected = value; Refresh(); }

        void Refresh()
        {
            block ??= new MaterialPropertyBlock();
            // Fetch renderers every time: the track task adds children later.
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (!hovered && !selected) { r.SetPropertyBlock(null); continue; }
                var mat = r.sharedMaterial;
                if (mat == null || !mat.HasProperty(BaseColor)) continue;
                Color target = selected ? selectedColor : hoverColor;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, Color.Lerp(mat.GetColor(BaseColor), target, tintAmount));
                r.SetPropertyBlock(block);
            }
        }

        void OnDisable() { hovered = selected = false; Refresh(); }
    }
}