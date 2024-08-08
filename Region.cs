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

        public string gfxCulture = "";
        public List<State> states = new();
        public Region(string name)
        {
            this.Name = name;
        }
        public Region() { }

        public void GetCenter(bool floodFill = false)
        {
            if(Coords.Count == 0)
            {
                foreach (State state in states){
                    if(state.Coords.Count == 0) {
                        state.SetCoords();
                    }
                    Coords.UnionWith(state.Coords);
                }
            }

            //check if coordList has elements
            if (Coords.Count == 0) return;

            (RectangleCenter, MaxRectangleSize) = MaximumRectangle.Center(Coords.ToList(), floodFill, false);
            (SquareCenter, MaxSquareSize) = MaximumRectangle.Center(Coords.ToList(), floodFill, true);
        }

        //tostring
        public override string ToString()
        {
            return Name + ": " + states.Count;
        }
    }
}