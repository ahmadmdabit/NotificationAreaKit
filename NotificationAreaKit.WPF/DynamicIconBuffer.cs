using System.Runtime.InteropServices;

using NotificationAreaKit.Wpf.Internal.Interop;

namespace NotificationAreaKit.Wpf;

/// <summary>
/// A high-performance, thread-safe, zero-allocation buffer for generating dynamic tray icons.
/// Uses a raw GDI DIB Section to allow direct memory access for pixel manipulation.
/// </summary>
public sealed class DynamicIconBuffer : IDisposable
{
    // Lock for thread safety (Critical for background timer updates)
    private readonly Lock locker = new();

    private readonly IntPtr hBitmap;
    private readonly IntPtr hMask; // Shared mask for CreateIconIndirect
    private readonly IntPtr pBits;
    private readonly int width;
    private readonly int height;
    private bool disposed;

    /// <summary>
    /// Defines a strongly-typed delegate for rendering pixels with state.
    /// Using this avoids closure allocations.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="pixels">The raw pixel buffer (BGRA).</param>
    /// <param name="state">The state passed from the caller.</param>
    public delegate void RenderAction<TState>(Span<uint> pixels, TState state);

    /// <summary>
    /// Gets the width of the icon buffer.
    /// </summary>
    public int Width => width;

    /// <summary>
    /// Gets the height of the icon buffer.
    /// </summary>
    public int Height => height;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicIconBuffer"/> class.
    /// </summary>
    /// <param name="width">The width of the icon (default 32).</param>
    /// <param name="height">The height of the icon (default 32).</param>
    public DynamicIconBuffer(int width = 32, int height = 32)
    {
        this.width = width;
        this.height = height;

        // 1. Create the DIB Section (The Color Bitmap)
        var bmi = new SystemPrimitives.BitmapInfo
        {
            bmiHeader = new SystemPrimitives.BitmapInfoHeader
            {
                biSize = (uint)Marshal.SizeOf<SystemPrimitives.BitmapInfoHeader>(),
                biWidth = width,
                biHeight = -height, // Negative for top-down DIB (Origin at Top-Left)
                biPlanes = 1,
                biBitCount = 32,    // ARGB
                biCompression = 0,  // BI_RGB
                biSizeImage = (uint)(width * height * 4)
            }
        };

        hBitmap = SystemPrimitives.CreateDIBSection(IntPtr.Zero, ref bmi, SystemPrimitives.DibRgbColors, out pBits, IntPtr.Zero, 0);

        if (hBitmap == IntPtr.Zero)
        {
            throw new OutOfMemoryException("Failed to create DIB Section for dynamic icon.");
        }

        // 2. Create a mask bitmap with matching dimensions.
        // This is critical: Mismatched mask size causes CreateIconIndirect to fail or produce invisible icons.
        hMask = SystemPrimitives.CreateBitmap(width, height, 1, 1, IntPtr.Zero);
    }

    /// <summary>
    /// Provides direct, unsafe access to the pixel buffer for rendering.
    /// This method is thread-safe and allocation-free.
    /// </summary>
    /// <typeparam name="TState">The type of the state object to pass to the renderer.</typeparam>
    /// <param name="state">The state object (e.g., the integer value to display).</param>
    /// <param name="renderAction">The static delegate to execute.</param>
    public unsafe void UpdatePixels<TState>(TState state, RenderAction<TState> renderAction)
    {
        lock (locker)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DynamicIconBuffer));

            // Create a span over the raw unmanaged memory.
            // This involves zero heap allocations.
            var span = new Span<uint>(pBits.ToPointer(), width * height);

            renderAction(span, state);
        }
    }

    /// <summary>
    /// Creates a new HICON from the current buffer state.
    /// This method is thread-safe.
    /// </summary>
    /// <returns>A handle to the created icon. You MUST call <see cref="DestroyIcon"/> on this handle after using it.</returns>
    public IntPtr CreateIcon()
    {
        lock (locker)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DynamicIconBuffer));

            // ICONINFO is a struct, so this is stack-allocated.
            var iconInfo = new SystemPrimitives.IconInfo
            {
                fIcon = 1, // TRUE (1) indicates an Icon, FALSE (0) indicates a Cursor
                xHotspot = 0,
                yHotspot = 0,
                hbmMask = hMask,
                hbmColor = hBitmap
            };

            return SystemPrimitives.CreateIconIndirect(ref iconInfo);
        }
    }

    /// <summary>
    /// Destroys an icon handle created by <see cref="CreateIcon"/>.
    /// This is a helper wrapper around the Win32 DestroyIcon API.
    /// </summary>
    /// <param name="hIcon">The icon handle to destroy.</param>
    public static void DestroyIcon(IntPtr hIcon)
    {
        if (hIcon != IntPtr.Zero)
        {
            SystemPrimitives.DestroyIcon(hIcon);
        }
    }

    /// <summary>
    /// Releases the unmanaged GDI resources.
    /// </summary>
    public void Dispose()
    {
        lock (locker)
        {
            if (disposed) return;

            SystemPrimitives.DeleteObject(hBitmap);
            SystemPrimitives.DeleteObject(hMask);

            disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~DynamicIconBuffer()
    {
        Dispose();
    }
}