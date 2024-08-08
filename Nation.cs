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

        public int internalID = -1;
        public Dictionary<Color, Province> provinces = new();
        public List<string> cultures = new();
        public string type = "";
        public string tier = "";
        public State? capital = null;
        public List<State> claimList = new();

        public Nation(string tag)
        {
            Name = tag;
        }

        public Nation()
        {
        }
        public void GetCenter(bool floodFill = false)
        {
            if(Coords.Count == 0) {
                foreach(Province province in provinces.Values)
                {
                    Coords.UnionWith(province.Coords);
                }
            }
            if (Coords.Count == 0) return;

            (RectangleCenter, MaxRectangleSize) = MaximumRectangle.Center(Coords.ToList(), floodFill, false);
            (SquareCenter, MaxSquareSize) = MaximumRectangle.Center(Coords.ToList(), floodFill, true);
        }

        public override string ToString()
        {
            //returns name, internalID, provinces.Count
            return Name + "\t ID: " + internalID + "\t Provs:" + provinces.Count;

        }
    }

}