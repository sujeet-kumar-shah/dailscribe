using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DayScribe.Services;
using H.NotifyIcon;

namespace DayScribe;

public partial class MainWindow : Window
{
    private TaskbarIcon? _notifyIcon;
    private bool _isExplicitExit = false;

    public MainWindow()
    {
        InitializeComponent();
        InitializeWebView();
        InitializeNotifyIcon();
    }

    private async void InitializeWebView()
    {
        try
        {
            await MyWebView.EnsureCoreWebView2Async();
            MyWebView.Source = new Uri("http://localhost:9103");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize WebView2: {ex.Message}\nMake sure WebView2 Runtime is installed.", 
                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new TaskbarIcon();
        _notifyIcon.Icon = SystemIcons.Application;
        _notifyIcon.ToolTipText = "DayScribe Local Activity Tracker";

        var contextMenu = new System.Windows.Controls.ContextMenu();

        var openMenu = new System.Windows.Controls.MenuItem { Header = "Open Dashboard" };
        openMenu.Click += (s, e) => ShowWindow();

        var toggleMenu = new System.Windows.Controls.MenuItem { Header = "Toggle Tracking" };
        toggleMenu.Click += (s, e) => ToggleTracking();

        var exitMenu = new System.Windows.Controls.MenuItem { Header = "Quit DayScribe" };
        exitMenu.Click += (s, e) => ExitApplication();

        contextMenu.Items.Add(openMenu);
        contextMenu.Items.Add(toggleMenu);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        contextMenu.Items.Add(exitMenu);

        _notifyIcon.ContextMenu = contextMenu;
        _notifyIcon.TrayMouseDoubleClick += (s, e) => ShowWindow();
    }

    private void ShowWindow()
    {
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
    }

    private void ToggleTracking()
    {
        try
        {
            if (Program.WebApp != null)
            {
                var tracker = Program.WebApp.Services.GetService<IActivityTracker>();
                if (tracker != null)
                {
                    if (tracker.IsRunning)
                    {
                        tracker.Stop();
                        _notifyIcon?.ShowNotification("DayScribe", "Activity tracking paused.", H.NotifyIcon.Core.NotificationIcon.Info);
                    }
                    else
                    {
                        tracker.Start();
                        _notifyIcon?.ShowNotification("DayScribe", "Activity tracking resumed.", H.NotifyIcon.Core.NotificationIcon.Info);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error toggling tracker: {ex.Message}");
        }
    }

    private void ExitApplication()
    {
        _isExplicitExit = true;
        _notifyIcon?.Dispose();
        Application.Current.Shutdown();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            e.Cancel = true; // Prevent app exit
            this.Hide();     // Hide window to system tray
            _notifyIcon?.ShowNotification("DayScribe", 
                "DayScribe is still tracking your activity in the background. Access it from the system tray.", 
                H.NotifyIcon.Core.NotificationIcon.Info);
        }
        else
        {
            base.OnClosing(e);
        }
    }
}