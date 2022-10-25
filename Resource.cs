using System.Drawing;
//Resource class stores type, knownAmmount, discoverableAmmount
public class Resource
{
    public string name = "";
    public int knownAmmount = 0;
    public int discoverableAmmount = 0;
    public string type = "";
    public Color color = Color.HotPink;
    public Color textColor = Color.DarkBlue;

    public Resource(string name) {
        this.name = name;
    }
    public Resource() {
    }
    //toString method
    public override string ToString() {
        return name + ": " + knownAmmount + "/" + discoverableAmmount;
    }
}
