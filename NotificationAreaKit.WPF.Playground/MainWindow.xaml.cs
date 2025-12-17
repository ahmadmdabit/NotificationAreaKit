using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using NotificationAreaKit.Wpf;
using NotificationAreaKit.Wpf.UI;

namespace NotificationAreaKit.WPF.Playground;

public partial class MainWindow : Window, IDisposable
{
    // 1. DEFINE APP CONSTANTS
    // These are required by the library to register the app for notifications.
    private const string AppID = "MyCompany.NotificationKit.Playground";

    private const string AppName = "NotificationKit Playground";

    // 2. MANAGE STATE
    // A list to hold our active tray icons and a counter for unique tooltips.
    private readonly List<WpfTrayIcon> managedIcons = [];

    private int iconCounter = 0;

    private bool isDirectClose;

    // An ObservableCollection automatically updates the UI when items are added.
    public ObservableCollection<string> LogMessages { get; } = [];

    // Commands for the ContextMenu, bound via DataContext.
    public ICommand ShowWindowCommand { get; }

    public ICommand ExitCommand { get; }

    public MainWindow()
    {
        InitializeComponent();

        // Set the DataContext so the ListView and ContextMenu can bind to our properties.
        DataContext = this;

        // Initialize commands
        ShowWindowCommand = new DelegateCommand(ShowAndActivate);
        ExitCommand = new DelegateCommand(() =>
        {
            isDirectClose = true;
            Close();
        });

        Log("Playground started. Add an icon to begin.");
    }

    // 3. ICON MANAGEMENT LOGIC
    private void AddIcon_Click(object sender, RoutedEventArgs e)
    {
        iconCounter++;
        // Alternate between two different icons for variety.
        var iconPath = iconCounter % 2 == 1
            ? "pack://application:,,,/Resources/Images/Icons/star.ico"
            : "pack://application:,,,/Resources/Images/Icons/profile.ico";

        var tooltip = $"My Tray Icon #{iconCounter}";

        try
        {
            // This is the core library call to create a new tray icon.
            var trayIcon = new WpfTrayIcon(iconPath, tooltip, AppID, AppName);

            // Assign hover content if the feature is enabled.
            if (EnableHoverCheckBox.IsChecked == true)
            {
                trayIcon.HoverContent = (UIElement)this.FindResource("SampleHoverContent");
            }

            // Wire up the events to our handlers.
            trayIcon.LeftClick += OnIconLeftClick;
            trayIcon.RightClick += OnIconRightClick;
            trayIcon.DoubleClick += OnIconDoubleClick;

            managedIcons.Add(trayIcon);
            Log($"SUCCESS: Added '{tooltip}' to the notification area.");
        }
        catch (Exception ex)
        {
            Log($"ERROR: Failed to create icon. {ex.Message}");
        }
    }

    private void RemoveIcon_Click(object sender, RoutedEventArgs e)
    {
        if (managedIcons.Count == 0)
        {
            Log("INFO: No icons to remove.");
            return;
        }

        // Gracefully remove and dispose of the last icon added.
        var lastIcon = managedIcons[^1];
        managedIcons.Remove(lastIcon);
        lastIcon.Dispose(); // This is CRITICAL for cleanup.

        Log("SUCCESS: Removed last icon.");
    }

    // 4. TRAY ICON EVENT HANDLERS
    private void OnIconLeftClick(object? sender, EventArgs e)
    {
        var icon = (WpfTrayIcon)sender!;
        Log("EVENT: LeftClick received.");

        // Use the ShowNotification method for the best experience (Toast or Balloon).
        icon.ShowNotification("Hello!", $"This is a modern toast notification. [{DateTime.Now:O}]");
        Log("ACTION: ShowNotification called.");
    }

    private void OnIconRightClick(object? sender, EventArgs e)
    {
        Log("EVENT: RightClick received.");

        // Find the shared ContextMenu resource from App.xaml.
        var contextMenu = (ContextMenu)Application.Current.FindResource("TrayContextMenu");

        // Set the DataContext before showing the menu.
        // The 'this' keyword refers to the MainWindow instance, which
        // is where the ShowWindowCommand and ExitCommand properties live.
        contextMenu.DataContext = this;

        // The library provides a convenient extension method to show it at the cursor.
        contextMenu.ShowAtCursor();
        Log("ACTION: ContextMenu shown.");
    }

    private void OnIconDoubleClick(object? sender, EventArgs e)
    {
        Log("EVENT: DoubleClick received.");
        ShowAndActivate();
        Log("ACTION: Main window shown and activated.");
    }

    // 5. EDGE CASE DEMONSTRATIONS
    private void ShowBalloon_Click(object sender, RoutedEventArgs e)
    {
        if (managedIcons.Count == 0)
        {
            Log("INFO: Add an icon first to show a balloon.");
            return;
        }
        // Explicitly request the legacy balloon style.
        managedIcons[0].ShowBalloon("Hello!", $"This is a legacy balloon tip. [{DateTime.Now:O}] ");
        Log("ACTION: Explicit balloon requested.");
    }

    // 6. WINDOW LIFECYCLE AND PATTERNS
    private void WindowClosing(object sender, CancelEventArgs e)
    {
        // This implements the common "minimize to tray" pattern.
        if (!isDirectClose && HideOnCloseCheckBox.IsChecked == true)
        {
            e.Cancel = true; // Prevents the window from actually closing.
            this.Hide();
            Log("PATTERN: Window hidden instead of closing.");
        }
        else
        {
            Log("INFO: Window closing. Disposing all icons.");
            Dispose(); // Ensure cleanup if the app is exiting.
        }
    }

    private void WindowClosed(object sender, EventArgs e)
    {
        Log("INFO: Window closed.");
        Application.Current.Shutdown();
    }

    private void ShowAndActivate()
    {
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
    }

    // 7. CLEANUP
    public void Dispose()
    {
        // Dispose of all managed tray icons to remove them from the tray.
        foreach (var icon in managedIcons)
        {
            icon.Dispose();
        }
        managedIcons.Clear();
        GC.SuppressFinalize(this);
    }

    // 8. HELPER METHODS
    private void Log(string message)
    {
        message = $"[{DateTime.Now:HH:mm:ss}] {message}";

        Debug.WriteLine(message);

        // Dispatch to the UI thread to safely update the ObservableCollection.
        Dispatcher.Invoke(() => LogMessages.Add(message));

        // Auto-scroll the log viewer to the latest message.
        if (LogListView.Items.Count > 0)
        {
            LogListView.ScrollIntoView(LogListView.Items[^1]);
        }
    }
}