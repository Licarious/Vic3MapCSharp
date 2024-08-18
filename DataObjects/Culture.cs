using System.Drawing;

namespace Vic3MapCSharp.DataObjects
{
    public class Culture : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public List<(int x, int y, int h, int w)> MaximumRectangles { get; set; } = [];
        public HashSet<(int x, int y)> Coords { get; set; } = [];

        public List<string> Traits { get; set; } = [];
        public string Religion { get; set; } = "";
        public string Graphics { get; set; } = "";
        public HashSet<State> States { get; set; } = [];

        public Culture() { }

        public Culture(string name) {
            Name = name;
        }

        public void GetCenter(bool floodFill = false) {
            if (Coords.Count == 0) {
                foreach (var s in States) {
                    if (s is null) continue;
                    Coords.UnionWith(s.Coords);
                }
            }

            if (Coords.Count == 0) return;
            MaximumRectangles = MaximumRectangle.Center(Coords.ToList(), floodFill);
        }
    }
}
