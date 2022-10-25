
using System.Drawing;
//class Region stores Name, Color, and States
public class Region
{
    public string name = "";
    public Color color = Color.FromArgb(0, 0, 0, 0);
    public List<State> states = new List<State>();
    public List<string> stateNames = new List<string>();
    public (int, int) center = (0, 0);
    public List<(int, int)> coordList = new List<(int, int)>();
    public(int, int) maxRecSize = (0, 0);
    public bool floodFillMaxRec = true;
    public Region(string name) {
        this.name = name;
    }
    public Region() { }
    
    public void getCenter2(bool squareDefault = false) {
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

        //create gfg object
        MaximumRectangle mr = new MaximumRectangle();

        (center, maxRecSize) = mr.center(coordList, floodFillMaxRec, squareDefault);

    }

    //tostring
    public override string ToString() {
        return name + ": " + states.Count;
    }
}
