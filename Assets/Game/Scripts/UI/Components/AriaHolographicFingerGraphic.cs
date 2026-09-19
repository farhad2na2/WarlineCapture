using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Input-transparent presentation. Local origin is the actual fingertip contact.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AriaHolographicFingerGraphic : MaskableGraphic
    {
        private AriaPlayModel state;
        private Vector2 position, approachFrom, approachTarget;
        private float approachAt, approachDuration, contactAt = -10;
        private bool initialized, aiming, pressed;
        private static readonly Vector2[] Outline =
        {
            new(-7, -3), new(-7, -42), new(-20, -32), new(-27, -39),
            new(-12, -69), new(-4, -82), new(31, -82), new(38, -57),
            new(36, -36), new(29, -30), new(23, -34), new(18, -28),
            new(11, -30), new(7, -3), new(3, 2), new(-3, 2)
        };

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            // Include the entire hand and contact ripple in the culling rectangle.
            rectTransform.sizeDelta = new Vector2(240, 240);
            initialized = aiming = pressed = false;
        }

        public void Present(AriaPlayModel value)
        {
            state = value;
            if (!initialized)
            {
                position = new Vector2(Screen.safeArea.center.x, Screen.safeArea.center.y);
                initialized = true;
            }
            bool nextAim = value.Phase == AriaPlayPhase.Aiming;
            if (nextAim && (!aiming || (approachTarget - value.Target).sqrMagnitude > 1))
            {
                approachFrom = position; approachTarget = value.Target;
                approachAt = Time.unscaledTime;
                approachDuration = Mathf.Clamp(value.AimDueAt - approachAt - .1f, .05f, .7f);
            }
            aiming = nextAim;
            if (value.Pressed && !pressed) contactAt = Time.unscaledTime;
            pressed = value.Pressed;
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            // Never smooth a pressed contact: a real drag must be shown at its exact position.
            if (state.Pressed || state.Phase == AriaPlayPhase.Touching) position = state.Contact;
            else if (aiming)
                position = Vector2.Lerp(approachFrom, approachTarget,
                    Mathf.SmoothStep(0, 1, (Time.unscaledTime - approachAt) / approachDuration));
            float scale = Mathf.Clamp(Screen.height / 720f, .8f, 2.5f);
            rectTransform.position = position;
            rectTransform.localScale = Vector3.one * scale;
            // Rotate about the fingertip, keeping the hand on screen without moving the touch.
            float angle = position.y < Screen.safeArea.yMin + 100 * scale ? 180 :
                position.x > Screen.safeArea.xMax - 55 * scale ? -50 : 0;
            rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var center = new Vector2(8, -47);
            var fill = new Color32(0, 85, 120, 190);
            for (int i = 0; i < Outline.Length; i++)
            {
                var a = Outline[i]; var b = Outline[(i + 1) % Outline.Length];
                int offset = mesh.currentVertCount;
                mesh.AddVert(center, fill, Vector2.zero);
                mesh.AddVert(a, new Color32(0, 180, 230, 165), Vector2.zero);
                mesh.AddVert(b, fill, Vector2.zero);
                mesh.AddTriangle(offset, offset + 1, offset + 2);
                Line(mesh, a, b, 9, new Color32(0, 190, 255, 35));
                Line(mesh, a, b, 5, new Color32(0, 220, 255, 90));
                Line(mesh, a, b, 1.8f, new Color32(165, 250, 255, 255));
                Line(mesh, center, a, .65f, new Color32(70, 220, 255, 100));
            }
            Ring(mesh, pressed ? 13 : 10, 2, new Color32(150, 250, 255, 240));
            Ring(mesh, pressed ? 19 : 15, 3, new Color32(0, 220, 255, 110));
            float ripple = (Time.unscaledTime - contactAt) / .65f;
            if (ripple >= 0 && ripple < 1)
                Ring(mesh, Mathf.Lerp(15, 36, ripple), 2.5f,
                    new Color32(70, 240, 255, (byte)(230 * (1 - ripple))));
        }

        private static void Ring(VertexHelper mesh, float radius, float width, Color32 color)
        {
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI / 24, b = (i + 1) * Mathf.PI / 24;
                Line(mesh, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, color);
            }
        }

        private static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color32 color)
        {
            Vector2 delta = b - a, normal = new Vector2(-delta.y, delta.x).normalized * (width * .5f);
            int offset = mesh.currentVertCount;
            mesh.AddVert(a - normal, color, Vector2.zero); mesh.AddVert(a + normal, color, Vector2.zero);
            mesh.AddVert(b + normal, color, Vector2.zero); mesh.AddVert(b - normal, color, Vector2.zero);
            mesh.AddTriangle(offset, offset + 1, offset + 2); mesh.AddTriangle(offset, offset + 2, offset + 3);
        }
    }
}
