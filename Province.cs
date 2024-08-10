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

        public int ID { get; set; } = -1;
        public string Terrain { get; set; } = "";
        public string HubName { get; set; } = "";
        public bool IsImpassible { get; set; } = false;
        public bool IsPrimeLand { get; set; } = false;
        public bool IsLake { get; set; } = false;
        public bool IsSea { get; set; } = false;

        public Province(Color color) {
            Name = $"x{color.R:X2}{color.G:X2}{color.B:X2}";
            Color = color;
        }

        public Province(string name) {
            Name = name;
            Color = ColorTranslator.FromHtml($"#{name.Replace("x", "")}");
        }

        public void GetCenter(bool squareDefault = false) {
            if (Coords.Count == 0) return;

            (RectangleCenter, MaxRectangleSize) = MaximumRectangle.Center(Coords.ToList(), false, squareDefault);
        }
    }
}