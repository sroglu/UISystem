using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Date Picker — modal calendar dialog.
    ///
    /// Displays a month grid calendar with year/month navigation.
    /// Built on M3Dialog for modal container behavior.
    ///
    /// Composition:
    ///   M3Dialog — modal backdrop and surface
    ///     VisualElement (_header) — month/year label + nav arrows
    ///     VisualElement (_daysHeader) — day-of-week labels (Sun–Sat)
    ///     VisualElement (_calendarGrid) — 6×7 day buttons grid
    ///     VisualElement (_actions) — Cancel + OK buttons
    ///
    /// M3 spec:
    ///   Surface: --m3-surface-container-high
    ///   Selected day: --m3-primary background, --m3-on-primary text
    ///   Today: --m3-primary outline
    ///
    /// USS: date-picker.uss. Colors via var(--m3-*) tokens.
    /// </summary>
    public class M3DatePicker : VisualElement
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        private const string BaseClass       = "m3-date-picker";
        private const string HeaderClass     = "m3-date-picker__header";
        private const string NavBtnClass     = "m3-date-picker__nav-btn";
        private const string MonthLabelClass = "m3-date-picker__month-label";
        private const string DaysHeaderClass = "m3-date-picker__days-header";
        private const string DayLabelClass   = "m3-date-picker__day-label";
        private const string GridClass       = "m3-date-picker__grid";
        private const string DayBtnClass     = "m3-date-picker__day-btn";
        private const string DayBtnSelectedClass = "m3-date-picker__day-btn--selected";
        private const string DayBtnTodayClass    = "m3-date-picker__day-btn--today";
        private const string DayBtnOtherClass    = "m3-date-picker__day-btn--other-month";
        private const string ActionsClass    = "m3-date-picker__actions";

        private static readonly string[] DayNames = { "S", "M", "T", "W", "T", "F", "S" };

        // ------------------------------------------------------------------ //
        //  Children                                                            //
        // ------------------------------------------------------------------ //
        private readonly M3Dialog      _dialog;
        private readonly Label         _monthLabel;
        private readonly VisualElement _calendarGrid;
        private readonly Label[]       _dayButtons = new Label[42]; // 6 weeks × 7 days

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private DateTime _displayMonth;
        private DateTime _selectedDate;
        private DateTime _pendingDate;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Fired when the user confirms a date. Arg is the chosen DateTime (date only).</summary>
        public event Action<DateTime> OnDateSelected;

        /// <summary>Fired when the dialog is cancelled.</summary>
        public event Action OnCancelled;

        /// <summary>Currently selected date.</summary>
        public DateTime Value
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value.Date;
                _pendingDate  = _selectedDate;
                _displayMonth = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
                RefreshCalendar();
            }
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //

        public M3DatePicker()
        {
            AddToClassList(BaseClass);

            var today     = DateTime.Today;
            _selectedDate = today;
            _pendingDate  = today;
            _displayMonth = new DateTime(today.Year, today.Month, 1);

            _dialog = new M3Dialog();

            // ── Header ──
            var header = new VisualElement();
            header.AddToClassList(HeaderClass);

            var prevBtn = new Label("\ue5cb"); // ChevronLeft
            prevBtn.AddToClassList("m3-icon");
            prevBtn.AddToClassList(NavBtnClass);
            prevBtn.RegisterCallback<ClickEvent>(_ => ChangeMonth(-1));

            _monthLabel = new Label();
            _monthLabel.AddToClassList(MonthLabelClass);
            _monthLabel.AddToClassList("m3-title-medium");

            var nextBtn = new Label("\ue5cc"); // ChevronRight
            nextBtn.AddToClassList("m3-icon");
            nextBtn.AddToClassList(NavBtnClass);
            nextBtn.RegisterCallback<ClickEvent>(_ => ChangeMonth(1));

            header.Add(prevBtn);
            header.Add(_monthLabel);
            header.Add(nextBtn);

            // ── Day-of-week header ──
            var daysHeader = new VisualElement();
            daysHeader.AddToClassList(DaysHeaderClass);
            foreach (var d in DayNames)
            {
                var lbl = new Label(d);
                lbl.AddToClassList(DayLabelClass);
                lbl.AddToClassList("m3-label-medium");
                daysHeader.Add(lbl);
            }

            // ── Calendar grid ──
            _calendarGrid = new VisualElement();
            _calendarGrid.AddToClassList(GridClass);
            for (int i = 0; i < 42; i++)
            {
                var dayBtn = new Label();
                dayBtn.AddToClassList(DayBtnClass);
                dayBtn.AddToClassList("m3-label-large");
                int capture = i;
                dayBtn.RegisterCallback<ClickEvent>(_ => OnDayClicked(capture));
                _dayButtons[i] = dayBtn;
                _calendarGrid.Add(dayBtn);
            }

            // ── Action buttons ──
            var actions = new VisualElement();
            actions.AddToClassList(ActionsClass);

            var cancelBtn = new M3Button { Text = "Cancel" };
            cancelBtn.Variant = Enums.ButtonVariant.Text;
            cancelBtn.OnClick += () =>
            {
                _pendingDate = _selectedDate;
                Hide();
                OnCancelled?.Invoke();
            };

            var okBtn = new M3Button { Text = "OK" };
            okBtn.Variant = Enums.ButtonVariant.Filled;
            okBtn.OnClick += () =>
            {
                _selectedDate = _pendingDate;
                Hide();
                OnDateSelected?.Invoke(_selectedDate);
            };

            actions.Add(cancelBtn);
            actions.Add(okBtn);

            // Assemble dialog content
            _dialog.Add(header);
            _dialog.Add(daysHeader);
            _dialog.Add(_calendarGrid);
            _dialog.Add(actions);

            Add(_dialog);
            RefreshCalendar();
        }

        // ------------------------------------------------------------------ //
        //  Public helpers                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>Shows the date picker as a child of the given parent element.</summary>
        public void Show(VisualElement parent)
        {
            _pendingDate = _selectedDate;
            _dialog.Show(parent);
        }

        /// <summary>Hides the date picker.</summary>
        public void Hide() => _dialog.Close();

        // ------------------------------------------------------------------ //
        //  Internal                                                            //
        // ------------------------------------------------------------------ //

        private void ChangeMonth(int delta)
        {
            _displayMonth = _displayMonth.AddMonths(delta);
            RefreshCalendar();
        }

        private void OnDayClicked(int index)
        {
            var date = GetDateForIndex(index);
            if (date == DateTime.MinValue) return;
            _pendingDate = date;
            RefreshCalendar();
        }

        private void RefreshCalendar()
        {
            _monthLabel.text = _displayMonth.ToString("MMMM yyyy");

            var firstDay  = _displayMonth;
            int startDow  = (int)firstDay.DayOfWeek; // 0=Sun
            var today     = DateTime.Today;
            int daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);

            for (int i = 0; i < 42; i++)
            {
                int dayNum = i - startDow + 1;
                var btn    = _dayButtons[i];

                btn.RemoveFromClassList(DayBtnSelectedClass);
                btn.RemoveFromClassList(DayBtnTodayClass);
                btn.RemoveFromClassList(DayBtnOtherClass);

                if (dayNum < 1 || dayNum > daysInMonth)
                {
                    btn.text = string.Empty;
                    btn.AddToClassList(DayBtnOtherClass);
                    continue;
                }

                btn.text = dayNum.ToString();

                var thisDate = new DateTime(_displayMonth.Year, _displayMonth.Month, dayNum);
                if (thisDate == _pendingDate.Date)
                    btn.AddToClassList(DayBtnSelectedClass);
                if (thisDate == today)
                    btn.AddToClassList(DayBtnTodayClass);
            }
        }

        private DateTime GetDateForIndex(int index)
        {
            int firstDow = (int)_displayMonth.DayOfWeek;
            int dayNum   = index - firstDow + 1;
            int daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
            if (dayNum < 1 || dayNum > daysInMonth) return DateTime.MinValue;
            return new DateTime(_displayMonth.Year, _displayMonth.Month, dayNum);
        }
    }
}
