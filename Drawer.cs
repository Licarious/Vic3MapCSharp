using System.Drawing;
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

            //print the type of the first drawable
            Console.WriteLine($"Drawing {drawables[0].GetType()} color map");

            using (Graphics g = Graphics.FromImage(newImage)) {
                foreach (var drawable in drawables) {
                    DrawColorMap(g, drawable, drawable.Color);
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

            Bitmap firstImage = images[0];
            int width = firstImage.Width;
            int height = firstImage.Height;
            MapSize = (width, height);
            Bitmap newImage = new(width, height);

            using (Graphics g = Graphics.FromImage(newImage)) {
                foreach (var image in images) {
                    g.DrawImage(image, 0, 0, width, height);
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
                    if (province.isSea || province.isLake) {
                        foreach (var (x, y) in province.Coords) {
                            newImage.SetPixel(x, y, waterColor);
                        }
                    }
                }
            }

            return newImage;
        }

        public static Bitmap WriteText(Bitmap image, string text, (int x, int y) center, (int w, int h) rectangleSize, int minimumFontSize, Color color) {
            return WriteText(image, text, center, rectangleSize, minimumFontSize, color, null);
        }
        public static Bitmap WriteText_old(Bitmap image, string text, (int x, int y) center, (int w, int h) rectangleSize, int minimumFontSize, Color color, Font? font) {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (color.IsEmpty) throw new ArgumentNullException(nameof(color));

            font ??= new Font("Verdna", minimumFontSize);

            //split the text by whitespace
            string[] words = text.Split();

            // Find the largest font rectangleSize that fits in the rectangle of rectangleSize h x w
            using (Graphics graphics = Graphics.FromImage(image)) {
                SizeF textSize;
                int fontSize = minimumFontSize;

                do {
                    font = new Font(font.FontFamily, fontSize);
                    textSize = graphics.MeasureString(text, font);
                    fontSize++;
                } while (textSize.Width < rectangleSize.w && textSize.Height < rectangleSize.h);

                // Use the last font size that fit within the dimensions
                font = new Font(font.FontFamily, fontSize - 1);

                // Organize the words into lines that fit within the width of the rectangle
                List<string> lines = new();
                StringBuilder currentLine = new();

                foreach (var word in words) {
                    if (graphics.MeasureString(currentLine + word, font).Width > rectangleSize.w) {
                        lines.Add(currentLine.ToString().Trim());
                        currentLine.Clear();
                    }
                    currentLine.Append(word + " ");
                }
                lines.Add(currentLine.ToString().Trim());

                // Calculate the top-left corner from the center
                int topLeftX = center.x - rectangleSize.w / 2;
                int topLeftY = center.y - rectangleSize.h / 2;

                // Calculate the total height of the text block
                float lineHeight = graphics.MeasureString("A", font).Height;
                float totalTextHeight = lines.Count * lineHeight;

                // Adjust the starting Y-coordinate to center the text vertically
                float startY = topLeftY + (rectangleSize.h - totalTextHeight) / 2;

                // Draw the text
                using (SolidBrush brush = new(color)) {
                    float currentY = startY;

                    StringFormat format = new StringFormat {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    foreach (var line in lines) {
                        if (currentY + lineHeight > topLeftY + rectangleSize.h) break; // Stop if the text exceeds the height
                        graphics.DrawString(line, font, brush, new RectangleF(topLeftX, currentY, rectangleSize.w, lineHeight), format);
                        currentY += lineHeight * 0.9f; // Reduce line spacing
                    }

                    Console.WriteLine($"Text: {text} - Font Size: {font.SizeInPoints}");
                }
            }
            return image;
        }

        public static Bitmap WriteText(Bitmap image, string text, (int x, int y) center, (int w, int h) rectangleSize, int minimumFontSize, Color color, Font? font) {
            return WriteText(image, text, center, rectangleSize, minimumFontSize, color, Color.FromArgb(0,0,0,0), font);
        }
        public static Bitmap WriteText(Bitmap image, string text, (int x, int y) center, (int w, int h) rectangleSize, int minimumFontSize, Color textColor, Color borderColor, Font? font) {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (textColor.IsEmpty) throw new ArgumentNullException(nameof(textColor));

            font ??= new Font("Verdana", minimumFontSize);

            StringFormat stringFormat = new() {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            // Split the text by whitespace
            string[] words = text.Split();
            using (Graphics graphics = Graphics.FromImage(image)) {
                SizeF textSize;
                int fontSize = minimumFontSize;

                double verticalBias = words.Length > 1 ? 1.0 : 1.2;

                do {
                    font = new Font(font.FontFamily, fontSize);
                    textSize = graphics.MeasureString(text, font);
                    fontSize++;
                } while (textSize.Width < rectangleSize.w * 1.2 && textSize.Height < rectangleSize.h * verticalBias);

                font = new Font(font.FontFamily, fontSize - 1);

                int yOffset = center.y - (words.Length > 1 ? (int)(textSize.Height * 0.35) : 0);

                using (SolidBrush textBrush = new(textColor)) {
                    foreach (var word in words) {
                        if (borderColor.A != 0) {
                            using (Pen borderPen = new(borderColor, 1)) {
                                SizeF wordSize = graphics.MeasureString(word, font);
                                int borderX = center.x - (int)(wordSize.Width / 2 * 1.2);
                                int borderY = yOffset - (int)(wordSize.Height / 2 * 1.2);
                                int borderWidth = (int)(wordSize.Width * 1.2);
                                int borderHeight = (int)(wordSize.Height * 1.2);
                                graphics.DrawRectangle(borderPen, borderX, borderY, borderWidth, borderHeight);
                            }
                        }
                        graphics.DrawString(word, font, textBrush, new Point(center.x, yOffset), stringFormat);
                        yOffset += (int)(textSize.Height * 0.6);
                    }
                }
            }
            return image;
        }

        public static Bitmap WriteText_old(Bitmap image, string text, (int x, int y) center, (int w, int h) rectangleSize, int minimumFontSize, Color color, Color borderColor, Font? font) {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (color.IsEmpty) throw new ArgumentNullException(nameof(color));

            font ??= new Font("Verdna", minimumFontSize);

            StringFormat stringFormat = new() {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            //split the text by whitespace
            string[] words = text.Split();
            using (Graphics graphics = Graphics.FromImage(image)) {
                SizeF textSize;
                int fontSize = minimumFontSize;

                double verticalBias = words.Length > 1 ? 1.0 : 1.2;

                do {
                    font = new Font(font.FontFamily, fontSize);
                    textSize = graphics.MeasureString(text, font);
                    fontSize++;
                } while (textSize.Width < rectangleSize.w *1.2 && textSize.Height < rectangleSize.h * verticalBias);

                int yOffset = center.y - (words.Length > 1 ? (int)(textSize.Height * 0.35) : 0);

                using (SolidBrush brush = new(color)) {
                    foreach (var word in words) {
                        graphics.DrawString(word, font, brush, new Point(center.x, yOffset), stringFormat);
                        yOffset += (int)(textSize.Height * 0.6);
                    }
                }
            }
            return image;
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
    }
}