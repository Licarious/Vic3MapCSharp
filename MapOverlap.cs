namespace Vic3MapCSharp
{
    public class MapOverlap
    {
        public Province province;
        public Nation nation;
        public string stateName;
        public string regionName;

        public int OverlapArea;

        public MapOverlap(Province p, Nation n, string s, string r) {
            province = p;
            nation = n;
            stateName = s;
            regionName = r;
            OverlapArea = new HashSet<(int, int)>(nation.coordSet).Intersect(new HashSet<(int, int)>(province.coordList)).Count();
        }



    }
}
