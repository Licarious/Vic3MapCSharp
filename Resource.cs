using System.Drawing;

namespace Vic3MapCSharp
{
    //Resource class stores Type, knownAmount, discoverableAmount
    public class Resource
    {
        public string Name { get; set; } = "";
        public int KnownAmount { get; set; } = 0;
        public int DiscoverableAmount { get; set; } = 0;
        public string Type { get; set; } = "";
        public Color Color { get; set; } = Color.HotPink;
        public Color TextColor { get; set; } = Color.DarkBlue;

        public Resource(string name) {
            Name = name;
        }

        public Resource() {
        }

        // ToString method
        public override string ToString() {
            return $"{Name}: {KnownAmount}/{DiscoverableAmount}";
        }

        public string AmountString() {
            if (DiscoverableAmount > 0 && KnownAmount>0)
                return $"{KnownAmount}|({DiscoverableAmount})";
            if (DiscoverableAmount > 0) {
                return $"({DiscoverableAmount})";
            }
            return $"{KnownAmount}";
        }
    }
}