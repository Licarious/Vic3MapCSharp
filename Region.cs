using System.Drawing;

namespace Vic3MapCSharp
{
    //class Region stores Name, Color, and States
    public class Region
    {
        public string name = "";
        public string gfxCulture = "";
        public Color color = Color.FromArgb(0, 0, 0, 0);
        public List<State> states = new();
        public List<string> stateNames = new();
        public (int, int) center = (0, 0);
        public List<(int, int)> coordList = new();
        public (int, int) maxRecSize = (0, 0);
        public bool floodFillMaxRec = true;
        public Region(string name) {
            this.name = name;
        }
        public Region() { }

        public void GetCenter2(bool squareDefault = false) {
            //all coords from each state in region to coordlist
            if (coordList.Count == 0) {
                for (int i = 0; i < states.Count; i++) {
                    for (int j = 0; j < states[i].coordList.Count; j++) {
                        coordList.Add(states[i].coordList[j]);
                    }
                }
            }

            //check if coordList has elements
            if (coordList.Count == 0) {
                return;
            }
            
            (center, maxRecSize) = MaximumRectangle.Center(coordList, floodFillMaxRec, squareDefault);
        }

        //tostring
        public override string ToString() {
            return name + ": " + states.Count;
        }
    }
}