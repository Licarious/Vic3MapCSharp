using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vic3MapCSharp
{
    public class Nation : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public (int x, int y) RectangleCenter { get; set; } = (0, 0);
        public (int w, int h) MaxRectangleSize { get; set; } = (0, 0);
        public (int x, int y) SquareCenter { get; set; } = (0, 0);
        public (int w, int h) MaxSquareSize { get; set; } = (0, 0);
        public HashSet<(int x, int y)> Coords { get; set; } = new();

        public int ID { get; set; } = -1;
        public Dictionary<Color, Province> Provinces { get; set; } = new();
        public List<string> Cultures { get; set; } = new();
        public string Type { get; set; } = "";
        public string Tier { get; set; } = "";
        public State? Capital { get; set; } = null;
        public List<State> ClaimList { get; set; } = new();

        public Nation(string tag) => Name = tag;
        public Nation() { }

        public Nation(Nation other) {
            Name = other.Name;
            Color = other.Color;
            Coords = new HashSet<(int x, int y)>(other.ClaimList.SelectMany(state => state.Coords));
            GetCenter();
        }

        public void GetCenter(bool floodFill = false) {
            if (Coords.Count == 0) {
                foreach (var province in Provinces.Values) {
                    Coords.UnionWith(province.Coords);
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

        public override string ToString() => $"{Name}\t ID: {ID}\t Provs: {Provinces.Count}";
    }

}