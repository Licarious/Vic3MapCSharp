using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vic3MapCSharp
{
    public class Culture
    {
        public string name = "";
        public Color color = Color.FromArgb(0, 0, 0, 0);
        public List<string> traits = new();
        public string religion = "";
        public string graphics = "";
        public List<State> states = new();
        public HashSet<(int x, int y)> coords = new();

        public Culture() {
        }

        public Culture(string name) {
            this.name = name;
        }

        public void SetCorrds() {
            foreach (var s in states) {
                if (s != null) {
                    coords.UnionWith(s.coordList);
                }
            }
        }



    }
}
