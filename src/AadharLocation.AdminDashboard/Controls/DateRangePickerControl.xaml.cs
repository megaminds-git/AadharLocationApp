using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace AadharLocation.AdminDashboard.Controls;

public partial class DateRangePickerControl : UserControl
{
    // ── Dependency Properties ──────────────────────────────────────────────────

    public static readonly DependencyProperty FromDateProperty =
        DependencyProperty.Register(nameof(FromDate), typeof(DateTime?), typeof(DateRangePickerControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnExternalDateChanged));

    public static readonly DependencyProperty ToDateProperty =
        DependencyProperty.Register(nameof(ToDate), typeof(DateTime?), typeof(DateRangePickerControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnExternalDateChanged));

    public DateTime? FromDate
    {
        get => (DateTime?)GetValue(FromDateProperty);
        set => SetValue(FromDateProperty, value);
    }

    public DateTime? ToDate
    {
        get => (DateTime?)GetValue(ToDateProperty);
        set => SetValue(ToDateProperty, value);
    }

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _suppressCalendarSync;
    private bool _awaitingEnd;
    private DateTime? _pendingStart;

    // True when PreviewMouseDown fires while popup is still open —
    // used in Click to prevent immediately re-opening after the outside-click close.
    private bool _wasOpenOnPress;

    // TextBlock inside the ToggleButton template — resolved on Loaded.
    private TextBlock? _rangeDisplay;

    // ── Constructor ───────────────────────────────────────────────────────────

    public DateRangePickerControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ToggleBtn.ApplyTemplate();
        _rangeDisplay = ToggleBtn.Template.FindName("RangeDisplay", ToggleBtn) as TextBlock;
        RefreshDisplay();
    }

    // ── Dependency property callbacks ─────────────────────────────────────────

    private static void OnExternalDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (DateRangePickerControl)d;
        ctrl.RefreshDisplay();
        ctrl.SyncCalendarHighlight();
    }

    // ── Display helpers ───────────────────────────────────────────────────────

    private void RefreshDisplay()
    {
        if (_rangeDisplay == null) return;
        if (FromDate.HasValue && ToDate.HasValue)
            _rangeDisplay.Text = $"{FromDate:MM-dd-yyyy}  —  {ToDate:MM-dd-yyyy}";
        else if (FromDate.HasValue)
            _rangeDisplay.Text = $"{FromDate:MM-dd-yyyy}  —  pick end date";
        else
            _rangeDisplay.Text = "Select date range";
    }

    private void SyncCalendarHighlight()
    {
        if (_suppressCalendarSync || RangeCalendar == null) return;
        _suppressCalendarSync = true;
        try
        {
            RangeCalendar.SelectedDates.Clear();
            if (FromDate.HasValue && ToDate.HasValue)
                RangeCalendar.SelectedDates.AddRange(FromDate.Value, ToDate.Value);
            else if (FromDate.HasValue)
                RangeCalendar.SelectedDates.Add(FromDate.Value);
        }
        finally { _suppressCalendarSync = false; }
    }

    // ── Toggle open / close ───────────────────────────────────────────────────

    // Capture the open state BEFORE the Popup's own outside-click-close fires.
    private void ToggleBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _wasOpenOnPress = CalendarPopup.IsOpen;
    }

    // Click fires after the Popup has already auto-closed (if it was open).
    private void ToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_wasOpenOnPress)
        {
            // Popup was closed by the outside-click handler — keep it closed.
            CalendarPopup.IsOpen = false;
            ToggleBtn.IsChecked = false;
        }
        else
        {
            // Popup was closed, open it now.
            _awaitingEnd = false;
            _pendingStart = null;
            HintText.Text = "Click to select start date";
            SyncCalendarHighlight();
            CalendarPopup.IsOpen = true;
            ToggleBtn.IsChecked = true;
        }
    }

    // When the popup closes for any reason, uncheck the toggle.
    private void CalendarPopup_Closed(object sender, EventArgs e)
    {
        ToggleBtn.IsChecked = false;
    }

    // ── Date click handler (two-click range) ──────────────────────────────────

    private void RangeCalendar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var dayButton = FindAncestor<CalendarDayButton>(e.OriginalSource as DependencyObject);
        if (dayButton?.DataContext is not DateTime clicked) return;

        // Block Calendar's own selection logic — we manage SelectedDates manually.
        e.Handled = true;

        HandleDateClick(clicked);
    }

    private void HandleDateClick(DateTime clicked)
    {
        _suppressCalendarSync = true;
        try
        {
            if (!_awaitingEnd)
            {
                // First click → start date
                _pendingStart = clicked;
                _awaitingEnd = true;
                FromDate = clicked;
                ToDate = null;
                RangeCalendar.SelectedDates.Clear();
                RangeCalendar.SelectedDates.Add(clicked);
                HintText.Text = "Now click to select end date";
            }
            else
            {
                // Second click → end date
                _awaitingEnd = false;
                var from = clicked < _pendingStart ? clicked : _pendingStart!.Value;
                var to   = clicked < _pendingStart ? _pendingStart!.Value : clicked;
                _pendingStart = null;
                FromDate = from;
                ToDate   = to;
                RangeCalendar.SelectedDates.Clear();
                RangeCalendar.SelectedDates.AddRange(from, to);
                HintText.Text = "Click to select start date";
            }
        }
        finally { _suppressCalendarSync = false; }

        RefreshDisplay();
    }

    // ── Footer buttons ────────────────────────────────────────────────────────

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _awaitingEnd = false;
        _pendingStart = null;
        FromDate = null;
        ToDate = null;
        _suppressCalendarSync = true;
        try { RangeCalendar.SelectedDates.Clear(); }
        finally { _suppressCalendarSync = false; }
        HintText.Text = "Click to select start date";
        CalendarPopup.IsOpen = false;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        CalendarPopup.IsOpen = false;
    }

    // ── Visual-tree helper ────────────────────────────────────────────────────

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source != null)
        {
            if (source is T match) return match;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }
}
