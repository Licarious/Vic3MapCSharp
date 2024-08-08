using System.Drawing;

namespace Vic3MapCSharp
{
    //Resource class stores type, knownAmount, discoverableAmount
    public class Resource
    {
        public string name = "";
        public int knownAmount = 0;
        public int discoverableAmount = 0;
        public string type = "";
        public Color color = Color.HotPink;
        public Color textColor = Color.DarkBlue;

        public Resource(string name)
        {
            this.name = name;
        }
        public Resource()
        {
        }
        //toString method
        public override string ToString()
        {
            return name + ": " + knownAmount + "/" + discoverableAmount;
        }
    }
}