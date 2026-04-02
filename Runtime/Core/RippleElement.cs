using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Core
{
    /// <summary>
    /// Overlay VisualElement that renders an expanding ripple circle.
    /// Add as a child of any interactive element. Drive via StartRipple(localPosition).
    ///
    /// Typical usage:
    ///   var ripple = new RippleElement();
    ///   button.Add(ripple);
    ///   button.RegisterCallback<PointerDownEvent>(e => ripple.StartRipple(e.localPosition));
    /// </summary>
    public class RippleElement : VisualElement
    {
        private const float RippleDurationMs = 350f;
        private const float FadeOutDurationMs = 200f;

        private Vector2 _center;
        private float   _radius;      // 0–1 normalized to element diagonal
        private float   _alpha;
        private bool    _running;
        private float   _elapsedMs;
        private float   _fadeElapsedMs;

        private IVisualElementScheduledItem _expandTimer;
        private IVisualElementScheduledItem _fadeTimer;

        /// <summary>Tint color of the ripple. Defaults to white (matching M3 OnSurface).</summary>
        public Color RippleColor { get; set; } = Color.white;

        /// <summary>Peak opacity of the ripple overlay. M3 pressed state = 0.10.</summary>
        public float PeakOpacity { get; set; } = 0.10f;

        public RippleElement()
        {
            style.position = Position.Absolute;
            style.left     = 0;
            style.right    = 0;
            style.top      = 0;
            style.bottom   = 0;
            pickingMode    = PickingMode.Ignore; // don't block events

            generateVisualContent += OnGenerateVisualContent;
        }

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Start the ripple animation from the given local position.
        /// Cancels any in-progress ripple before starting a new one.
        /// </summary>
        public void StartRipple(Vector2 localPosition)
        {
            // Cancel any in-progress timers
            _expandTimer?.Pause();
            _fadeTimer?.Pause();
            _expandTimer = null;
            _fadeTimer   = null;

            _center        = localPosition;
            _radius        = 0f;
            _alpha         = PeakOpacity;
            _elapsedMs     = 0f;
            _fadeElapsedMs = 0f;
            _running       = true;

            _expandTimer = schedule.Execute(Tick).Every(16).Until(() => !_running);
            MarkDirtyRepaint();
        }

        // ------------------------------------------------------------------ //
        //  Animation                                                           //
        // ------------------------------------------------------------------ //
        private void Tick(TimerState ts)
        {
            if (!_running) return;

            _elapsedMs += ts.deltaTime;
            float t = Mathf.Clamp01(_elapsedMs / RippleDurationMs);

            // Ease-out expansion (M3 Emphasized easing)
            _radius = Mathf.Lerp(0f, 1f, 1f - Mathf.Pow(1f - t, 3f));

            if (_radius >= 0.99f)
            {
                _running = false;
                FadeOut();
            }

            MarkDirtyRepaint();
        }

        private void FadeOut()
        {
            _fadeElapsedMs = 0f;
            _fadeTimer = schedule.Execute(FadeTick).Every(16).Until(() => _alpha <= 0f);
        }

        private void FadeTick(TimerState ts)
        {
            _alpha -= (PeakOpacity / FadeOutDurationMs) * ts.deltaTime;
            if (_alpha < 0f) _alpha = 0f;
            MarkDirtyRepaint();
        }

        // ------------------------------------------------------------------ //
        //  Rendering                                                           //
        // ------------------------------------------------------------------ //
        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            if (_radius <= 0f || _alpha <= 0f) return;

            Rect rect = contentRect;
            float diag   = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height);
            float radius = _radius * diag * 0.5f;

            var painter = ctx.painter2D;
            var c       = RippleColor;
            c.a         = _alpha;
            painter.fillColor = c;
            painter.BeginPath();
            painter.Arc(_center, radius, 0f, 360f);
            painter.ClosePath();
            painter.Fill(FillRule.OddEven);
        }
    }
}
