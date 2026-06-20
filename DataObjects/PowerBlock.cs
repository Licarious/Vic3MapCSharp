namespace Vic3MapCSharp.DataObjects
{
    public class PowerBlock : IDrawable
    {
        private static readonly List<String> SuybectTypes = ["chartered_company", "colony", "crown_land", "dominion", "personal_union", "protectorate", "puppet", "tributary", "vassal"];
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public List<(int x, int y, int h, int w)> MaximumRectangles { get; set; } = [];
        public HashSet<(int x, int y)> Coords { get; set; } = [];
        public Nation? Leader { get; set; } = null;
        public List<Nation> Members { get; set; } = [];

        public PowerBlock() { }
        public PowerBlock(Nation leader)
        {
            Leader = leader;
            Name = leader.Name;
            Color = leader.Color;
            Members.Add(leader);
            AddSubjects(leader);
        }


        public void GetCenter(bool floodFill = false)
        {
            if (Coords.Count == 0)
            {
                foreach(var member in Members)
                {
                    Coords.UnionWith(member.Coords);
                }
            }
            if (Coords.Count == 0) return;
            MaximumRectangles = MaximumRectangle.Center([.. Coords], floodFill);
        }

        public List<Nation> AddSubjects(Nation nation)
        {
            List<Nation> addedSubjects = [];
            foreach (var subject in nation.DiplomaticPactByType)
            {
                foreach (var type in SuybectTypes)
                {
                    if (subject.Key != type) continue;
                    foreach (var subjNation in subject.Value)
                    {
                        if (!Members.Contains(subjNation))
                        {
                            Members.Add(subjNation);
                            addedSubjects.Add(subjNation);
                        }
                        AddSubjects(subjNation);
                    }
                }
            }
            return addedSubjects;
        }

        public override string ToString() => $"{Name}\t Leader: {Leader?.Name}\t Members: {Members.Count}";
    }
}
