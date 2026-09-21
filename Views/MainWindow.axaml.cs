using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PortBan.Models;
using PortBan.Services;

namespace PortBan.Views;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<PortRow> _rows = [];
    private IReadOnlyList<PortRow> _all = [];
    private readonly DispatcherTimer _timer;
    private int _scanning;
    private bool _allowClose;
    private bool _paused;
    private bool _ready;
    private bool _hasLoaded;
    private string? _error;
    private string _signature = "";

    public MainWindow()
    {
        InitializeComponent();
        PortGrid.ItemsSource = _rows;
        PropertyChanged += OnWindowPropertyChanged;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += OnTimerTick;
        Opened += OnOpened;
        _ready = true;
    }

    public void AllowClose()
    {
        _allowClose = true;
        _timer.Stop();
    }

    public void ShowFromTray()
    {
        if (!IsVisible)
            Show();

        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        Activate();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _timer.Stop();
        base.OnClosing(e);
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await RefreshAsync();
        _timer.Start();
    }

    private async void OnTimerTick(object? sender, EventArgs e) => await RefreshAsync();

    private async void OnRefreshClick(object? sender, RoutedEventArgs e) => await RefreshAsync();

    private void OnFilterChanged(object? sender, TextChangedEventArgs e)
    {
        if (_ready)
            FillGrid();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => UpdateCommand();

    private async void OnKillClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PortRow row } || !row.CanKill)
            return;

        var siblings = _all.Count(item => item.Pid == row.Pid);
        var shouldRefresh = false;
        _paused = true;
        _timer.Stop();
        try
        {
            var confirmed = await AppDialog.ConfirmAsync(this, KillMessage(row, siblings));
            if (!confirmed)
                return;

            shouldRefresh = true;
            try
            {
                await Task.Run(() => ProcessKiller.Kill(row.Pid));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                await AppDialog.AlertAsync(this, "終了できませんでした", ex.Message);
            }
        }
        finally
        {
            _paused = false;
            if (!_allowClose)
                _timer.Start();
        }

        if (shouldRefresh)
            await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_paused || Interlocked.Exchange(ref _scanning, 1) == 1)
            return;

        try
        {
            SetBusy(true);
            var rows = await Task.Run(PortScanner.Scan);
            Apply(rows);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SetBusy(false);
            Interlocked.Exchange(ref _scanning, 0);
        }
    }

    private void Apply(IReadOnlyList<PortRow> rows)
    {
        _error = null;
        ErrorText.Text = "";
        ErrorText.IsVisible = false;
        var signature = Signature(rows);
        var changed = signature != _signature;
        _signature = signature;
        _all = rows;
        _hasLoaded = true;
        if (changed)
            FillGrid();
        else
            UpdateEmptyAndCount(_rows.Count);
    }

    private void ShowError(string message)
    {
        _error = message;
        _hasLoaded = true;
        ErrorText.Text = message + " 更新で再試行できます。";
        ErrorText.IsVisible = true;
        UpdateEmptyAndCount(_rows.Count);
    }

    private void FillGrid()
    {
        var selected = PortGrid.SelectedItem as PortRow;
        var selectedKey = selected is null ? null : KeyOf(selected);
        var query = FilterBox.Text?.Trim() ?? "";
        IEnumerable<PortRow> source = _all;
        if (query.Length > 0)
        {
            source = _all.Where(row =>
                row.Port.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.Protocol.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.Address.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.PidText.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.ProcessName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.Purpose.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.CommandLine.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var visible = source.ToList();
        _rows.Clear();
        foreach (var row in visible)
            _rows.Add(row);

        if (selectedKey is not null)
            PortGrid.SelectedItem = _rows.FirstOrDefault(row => KeyOf(row) == selectedKey);

        UpdateCommand();
        UpdateEmptyAndCount(visible.Count);
    }

    private void UpdateEmptyAndCount(int visibleCount)
    {
        var query = FilterBox.Text?.Trim() ?? "";
        CountText.Text = query.Length == 0
            ? _all.Count.ToString(CultureInfo.InvariantCulture) + " 件"
            : visibleCount.ToString(CultureInfo.InvariantCulture) + " / " + _all.Count.ToString(CultureInfo.InvariantCulture) + " 件";

        if (visibleCount > 0)
        {
            EmptyText.IsVisible = false;
            PortGrid.IsVisible = true;
            return;
        }

        PortGrid.IsVisible = false;
        EmptyText.IsVisible = true;
        if (!_hasLoaded)
            EmptyText.Text = "読み込み中…";
        else if (_error is not null)
            EmptyText.Text = "ポート一覧を取得できませんでした";
        else if (query.Length > 0)
            EmptyText.Text = "一致するポートはありません";
        else
            EmptyText.Text = "待ち受け中のポートはありません";
    }

    private void UpdateCommand()
    {
        if (PortGrid.SelectedItem is not PortRow row)
        {
            CommandText.Text = "";
            return;
        }

        CommandText.Text = string.IsNullOrWhiteSpace(row.CommandLine)
            ? "コマンドラインは取得できません"
            : row.CommandLine;
    }

    private void SetBusy(bool busy)
    {
        RefreshButton.IsEnabled = !busy;
        RefreshButton.Content = busy ? "更新中" : "更新";
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!_allowClose && e.Property == WindowStateProperty && WindowState == WindowState.Minimized)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
    }

    private static string KillMessage(PortRow row, int siblingPorts)
    {
        var builder = new StringBuilder();
        builder.AppendLine("このポートを使っているプロセスを終了します。");
        builder.AppendLine();
        builder.Append(row.Port.ToString(CultureInfo.InvariantCulture));
        builder.Append('/');
        builder.AppendLine(row.Protocol);
        builder.Append(row.ProcessName);
        builder.Append("（PID ");
        builder.Append(row.Pid.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("）");
        builder.AppendLine(row.Purpose);
        if (siblingPorts > 1)
        {
            builder.AppendLine();
            builder.Append("このプロセスは、ほかに ");
            builder.Append((siblingPorts - 1).ToString(CultureInfo.InvariantCulture));
            builder.AppendLine(" 件の待ち受けポートも使っています。");
        }

        builder.AppendLine();
        builder.Append("子プロセスも含めて終了します。保存していない作業は失われます。");
        return builder.ToString();
    }

    private static string Signature(IReadOnlyList<PortRow> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.Append(KeyOf(row));
            builder.Append('|');
            builder.Append(row.ProcessName);
            builder.Append('|');
            builder.Append(row.Purpose);
            builder.Append('|');
            builder.Append(row.CommandLine);
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string KeyOf(PortRow row) =>
        row.Protocol
        + "|"
        + row.Address
        + "|"
        + row.Port.ToString(CultureInfo.InvariantCulture)
        + "|"
        + row.Pid.ToString(CultureInfo.InvariantCulture);
}
