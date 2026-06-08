using System.Windows;
using System.Windows.Controls;
using ProcessLimit.Helpers;
using WinForms = System.Windows.Forms;

namespace ProcessLimit;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private WinForms.NotifyIcon? _notifyIcon;
    private bool _isClosing;

    public MainWindow()
    {
        var args = Environment.GetCommandLineArgs();
        var startMinimized = args.Contains("--minimized");

        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        InitializeComponent();

        InitTrayIcon();
        _viewModel.IsAutoStartEnabled = AutoStartHelper.IsAutoStartEnabled();

        if (startMinimized)
        {
            Hide();
            _notifyIcon?.ShowBalloonTip(2000, "ProcessLimit", "正在后台运行，双击图标打开主界面", WinForms.ToolTipIcon.Info);
        }

        Closed += OnClosed;
    }

    private void InitTrayIcon()
    {
        _notifyIcon = new WinForms.NotifyIcon();
        _notifyIcon.Icon = System.Drawing.SystemIcons.Information;
        _notifyIcon.Text = "ProcessLimit - 进程内存限制";
        _notifyIcon.Visible = true;
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

        var contextMenu = new WinForms.ContextMenuStrip();
        contextMenu.Items.Add("打开主界面", null, (s, e) => ShowMainWindow());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("退出", null, (s, e) =>
        {
            _isClosing = true;
            Close();
        });
        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _notifyIcon?.Dispose();
        base.OnClosing(e);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.Dispose();
    }

    private void SetMemoryPreset(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && long.TryParse(tag, out var mb))
        {
            _viewModel.NewRuleMemoryMB = mb;
        }
    }
}
