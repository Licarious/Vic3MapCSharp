using System.Drawing;

namespace Vic3MapCSharp
{
    public class Province
    {
        public string name = "";
        public int internalID = -1;
        public string terrain = "";
        public Color color = Color.FromArgb(0, 0, 0, 0);
        public List<(int, int)> coordList = new();
        public HashSet<(int, int)> coordSet = new();
        public string hubName = "";
        public bool isImpassible = false;
        public bool isPrimeLand = false;
        public bool isLake = false;
        public bool isSea = false;

        public Province(Color color) {
            this.name = "x" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
            this.color = color;
        }

        public Province(string name) {
            this.name = name;
            this.color = ColorTranslator.FromHtml("#" + name.Replace("x", ""));
        }

        public void SetHashSet() {
            coordSet = new HashSet<(int, int)>(coordList);
        }
    }
}