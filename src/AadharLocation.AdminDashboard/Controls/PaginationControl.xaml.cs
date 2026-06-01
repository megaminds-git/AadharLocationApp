using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AadharLocation.AdminDashboard.Controls;

public partial class PaginationControl : UserControl
{
    // ── Dependency Properties ──────────────────────────────────────────────────

    public static readonly DependencyProperty CurrentPageProperty =
        DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(PaginationControl),
            new PropertyMetadata(1, static (d, _) => ((PaginationControl)d).RefreshNav()));

    public static readonly DependencyProperty TotalPagesProperty =
        DependencyProperty.Register(nameof(TotalPages), typeof(int), typeof(PaginationControl),
            new PropertyMetadata(1, static (d, _) => ((PaginationControl)d).RefreshNav()));

    public static readonly DependencyProperty TotalCountProperty =
        DependencyProperty.Register(nameof(TotalCount), typeof(int), typeof(PaginationControl),
            new PropertyMetadata(0, static (d, _) => ((PaginationControl)d).RefreshTotalCount()));

    public static readonly DependencyProperty PageSizeProperty =
        DependencyProperty.Register(nameof(PageSize), typeof(int), typeof(PaginationControl),
            new PropertyMetadata(20, static (d, _) => ((PaginationControl)d).RefreshPageSizeCombo()));

    public static readonly DependencyProperty GoToPageCommandProperty =
        DependencyProperty.Register(nameof(GoToPageCommand), typeof(ICommand), typeof(PaginationControl));

    public static readonly DependencyProperty ChangePageSizeCommandProperty =
        DependencyProperty.Register(nameof(ChangePageSizeCommand), typeof(ICommand), typeof(PaginationControl));

    // ── Properties ────────────────────────────────────────────────────────────

    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    public int TotalCount
    {
        get => (int)GetValue(TotalCountProperty);
        set => SetValue(TotalCountProperty, value);
    }

    public int PageSize
    {
        get => (int)GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }

    public ICommand? GoToPageCommand
    {
        get => (ICommand?)GetValue(GoToPageCommandProperty);
        set => SetValue(GoToPageCommandProperty, value);
    }

    public ICommand? ChangePageSizeCommand
    {
        get => (ICommand?)GetValue(ChangePageSizeCommandProperty);
        set => SetValue(ChangePageSizeCommandProperty, value);
    }

    // ── State ─────────────────────────────────────────────────────────────────

    private static readonly IReadOnlyList<int> PageSizeOptions = [10, 20, 50, 100];
    private bool _suppressing;

    // ── Constructor ───────────────────────────────────────────────────────────

    public PaginationControl()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            PageSizeCombo.ItemsSource = PageSizeOptions;
            RefreshPageSizeCombo();
            RefreshNav();
            RefreshTotalCount();
        };
    }

    // ── Refresh helpers ───────────────────────────────────────────────────────

    private void RefreshNav()
    {
        if (!IsLoaded) return;
        _suppressing = true;
        try
        {
            var total = Math.Max(1, TotalPages);
            var cur   = Math.Clamp(CurrentPage, 1, total);
            PageInput.Text       = cur.ToString();
            TotalPagesBlock.Text = $"of {total}";
            PrevBtn.IsEnabled    = cur > 1;
            NextBtn.IsEnabled    = cur < total;
        }
        finally { _suppressing = false; }
    }

    private void RefreshTotalCount()
    {
        if (!IsLoaded) return;
        TotalCountBlock.Text = TotalCount > 0 ? $"{TotalCount:N0} records" : string.Empty;
    }

    private void RefreshPageSizeCombo()
    {
        if (!IsLoaded) return;
        _suppressing = true;
        try { PageSizeCombo.SelectedItem = PageSize; }
        finally { _suppressing = false; }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void PageSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressing) return;
        if (PageSizeCombo.SelectedItem is int size && size != PageSize)
            ChangePageSizeCommand?.Execute(size);
    }

    private void PrevBtn_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentPage > 1) GoToPageCommand?.Execute(CurrentPage - 1);
    }

    private void NextBtn_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentPage < TotalPages) GoToPageCommand?.Execute(CurrentPage + 1);
    }

    private void PageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return)) return;
        NavigateToInput();
        e.Handled = true;
    }

    private void PageInput_LostFocus(object sender, RoutedEventArgs e)
    {
        PageInput.Text = CurrentPage.ToString();
    }

    private void NavigateToInput()
    {
        if (!int.TryParse(PageInput.Text, out var page)) return;
        page = Math.Clamp(page, 1, Math.Max(1, TotalPages));
        if (page != CurrentPage)
            GoToPageCommand?.Execute(page);
        else
            PageInput.Text = CurrentPage.ToString();
    }
}
