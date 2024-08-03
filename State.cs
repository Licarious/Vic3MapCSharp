using System.Drawing;

namespace Vic3MapCSharp
{
    //State class stores provIDlist, name, arableResources, cappedResources, and discoverableResources
    public class State
    {
        public List<string> provIDList = new();
        public string name = "";
        public int stateID = 0;
        public List<string> traits = new();
        public string subsistenceBuilding = "";
        public int navalID = 0;
        public List<Resource> resources = new();
        public int arableLand = 0;
        public List<Color> provColors = new();
        public Color color = Color.FromArgb(0, 0, 0, 0);
        public List<(int, int)> coordList = new();
        public (int x, int y) center = (0, 0);
        public (int, int) maxRecSize = (0, 0);
        public bool floodFillMaxRec = false;
        public List<string> homeLandList = new();

        //dictionary of color and its province
        public Dictionary<Color, Province> provDict = new();

        public State(string name) {
            this.name = name;
        }
        public State() { }

        //convert hexadecimal to color
        public void HexToColor() {
            for (int i = 0; i < provIDList.Count; i++) {
                Color c = ColorTranslator.FromHtml("#" + provIDList[i]);
                provColors.Add(c);
            }
        }
        public void GetCenter2(bool squareDefault = false) {
            //check if coordList has elements
            if (coordList.Count == 0) {
                return;
            }
            
            (center, maxRecSize) = MaximumRectangle.Center(coordList, floodFillMaxRec, squareDefault);
        }

        //create a new province object and add it to provDict
        public void AddProv(string p) {
            
            Province prov = new(p);
            provDict.Add(prov.color, prov);
        }


        //set coords of state from provDict
        public void SetCoords() {
            coordList = new List<(int, int)>();
            foreach (KeyValuePair<Color, Province> entry in provDict) {
                coordList.AddRange(entry.Value.coordList);
            }
        }


        //tostring
        public override string ToString() {
            return name + ": " + provIDList.Count;
        }
    }
}