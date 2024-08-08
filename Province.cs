using System.Drawing;

namespace Vic3MapCSharp
{
    public class Province : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public (int x, int y) RectangleCenter { get; set; } = (0, 0);
        public (int w, int h) MaxRectangleSize { get; set; } = (0, 0);
        public (int x, int y) SquareCenter { get; set; } = (0, 0);
        public (int w, int h) MaxSquareSize { get; set; } = (0, 0);

        public HashSet<(int x, int y)> Coords { get; set; } = new();

        public int internalID = -1;
        public string terrain = "";
        public string hubName = "";
        public bool isImpassible = false;
        public bool isPrimeLand = false;
        public bool isLake = false;
        public bool isSea = false;

        public Province(Color color)
        {
            Name = "x" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
            this.Color = color;
        }

        public Province(string name)
        {
            this.Name = name;
            Color = ColorTranslator.FromHtml("#" + name.Replace("x", ""));
        }

        public void GetCenter(bool squareDefault = false) {
            if (Coords.Count == 0) return;

            (RectangleCenter, MaxRectangleSize) = MaximumRectangle.Center(Coords.ToList(), false, squareDefault);
        }
    }
}