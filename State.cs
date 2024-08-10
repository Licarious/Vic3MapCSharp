using System.Drawing;

namespace Vic3MapCSharp
{
    // State class stores provIDlist, name, arableResources, cappedResources, and discoverableResources
    public class State : IDrawable
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public (int x, int y) RectangleCenter { get; set; } = (0, 0);
        public (int w, int h) MaxRectangleSize { get; set; } = (0, 0);
        public (int x, int y) SquareCenter { get; set; } = (0, 0);
        public (int w, int h) MaxSquareSize { get; set; } = (0, 0);
        public HashSet<(int x, int y)> Coords { get; set; } = new();

        public int StateID { get; set; } = 0;
        public List<string> Traits { get; set; } = new();
        public string SubsistenceBuilding { get; set; } = "";
        public int NavalID { get; set; } = 0;
        public Dictionary<string, Resource> Resources { get; set; } = new();
        public int ArableLand { get; set; } = 0;
        public List<string> HomeLandList { get; set; } = new();

        // Dictionary of color and its province
        public Dictionary<Color, Province> Provinces { get; set; } = new();

        public State(string name) {
            Name = name;
        }

        public State() { }

        public void GetCenter(bool floodFill = false) {
            // Check if coordList has elements
            if (Coords.Count == 0) {
                SetCoords();
            }

            if (Coords.Count == 0) return;
            List<Task> tasks = new() {
                Task.Run(() => (RectangleCenter, MaxRectangleSize) = MaximumRectangle.Center(Coords.ToList(), floodFill, false)),
                Task.Run(() => (SquareCenter, MaxSquareSize) = MaximumRectangle.Center(Coords.ToList(), floodFill, true))
            };

            Task.WaitAll(tasks.ToArray());
        }

        // Set coords of state from Provinces
        public void SetCoords() {
            Coords = Provinces.Values.SelectMany(province => province.Coords).ToHashSet();
            //Console.WriteLine($"{this}: {Coords.Count}");
        }

        // ToString method
        public override string ToString() {
            return $"{Name}: {Provinces.Count}";
        }
    }
}