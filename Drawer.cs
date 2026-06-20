using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vic3MapCSharp.DataObjects;

namespace Vic3MapCSharp
{
    public class Drawer
    {
        public static (int w, int h) MapSize { get; set; } = (0, 0);

        public static Bitmap DrawBorders(string inputImagePath, Color borderColor, bool alphaZeroBorders = false, int borderWidth = 1) {
            ArgumentNullException.ThrowIfNull(inputImagePath);

            using Bitmap image = Image.Load<Rgba32>(inputImagePath);
            return DrawBorders(image, borderColor, alphaZeroBorders, borderWidth);
        }

        public static Bitmap DrawBorders(Bitmap image, Color borderColor, bool alphaZeroBorders = false, int borderWidth = 1) {
            ArgumentNullException.ThrowIfNull(image);
            if (borderWidth < 1) throw new ArgumentOutOfRangeException(nameof(borderWidth), "Border width must be greater than 0");
            if (borderColor.IsEmpty) throw new ArgumentNullException(nameof(borderColor));

            int width = image.Width;
            int height = image.Height;
            MapSize = (width, height);
            Bitmap newImage = new(width, height);
            Rgba32 borderPixel = borderColor.ToRgba32();

            image.ProcessPixelRows(newImage, (source, target) => {
                for (int y = 0; y < height - 1; y++) {
                    Span<Rgba32> currentRow = source.GetRowSpan(y);
                    Span<Rgba32> nextRow = source.GetRowSpan(y + 1);

                    for (int x = 0; x < width - 1; x++) {
                        Rgba32 currentPixel = currentRow[x];
                        Rgba32 rightPixel = currentRow[x + 1];
                        Rgba32 bottomPixel = nextRow[x];

                        if (!currentPixel.Equals(rightPixel) && (alphaZeroBorders || (currentPixel.A == 255 && rightPixel.A == 255))) {
                            DrawBorder(target, x, y, borderPixel, borderWidth, true);
                        }

                        if (!currentPixel.Equals(bottomPixel) && (alphaZeroBorders || (currentPixel.A == 255 && bottomPixel.A == 255))) {
                            DrawBorder(target, x, y, borderPixel, borderWidth, false);
                        }
                    }
                }
            });

            return newImage;
        }

        private static void DrawBorder(PixelAccessor<Rgba32> target, int x, int y, Rgba32 borderColor, int borderWidth, bool horizontal) {
            if (borderWidth == 1) {
                if (horizontal) {
                    SetPixel(target, x + 1, y, borderColor);
                    SetPixel(target, x + 1, y + 1, borderColor);
                }
                else {
                    SetPixel(target, x, y + 1, borderColor);
                    SetPixel(target, x + 1, y + 1, borderColor);
                }
                return;
            }

            int half = borderWidth / 2;
            for (int i = -half; i <= half; i++) {
                if (horizontal) SetPixel(target, x + i, y, borderColor);
                else SetPixel(target, x, y + i, borderColor);
            }
        }

        private static void SetPixel(PixelAccessor<Rgba32> accessor, int x, int y, Rgba32 color) {
            if (x < 0 || y < 0 || x >= accessor.Width || y >= accessor.Height) return;
            accessor.GetRowSpan(y)[x] = color;
        }

        public static Bitmap DrawColorMap(List<IDrawable> drawables) {
            ArgumentNullException.ThrowIfNull(drawables);
            if (MapSize.w == 0 || MapSize.h == 0) throw new InvalidOperationException("MapSize must be set before calling DrawColorMap");

            Bitmap newImage = new(MapSize.w, MapSize.h);
            Console.WriteLine($"Drawing {drawables[0].GetType()} color map");

            foreach (var drawable in drawables) {
                DrawColorMap(newImage, drawable, drawable.Color);
            }

            return newImage;
        }

        public static Bitmap DrawMap(List<IDrawable> drawables, Color color) {
            ArgumentNullException.ThrowIfNull(drawables);
            if (MapSize.w == 0 || MapSize.h == 0) throw new InvalidOperationException("MapSize must be set before calling DrawColorMap");

            Bitmap newImage = new(MapSize.w, MapSize.h);
            foreach (var drawable in drawables) {
                DrawColorMap(newImage, drawable, color);
            }

            return newImage;
        }

        public static void DrawColorMap(Bitmap image, IDrawable drawable, Color color) {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(drawable);

            Rgba32 pixel = color.ToRgba32();
            image.ProcessPixelRows(accessor => {
                foreach (var (x, y) in drawable.Coords) {
                    if (x < 0 || y < 0 || x >= accessor.Width || y >= accessor.Height) continue;
                    accessor.GetRowSpan(y)[x] = pixel;
                }
            });
        }

        public static Bitmap MergeImages(List<string> imagesPaths) {
            ArgumentNullException.ThrowIfNull(imagesPaths);
            if (imagesPaths.Count == 0) throw new ArgumentException("The images list cannot be empty", nameof(imagesPaths));

            List<Bitmap> images = [];
            try {
                foreach (var imagePath in imagesPaths) {
                    images.Add(Image.Load<Rgba32>(imagePath));
                }

                return MergeImages(images);
            }
            catch (Exception ex) {
                foreach (var image in images) {
                    image.Dispose();
                }
                throw new InvalidOperationException("An error occurred while loading images.", ex);
            }
        }

        public static Bitmap MergeImages(List<Bitmap> images) {
            ArgumentNullException.ThrowIfNull(images);
            if (images.Count == 0) throw new ArgumentException("The images list cannot be empty", nameof(images));

            int width, height;
            lock (images[0]) {
                Bitmap firstImage = images[0];
                width = firstImage.Width;
                height = firstImage.Height;
            }
            MapSize = (width, height);
            Bitmap newImage = new(width, height);

            newImage.Mutate(ctx => {
                foreach (var image in images) {
                    lock (image) {
                        ctx.DrawImage(image, new Point(0, 0), 1f);
                    }
                }
            });

            return newImage;
        }

        public static void WriteText(Bitmap image, string text, List<(int x, int y, int h, int w)> maxRectanges, int minimumFontSize, Color color) {
            WriteText(image, text, maxRectanges, minimumFontSize, color, null);
        }

        public static void WriteText(Bitmap image, string text, List<(int x, int y, int h, int w)> maxRectanges, int minimumFontSize, Color color, Font? font) {
            WriteText(image, text, maxRectanges, minimumFontSize, color, Color.FromArgb(0, 0, 0, 0), font);
        }

        public static void WriteText(Bitmap image, string text, List<(int x, int y, int h, int w)> maxRectanges, int minimumFontSize, Color textColor, Color borderColor, Font? font, bool splitLine = true) {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(text);
            if (textColor.IsEmpty) throw new ArgumentNullException(nameof(textColor));
            if (maxRectanges == null || maxRectanges.Count == 0) throw new ArgumentNullException(nameof(maxRectanges));

            Font defaultFont = font?.Family.CreateFont(minimumFontSize) ?? CreateFallbackFont(minimumFontSize);
            font ??= defaultFont;

            string[] words = splitLine ? text.Split() : [text];
            int fontSizeNeededToNotUseDefault = 16;
            var bestFontSizeResult = CalculateBestFontSize(words, maxRectanges, font, defaultFont, minimumFontSize);
            var (bestRectangle, bestFont, mergedWords) = bestFontSizeResult;

            if (bestFont.Size < fontSizeNeededToNotUseDefault && font.Family != defaultFont.Family) {
                bestFontSizeResult = CalculateBestFontSize(words, maxRectanges, defaultFont, defaultFont, minimumFontSize);
                (bestRectangle, bestFont, mergedWords) = bestFontSizeResult;
            }

            if (bestRectangle == (0, 0, 0, 0)) {
                return;
            }

            float textHeight = MeasureText(mergedWords[0], bestFont).Height;
            float adjustmentFactor = 0.3f * (mergedWords.Length - 1);
            float scalingFactor = 12 / (float)(Math.Log(textHeight + 1) + 10);
            int yOffset = bestRectangle.y - (int)(textHeight * adjustmentFactor * scalingFactor);

            image.Mutate(ctx => {
                foreach (var word in mergedWords) {
                    RichTextOptions options = new(bestFont) {
                        Origin = new PointF(bestRectangle.x, yOffset),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (borderColor.A == 0) {
                        ctx.DrawText(options, word, textColor.ToImageSharpColor());
                    }
                    else {
                        IPathCollection glyphs = TextBuilder.GenerateGlyphs(word, options);
                        ctx.Draw(borderColor.ToImageSharpColor(), 4, glyphs);
                        ctx.Fill(textColor.ToImageSharpColor(), glyphs);
                    }
                    yOffset += (int)(bestFont.Size * scalingFactor);
                }
            });
        }

        private static ((int x, int y, int h, int w) bestRectangle, Font bestFont, string[] mergedWords) CalculateBestFontSize(string[] words, List<(int x, int y, int h, int w)> maxRectanges, Font font, Font defaultFont, int minimumFontSize) {
            (int x, int y, int h, int w) bestRectangle = maxRectanges[0];
            Font bestFont = defaultFont.Family.CreateFont(minimumFontSize);
            int bestFontSize = minimumFontSize / 2;
            string[] bestMergedWords = words;

            for (int i = 0; i < maxRectanges.Count; i++) {
                for (int j = 1; j <= words.Length; j++) {
                    string[] mergedWords = MergeWords(words, j);
                    var currentFont = CalculateFontSize(string.Join("\n", mergedWords), font, maxRectanges[i], minimumFontSize, mergedWords.Length > 1 ? 1.0 : 1.2);
                    if (currentFont.Size > bestFontSize) {
                        bestFontSize = (int)currentFont.Size;
                        bestFont = currentFont;
                        bestRectangle = maxRectanges[i];
                        bestMergedWords = mergedWords;
                    }
                }
            }

            return (bestRectangle, bestFont, bestMergedWords);
        }

        private static string[] MergeWords(string[] words, int mergeCount) {
            if (mergeCount >= words.Length) {
                return [string.Join(" ", words)];
            }

            List<string> mergedWords = [];
            for (int i = 0; i < words.Length; i += mergeCount) {
                mergedWords.Add(string.Join(" ", words.Skip(i).Take(mergeCount)));
            }

            return [.. mergedWords];
        }

        public static Font CalculateFontSize(string text, Font font, (int x, int y, int h, int w) bestRectangle, int minimumFontSize, double verticalBias) {
            FontRectangle textSize;
            int fontSize = minimumFontSize;

            do {
                font = font.Family.CreateFont(fontSize);
                textSize = MeasureText(text, font);
                fontSize++;
            } while (textSize.Width < bestRectangle.w * 1.2 && textSize.Height * Math.Pow(0.8, text.Count(c => c == '\n')) < bestRectangle.h * verticalBias);

            return font.Family.CreateFont(fontSize);
        }

        private static FontRectangle MeasureText(string text, Font font) {
            return TextMeasurer.MeasureSize(text, new TextOptions(font));
        }

        private static Font CreateFallbackFont(float size) {
            string[] preferredFonts = ["Verdana", "DejaVu Sans", "Arial", "Liberation Sans"];
            foreach (string name in preferredFonts) {
                if (SystemFonts.TryGet(name, out FontFamily family)) {
                    return family.CreateFont(size);
                }
            }

            FontFamily? firstFamily = SystemFonts.Families.FirstOrDefault();
            if (!firstFamily.HasValue) {
                throw new InvalidOperationException("No system font is available and no font was provided.");
            }

            return firstFamily.Value.CreateFont(size);
        }

        public static Bitmap DrawDebugRectangle(Bitmap image, (int x, int y, int h, int w) rectangle, Color color) {
            ArgumentNullException.ThrowIfNull(image);
            if (color.IsEmpty) throw new ArgumentNullException(nameof(color));

            image.Mutate(ctx => ctx.Fill(
                color.ToImageSharpColor(),
                new Rectangle(rectangle.x - rectangle.w / 2, rectangle.y - rectangle.h / 2, rectangle.w, rectangle.h)));

            return image;
        }

        public static Color OppositeExtremeColor(Color color) {
            int r = color.R > 127 ? 0 : 255;
            int g = color.G > 127 ? 0 : 255;
            int b = color.B > 127 ? 0 : 255;

            return Color.FromArgb(r, g, b);
        }
    }
}
