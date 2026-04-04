using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Time Picker — modal clock-face time selector.
    ///
    /// Displays a circular clock face with hour and minute selection.
    /// Built on M3Dialog for modal container behavior.
    ///
    /// Composition:
    ///   M3Dialog — modal backdrop
    ///     Label (_timeDisplay) — current time display HH:MM
    ///     VisualElement (_amPmToggle) — AM/PM buttons
    ///     VisualElement (_clockFace) — Painter2D clock face (Painter2D)
    ///     VisualElement (_actions) — Cancel + OK buttons
    ///
    /// M3 spec:
    ///   Clock face: circular, ticks at each hour/5-min interval
    ///   Selected: --m3-primary background on selection arm
    ///   Surface: --m3-surface-container-high
    ///
    /// USS: time-picker.uss. Colors via var(--m3-*) tokens.
    /// </summary>
    public class M3TimePicker : VisualElement
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        private const string BaseClass       = "m3-time-picker";
        private const string DisplayClass    = "m3-time-picker__display";
        private const string AmPmClass       = "m3-time-picker__ampm";
        private const string AmPmBtnClass    = "m3-time-picker__ampm-btn";
        private const string AmPmActiveCls   = "m3-time-picker__ampm-btn--active";
        private const string ClockClass      = "m3-time-picker__clock";
        private const string ActionsClass    = "m3-time-picker__actions";

        private const float ClockSize    = 256f;
        private const float ClockRadius  = 96f;
        private const float TickLength   = 12f;
        private const float SelectRadius = 20f;

        // ------------------------------------------------------------------ //
        //  Children                                                            //
        // ------------------------------------------------------------------ //
        private readonly M3Dialog      _dialog;
        private readonly Label         _timeDisplay;
        private readonly Label         _amBtn;
        private readonly Label         _pmBtn;
        private readonly VisualElement _clockFace;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private TimeSpan _selectedTime;
        private TimeSpan _pendingTime;
        private bool     _selectingMinutes;

        // Cached theme colors
        private Color _themePrimary   = new Color(0.404f, 0.314f, 0.643f);
        private Color _themeOnSurface = new Color(0.110f, 0.106f, 0.122f);
        private Color _themeOnSurfaceVariant = new Color(0.290f, 0.275f, 0.306f);
        private Color _themeSecondaryContainer = new Color(0.878f, 0.843f, 0.961f);

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Fired when the user confirms a time. Arg is the chosen TimeSpan.</summary>
        public event Action<TimeSpan> OnTimeSelected;

        /// <summary>Fired when the dialog is cancelled.</summary>
        public event Action OnCancelled;

        /// <summary>Currently selected time.</summary>
        public TimeSpan Value
        {
            get => _selectedTime;
            set
            {
                _selectedTime = value;
                _pendingTime  = value;
                UpdateDisplay();
            }
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //

        public M3TimePicker()
        {
            AddToClassList(BaseClass);

            _selectedTime = TimeSpan.FromHours(12);
            _pendingTime  = _selectedTime;

            _dialog = new M3Dialog();

            // ── Time display ──
            _timeDisplay = new Label();
            _timeDisplay.AddToClassList(DisplayClass);
            _timeDisplay.AddToClassList("m3-display-large");

            // ── AM/PM toggle ──
            var amPm = new VisualElement();
            amPm.AddToClassList(AmPmClass);

            _amBtn = new Label("AM");
            _amBtn.AddToClassList(AmPmBtnClass);
            _amBtn.RegisterCallback<ClickEvent>(_ =>
            {
                if (_pendingTime.Hours >= 12)
                    _pendingTime = _pendingTime.Subtract(TimeSpan.FromHours(12));
                UpdateDisplay();
            });

            _pmBtn = new Label("PM");
            _pmBtn.AddToClassList(AmPmBtnClass);
            _pmBtn.RegisterCallback<ClickEvent>(_ =>
            {
                if (_pendingTime.Hours < 12)
                    _pendingTime = _pendingTime.Add(TimeSpan.FromHours(12));
                UpdateDisplay();
            });

            amPm.Add(_amBtn);
            amPm.Add(_pmBtn);

            // ── Clock face ──
            _clockFace = new VisualElement();
            _clockFace.AddToClassList(ClockClass);
            _clockFace.style.width  = ClockSize;
            _clockFace.style.height = ClockSize;
            _clockFace.generateVisualContent += DrawClockFace;
            _clockFace.RegisterCallback<ClickEvent>(OnClockClicked);

            // ── Actions ──
            var actions = new VisualElement();
            actions.AddToClassList(ActionsClass);

            var cancelBtn = new M3Button { Text = "Cancel" };
            cancelBtn.Variant = Enums.ButtonVariant.Text;
            cancelBtn.OnClick += () =>
            {
                _pendingTime = _selectedTime;
                Hide();
                OnCancelled?.Invoke();
            };

            var okBtn = new M3Button { Text = "OK" };
            okBtn.Variant = Enums.ButtonVariant.Filled;
            okBtn.OnClick += () =>
            {
                _selectedTime = _pendingTime;
                Hide();
                OnTimeSelected?.Invoke(_selectedTime);
            };

            actions.Add(cancelBtn);
            actions.Add(okBtn);

            _dialog.Add(_timeDisplay);
            _dialog.Add(amPm);
            _dialog.Add(_clockFace);
            _dialog.Add(actions);
            Add(_dialog);

            UpdateDisplay();
        }

        // ------------------------------------------------------------------ //
        //  Public helpers                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>Shows the time picker as a child of the given parent element.</summary>
        public void Show(VisualElement parent)
        {
            _pendingTime      = _selectedTime;
            _selectingMinutes = false;
            UpdateDisplay();
            _dialog.Show(parent);
        }

        /// <summary>Hides the time picker.</summary>
        public void Hide() => _dialog.Close();

        // ------------------------------------------------------------------ //
        //  Internal                                                            //
        // ------------------------------------------------------------------ //

        private void UpdateDisplay()
        {
            int hours   = _pendingTime.Hours % 12;
            if (hours == 0) hours = 12;
            int minutes = _pendingTime.Minutes;
            _timeDisplay.text = $"{hours:D2}:{minutes:D2}";

            bool isPm = _pendingTime.Hours >= 12;
            _amBtn.EnableInClassList(AmPmActiveCls, !isPm);
            _pmBtn.EnableInClassList(AmPmActiveCls, isPm);

            _clockFace.MarkDirtyRepaint();
        }

        private void DrawClockFace(MeshGenerationContext ctx)
        {
            float w  = _clockFace.layout.width;
            float h  = _clockFace.layout.height;
            if (w < 1f || h < 1f) return;

            var p  = ctx.painter2D;
            float cx = w / 2f;
            float cy = h / 2f;

            // ── Background circle ──
            p.fillColor   = _themeSecondaryContainer;
            p.BeginPath();
            p.Arc(new Vector2(cx, cy), ClockRadius, 0f, 360f);
            p.Fill();

            int value = _selectingMinutes ? _pendingTime.Minutes : (_pendingTime.Hours % 12);
            int steps = _selectingMinutes ? 60 : 12;

            // ── Selection arm ──
            float angle   = (value / (float)steps) * 360f - 90f;
            float armRad  = (ClockRadius - SelectRadius - 4f);
            float selX    = cx + Mathf.Cos(angle * Mathf.Deg2Rad) * armRad;
            float selY    = cy + Mathf.Sin(angle * Mathf.Deg2Rad) * armRad;

            p.strokeColor = _themePrimary;
            p.lineWidth   = 2f;
            p.BeginPath();
            p.MoveTo(new Vector2(cx, cy));
            p.LineTo(new Vector2(selX, selY));
            p.Stroke();

            // Center dot
            p.fillColor = _themePrimary;
            p.BeginPath();
            p.Arc(new Vector2(cx, cy), 4f, 0f, 360f);
            p.Fill();

            // Selection circle
            p.fillColor = _themePrimary;
            p.BeginPath();
            p.Arc(new Vector2(selX, selY), SelectRadius, 0f, 360f);
            p.Fill();

            // ── Hour / minute ticks ──
            for (int i = 0; i < steps; i++)
            {
                float a    = (i / (float)steps) * 360f - 90f;
                float rad  = Mathf.Deg2Rad * a;
                float tx   = cx + Mathf.Cos(rad) * (ClockRadius - SelectRadius - 4f);
                float ty   = cy + Mathf.Sin(rad) * (ClockRadius - SelectRadius - 4f);

                bool active = i == value;
                p.fillColor = active ? _themeSecondaryContainer : _themeOnSurface;
                p.BeginPath();
                p.Arc(new Vector2(tx, ty), active ? 0f : 2f, 0f, 360f);
                p.Fill();

                // Number label for hours only (via another pass using p is not possible;
                // numbers are drawn as Painter2D text is unsupported — skip number rendering)
            }
        }

        private void OnClockClicked(ClickEvent evt)
        {
            float w  = _clockFace.layout.width;
            float h  = _clockFace.layout.height;
            float cx = w / 2f;
            float cy = h / 2f;

            var local = evt.localPosition;
            float dx  = local.x - cx;
            float dy  = local.y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            if (dist > ClockRadius) return;

            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 90f;
            if (angle < 0) angle += 360f;

            if (_selectingMinutes)
            {
                int minutes = Mathf.RoundToInt(angle / 6f) % 60;
                bool isPm   = _pendingTime.Hours >= 12;
                int hours   = _pendingTime.Hours;
                _pendingTime = new TimeSpan(hours, minutes, 0);
            }
            else
            {
                int hour12 = Mathf.RoundToInt(angle / 30f) % 12;
                bool isPm  = _pendingTime.Hours >= 12;
                int actualHour = isPm ? (hour12 + 12) % 24 : hour12;
                if (actualHour == 0 && isPm) actualHour = 12;
                _pendingTime = new TimeSpan(actualHour, _pendingTime.Minutes, 0);
                _selectingMinutes = true; // progress to minute selection
            }

            UpdateDisplay();
        }
    }
}
