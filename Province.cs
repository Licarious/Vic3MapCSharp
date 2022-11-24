using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Province
{
    public string name = "";
    public int intearnlID = -1;
    public string terrain = "";
    public Color color = Color.FromArgb(0, 0, 0, 0);
    public List<(int, int)> coordList = new List<(int, int)>();
    public string hubName = "";
    public bool isImpassible = false;
    public bool isPrimeLand = false;
    public bool isLake = false;
    public bool isSea = false;

    public Province(string name, Color color) {
        this.name = name;
        this.color = color;
    }
}
