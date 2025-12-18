namespace NotificationAreaKit.WPF;

/// <summary>
/// Provides high-performance, zero-allocation rendering logic for drawing anti-aliased numeric digits onto raw pixel buffers.
/// </summary>
/// <remarks>
/// This class is designed to work with <see cref="DynamicIconBuffer"/> to generate dynamic tray icons
/// (e.g., CPU meters, unread counts) without the overhead of GDI+ or WPF rendering pipelines.
/// It utilizes a custom 6x12 pixel font with support for integer scaling and direct memory manipulation via <see cref="Span{T}"/>.
/// </remarks>
public static class SmoothDigitRendererLegacy
{
    /// <summary>
    /// Renders a single digit onto the raw pixel buffer using a custom anti-aliased font.
    /// </summary>
    /// <param name="pixels">The target pixel buffer, typically representing a 32x32 ARGB icon.</param>
    /// <param name="digit">The digit to render (0-9). Values outside this range are ignored.</param>
    /// <param name="startX">The X-coordinate of the top-left corner where rendering begins.</param>
    /// <param name="startY">The Y-coordinate of the top-left corner where rendering begins.</param>
    /// <param name="baseColor">The base RGB color in <c>0x00RRGGBB</c> format. The Alpha channel is dynamically calculated based on the font glyph.</param>
    /// <param name="scale">The integer scaling factor (e.g., 2). Applies nearest-neighbor scaling to the glyph pixels.</param>
    /// <remarks>
    /// This method operates directly on the <paramref name="pixels"/> span for zero-allocation performance.
    /// It includes boundary checks to ensure no writes occur outside the 32x32 buffer limits.
    /// </remarks>
    public static void DrawDigit(Span<uint> pixels, int digit, int startX, int startY, uint baseColor, int scale)
    {
        if (digit < 0 || digit > 9) return;

        string data = SmoothDigits[digit];
        const int width = 6;
        const int height = 12;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = data[(y * width) + x];
                uint alpha = c switch
                {
                    '#' => 255,
                    '+' => 170,
                    '.' => 85,
                    _ => 0
                };

                if (alpha > 0)
                {
                    // Straight Alpha (No Premultiplication)
                    uint finalPixel = (alpha << 24) | baseColor;

                    for (int dy = 0; dy < scale; dy++)
                    {
                        for (int dx = 0; dx < scale; dx++)
                        {
                            int px = startX + (x * scale) + dx;
                            int py = startY + (y * scale) + dy;

                            if (px >= 0 && px < 32 && py >= 0 && py < 32)
                            {
                                pixels[(py * 32) + px] = finalPixel;
                            }
                        }
                    }
                }
            }
        }
    }

    public static void DrawDigitMinimal(Span<uint> pixels, int digit, int x, int y, uint color, int scale)
    {
        if (digit < 0 || digit > 9) return;

        ushort mask = DigitMasks[digit];

        // Iterate 5 rows
        for (int row = 0; row < 5; row++)
        {
            // Iterate 3 cols
            for (int col = 0; col < 3; col++)
            {
                // Check bit at (row, col).
                // Mask is stored row-major. Bit 14 is (0,0), Bit 0 is (4,2).
                // Actually, let's simplify the bit math:
                // 14 - (row * 3 + col)
                int bitIndex = 14 - ((row * 3) + col);
                bool isSet = ((mask >> bitIndex) & 1) == 1;

                if (isSet)
                {
                    // Draw scaled pixel
                    for (int dy = 0; dy < scale; dy++)
                    {
                        for (int dx = 0; dx < scale; dx++)
                        {
                            int px = x + (col * scale) + dx;
                            int py = y + (row * scale) + dy;

                            if (px >= 0 && px < 32 && py >= 0 && py < 32)
                            {
                                pixels[(py * 32) + px] = color;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// A static font atlas defining the visual appearance of digits 0-9.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each string represents a 6x12 pixel grid flattened into a 72-character string.
    /// The characters encode alpha intensity values to achieve anti-aliased edges:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>' '</c> (Space): 0% Opacity (Transparent)</description></item>
    /// <item><description><c>'.'</c> (Dot): ~33% Opacity</description></item>
    /// <item><description><c>'+'</c> (Plus): ~66% Opacity</description></item>
    /// <item><description><c>'#'</c> (Hash): 100% Opacity</description></item>
    /// </list>
    /// </remarks>
    private static readonly string[] SmoothDigits =
    [
        // 0 (12 rows)
        "      " +
        " #### " +
        "#    #" +
        "#    #" +
        "#    #" +
        "#    #" +
        "#    #" +
        "#    #" +
        "#    #" +
        " #### " +
        "      " +
        "      ",

        // 1 (12 rows)
        "      " +
        "   #  " +
        "  ##  " +
        "   #  " +
        "   #  " +
        "   #  " +
        "   #  " +
        "   #  " +
        "   #  " +
        " #### " +
        "      " +
        "      ",

        // 2 (12 rows)
        "      " +
        " #### " +
        "#    #" +
        "     #" +
        "    # " +
        "   #  " +
        "  #   " +
        " #    " +
        "#     " +
        "######" +
        "      " +
        "      ",

        // 3 (12 rows)
        "      " +
        " #### " +
        "#    #" +
        "     #" +
        "    # " +
        "  ### " +
        "     #" +
        "     #" +
        "#    #" +
        " #### " +
        "      " +
        "      ",

        // 4 (12 rows)
        "      " +
        "    # " +
        "   ## " +
        "  # # " +
        " #  # " +
        "#   # " +
        "######" +
        "    # " +
        "    # " +
        "    # " +
        "      " +
        "      ",

        // 5 (12 rows)
        "      " +
        "######" +
        "#     " +
        "#     " +
        "##### " +
        "     #" +
        "     #" +
        "     #" +
        "#    #" +
        " #### " +
        "      " +
        "      ",

        // 6 (12 rows)
        "      " +
        " #### " +
        "#     " +
        "#     " +
        "##### " +
        "#    #" +
        "#    #" +
        "#    #" +
        "#    #" +
        " #### " +
        "      " +
        "      ",

        // 7 (12 rows)
        "      " +
        "######" +
        "     #" +
        "    # " +
        "   #  " +
        "  #   " +
        "  #   " +
        "  #   " +
        "  #   " +
        "  #   " +
        "      " +
        "      ",

        // 8 (12 rows)
        "      " +
        " #### " +
        "#    #" +
        "#    #" +
        " #### " +
        "#    #" +
        "#    #" +
        "#    #" +
        "#    #" +
        " #### " +
        "      " +
        "      ",

        // 9 (12 rows)
        "      " +
        " #### " +
        "#    #" +
        "#    #" +
        "#    #" +
        " #####" +
        "     #" +
        "     #" +
        "    # " +
        " #### " +
        "      " +
        "      ",
    ];

    // A minimal 3x5 font renderer.
    // 1 = Pixel On, 0 = Pixel Off
    private static readonly ushort[] DigitMasks =
    [
        0b111_101_101_101_111, // 0
        0b010_010_010_010_010, // 1
        0b111_001_111_100_111, // 2
        0b111_001_111_001_111, // 3
        0b101_101_111_001_001, // 4
        0b111_100_111_001_111, // 5
        0b111_100_111_101_111, // 6
        0b111_001_001_001_001, // 7
        0b111_101_111_101_111, // 8
        0b111_101_111_001_111  // 9
    ];
}