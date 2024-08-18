using System.Drawing;

namespace Vic3MapCSharp.DataObjects
{
    //class Region stores Name, Color, and States
    public class Region : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public List<(int x, int y, int h, int w)> MaximumRectangles { get; set; } = [];
        public HashSet<(int x, int y)> Coords { get; set; } = [];
        public string GfxCulture { get; set; } = "";
        public List<State> States { get; set; } = [];

        public Region(string name) => Name = name;
        public Region() { }

        public void GetCenter(bool floodFill = false)
        {
            if (Coords.Count == 0)
            {
                foreach (var state in States)
                {
                    if (state.Coords.Count == 0)
                    {
                        state.SetCoords();
                    }
                    Coords.UnionWith(state.Coords);
                }
            }

            if (Coords.Count == 0) return;

            MaximumRectangles = MaximumRectangle.Center(Coords.ToList(), floodFill);
        }

        public override string ToString() => $"{Name}: {States.Count}";
    }
}