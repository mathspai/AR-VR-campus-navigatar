using UnityEngine;

namespace Quest3IndoorNavigation.UI
{
    [DisallowMultipleComponent]
    public sealed class NavigationPointMarker : MonoBehaviour
    {
        [SerializeField] private string label;
        [SerializeField] private float markerRadius = 0.08f;
        [SerializeField] private float labelHeight = 0.18f;
        [SerializeField] private Color markerColor = new(0.18f, 0.82f, 0.62f, 1f);
        [SerializeField] private Color labelColor = Color.white;

        private TextMesh labelMesh;

        private void Awake()
        {
            BuildMarker();
        }

        private void LateUpdate()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null && labelMesh != null)
            {
                labelMesh.transform.rotation = Quaternion.LookRotation(labelMesh.transform.position - mainCamera.transform.position);
            }
        }

        public void SetLabel(string value)
        {
            label = value;

            if (labelMesh != null)
            {
                labelMesh.text = label;
            }
        }

        private void BuildMarker()
        {
            if (transform.Find("MarkerSphere") == null)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "MarkerSphere";
                sphere.transform.SetParent(transform, false);
                sphere.transform.localScale = Vector3.one * markerRadius;

                var renderer = sphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreateMarkerMaterial();
                }
            }

            var labelTransform = transform.Find("MarkerLabel");
            if (labelTransform == null)
            {
                var labelObject = new GameObject("MarkerLabel", typeof(TextMesh));
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = Vector3.up * labelHeight;
                labelMesh = labelObject.GetComponent<TextMesh>();
            }
            else
            {
                labelMesh = labelTransform.GetComponent<TextMesh>();
            }

            if (labelMesh == null)
            {
                return;
            }

            labelMesh.text = string.IsNullOrWhiteSpace(label) ? gameObject.name : label;
            labelMesh.anchor = TextAnchor.MiddleCenter;
            labelMesh.alignment = TextAlignment.Center;
            labelMesh.characterSize = 0.045f;
            labelMesh.fontSize = 64;
            labelMesh.color = labelColor;
        }

        private Material CreateMarkerMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            material.color = markerColor;
            return material;
        }
    }
}
