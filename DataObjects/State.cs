using System.Drawing;

namespace Vic3MapCSharp.DataObjects
{
    public class State : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public List<(int x, int y, int h, int w)> MaximumRectangles { get; set; } = new();
        public HashSet<(int x, int y)> Coords { get; set; } = new();

        public int StateID { get; set; } = 0;
        public List<string> Traits { get; set; } = new();
        public string SubsistenceBuilding { get; set; } = "";
        public int NavalID { get; set; } = 0;
        public Dictionary<string, Resource> Resources { get; set; } = new();
        public int ArableLand { get; set; } = 0;
        public List<string> HomeLandList { get; set; } = new();
        public Dictionary<Color, Province> Provinces { get; set; } = new();

        public State(string name) => Name = name;
        public State() { }

        public void GetCenter(bool floodFill = false) {
            if (Coords.Count == 0) SetCoords();
            if (Coords.Count == 0) return;
            MaximumRectangles = MaximumRectangle.Center(Coords.ToList(), floodFill);
        }

        public void SetCoords() => Coords = Provinces.Values.SelectMany(province => province.Coords).ToHashSet();

        public override string ToString() => $"{Name}: {Provinces.Count}";
    }
}