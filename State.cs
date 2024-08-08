using System.Drawing;

namespace Vic3MapCSharp
{
    //State class stores provIDlist, name, arableResources, cappedResources, and discoverableResources
    public class State : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public (int x, int y) RectangleCenter { get; set; } = (0, 0);
        public (int w, int h) MaxRectangleSize { get; set; } = (0, 0);
        public (int x, int y) SquareCenter { get; set; } = (0, 0);
        public (int w, int h) MaxSquareSize { get; set; } = (0, 0);
        public HashSet<(int x, int y)> Coords { get; set; } = new();

        public int stateID = 0;
        public List<string> traits = new();
        public string subsistenceBuilding = "";
        public int navalID = 0;
        public Dictionary<string, Resource> resources = new();
        public int arableLand = 0;
        public List<string> homeLandList = new();

        //dictionary of color and its province
        public Dictionary<Color, Province> provDict = new();

        public State(string name)
        {
            this.Name = name;
        }
        public State() { }

        public void GetCenter(bool floodFill = false)
        {
            //check if coordList has elements
            if (Coords.Count == 0)
            {
                SetCoords();
            }

            if (Coords.Count == 0) return;

            (RectangleCenter, MaxRectangleSize) = MaximumRectangle.Center(Coords.ToList(), floodFill, false);
            (SquareCenter, MaxSquareSize) = MaximumRectangle.Center(Coords.ToList(), floodFill, true);
        }

        //set coords of state from provinces
        public void SetCoords() {
            Coords = new HashSet<(int, int)>();
            foreach (KeyValuePair<Color, Province> entry in provDict) {
                Coords.UnionWith(entry.Value.Coords);
            }
        }

        //tostring
        public override string ToString()
        {
            return Name + ": " + provDict.Count;
        }
    }
}