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

    // Dynamic Icon State
    private WpfTrayIcon? cpuTrayIcon;

    private DynamicIconBuffer? cpuIconBuffer;
    private System.Threading.Timer? cpuTimer;
    private readonly Random random = new();

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
    private void AddIconClick(object sender, RoutedEventArgs e)
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

    private void RemoveIconClick(object sender, RoutedEventArgs e)
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
    private void ShowBalloonClick(object sender, RoutedEventArgs e)
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

    #region Dynamic Icon Implementation

    private void StartCpuMockClick(object sender, RoutedEventArgs e)
    {
        if (cpuTrayIcon != null)
        {
            Log("INFO: CPU Mock is already running.");
            return;
        }

        try
        {
            // 1. Create a dedicated tray icon for the CPU meter
            // We use a placeholder icon initially.
            cpuTrayIcon = new WpfTrayIcon(
                "pack://application:,,,/Resources/Images/Icons/notification.ico",
                "CPU Usage: 0%",
                AppID + ".CPU", // Unique AppID for this icon
                AppName);

            // 2. Initialize the Zero-Allocation Buffer (32x32 pixels)
            cpuIconBuffer = new DynamicIconBuffer(32, 32);

            // 3. Start the background timer (Updates every 500ms)
            cpuTimer = new System.Threading.Timer(CpuTimerTick, null, 0, 500);

            managedIcons.Add(cpuTrayIcon);
            Log("SUCCESS: Started CPU Mock (Dynamic Icon).");
        }
        catch (Exception ex)
        {
            Log($"ERROR: Failed to start CPU Mock. {ex.Message}");
        }
    }

    private void StopCpuMockClick(object sender, RoutedEventArgs e)
    {
        if (cpuTrayIcon == null) return;

        // Stop timer
        cpuTimer?.Dispose();
        cpuTimer = null;

        // Dispose buffer
        cpuIconBuffer?.Dispose();
        cpuIconBuffer = null;

        // Remove icon
        cpuTrayIcon.Dispose();
        managedIcons.Remove(cpuTrayIcon);
        cpuTrayIcon = null;

        Log("SUCCESS: Stopped CPU Mock.");
    }

    private int number = 0;

    // This runs on a ThreadPool thread.
    private void CpuTimerTick(object? state)
    {
        if (cpuTrayIcon == null || cpuIconBuffer == null) return;

        //if (++number > 100)
        //{
        //    number = 0;
        //}

        // 1. Generate Mock Data
        int cpuUsage = random.Next(0, 101); // number;

        // 2. Render to Buffer (Zero Allocation)
        // We use the unsafe Span<uint> API to write pixels directly.
        // Pass 'cpuUsage' as state. Use a static method or lambda that doesn't capture.
#pragma warning disable CA1416 // Validate platform compatibility
        cpuIconBuffer.UpdatePixels(cpuUsage, static (pixels, usage) =>
        {
            // Clear background (Transparent)
            pixels.Clear();

            // Determine Color
            uint color = usage switch
            {
                >= 80 => 0xFFDD0088, // Red (ARGB)
                >= 50 => 0xFFFFFF00, // Yellow
                _ => 0xFF00FF00      // Green
            };

            string text = usage.ToString();

            // STRATEGY:
            // 0-99: Use Standard Font (Proportional Spacing) at 2x Scale.
            // 100:  Use Condensed Font at 2x Scale.

            bool useCondensed = usage >= 100;
            int scale = 2; // Always use 2x for readability
            //int spacing = 1 * scale; // 2px spacing

            // GAP MANAGEMENT
            // User Configurable Gap (in screen pixels).
            // CRITICAL: Must be an EVEN number (0, 2, 4, -2) to prevent blurring.
            // If gap is odd, the second digit starts on a half-pixel boundary during downscaling.
            int desiredGap = 2;

            // Enforce evenness for sharpness
            desiredGap = (desiredGap / 2) * 2;

            // UNIFIED RENDER LOGIC
            // The new 6px font fits "100" exactly (4+6+6=16px base -> 32px scaled).
            // No special "Condensed" logic needed anymore.

            // 1. Calculate VISUAL Content Width
            int contentWidth = 0;
            for (int i = 0; i < text.Length; i++)
            {
                contentWidth += GetVisualWidth(text[i] - '0') * scale;
            }

            // 2. Calculate Gap
            int gap = desiredGap;
            int gapCount = text.Length - 1;

            // 3. Auto-Fit Logic
            // Reduces gap if content + gaps > 32px
            if (gapCount > 0)
            {
                while ((contentWidth + (gapCount * gap)) > 32)
                {
                    gap -= 2;
                    if (gap < 0) break;
                }
            }

            // 4. Calculate Start X
            int totalWidth = contentWidth + (gapCount * gap);
            int startX = (32 - totalWidth) / 2;
            startX = (startX / 2) * 2; // Snap to even grid

            int startY = (32 - (12 * scale)) / 2;

            for (int i = 0; i < text.Length; i++)
            {
                int digit = text[i] - '0';

                // Adjust for padding
                int drawX = startX - (GetPaddingLeft(digit) * scale);

                SmoothDigitRendererLegacy.DrawDigit(pixels, digit, drawX, startY, color, scale);

                startX += (GetVisualWidth(digit) * scale) + gap;
            }
        });

        // 3. Create HICON from Buffer
        IntPtr hIcon = cpuIconBuffer.CreateIcon();

        // 4. Update Tray Icon
        // The UpdateIcon method is thread-safe.
        cpuTrayIcon.UpdateIcon(hIcon);

        // 5. Cleanup HICON immediately
        // The tray icon makes a copy, so we must destroy our handle.
        DynamicIconBuffer.DestroyIcon(hIcon);
#pragma warning restore CA1416 // Validate platform compatibility
    }

    // Returns the width of the visible "ink" based on SmoothDigits analysis
    private static int GetVisualWidth(int digit)
    {
        // '1' uses cols 2-5 (width 4)
        // Others use cols 1-6 (width 6)
        return digit == 1 ? 4 : 6;
    }

    // Returns the empty space to the left of the ink in the 8px sprite
    private static int GetPaddingLeft(int digit)
    {
        // '1' starts at col 2
        // Others start at col 1
        return digit == 1 ? 1 : 0;
    }

    #endregion Dynamic Icon Implementation

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
        // Stop dynamic timer
        cpuTimer?.Dispose();
        cpuIconBuffer?.Dispose();

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