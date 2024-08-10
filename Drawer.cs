using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

namespace Vic3MapCSharp
{
    public class Drawer
    {
        public static (int w, int h) MapSize { get; set; } = (0, 0);

        public static Bitmap DrawBorders(string inputImagePath, Color borderColor, bool alphaZeroBorders = false, int borderWidth = 1) {
            if (inputImagePath == null) throw new ArgumentNullException(nameof(inputImagePath));

            using Bitmap image = new(inputImagePath);
            return DrawBorders(image, borderColor, alphaZeroBorders, borderWidth);
        }

        public static Bitmap DrawBorders(Bitmap image, Color borderColor, bool alphaZeroBorders = false, int borderWidth = 1) {
            if (image == null) throw new ArgumentNullException(nameof(image));
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

        private static void DrawBorder(Graphics g, int x, int y, Color borderColor, int borderWidth, bool horizontal) {
            if (borderWidth == 1) {
                using (Pen pen = new(borderColor, 1)) {
                    if (horizontal) {
                        g.DrawLine(pen, x + 1, y, x + 1, y + 1);
                    }
                    else {
                        g.DrawLine(pen, x, y + 1, x + 1, y + 1);
                    }
                }
            }
            else {
                using (Pen pen = new(borderColor, borderWidth)) {
                    if (horizontal) {
                        g.DrawLine(pen, x - borderWidth / 2, y, x + borderWidth / 2, y);
                    }
                    else {
                        g.DrawLine(pen, x, y - borderWidth / 2, x, y + borderWidth / 2);
                    }
                }
            }
        }

        public static Bitmap DrawColorMap(List<IDrawable> drawables) {
            if (drawables == null) throw new ArgumentNullException(nameof(drawables));
            if (MapSize.w == 0 || MapSize.h == 0) throw new InvalidOperationException("MapSize must be set before calling DrawColorMap");

            Bitmap newImage = new(MapSize.w, MapSize.h);

            //print the Type of the first drawable
            Console.WriteLine($"Drawing {drawables[0].GetType()} color map");

            using (Graphics g = Graphics.FromImage(newImage)) {
                foreach (var drawable in drawables) {
                    DrawColorMap(g, drawable, drawable.Color);
                }
            }

            return newImage;
        }

        public static Bitmap DrawMap(List<IDrawable> drawables, Color color) {
            if (drawables == null) throw new ArgumentNullException(nameof(drawables));
            if (MapSize.w == 0 || MapSize.h == 0) throw new InvalidOperationException("MapSize must be set before calling DrawColorMap");

            Bitmap newImage = new(MapSize.w, MapSize.h);

            using (Graphics g = Graphics.FromImage(newImage)) {
                foreach (var drawable in drawables) {
                    DrawColorMap(g, drawable, color);
                }
            }

            return newImage;
        }

        public static void DrawColorMap(Graphics g, IDrawable drawable, Color color) {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (drawable == null) throw new ArgumentNullException(nameof(drawable));

            using (SolidBrush brush = new(color)) {
                foreach (var (x, y) in drawable.Coords) {
                    g.FillRectangle(brush, x, y, 1, 1);
                }
            }
        }

        public static Bitmap MergeImages(List<string> imagesPaths) {
            if (imagesPaths == null) throw new ArgumentNullException(nameof(imagesPaths));
            if (imagesPaths.Count == 0) throw new ArgumentException("The images list cannot be empty", nameof(imagesPaths));

            List<Bitmap> images = new();
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

        public static Bitmap MergeImages(List<Bitmap> images) {
            if (images == null) throw new ArgumentNullException(nameof(images));
            if (images.Count == 0) throw new ArgumentException("The images list cannot be empty", nameof(images));

            int width, height;
            lock (images[0]) {
                Bitmap firstImage = images[0];
                width = firstImage.Width;
                height = firstImage.Height;
            }
            MapSize = (width, height);
            Bitmap newImage = new(width, height);

            using (Graphics g = Graphics.FromImage(newImage)) {
                foreach (var image in images) {
                    lock (image) {
                        g.DrawImage(image, 0, 0, width, height);
                    }
                }
            }

            return newImage;
        }

        public static Bitmap DrawWaterMap(List<Province> provinces) {
            if (provinces == null) throw new ArgumentNullException(nameof(provinces));
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

        public static void WriteText(Bitmap image, string text, List<(int x, int y)> centers, List<(int w, int h)> rectangleSizes, int minimumFontSize, Color color) {
            WriteText(image, text, centers, rectangleSizes, minimumFontSize, color, null);
        }

        public static void WriteText(Bitmap image, string text, List<(int x, int y)> centers, List<(int w, int h)> rectangleSizes, int minimumFontSize, Color color, Font? font) {
            WriteText(image, text, centers, rectangleSizes, minimumFontSize, color, Color.FromArgb(0, 0, 0, 0), font);
        }

        public static void WriteText_old(Bitmap image, string text, List<(int x, int y)> centers, List<(int w, int h)> rectangleSizes, int minimumFontSize, Color textColor, Color borderColor, Font? font, bool splitLine = true) {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (textColor.IsEmpty) throw new ArgumentNullException(nameof(textColor));
            if (centers == null || centers.Count == 0) throw new ArgumentNullException(nameof(centers));
            if (rectangleSizes == null || rectangleSizes.Count == 0) throw new ArgumentNullException(nameof(rectangleSizes));
            if (centers.Count != rectangleSizes.Count) throw new ArgumentException("Centers and rectangleSizes lists must have the same length");

            Font defaultFont = new("Verdana", minimumFontSize);
            font ??= defaultFont;

            StringFormat stringFormat = new() {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            // Split the text by whitespace
            string[] words = text.Split();

            // If there are more than 2 words, try to split the text into 2 lines, such that the 2 lines are roughly equal in length and the words retain their order
            if (!splitLine) {
                words = new string[] { text };
            }
            else if (words.Length > 2) {
                StringBuilder firstLine = new();
                StringBuilder secondLine = new();
                int halfLength = (int)Math.Ceiling(words.Length / 2.0); // Use Math.Ceiling for better balance

                for (int i = 0; i < words.Length; i++) {
                    if (i < halfLength) {
                        firstLine.Append(words[i]).Append(' ');
                    }
                    else {
                        secondLine.Append(words[i]).Append(' ');
                    }
                }

                words = new string[] { firstLine.ToString().Trim(), secondLine.ToString().Trim() };
            }

            int fontSizeNeededToNotUseDefault = 16;

            using (Graphics graphics = Graphics.FromImage(image)) {
                // Calculate the largest font size for each center and rectangle size
                (int x, int y) bestCenter = centers[0];
                (int w, int h) bestRectangleSize = rectangleSizes[0];
                Font bestFont = new(defaultFont.FontFamily, minimumFontSize);
                int bestFontSize = minimumFontSize / 2;

                for (int i = 0; i < centers.Count; i++) {
                    var currentFont = CalculateFontSize(graphics, string.Join("\n", words), font, new List<(int w, int h)> { rectangleSizes[i] }, minimumFontSize, words.Length > 1 ? 1.0 : 1.2);
                    if (currentFont.Size > bestFontSize) {
                        bestFontSize = (int)currentFont.Size;
                        bestFont = currentFont;
                        bestCenter = centers[i];
                        bestRectangleSize = rectangleSizes[i];
                    }
                }

                if (bestFont.Size < fontSizeNeededToNotUseDefault && font.FontFamily != defaultFont.FontFamily) {
                    bestFont = new(defaultFont.FontFamily, minimumFontSize);
                    bestFontSize = minimumFontSize / 2;
                    for (int i = 0; i < centers.Count; i++) {
                        var currentFont = CalculateFontSize(graphics, string.Join("\n", words), defaultFont, new List<(int w, int h)> { rectangleSizes[i] }, minimumFontSize, words.Length > 1 ? 1.0 : 1.2);
                        if (currentFont.Size > bestFontSize) {
                            bestFontSize = (int)currentFont.Size;
                            bestFont = currentFont;
                            bestCenter = centers[i];
                            bestRectangleSize = rectangleSizes[i];
                        }
                    }
                }



                // Return if non-valid values for center, rectangleSize, or yOffset
                if (bestCenter == (0, 0) || bestRectangleSize == (0, 0)) {
                    return;
                }

                int yOffset = bestCenter.y - (words.Length > 1 ? (int)(graphics.MeasureString(text, bestFont).Height * 0.35) : 0);


                using (SolidBrush textBrush = new(textColor)) {
                    foreach (var word in words) {
                        if (borderColor.A == 0) {
                            graphics.DrawString(word, bestFont, textBrush, new Point(bestCenter.x, yOffset), stringFormat);
                        }
                        else {
                            GraphicsPath graphicsPath = new();
                            Pen borderPen = new(borderColor, 4);
                            graphicsPath.AddString(word, bestFont.FontFamily, (int)bestFont.Style, bestFont.Size, new Point(bestCenter.x, yOffset), stringFormat);
                            graphics.DrawPath(borderPen, graphicsPath);
                            graphics.FillPath(textBrush, graphicsPath);
                        }
                        yOffset += (int)(graphics.MeasureString(text, bestFont).Height * 0.65);
                    }
                }
            }
        }

        public static void WriteText(Bitmap image, string text, List<(int x, int y)> centers, List<(int w, int h)> rectangleSizes, int minimumFontSize, Color textColor, Color borderColor, Font? font, bool splitLine = true) {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (textColor.IsEmpty) throw new ArgumentNullException(nameof(textColor));
            if (centers == null || centers.Count == 0) throw new ArgumentNullException(nameof(centers));
            if (rectangleSizes == null || rectangleSizes.Count == 0) throw new ArgumentNullException(nameof(rectangleSizes));
            if (centers.Count != rectangleSizes.Count) throw new ArgumentException("Centers and rectangleSizes lists must have the same length");

            Font defaultFont = new("Verdana", minimumFontSize);
            font ??= defaultFont;

            StringFormat stringFormat = new() {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            // Split the text by whitespace
            string[] words = text.Split();

            using (Graphics graphics = Graphics.FromImage(image)) {
                int fontSizeNeededToNotUseDefault = 16;
                var bestFontSizeResult = CalculateBestFontSize(graphics, words, centers, rectangleSizes, font, defaultFont, minimumFontSize);
                var (bestCenter, bestRectangleSize, bestFont, mergedWords) = bestFontSizeResult;

                // If the font size is too small, try with the default font
                if (bestFont.Size < fontSizeNeededToNotUseDefault && font.FontFamily != defaultFont.FontFamily) {
                    bestFontSizeResult = CalculateBestFontSize(graphics, words, centers, rectangleSizes, defaultFont, defaultFont, minimumFontSize);
                    (bestCenter, bestRectangleSize, bestFont, mergedWords) = bestFontSizeResult;
                }

                // Return if non-valid values for center, rectangleSize, or yOffset
                if (bestCenter == (0, 0) || bestRectangleSize == (0, 0)) {
                    return;
                }

                int wordsCount = mergedWords.Length;
                float textHeight = graphics.MeasureString(mergedWords[0], bestFont).Height;
                //float adjustmentFactor = wordsCount > 1 ? 0.35f : 0.0f;
                float adjustmentFactor = 0.3f * (mergedWords.Count() - 1);
                float scalingFactor = 12/(float)(Math.Log(textHeight + 1)+10); // Logarithmic function

                int yOffset = bestCenter.y - (int)(textHeight * adjustmentFactor * scalingFactor);

                using (SolidBrush textBrush = new(textColor)) {
                    foreach (var word in mergedWords) {
                        if (borderColor.A == 0) {
                            graphics.DrawString(word, bestFont, textBrush, new Point(bestCenter.x, yOffset), stringFormat);
                        }
                        else {
                            GraphicsPath graphicsPath = new();
                            Pen borderPen = new(borderColor, 4);
                            graphicsPath.AddString(word, bestFont.FontFamily, (int)bestFont.Style, bestFont.Size, new Point(bestCenter.x, yOffset), stringFormat);
                            graphics.DrawPath(borderPen, graphicsPath);
                            graphics.FillPath(textBrush, graphicsPath);
                        }
                        yOffset += (int)(bestFont.Size * scalingFactor); // Use a fixed increment based on the font size
                    }
                }
            }
        }

        private static ((int x, int y) bestCenter, (int w, int h) bestRectangleSize, Font bestFont, string[] mergedWords) CalculateBestFontSize(Graphics graphics, string[] words, List<(int x, int y)> centers, List<(int w, int h)> rectangleSizes, Font font, Font defaultFont, int minimumFontSize) {
            (int x, int y) bestCenter = centers[0];
            (int w, int h) bestRectangleSize = rectangleSizes[0];
            Font bestFont = new(defaultFont.FontFamily, minimumFontSize);
            int bestFontSize = minimumFontSize / 2;
            string[] bestMergedWords = words;

            for (int i = 0; i < centers.Count; i++) {
                // Try different combinations of merging words
                for (int j = 1; j <= words.Length; j++) {
                    string[] mergedWords = MergeWords(words, j);
                    var currentFont = CalculateFontSize(graphics, string.Join("\n", mergedWords), font, new List<(int w, int h)> { rectangleSizes[i] }, minimumFontSize, mergedWords.Length > 1 ? 1.0 : 1.2);
                    if (currentFont.Size > bestFontSize) {
                        bestFontSize = (int)currentFont.Size;
                        bestFont = currentFont;
                        bestCenter = centers[i];
                        bestRectangleSize = rectangleSizes[i];
                        bestMergedWords = mergedWords;
                    }
                }
            }

            return (bestCenter, bestRectangleSize, bestFont, bestMergedWords);
        }

        private static string[] MergeWords(string[] words, int mergeCount) {
            if (mergeCount >= words.Length) {
                return new string[] { string.Join(" ", words) };
            }

            List<string> mergedWords = new();
            for (int i = 0; i < words.Length; i += mergeCount) {
                mergedWords.Add(string.Join(" ", words.Skip(i).Take(mergeCount)));
            }

            return mergedWords.ToArray();
        }

        public static Font CalculateFontSize(Graphics graphics, string text, Font font, List<(int w, int h)> rectangleSizes, int minimumFontSize, double verticalBias) {
            SizeF textSize;
            int fontSize = minimumFontSize;

            do {
                font = new Font(font.FontFamily, fontSize);
                textSize = graphics.MeasureString(text, font);
                fontSize++;
            } while (rectangleSizes.All(rectangleSize => textSize.Width < rectangleSize.w * 1.2 && textSize.Height * Math.Pow(0.8, text.Count(c => c == '\n')) < rectangleSize.h * verticalBias));

            return new Font(font.FontFamily, fontSize);
        }

        public static Bitmap DrawDebugRectangle(Bitmap image, (int x, int y) center, (int w, int h) rectangleSize, Color color) {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (color.IsEmpty) throw new ArgumentNullException(nameof(color));

            using (Graphics g = Graphics.FromImage(image)) {
                using (SolidBrush brush = new(color)) {
                    // Calculate the top-left corner from the center
                    int topLeftX = center.x - rectangleSize.w / 2;
                    int topLeftY = center.y - rectangleSize.h / 2;
                    g.FillRectangle(brush, topLeftX, topLeftY, rectangleSize.w, rectangleSize.h);
                }
            }

            return image;
        }

        public static Color OppositeExtremeColor(Color c) {
            int r = c.R > 127 ? 0 : 255;
            int g = c.G > 127 ? 0 : 255;
            int b = c.B > 127 ? 0 : 255;

            return Color.FromArgb(r, g, b);
        }
    }
}