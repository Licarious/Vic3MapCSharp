namespace Vic3MapCSharp.DataObjects
{
    //class Region stores Name, Color, and States
    public class Georegion : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public List<(int x, int y, int h, int w)> MaximumRectangles { get; set; } = [];
        public HashSet<(int x, int y)> Coords { get; set; } = [];
        public string ShortKey { get; set; } = "";
        public List<State> States { get; set; } = [];
        public List<Region> Regions { get; set; } = [];

        public Georegion(string name) => Name = name;
        public Georegion() { }

        public void GetRegionStates()
        {
            foreach (var region in Regions)
            {
                foreach (var state in region.States)
                {
                    States.Add(state);
                }
            }
        }
        public void GetCenter(bool floodFill = false)
        {
            if (Coords.Count == 0)
            {
                foreach (var region in Regions)
                {
                    if (Color.A == 0) { Color = region.Color; }
                    region.GetCenter(true);
                    Coords.UnionWith(region.Coords);
                }
                foreach (var state in States)
                {
                    if (Color.A == 0) { Color = state.Color; }
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
