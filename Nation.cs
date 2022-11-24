using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Nation
{
    public string name = "";
    public int interalID = -1;
    public Color color = Color.FromArgb(0, 0, 0, 0);
    public Dictionary<Color, Province> provDict = new Dictionary<Color, Province>();
    public List<String> cultures = new List<string>();
    public string type = "";
    public string tier = "";
    public State capital = null;
    public List<State> claimList = new List<State>();

    public (int, int) center = (0, 0);
    public (int, int) maxRecSize = (0, 0);
    public bool floodFillMaxRec = false;

    public Nation(string tag) {
        this.name = tag;
    }

    public Nation() {
    }
    public void getCenter2(bool squareDefault = false) {
        //check if coordList has elements
        if (provDict.Count == 0) {
            return;
        }
        //add all coords from each province in nation to coordlist
        List<(int, int)> coordList = new List<(int, int)>();
        foreach (KeyValuePair<Color, Province> entry in provDict) {
            for (int i = 0; i < entry.Value.coordList.Count; i++) {
                coordList.Add(entry.Value.coordList[i]);
            }
        }


        //create gfg object
        MaximumRectangle mr = new MaximumRectangle();

        (center, maxRecSize) = mr.center(coordList, floodFillMaxRec, squareDefault);
    }

    public override string ToString() {
        //returns name, interalID, provDict.Count
        return name + "\t ID: " + interalID + "\t Provs:" + provDict.Count;

    }
}

