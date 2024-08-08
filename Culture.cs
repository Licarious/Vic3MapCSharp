using System.Drawing;

namespace Vic3MapCSharp
{
    public class Culture : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public (int x, int y) RectangleCenter { get; set; } = (0, 0);
        public (int w, int h) MaxRectangleSize { get; set; } = (0, 0);
        public (int x, int y) SquareCenter { get; set; } = (0, 0);
        public (int w, int h) MaxSquareSize { get; set; } = (0, 0);
        public HashSet<(int x, int y)> Coords { get; set; } = new();


        public List<string> traits = new();
        public string religion = "";
        public string graphics = "";
        public List<State> states = new();

        public Culture() {
        }

        public Culture(string name) {
            this.Name = name;
        }

        public void GetCenter(bool floodFill = false) {
            if (Coords.Count == 0) {
                foreach (var s in states) {
                    if (s is null) continue;
                    Coords.UnionWith(s.Coords);
                }
            }

            if (Coords.Count == 0) return;

            (RectangleCenter, MaxRectangleSize) = MaximumRectangle.Center(Coords.ToList(), floodFill, false);
            (SquareCenter, MaxSquareSize) = MaximumRectangle.Center(Coords.ToList(), floodFill, true);
        }

    }
}
