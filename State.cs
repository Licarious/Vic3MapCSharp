using System.Drawing;
//State class stores provIDlsit, name, arabelResoures, cappedResoures, and discoverableResources
public class State
{
    public List<string> provIDList = new List<string>();
    public string name = "";
    public int stateID = 0;
    public List<string> traits = new List<string>();
    public string subsistanceBuilding = "";
    public int navalID = 0;
    public List<(string type,Color color)> hubs = new List<(string, Color)>();
    public List<Color> impassables = new List<Color>();
    public List<Color> primeLand = new List<Color>();
    public List<Resource> resoures = new List<Resource>();
    public int arableLand = 0;
    public List<Color> provColors = new List<Color>();
    public Color color = Color.FromArgb(0, 0, 0, 0);
    public List<(int,int)> coordList = new List<(int, int)>();
    public (int, int) center = (0, 0);
    public (int, int) maxRecSize = (0, 0);
    public bool floodFillMaxRec = false;
    public State(string name) {
        this.name = name;
    }
    public State() { }

    //convert hexdicimal to color
    public void hexToColor() {
        for (int i = 0; i< provIDList.Count; i++) {
            Color c = ColorTranslator.FromHtml("#"+provIDList[i]);
            provColors.Add(c);
        }
    }
    public void getCenter2(bool squareDefault = false) {
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
        return name + ": " + provIDList.Count;
    }
}
