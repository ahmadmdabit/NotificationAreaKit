using System.Runtime.CompilerServices;

namespace NotificationAreaKit.WPF;

/// <summary>
/// Provides a high-performance, zero-allocation rasterization engine for rendering anti-aliased numeric glyphs directly into raw memory buffers.
/// </summary>
/// <remarks>
/// <para>
/// This static utility is optimized for high-frequency UI updates (e.g., system tray animations, live status indicators) where standard GDI+ or WPF rendering overhead is prohibitive.
/// </para>
/// <para>
/// <b>Key Architectural Features:</b>
/// <list type="bullet">
/// <item><description><b>Zero-Allocation:</b> Operates exclusively on <see cref="Span{T}"/> and static readonly data structures, inducing zero Garbage Collector (GC) pressure.</description></item>
/// <item><description><b>Cache-Optimized:</b> Utilizes a flattened byte-array font atlas and integer lookup tables to maximize CPU cache locality and minimize pointer indirection.</description></item>
/// <item><description><b>Branch Prediction Optimization:</b> Implements distinct "Fast Path" (no bounds checking) and "Safe Path" (clipped) rendering strategies to maximize throughput.</description></item>
/// <item><description><b>Custom Typography:</b> Renders a specialized 6x12 pixel font designed for maximum legibility at small icon sizes (16x16 to 32x32).</description></item>
/// </list>
/// </para>
/// </remarks>
public static class SmoothDigitRenderer
{
    // 6 cols * 12 rows = 72 bytes per digit
    private const int DigitWidth = 6;

    private const int DigitHeight = 12;
    private const int BytesPerDigit = DigitWidth * DigitHeight;
    private const int BufferSize = 32;

    // Lookup table for Alpha values (0, 85, 170, 255)
    // Maps 0->0, 1->85, 2->170, 3->255
    private static readonly uint[] AlphaLut = [0, 85, 170, 255];

    /// <summary>
    /// Renders a single digit onto the raw pixel buffer using a custom anti-aliased font.
    /// </summary>
    /// <param name="pixels">The target pixel buffer, typically representing a 32x32 ARGB icon.</param>
    /// <param name="digit">The digit to render (0-9).</param>
    /// <param name="startX">The X-coordinate of the top-left corner.</param>
    /// <param name="startY">The Y-coordinate of the top-left corner.</param>
    /// <param name="baseColor">The base RGB color (0x00RRGGBB).</param>
    /// <param name="scale">The integer scaling factor.</param>
    public static void DrawDigit(Span<uint> pixels, int digit, int startX, int startY, uint baseColor, int scale)
    {
        // Validation
        if ((uint)digit > 9) return; // Unsigned check handles negative and >9
        if (pixels.Length < BufferSize * BufferSize) return; // Safety check

        // Calculate offset into the flattened font array
        int fontOffset = digit * BytesPerDigit;
        ReadOnlySpan<byte> digitPixels = FontData.AsSpan(fontOffset, BytesPerDigit);

        // Check if the entire glyph fits within the buffer to use the Fast Path
        // Glyph dimensions: (Width * scale) x (Height * scale)
        int renderWidth = DigitWidth * scale;
        int renderHeight = DigitHeight * scale;

        bool isFullyVisible = startX >= 0 && startY >= 0 &&
                              (startX + renderWidth) <= BufferSize &&
                              (startY + renderHeight) <= BufferSize;

        if (isFullyVisible)
        {
            DrawDigitFast(pixels, digitPixels, startX, startY, baseColor, scale);
        }
        else
        {
            DrawDigitSafe(pixels, digitPixels, startX, startY, baseColor, scale);
        }
    }

    /// <summary>
    /// Optimized rendering path with NO bounds checks.
    /// Only called when the glyph is guaranteed to be fully inside the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawDigitFast(Span<uint> pixels, ReadOnlySpan<byte> digitPixels, int startX, int startY, uint baseColor, int scale)
    {
        int pixelIdx = 0;

        for (int y = 0; y < DigitHeight; y++)
        {
            for (int x = 0; x < DigitWidth; x++)
            {
                byte intensity = digitPixels[pixelIdx++];

                // Skip transparent pixels (Intensity 0)
                if (intensity == 0) continue;

                uint alpha = AlphaLut[intensity];
                uint finalPixel = (alpha << 24) | baseColor;

                // Scatter the pixel (Scale x Scale)
                int targetYBase = startY + (y * scale);
                int targetXBase = startX + (x * scale);

                for (int dy = 0; dy < scale; dy++)
                {
                    int rowOffset = (targetYBase + dy) * BufferSize;
                    for (int dx = 0; dx < scale; dx++)
                    {
                        pixels[rowOffset + targetXBase + dx] = finalPixel;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Safe rendering path with bounds checks for every pixel.
    /// Handles clipping at the edges of the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DrawDigitSafe(Span<uint> pixels, ReadOnlySpan<byte> digitPixels, int startX, int startY, uint baseColor, int scale)
    {
        int pixelIdx = 0;

        for (int y = 0; y < DigitHeight; y++)
        {
            for (int x = 0; x < DigitWidth; x++)
            {
                byte intensity = digitPixels[pixelIdx++];
                if (intensity == 0) continue;

                uint alpha = AlphaLut[intensity];
                uint finalPixel = (alpha << 24) | baseColor;

                int targetYBase = startY + (y * scale);
                int targetXBase = startX + (x * scale);

                for (int dy = 0; dy < scale; dy++)
                {
                    int py = targetYBase + dy;
                    if (py < 0 || py >= BufferSize) continue;

                    int rowOffset = py * BufferSize;

                    for (int dx = 0; dx < scale; dx++)
                    {
                        int px = targetXBase + dx;
                        if (px >= 0 && px < BufferSize)
                        {
                            pixels[rowOffset + px] = finalPixel;
                        }
                    }
                }
            }
        }
    }

    // Flattened Font Data (0=Space, 1=Dot, 2=Plus, 3=Hash)
    // 10 digits * 72 bytes = 720 bytes
    private static readonly byte[] FontData =
    [
        // 0
        0,0,0,0,0,0, 0,3,3,3,3,0, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 0,3,3,3,3,0, 0,0,0,0,0,0,
        // 1
        0,0,0,0,0,0, 0,0,0,3,0,0, 0,0,3,3,0,0, 0,0,0,3,0,0, 0,0,0,3,0,0, 0,0,0,3,0,0, 0,0,0,3,0,0, 0,0,0,3,0,0, 0,0,0,3,0,0, 0,3,3,3,3,0, 0,0,0,0,0,0, 0,0,0,0,0,0,
        // 2
        0,0,0,0,0,0, 0,3,3,3,3,0, 3,0,0,0,0,3, 0,0,0,0,0,3, 0,0,0,0,3,0, 0,0,0,3,0,0, 0,0,3,0,0,0, 0,3,0,0,0,0, 3,0,0,0,0,0, 3,3,3,3,3,3, 0,0,0,0,0,0, 0,0,0,0,0,0,
        // 3
        0,0,0,0,0,0, 0,3,3,3,3,0, 3,0,0,0,0,3, 0,0,0,0,0,3, 0,0,0,0,3,0, 0,0,3,3,3,0, 0,0,0,0,0,3, 0,0,0,0,0,3, 3,0,0,0,0,3, 0,3,3,3,3,0, 0,0,0,0,0,0, 0,0,0,0,0,0,
        // 4
        0,0,0,0,0,0, 0,0,0,3,0,0, 0,0,3,3,0,0, 0,3,0,3,0,0, 3,0,0,3,0,0, 3,0,0,3,0,0, 3,3,3,3,3,3, 0,0,0,3,0,0, 0,0,0,3,0,0, 0,0,0,3,0,0, 0,0,0,0,0,0, 0,0,0,0,0,0,
        // 5
        0,0,0,0,0,0, 3,3,3,3,3,3, 3,0,0,0,0,0, 3,0,0,0,0,0, 3,3,3,3,3,0, 0,0,0,0,0,3, 0,0,0,0,0,3, 0,0,0,0,0,3, 3,0,0,0,0,3, 0,3,3,3,3,0, 0,0,0,0,0,0, 0,0,0,0,0,0,
        // 6
        0,0,0,0,0,0, 0,3,3,3,3,0, 3,0,0,0,0,0, 3,0,0,0,0,0, 3,3,3,3,3,0, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 0,3,3,3,3,0, 0,0,0,0,0,0, 0,0,0,0,0,0,
        // 7
        0,0,0,0,0,0, 3,3,3,3,3,3, 0,0,0,0,0,3, 0,0,0,0,3,0, 0,0,0,3,0,0, 0,0,3,0,0,0, 0,0,3,0,0,0, 0,0,3,0,0,0, 0,0,3,0,0,0, 0,0,3,0,0,0, 0,0,0,0,0,0, 0,0,0,0,0,0,
        // 8
        0,0,0,0,0,0, 0,3,3,3,3,0, 3,0,0,0,0,3, 3,0,0,0,0,3, 0,3,3,3,3,0, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 0,3,3,3,3,0, 0,0,0,0,0,0, 0,0,0,0,0,0,
        // 9
        0,0,0,0,0,0, 0,3,3,3,3,0, 3,0,0,0,0,3, 3,0,0,0,0,3, 3,0,0,0,0,3, 0,3,3,3,3,3, 0,0,0,0,0,3, 0,0,0,0,0,3, 0,0,0,0,3,0, 0,3,3,3,3,0, 0,0,0,0,0,0, 0,0,0,0,0,0
    ];
}