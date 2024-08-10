using System.Drawing;

namespace Vic3MapCSharp
{
    //class Region stores Name, Color, and States
    public class Region : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public (int x, int y) RectangleCenter { get; set; } = (0, 0);
        public (int w, int h) MaxRectangleSize { get; set; } = (0, 0);
        public (int x, int y) SquareCenter { get; set; } = (0, 0);
        public (int w, int h) MaxSquareSize { get; set; } = (0, 0);
        public HashSet<(int x, int y)> Coords { get; set; } = new();
        public string GfxCulture { get; set; } = "";
        public List<State> States { get; set; } = new();

        public Region(string name) => Name = name;
        public Region() { }

        public void GetCenter(bool floodFill = false) {
            if (Coords.Count == 0) {
                foreach (var state in States) {
                    if (state.Coords.Count == 0) {
                        state.SetCoords();
                    }
                    Coords.UnionWith(state.Coords);
                }
            }

            if (Coords.Count == 0) return;

            var coordList = Coords.ToList();
            var tasks = new[]
            {
                Task.Run(() => (RectangleCenter, MaxRectangleSize) = MaximumRectangle.Center(coordList, floodFill, false)),
                Task.Run(() => (SquareCenter, MaxSquareSize) = MaximumRectangle.Center(coordList, floodFill, true))
            };

            Task.WaitAll(tasks);
        }

        public override string ToString() => $"{Name}: {States.Count}";
    }
}