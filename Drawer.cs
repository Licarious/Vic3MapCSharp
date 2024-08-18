using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using Vic3MapCSharp.DataObjects;

namespace Vic3MapCSharp
{
    /// <summary>
    /// Provides methods for drawing and manipulating maps and images.
    /// </summary>
    public class Drawer
    {
        public static (int w, int h) MapSize { get; set; } = (0, 0);

        /// <summary>
        /// Draws borders on an image from the specified file path.
        /// </summary>
        /// <param name="inputImagePath">The path to the input image file.</param>
        /// <param name="borderColor">The color of the borders.</param>
        /// <param name="alphaZeroBorders">Whether to draw borders for alpha-zero pixels.</param>
        /// <param name="borderWidth">The width of the borders.</param>
        /// <returns>A new image with borders drawn.</returns>
        public static Bitmap DrawBorders(string inputImagePath, Color borderColor, bool alphaZeroBorders = false, int borderWidth = 1) {
            ArgumentNullException.ThrowIfNull(inputImagePath);

            using Bitmap image = new(inputImagePath);
            return DrawBorders(image, borderColor, alphaZeroBorders, borderWidth);
        }

        /// <summary>
        /// Draws borders on the specified image.
        /// </summary>
        /// <param name="image">The input image.</param>
        /// <param name="borderColor">The color of the borders.</param>
        /// <param name="alphaZeroBorders">Whether to draw borders for alpha-zero pixels.</param>
        /// <param name="borderWidth">The width of the borders.</param>
        /// <returns>A new image with borders drawn.</returns>
        public static Bitmap DrawBorders(Bitmap image, Color borderColor, bool alphaZeroBorders = false, int borderWidth = 1) {
            ArgumentNullException.ThrowIfNull(image);
            if (borderWidth < 1) throw new ArgumentOutOfRangeException(nameof(borderWidth), "Border width must be greater than 0");
            if (borderColor.IsEmpty) throw new ArgumentNullException(nameof(borderColor));

            int width = image.Width;
            int height = image.Height;
            MapSize = (width, height);
            Bitmap newImage = new(width, height);

            using (Graphics g = Graphics.FromImage(newImage)) {
                g.DrawImage(newImage, 0, 0, width, height);

                for (int x = 0; x < width - 1; x++) {
                    for (int y = 0; y < height - 1; y++) {
                        Color currentPixel = image.GetPixel(x, y);
                        Color rightPixel = image.GetPixel(x + 1, y);
                        Color bottomPixel = image.GetPixel(x, y + 1);

                        if (currentPixel != rightPixel && (alphaZeroBorders || (currentPixel.A == 255 && rightPixel.A == 255))) {
                            DrawBorder(g, x, y, borderColor, borderWidth, true);
                        }

                        if (currentPixel != bottomPixel && (alphaZeroBorders || (currentPixel.A == 255 && bottomPixel.A == 255))) {
                            DrawBorder(g, x, y, borderColor, borderWidth, false);
                        }
                    }
                }
            }

            return newImage;
        }

        /// <summary>
        /// Draws a border on the specified graphics object.
        /// </summary>
        /// <param name="graphics">The graphics object.</param>
        /// <param name="x">The x-coordinate of the border.</param>
        /// <param name="y">The y-coordinate of the border.</param>
        /// <param name="borderColor">The color of the border.</param>
        /// <param name="borderWidth">The width of the border.</param>
        /// <param name="horizontal">Whether the border is horizontal.</param>
        private static void DrawBorder(Graphics graphics, int x, int y, Color borderColor, int borderWidth, bool horizontal) {
            if (borderWidth == 1) {
                using Pen pen = new(borderColor, 1);
                if (horizontal) {
                    graphics.DrawLine(pen, x + 1, y, x + 1, y + 1);
                }
                else {
                    graphics.DrawLine(pen, x, y + 1, x + 1, y + 1);
                }
            }
            else {
                using Pen pen = new(borderColor, borderWidth);
                if (horizontal) {
                    graphics.DrawLine(pen, x - borderWidth / 2, y, x + borderWidth / 2, y);
                }
                else {
                    graphics.DrawLine(pen, x, y - borderWidth / 2, x, y + borderWidth / 2);
                }
            }
        }

        /// <summary>
        /// Draws a color map based on a list of drawable objects.
        /// </summary>
        /// <param name="drawables">The list of drawable objects.</param>
        /// <returns>A new image with the color map drawn.</returns>
        public static Bitmap DrawColorMap(List<IDrawable> drawables) {
            ArgumentNullException.ThrowIfNull(drawables);
            if (MapSize.w == 0 || MapSize.h == 0) throw new InvalidOperationException("MapSize must be set before calling DrawColorMap");

            Bitmap newImage = new(MapSize.w, MapSize.h);

            // Print the type of the first drawable
            Console.WriteLine($"Drawing {drawables[0].GetType()} color map");

            using (Graphics g = Graphics.FromImage(newImage)) {
                foreach (var drawable in drawables) {
                    DrawColorMap(g, drawable, drawable.Color);
                }
            }

            return newImage;
        }

        /// <summary>
        /// Draws a map with a specified color based on a list of drawable objects.
        /// </summary>
        /// <param name="drawables">The list of drawable objects.</param>
        /// <param name="color">The color to use for drawing the map.</param>
        /// <returns>A new image with the map drawn.</returns>
        public static Bitmap DrawMap(List<IDrawable> drawables, Color color) {
            ArgumentNullException.ThrowIfNull(drawables);
            if (MapSize.w == 0 || MapSize.h == 0) throw new InvalidOperationException("MapSize must be set before calling DrawColorMap");

            Bitmap newImage = new(MapSize.w, MapSize.h);

            using (Graphics g = Graphics.FromImage(newImage)) {
                foreach (var drawable in drawables) {
                    DrawColorMap(g, drawable, color);
                }
            }

            return newImage;
        }

        /// <summary>
        /// Draws a color map on the specified graphics object.
        /// </summary>
        /// <param name="graphics">The graphics object.</param>
        /// <param name="drawable">The drawable object.</param>
        /// <param name="color">The color to use for drawing.</param>
        public static void DrawColorMap(Graphics graphics, IDrawable drawable, Color color) {
            ArgumentNullException.ThrowIfNull(graphics);
            ArgumentNullException.ThrowIfNull(drawable);

            using SolidBrush brush = new(color);
            foreach (var (x, y) in drawable.Coords) {
                graphics.FillRectangle(brush, x, y, 1, 1);
            }
        }

        /// <summary>
        /// Merges multiple images from the specified file paths into one image.
        /// </summary>
        /// <param name="imagesPaths">The list of image file paths.</param>
        /// <returns>A new image with all the images merged.</returns>
        public static Bitmap MergeImages(List<string> imagesPaths) {
            ArgumentNullException.ThrowIfNull(imagesPaths);
            if (imagesPaths.Count == 0) throw new ArgumentException("The images list cannot be empty", nameof(imagesPaths));

            List<Bitmap> images = [];
            try {
                foreach (var imagePath in imagesPaths) {
                    images.Add(new Bitmap(imagePath));
                }

                return MergeImages(images);
            }
            catch (Exception ex) {
                // Clean up any loaded images in case of an exception
                foreach (var image in images) {
                    image.Dispose();
                }
                throw new InvalidOperationException("An error occurred while loading images.", ex);
            }
        }

        /// <summary>
        /// Merges multiple images into one image.
        /// </summary>
        /// <param name="images">The list of images.</param>
        /// <returns>A new image with all the images merged.</returns>
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

            using (Graphics graphics = Graphics.FromImage(newImage)) {
                foreach (var image in images) {
                    lock (image) {
                        graphics.DrawImage(image, 0, 0, width, height);
                    }
                }
            }

            return newImage;
        }

        /// <summary>
        /// Draws a water map based on a list of provinces.
        /// </summary>
        /// <param name="provinces">The list of provinces.</param>
        /// <returns>A new image with the water map drawn.</returns>
        public static Bitmap DrawWaterMap(List<Province> provinces) {
            ArgumentNullException.ThrowIfNull(provinces);
            if (MapSize.w == 0 || MapSize.h == 0) throw new InvalidOperationException("MapSize must be set before calling DrawColorMap");

            Bitmap newImage = new(MapSize.w, MapSize.h);
            Color waterColor = Color.FromArgb(254, 173, 216, 230);

            using (Graphics g = Graphics.FromImage(newImage)) {
                foreach (var province in provinces) {
                    if (province.IsSea || province.IsLake) {
                        foreach (var (x, y) in province.Coords) {
                            newImage.SetPixel(x, y, waterColor);
                        }
                    }
                }
            }

            return newImage;
        }

        /// <summary>
        /// Writes text on the specified image within the given rectangles.
        /// </summary>
        /// <param name="image">The image to write text on.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="maxRectanges">The list of maximum rectangles to fit the text.</param>
        /// <param name="minimumFontSize">The minimum font size to use.</param>
        /// <param name="color">The color of the text.</param>
        public static void WriteText(Bitmap image, string text, List<(int x, int y, int h, int w)> maxRectanges, int minimumFontSize, Color color) {
            WriteText(image, text, maxRectanges, minimumFontSize, color, null);
        }

        /// <summary>
        /// Writes text on the specified image within the given rectangles with an optional font.
        /// </summary>
        /// <param name="image">The image to write text on.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="maxRectanges">The list of maximum rectangles to fit the text.</param>
        /// <param name="minimumFontSize">The minimum font size to use.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="font">The optional font to use.</param>
        public static void WriteText(Bitmap image, string text, List<(int x, int y, int h, int w)> maxRectanges, int minimumFontSize, Color color, Font? font) {
            WriteText(image, text, maxRectanges, minimumFontSize, color, Color.FromArgb(0, 0, 0, 0), font);
        }

        /// <summary>
        /// Writes text on the specified image within the given rectangles with an optional font and border color.
        /// </summary>
        /// <param name="image">The image to write text on.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="maxRectanges">The list of maximum rectangles to fit the text.</param>
        /// <param name="minimumFontSize">The minimum font size to use.</param>
        /// <param name="textColor">The color of the text.</param>
        /// <param name="borderColor">The color of the text border.</param>
        /// <param name="font">The optional font to use.</param>
        /// <param name="splitLine">Whether to split the text into multiple lines.</param>
        public static void WriteText(Bitmap image, string text, List<(int x, int y, int h, int w)> maxRectanges, int minimumFontSize, Color textColor, Color borderColor, Font? font, bool splitLine = true) {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(text);
            if (textColor.IsEmpty) throw new ArgumentNullException(nameof(textColor));
            if (maxRectanges == null || maxRectanges.Count == 0) throw new ArgumentNullException(nameof(maxRectanges));

            Font defaultFont = new("Verdana", minimumFontSize);
            font ??= defaultFont;

            StringFormat stringFormat = new() {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            // Split the text by whitespace
            string[] words = text.Split();

            using Graphics graphics = Graphics.FromImage(image);
            int fontSizeNeededToNotUseDefault = 16;
            var bestFontSizeResult = CalculateBestFontSize(graphics, words, maxRectanges, font, defaultFont, minimumFontSize);
            var (bestRectangle, bestFont, mergedWords) = bestFontSizeResult;

            // If the font size is too small, try with the default font
            if (bestFont.Size < fontSizeNeededToNotUseDefault && font.FontFamily != defaultFont.FontFamily) {
                bestFontSizeResult = CalculateBestFontSize(graphics, words, maxRectanges, defaultFont, defaultFont, minimumFontSize);
                (bestRectangle, bestFont, mergedWords) = bestFontSizeResult;
            }

            // Return if non-valid values for rectangleSize
            if (bestRectangle == (0, 0, 0, 0)) {
                return;
            }

            int wordsCount = mergedWords.Length;
            float textHeight = graphics.MeasureString(mergedWords[0], bestFont).Height;
            float adjustmentFactor = 0.3f * (mergedWords.Length - 1);
            float scalingFactor = 12 / (float)(Math.Log(textHeight + 1) + 10); // Logarithmic function

            int yOffset = bestRectangle.y - (int)(textHeight * adjustmentFactor * scalingFactor);

            using SolidBrush textBrush = new(textColor);
            foreach (var word in mergedWords) {
                if (borderColor.A == 0) {
                    graphics.DrawString(word, bestFont, textBrush, new Point(bestRectangle.x, yOffset), stringFormat);
                }
                else {
                    GraphicsPath graphicsPath = new();
                    Pen borderPen = new(borderColor, 4);
                    graphicsPath.AddString(word, bestFont.FontFamily, (int)bestFont.Style, bestFont.Size, new Point(bestRectangle.x, yOffset), stringFormat);
                    graphics.DrawPath(borderPen, graphicsPath);
                    graphics.FillPath(textBrush, graphicsPath);
                }
                yOffset += (int)(bestFont.Size * scalingFactor); // Use a fixed increment based on the font size
            }
        }

        /// <summary>
        /// Calculates the best font size for the given text and rectangles.
        /// </summary>
        /// <param name="graphics">The graphics object.</param>
        /// <param name="words">The words to fit.</param>
        /// <param name="maxRectanges">The list of maximum rectangles to fit the text.</param>
        /// <param name="font">The font to use.</param>
        /// <param name="defaultFont">The default font to use.</param>
        /// <param name="minimumFontSize">The minimum font size to use.</param>
        /// <returns>The best rectangle, font, and merged words.</returns>
        private static ((int x, int y, int h, int w) bestRectangle, Font bestFont, string[] mergedWords) CalculateBestFontSize(Graphics graphics, string[] words, List<(int x, int y, int h, int w)> maxRectanges, Font font, Font defaultFont, int minimumFontSize) {
            (int x, int y, int h, int w) bestRectangle = maxRectanges[0];
            Font bestFont = new(defaultFont.FontFamily, minimumFontSize);
            int bestFontSize = minimumFontSize / 2;
            string[] bestMergedWords = words;

            for (int i = 0; i < maxRectanges.Count; i++) {
                // Try different combinations of merging words
                for (int j = 1; j <= words.Length; j++) {
                    string[] mergedWords = MergeWords(words, j);
                    var currentFont = CalculateFontSize(graphics, string.Join("\n", mergedWords), font, maxRectanges[i], minimumFontSize, mergedWords.Length > 1 ? 1.0 : 1.2);
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

        /// <summary>
        /// Merges words into groups of the specified count.
        /// </summary>
        /// <param name="words">The words to merge.</param>
        /// <param name="mergeCount">The number of words to merge.</param>
        /// <returns>The merged words.</returns>
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

        /// <summary>
        /// Calculates the font size for the given text and rectangle.
        /// </summary>
        /// <param name="graphics">The graphics object.</param>
        /// <param name="text">The text to fit.</param>
        /// <param name="font">The font to use.</param>
        /// <param name="bestRectangle">The rectangle to fit the text in.</param>
        /// <param name="minimumFontSize">The minimum font size to use.</param>
        /// <param name="verticalBias">The vertical bias for fitting the text.</param>
        /// <returns>The calculated font.</returns>
        public static Font CalculateFontSize(Graphics graphics, string text, Font font, (int x, int y, int h, int w) bestRectangle, int minimumFontSize, double verticalBias) {
            SizeF textSize;
            int fontSize = minimumFontSize;

            do {
                font = new Font(font.FontFamily, fontSize);
                textSize = graphics.MeasureString(text, font);
                fontSize++;
            } while (textSize.Width < bestRectangle.w * 1.2 && textSize.Height * Math.Pow(0.8, text.Count(c => c == '\n')) < bestRectangle.h * verticalBias);

            return new Font(font.FontFamily, fontSize);
        }

        /// <summary>
        /// Draws a debug rectangle on the specified image.
        /// </summary>
        /// <param name="image">The image to draw on.</param>
        /// <param name="rectangle">The rectangle to draw.</param>
        /// <param name="color">The color of the rectangle.</param>
        /// <returns>The image with the debug rectangle drawn.</returns>
        public static Bitmap DrawDebugRectangle(Bitmap image, (int x, int y, int h, int w) rectangle, Color color) {
            ArgumentNullException.ThrowIfNull(image);
            if (color.IsEmpty) throw new ArgumentNullException(nameof(color));

            using Graphics g = Graphics.FromImage(image);
            using SolidBrush brush = new(color);
            g.FillRectangle(brush, rectangle.x - rectangle.w / 2, rectangle.y - rectangle.h / 2, rectangle.w, rectangle.h);

            return image;
        }

        /// <summary>
        /// Calculates the opposite extreme color of the given color.
        /// </summary>
        /// <param name="color">The color to calculate the opposite extreme for.</param>
        /// <returns>The opposite extreme color.</returns>
        public static Color OppositeExtremeColor(Color color) {
            int r = color.R > 127 ? 0 : 255;
            int g = color.G > 127 ? 0 : 255;
            int b = color.B > 127 ? 0 : 255;

            return Color.FromArgb(r, g, b);
        }
    }
}