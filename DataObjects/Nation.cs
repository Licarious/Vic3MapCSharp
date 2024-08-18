using System.Drawing;

namespace Vic3MapCSharp.DataObjects
{
    public class Nation : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public HashSet<(int x, int y)> Coords { get; set; } = [];
        public List<(int x, int y, int h, int w)> MaximumRectangles { get; set; } = [];

        public int ID { get; set; } = -1;
        public Dictionary<Color, Province> Provinces { get; set; } = [];
        public List<string> Cultures { get; set; } = [];
        public string Type { get; set; } = "";
        public string Tier { get; set; } = "";
        public State? Capital { get; set; } = null;
        public List<State> ClaimList { get; set; } = [];

        public Nation(string tag) => Name = tag;
        public Nation() { }

        public Nation(Nation other)
        {
            Name = other.Name;
            Color = other.Color;
            Coords = new HashSet<(int x, int y)>(other.ClaimList.SelectMany(state => state.Coords));
            GetCenter();
        }

        public void GetCenter(bool floodFill = false)
        {
            if (Coords.Count == 0)
            {
                foreach (var province in Provinces.Values)
                {
                    Coords.UnionWith(province.Coords);
                }
            }

            if (Coords.Count == 0) return;
            MaximumRectangles = MaximumRectangle.Center(Coords.ToList(), floodFill);
        }

        public override string ToString() => $"{Name}\t ID: {ID}\t Provs: {Provinces.Count}";
    }

}