using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;

namespace Vic3MapCSharp
{
    public class Parser
    {
        private static readonly Random rand = new();

        public static Dictionary<string, State> ParseStateFiles(Dictionary<Color, Province> provDict, string localDir) {
            Dictionary<string, State> stateDict = new();

            // Read all files in localDir/_Input/state_regions
            string[] files = Directory.GetFiles(Path.Combine(localDir, "_Input", "map_data", "state_regions"), "*.txt");
            int count = 0;

            foreach (string file in files) {
                // Read file
                string[] lines = File.ReadAllLines(file);
                State s = new();
                Resource dr = new();
                bool cappedResourceFound = false;
                bool discoverableResourceFound = false;
                bool traitsFound = false;

                foreach (string line in lines) {
                    string cl = CleanLine(line);

                    // Get STATE_NAME
                    if (cl.StartsWith("STATE_")) {
                        s = new State(cl.Split()[0]);

                        // In case people are overriding states in later files
                        // Check if state with the same name already exists in states and if so, delete it
                        if (stateDict.ContainsKey(s.Name)) {
                            stateDict.Remove(s.Name);
                        }

                        stateDict.Add(s.Name, s);
                    }
                    // Get stateID
                    if (cl.StartsWith("id")) {
                        s.stateID = int.Parse(cl.Split()[2]);
                    }
                    if (cl.StartsWith("subsistence_building")) {
                        s.subsistenceBuilding = cl.Split("=")[1].Replace("\"", "").Trim();
                    }

                    // Get provinces
                    if (cl.TrimStart().StartsWith("provinces")) {
                        string[] l2 = cl.Replace("{", "").Replace("}", "").Split("=")[1].ToLower().Split();
                        foreach (string provinceCode in l2) {
                            if (provinceCode.StartsWith("\"x") || provinceCode.StartsWith("x")) {
                                string colorValue = provinceCode.Replace("\"", "").Replace("x", "");
                                Color color = ColorTranslator.FromHtml("#" + colorValue);
                                if (provDict.TryGetValue(color, out var province)) {
                                    s.provDict[color] = province;
                                }
                            }
                        }
                    }
                    // Get impassable colors
                    if (cl.TrimStart().StartsWith("impassable")) {
                        var colorCodes = cl.Split('=')[1].Split()
                                           .Where(code => code.StartsWith("\"x") || code.StartsWith("x"))
                                           .Select(code => code.Replace("\"", "").Replace("x", ""));

                        foreach (var colorValue in colorCodes) {
                            Color color = ColorTranslator.FromHtml("#" + colorValue);
                            // Set province with color to isImpassible
                            if (provDict.TryGetValue(color, out var province)) {
                                province.isImpassible = true;
                            }
                        }
                    }
                    // Get prime_land colors
                    if (cl.TrimStart().StartsWith("prime_land")) {
                        var colorCodes = cl.Split('=')[1].Split()
                                           .Where(code => code.StartsWith("\"x") || code.StartsWith("x"))
                                           .Select(code => code.Replace("\"", "").Replace("x", ""));

                        foreach (var colorValue in colorCodes) {
                            Color color = ColorTranslator.FromHtml("#" + colorValue);
                            // Set province with color to prime land
                            if (provDict.TryGetValue(color, out var province)) {
                                province.isPrimeLand = true;
                            }
                        }
                    }

                    // Get traits
                    if (cl.Trim().StartsWith("traits")) {
                        traitsFound = true;
                    }
                    if (traitsFound) {
                        foreach (var trait in cl.Split().Where(t => t.StartsWith("\""))) {
                            s.traits.Add(trait.Replace("\"", ""));
                        }
                    }

                    // Get arable_land
                    if (cl.TrimStart().StartsWith("arable_land")) {
                        s.arableLand = int.Parse(cl.Split("=")[1].Trim());
                        count++;
                    }
                    // Get arable_resources
                    if (cl.TrimStart().StartsWith("arable_resources")) {
                        foreach (var res in cl.Split("=")[1].Replace("\"", "").Split().Where(r => r.StartsWith("bg_"))) {
                            s.resources[res] = new Resource(res) {
                                knownAmount = s.arableLand,
                                type = "agriculture"
                            };
                        }
                    }
                    // Get capped_resources
                    if (cl.TrimStart().StartsWith("capped_resources")) {
                        cappedResourceFound = true;
                    }
                    if (cappedResourceFound && cl.TrimStart().StartsWith("bg_")) {
                        var l2 = cl.Replace("\"", "").Split("=");
                        s.resources[l2[0].Trim()] = new Resource(l2[0].Trim()) {
                            knownAmount = int.Parse(l2[1].Trim()),
                            type = "resource"
                        };
                    }
                    // Get discoverable resources
                    if (cl.TrimStart().StartsWith("resource")) {
                        discoverableResourceFound = true;
                    }
                    if (discoverableResourceFound) {
                        var l2 = cl.Split("=");
                        if (cl.TrimStart().StartsWith("type")) {
                            dr = new Resource(l2[1].Trim().Replace("\"", "")) {
                                type = "discoverable"
                            };
                            s.resources[dr.name] = dr;
                        }
                        else if (cl.TrimStart().StartsWith("undiscovered_amount")) {
                            dr.discoverableAmount = int.Parse(l2[1].Trim());
                        }
                        else if (cl.TrimStart().StartsWith("amount") || cl.TrimStart().StartsWith("discovered_amount")) {
                            dr.knownAmount = int.Parse(l2[1].Trim());
                        }
                    }
                    // Get naval id
                    if (cl.TrimStart().StartsWith("naval_exit_id")) {
                        s.navalID = int.Parse(cl.Split("=")[1].Trim());
                    }

                    // Get city color
                    if (cl.TrimStart().StartsWith("city") || cl.TrimStart().StartsWith("port") || cl.TrimStart().StartsWith("farm") || cl.TrimStart().StartsWith("mine") || cl.TrimStart().StartsWith("wood")) {
                        if (cl.Contains('x')) {
                            try {
                                Color hubC = ColorTranslator.FromHtml("#" + cl.Split("=")[1].Replace("\"", "").Replace("x", "").Trim());
                                // Set province with color hubName to name
                                if (provDict.TryGetValue(hubC, out var p)) {
                                    p.hubName = cl.Split("=")[0].Trim();
                                    if (s.Color.A == 0) s.Color = p.Color;
                                }
                            }
                            catch {
                                // Console.WriteLine("Error: can not parse color for hub " + cl + " in state " +state.name);
                            }
                        }
                    }
                    // Reset cappedResourceFound and discoverableResourceFound
                    if (cl.Trim().StartsWith("}")) {
                        cappedResourceFound = false;
                        discoverableResourceFound = false;
                        traitsFound = false;
                    }
                }
            }

            Console.WriteLine("States: " + count + " | " + stateDict.Count);
            return stateDict;
        }

        public static Dictionary<string, Region> ParseRegionFiles(Dictionary<string, State> stateDict, string localDir) {
            Dictionary<string, Region> regions = new();
            string[] files = Directory.GetFiles(Path.Combine(localDir, "_Input", "common", "strategic_regions"), "*.txt");

            int count = 0;
            foreach (string file in files) {
                string[] lines = File.ReadAllLines(file);
                bool stateStart = false;
                int indentation = 0;
                Region? r = null;

                foreach (string line in lines) {
                    string cl = CleanLine(line);

                    if (cl.Trim().StartsWith("region_")) {
                        string regionName = cl.Split('=')[0].Trim();
                        r = new Region(regionName);

                        // In case people are overriding regions in later files
                        // Check if region with the same name already exists in regions and if so, delete it
                        if (regions.ContainsKey(regionName)) {
                            regions.Remove(regionName);
                        }

                        regions.Add(regionName, r);
                    }
                    if (r == null) {
                        continue;
                    }
                    if (cl.Trim().StartsWith("states")) {
                        stateStart = true;
                    }
                    else if (cl.Trim().StartsWith("map_color")) {
                        count++;
                        r.Color = LineToColor(cl);
                    }
                    else if (cl.StartsWith("graphical_culture")) {
                        r.gfxCulture = cl.Split('=')[1].Replace("\"", "").Trim();
                    }

                    if (stateStart) {
                        string[] states = cl.Split();
                        foreach (string state in states) {
                            if (state.StartsWith("STATE_") && stateDict.TryGetValue(state, out var stateObj)) {
                                r.states.Add(stateObj);
                            }
                        }
                    }

                    if (cl.Contains('{') || cl.Contains('}')) {
                        foreach (char c in cl) {
                            if (c == '{') {
                                indentation++;
                            }
                            else if (c == '}') {
                                indentation--;
                                if (indentation == 1) {
                                    stateStart = false;
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Regions: " + count + " | " + regions.Count);
            return regions;
        }

        public static Dictionary<Color, Province> ParseTerrain(string localDir) {
            Dictionary<Color, Province> provDict = new();
            string[] lines = File.ReadAllLines(Path.Combine(localDir, "_Input", "map_data", "province_terrains.txt"));

            int count = 1;
            //for each cl in lines
            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) {
                    continue;
                }
                if (line.Contains('=')) {
                    string[] l1 = line.Replace("\"", "").Trim().Split('=');
                    Color color = ColorTranslator.FromHtml(l1[0].ToLower().Replace("x", "#"));

                    if (!provDict.ContainsKey(color)) {
                        provDict[color] = new Province(color);
                    }

                    provDict[color].terrain = l1[1];
                    provDict[color].internalID = count;
                }
                count++;
            }
            return provDict;
        }

        public static void ParseDefaultMap(Dictionary<Color, Province> colorToProvDic, string localDir) {
            string[] lines = File.ReadAllLines(Path.Combine(localDir, "_Input", "map_data", "default.map"));
            bool seaStart = false;
            bool lakeStart = false;

            foreach (string line in lines) {
                if (line.Trim().StartsWith("sea_starts")) {
                    seaStart = true;
                }
                else if (line.Trim().StartsWith("lakes")) {
                    lakeStart = true;
                }

                if (seaStart || lakeStart) {
                    string[] parts = CleanLine(line).Split();
                    foreach (string part in parts) {
                        if (part.StartsWith("x")) {
                            Color color = ColorTranslator.FromHtml("#" + part.Replace("x", "").Trim());
                            if (colorToProvDic.TryGetValue(color, out var province)) {
                                if (seaStart) {
                                    province.isSea = true;
                                }
                                else if (lakeStart) {
                                    province.isLake = true;
                                }
                            }
                        }
                        else if (part.StartsWith("}")) {
                            if (seaStart) {
                                seaStart = false;
                            }
                            else if (lakeStart) {
                                lakeStart = false;
                            }
                            break;
                        }
                    }
                }
            }
        }

        public static void ParseProvMap(Dictionary<Color, Province> provinceDict, string localDir) {
            Stopwatch sw = new();
            sw.Start();
            using Bitmap image = new(localDir + "/_Input/map_data/provinces.png");

            Console.WriteLine("Parsing Map");

            // Lock the bitmap's bits
            Rectangle rect = new(0, 0, image.Width, image.Height);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);

            // Get the address of the first line
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap
            int bytes = Math.Abs(bmpData.Stride) * image.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Unlock the bits
            image.UnlockBits(bmpData);

            // Process the pixel data
            int pixelSize = Image.GetPixelFormatSize(image.PixelFormat) / 8;
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    int index = (y * bmpData.Stride) + (x * pixelSize);
                    if (index + 2 < rgbValues.Length) {
                        Color c = Color.FromArgb(
                            255,                  // A
                            rgbValues[index + 2], // R
                            rgbValues[index + 1], // G
                            rgbValues[index]      // B
                        );

                        if (provinceDict.ContainsKey(c)) {
                            provinceDict[c].Coords.Add((x, y));
                        }
                    }
                }
            }
            sw.Stop();
            Console.WriteLine("new: Map parsed in " + sw.ElapsedMilliseconds + "ms");
        }

        public static Dictionary<string, Nation> ParseNationFiles(Dictionary<Color, Province> provinces, Dictionary<string, State> states, string localDir) {
            if (provinces is null) {
                throw new ArgumentNullException(nameof(provinces));
            }

            Dictionary<string, Nation> nationDict = new();
            string[] files = Directory.GetFiles(Path.Combine(localDir, "_Input", "common", "country_definitions"), "*.txt");

            //read all lines in each file
            foreach (string file in files) {
                string[] lines = File.ReadAllLines(file);
                int indent = 0;
                Nation n = new();
                foreach (string line in lines) {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    string cl = CleanLine(line);

                    if (indent == 0 && cl.Contains('=')) {
                        n = new Nation(cl.Split('=')[0].Trim());
                        nationDict.TryAdd(n.Name, n);
                    }
                    if (indent == 1) {
                        if (cl.StartsWith("color")) n.Color = LineToColor(cl);
                        else if (cl.StartsWith("country_type")) n.type = cl.Split('=')[1].Trim();
                        else if (cl.StartsWith("tier")) n.tier = cl.Split('=')[1].Trim();
                        else if (cl.StartsWith("cultures")) n.cultures = cl.Replace("{", "").Replace("}", "").Split('=')[1].Trim().Split().ToList();
                        if (cl.StartsWith("capital")) {
                            string capital = cl.Split('=')[1].Trim();
                            n.capital = states.ContainsKey(capital) ? states[capital] : null;
                        }
                    }

                    if (cl.Contains('{') || cl.Contains('}')) {
                        string[] parts = cl.Split();
                        foreach (string part in parts) {
                            if (part.Contains('{')) indent++;
                            if (part.Contains('}')) indent--;
                        }
                    }
                }
            }

            files = Directory.GetFiles(Path.Combine(localDir, "_Input", "common", "history", "states"), "*.txt");

            if (files.Length == 0) {
                Console.WriteLine("No country definitions found");
                throw new FileNotFoundException("No country definitions found");
            }

            //read all lines in each file
            foreach (string file in files) {
                string[] lines = File.ReadAllLines(file);
                int indent = 0;
                Nation n = new();
                State s = new();
                bool createStateFound = false;
                int stateIndent = -1;
                bool stateProvsFound = false;
                int stateProvsIndent = -1;
                foreach (string line in lines) {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    string cl = CleanLine(line);

                    if (indent == 1 && cl.StartsWith("s:")) {
                        string stateName = cl.Split('=')[0].Split("s:")[1].Trim();
                        if (states.ContainsKey(stateName)) {
                            s = states[stateName];
                        }
                    }
                    if(s is null) continue;
                    else if (indent >= 2) {
                        if (cl.StartsWith("add_homeland")) s.homeLandList.Add(cl.Split('=')[1].Replace("cu:","").Trim());
                        else if (cl.StartsWith("create_state")) {
                            createStateFound = true;
                            stateIndent = indent;
                        }
                        else if (cl.StartsWith("add_claim")) {
                            string stateTag = cl.Split('=')[1].Trim();
                            if (nationDict.ContainsKey(stateTag)) nationDict[stateTag].claimList.Add(s);
                        }
                    }

                    if (createStateFound) {
                        if (cl.StartsWith("country")) {
                            string tag = cl.Replace("C:", "c:").Split("c:")[1].Trim();
                            if (nationDict.ContainsKey(tag)) n = nationDict[tag];
                        }
                        else if (cl.StartsWith("owned_provinces")) {
                            stateProvsFound = true;
                            stateProvsIndent = indent;
                        }
                        else if (cl.StartsWith("state_type")) n.type = cl.Split('=')[1].Trim();
                    }

                    if (stateProvsFound) {
                        string[] provincesNames = cl.Replace("\"", " ").Trim()
                            .Split()
                            .Where(p => p.StartsWith("x"))
                            .Select(p => p.Replace("x", "#"))
                            .ToArray();

                        foreach (string p in provincesNames) {
                            string colorCode = p.Trim();
                            try {
                                // Replace 'x' with '#' to form a valid HTML color code
                                
                                Color c = ColorTranslator.FromHtml(colorCode);
                                if (provinces.ContainsKey(c)) {
                                    
                                    n.provinces.TryAdd(c, provinces[c]);
                                    //Console.WriteLine("Added province " + colorCode + " to state " + s.Name);
                                }
                                //Console.WriteLine("Added province " + colorCode + " to state " + s.Name);
                            }
                            catch {
                                //Console.WriteLine("Error: can not parse color for province " + colorCode + " in state " + s.Name);
                            }
                        }
                    }

                    if (cl.Contains('{') || cl.Contains('}')) {
                        string[] parts = cl.Split();
                        foreach (string part in parts) {
                            if (part.Contains('{')) indent++;
                            if (part.Contains('}')) {
                                indent--;
                                if (indent == stateIndent) createStateFound = false;
                                if (indent == stateProvsIndent) stateProvsFound = false;
                            }
                        }
                    }
                }
            }
            return nationDict;
        }

        public static Dictionary<string, Culture> ParseCultureFiles(Dictionary<string, State> states, string localDir) {
            Dictionary<string, Culture> cultures = new();
            string[] files = Directory.GetFiles(Path.Combine(localDir, "_Input", "common", "cultures"), "*.txt");

            foreach (string file in files) {
                string[] lines = File.ReadAllLines(file);
                int indent = 0;
                bool traitsFound = false;
                Culture c = new();
                foreach (string line in lines) {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    string cl = CleanLine(line);

                    if (indent == 0 && cl.Contains('=')) {
                        c = new Culture(cl.Split('=')[0].Trim());
                        cultures.Add(c.Name, c);

                        foreach (var state in states.Values) {
                            if (state.homeLandList.Contains(c.Name)) c.states.Add(state);
                        }
                    }

                    if (indent == 1) {
                        if (cl.StartsWith("color")) c.Color = LineToColor(cl);
                        else if (cl.StartsWith("graphical_culture")) c.graphics = cl.Split('=')[1].Trim();
                        else if (cl.StartsWith("religion")) c.religion = cl.Split('=')[1].Trim();
                        else if (cl.StartsWith("traits")) traitsFound = true;

                    }

                    if (traitsFound) {
                        string[] parts = cl.Split();
                        //get only parts after {
                        if (cl.Contains('{')) {
                            parts = cl.Split('{')[1].Split();
                        }

                        foreach (string part in parts) {
                            if(part.Contains('}')) break;
                            c.traits.Add(part.Replace("\"", ""));
                        }
                    }

                    if (cl.Contains('{') || cl.Contains('}')){
                        string[] parts = cl.Split();
                        foreach (string part in parts) {
                            if (part.Contains('{')) indent++;
                            if (part.Contains('}')) {
                                indent--;
                                traitsFound = false;
                            }
                        }
                    }
                }
            }

            return cultures;
        }

        public static void ParseRGOsCSV(Dictionary<string, Region> regionDict, string localDir) {
            string filePath = Path.Combine(localDir, "_Input", "TextFiles", "RGOs.csv");
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException("RGOs.csv not found");
            }

            string[] lines = File.ReadAllLines(filePath);
            string[] header = lines[0].Split(';');

            // Go through each line in RGOs.csv
            for (int i = 1; i < lines.Length; i++) {
                string[] fields = lines[i].Split(';');
                if (fields.Length == 0) continue;

                string regionName = "region_" + fields[0];
                if (!regionDict.TryGetValue(regionName, out var region)) {
                    Console.WriteLine("Region not found: " + fields[0]);
                    continue;
                }

                string stateName = "STATE_" + fields[1].ToUpper();
                State state = region.states.Find(s => s.Name == stateName);
                if (state == null) {
                    Console.WriteLine("State not found: " + fields[1]);
                    continue;
                }

                state.resources.Clear();
                for (int j = 2; j < header.Length - 2; j++) {
                    if (header[j].StartsWith("Known ")) {
                        Resource resource = new("bg_" + header[j].Replace("Known ", "")) {
                            type = "discoverable",
                            knownAmount = int.Parse(fields[j]),
                            discoverableAmount = int.Parse(fields[j + 1])
                        };
                        j++;
                        if (resource.knownAmount > 0 || resource.discoverableAmount > 0) {
                            state.resources.Add(resource.name, resource);
                        }
                    }
                    else {
                        Resource resource = new("bg_" + header[j]) {
                            type = "resource",
                            knownAmount = int.Parse(fields[j])
                        };
                        if (resource.knownAmount > 0) {
                            state.resources.Add(resource.name, resource);
                        }
                    }
                }

                state.arableLand = int.Parse(fields[^2]);
                List<string> arableResources = fields[^1].Split().ToList();
                foreach (string ar in arableResources) {
                    if (!string.IsNullOrWhiteSpace(ar)) {
                        Resource resource = new("bg_" + ar) {
                            knownAmount = state.arableLand,
                            type = "arable"
                        };
                        state.resources.Add(resource.name, resource);
                    }
                }
            }
        }

        public static string CleanLine(string line) {
            return line.Split('#')[0].Replace("=", " = ").Replace("{", " { ").Replace("}", " } ").Replace("  ", " ").Trim();
        }

        public static Dictionary<string, object> ParseConfig(string localDir) {
            var configDict = SetDefaultConfig();
            string filePath = Path.Combine(localDir, "_Input", "config.cfg");

            if (!File.Exists(filePath)) {
                Console.WriteLine("Configuration file not found: " + filePath);
                return configDict;
            }

            string[] lines = File.ReadAllLines(filePath);

            bool colorFound = false;
            bool ignoreFound = false;
            List<string> ignoreRGONames = new();
            List<(List<string> rgoNames, Color hColor, Color tColor)> rgoColors = new();
            //go through each line in input.cfg
            foreach (string line in lines) {
                string cleanedLine = CleanLine(line);
                if (string.IsNullOrWhiteSpace(cleanedLine) || cleanedLine.StartsWith("#")) continue;

                string[] parts = cleanedLine.Split('=');
                string key, value;
                if (parts.Length == 2) {
                    key = parts[0].Trim();
                    value = parts[1].Trim();
                }
                else {
                    key = parts[0].Trim();
                    value = "";
                }

                if (key == "Color") {
                    colorFound = true;
                }
                else if (key == "IgnoreRGO") {
                    ignoreFound = true;
                }
                else {
                    configDict[key] = ParseValue(value);
                }

                if (colorFound && cleanedLine.Contains('(')) {
                    List<string> buildingSubstring = key.Split(',').Select(s => s.Trim()).ToList();
                    List<int> color = value.Split(new char[] { '(', ')', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => int.TryParse(s, out int i) ? Math.Clamp(i, 0, 255) : 128)
                                           .ToList();

                    while (color.Count < 6) {
                        color.Add(128);
                    }

                    Color hColor = Color.FromArgb(color[0], color[1], color[2]);
                    Color tColor = Color.FromArgb(color[3], color[4], color[5]);

                    rgoColors.Add((buildingSubstring, hColor, tColor));
                }
                else if (ignoreFound && cleanedLine.Contains('"')) {
                    List<string> ignoreSubstring = cleanedLine.Split(new char[] { '"' }, StringSplitOptions.RemoveEmptyEntries)
                                                              .Select(s => s.Trim())
                                                              .Where(s => !string.IsNullOrEmpty(s))
                                                              .ToList();
                    ignoreRGONames.AddRange(ignoreSubstring);
                }

                if (cleanedLine.Contains('}')) {
                    if (colorFound) {
                        configDict["RgoColors"] = rgoColors;
                        colorFound = false;
                    }
                    if (ignoreFound) {
                        configDict["IgnoreRGONames"] = ignoreRGONames;
                        ignoreFound = false;
                    }
                }
            }
            return configDict;
        }

        private static object ParseValue(string value) {
            if (bool.TryParse(value, out bool boolResult)) {
                return boolResult;
            }
            if (int.TryParse(value, out int intResult)) {
                return intResult;
            }
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleResult)) {
                return doubleResult;
            }
            return value;
        }

        private static Dictionary<string, object> SetDefaultConfig() {
            var defaultRgoColors = new List<(List<string> rgoNames, Color hColor, Color tColor)> {
                (new List<string> { "gold", "sulfur" }, Color.FromArgb(255, 215, 0), Color.FromArgb(0, 0, 187)),
                (new List<string> { "farms", "banana" }, Color.FromArgb(255, 255, 0), Color.FromArgb(165, 42, 42)),
                (new List<string> { "oil_", "coal_" }, Color.FromArgb(37, 37, 37), Color.FromArgb(255, 0, 0)),
                (new List<string> { "coffee", "ranches" }, Color.FromArgb(139, 69, 19), Color.FromArgb(50, 205, 50)),
                (new List<string> { "cotton", "sugar" }, Color.FromArgb(85, 188, 187), Color.FromArgb(148, 0, 211)),
                (new List<string> { "dye_", "silk_", "vineyard_" }, Color.FromArgb(148, 0, 211), Color.FromArgb(85, 188, 187)),
                (new List<string> { "logging", "rubber" }, Color.FromArgb(222, 184, 135), Color.FromArgb(0, 100, 0)),
                (new List<string> { "plantation" }, Color.FromArgb(0, 128, 0), Color.FromArgb(128, 0, 128)),
                (new List<string> { "copper" }, Color.FromArgb(255, 165, 0), Color.FromArgb(0, 100, 0)),
                (new List<string> { "gemstone" }, Color.FromArgb(0, 139, 139), Color.FromArgb(255, 0, 0)),
                (new List<string> { "mining", "tin_" }, Color.FromArgb(112, 128, 144), Color.FromArgb(165, 42, 42)),
                (new List<string> { "fish", "whal" }, Color.FromArgb(0, 139, 139), Color.FromArgb(0, 0, 64))
            };

            var defaultIgnoreRGONames = new List<string> {
                "bg_monuments",
                "bg_skyscraper"
            };

            var configDict = new Dictionary<string, object> {
                { "DrawRGOs", true },
                { "DrawStartingNations", true },
                { "DrawSaves", true },
                { "DrawDecentralized", true },
                { "DrawDebug", false },
                { "UseRGOsCSV", false },
                { "DrawCoastalBordersRegions", false },
                { "DrawCoastalBordersStates", false },
                { "DrawCoastalBordersNations", false },
                { "RgoColors", defaultRgoColors },
                { "IgnoreRGONames", defaultIgnoreRGONames }
            };

            return configDict;
        }

        public static void ParseSave(Dictionary<string, Region> regions, Dictionary<string, Nation> nations, string filePath) {
            if (regions is null) {
                throw new ArgumentNullException(nameof(regions));
            }

            if (nations is null) {
                throw new ArgumentNullException(nameof(nations));
            }
            //province dictionary for matching province internalID to province object
            Dictionary<int, Province> provDict = new();

            foreach (var region in regions.Values) {
                foreach (var state in region.states) {
                    foreach (var province in state.provDict.Values) {
                        if (province.internalID == -1) {
                            Console.WriteLine($"Error: {province.Name} has no internalID");
                            continue;
                        }
                        provDict[province.internalID] = province;
                    }
                }
            }

            //sort on internalID
            provDict = provDict.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

            //reset all provinces in nations
            foreach (Nation nat in nations.Values) {
                nat.provinces = new Dictionary<Color, Province>();
                nat.MaxRectangleSize = (0, 0);
                nat.RectangleCenter = (0, 0);
            }

            //read all lines in save file from filePath
            string[] lines = File.ReadAllLines(filePath);

            int indentation = -2;
            Nation? n = null;
            int potentialID = -1;
            bool foundSeaNodes = false;
            bool civilWarFound = false;
            //for each line in lines
            foreach (string line in lines) {
                string cl = CleanLine(line);

                if (cl.StartsWith("#") || string.IsNullOrEmpty(cl)) continue;

                if (cl.Contains("sea_nodes")) {
                    foundSeaNodes = true;
                }

                if (indentation == 0 && cl.Contains("= {")) {
                    string l2 = cl.Split("=")[0].Trim();
                    _ = int.TryParse(l2, out potentialID);
                }

                if (indentation == 1) {
                    if (cl.StartsWith("definition=")) {
                        string tag = cl.Split("=")[1].Replace("\"", "").Trim();
                        if (tag.Length < 8) {
                            if (nations.ContainsKey(tag) && !civilWarFound) {
                                n = nations[tag];
                                n.internalID = potentialID;
                            }
                            else if (civilWarFound) {
                                try {
                                    n = new Nation(tag + "_cw") {
                                        internalID = potentialID,
                                        Color = ColorFromHSV360(rand.Next(0, 360), 100, 100)
                                    };
                                    nations.Add(tag + "_cw", n);
                                    Console.WriteLine(n);
                                    civilWarFound = false;
                                }
                                catch {
                                    // Handle duplicate Civil War tag error
                                }
                            }
                        }
                    }
                    else if (n == null) continue;
                    else if (cl.StartsWith("map_color=rgb")) {
                        var rgbValues = cl.Split("=")[1]
                            .Replace("(", "").Replace(")", "").Replace("rgb", "")
                            .Split(",")
                            .Select(s => int.TryParse(s, out int i) ? i : (int?)null)
                            .Where(i => i.HasValue)
                            .Select(i => i.Value)
                            .ToList();

                        if (rgbValues.Count == 3) {
                            n.Color = Color.FromArgb(rgbValues[0], rgbValues[1], rgbValues[2]);
                        }
                    }
                    else if (cl.StartsWith("civil_war=yes")) {
                        civilWarFound = true;
                    }
                    else if (cl.StartsWith("country=") && int.TryParse(cl.Split("=")[1].Trim(), out potentialID)) {
                        try {
                            n = nations.Values.First(x => x.internalID == potentialID);
                        }
                        catch {
                            // Handle potentialID not found in nations error
                        }
                    }
                }
                if (indentation > 1 && !foundSeaNodes && cl.StartsWith("provinces=")) {
                    try {
                        n = nations.Values.First(x => x.internalID == potentialID);

                        string[] l2 = cl.Split("=")[1].Replace("{", "").Replace("}", "").Trim().Split();
                        var idList = l2.Select(s => int.TryParse(s, out int id) ? id : (int?)null)
                                       .Where(id => id.HasValue)
                                       .Select(id => id.Value)
                                       .ToList();

                        for (int i = 0; i < idList.Count; i += 2) {
                            for (int j = idList[i]; j <= idList[i] + idList[i + 1]; j++) {
                                if (provDict.ContainsKey(j)) {
                                    n.provinces[provDict[j].Color] = provDict[j];
                                }
                                else {
                                    // Handle j not found in provinces error
                                }
                            }
                        }
                    }
                    catch {
                        // Handle general error
                    }
                }

                if (cl.Contains('{') || cl.Contains('}')) {
                    foreach (string s in cl.Split()) {
                        if (s == "{") {
                            indentation++;
                        }
                        else if (s == "}") {
                            indentation--;
                        }
                    }
                }
            }
        }

        public static Color LineToColor(string line) {
            List<double> rgbValues = new();
            string[] e = line.Split('=')[1].Trim().Split();
            foreach (string s in e) {
                //try to parse state as double
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double d)) {
                    //if d is between 0 and 1.01 then multiply it by 255
                    if (d >= 0 && d <= 1.01) {
                        d *= 255;
                    }
                    //if d is outside of 0 and 255 then set it to 0 or 255
                    if (d < 0) {
                        d = 0;
                    }
                    if (d > 255) {
                        d = 255;
                    }

                    rgbValues.Add(d);
                }
            }
            //if rgbValues has less than 3 values, add 128 till it has 3
            while (rgbValues.Count < 3) {
                rgbValues.Add(128);
            }
            if (line.Contains("hsv360")) {
                return ColorFromHSV360(rgbValues[0], rgbValues[1], rgbValues[2]);
            }
            else if (line.Contains("hvs")) {
                return ColorFromHSV(rgbValues[0], rgbValues[1], rgbValues[2]);
            }
            else {
                //set n.color to rgbValues
                return Color.FromArgb((int)rgbValues[0], (int)rgbValues[1], (int)rgbValues[2]);
            }
        }

        public static Color ColorFromHSV(double v1, double v2, double v3) {
            //convert hsv to rgb
            double r, g, b;
            if (v3 == 0) {
                r = g = b = 0;
            }
            else {
                if (v2 == -1) v2 = 1;
                int i = (int)Math.Floor(v1 * 6);
                double f = v1 * 6 - i;
                double p = v3 * (1 - v2);
                double q = v3 * (1 - f * v2);
                double t = v3 * (1 - (1 - f) * v2);
                switch (i % 6) {
                    case 0: r = v3; g = t; b = p; break;
                    case 1: r = q; g = v3; b = p; break;
                    case 2: r = p; g = v3; b = t; break;
                    case 3: r = p; g = q; b = v3; break;
                    case 4: r = t; g = p; b = v3; break;
                    case 5: r = v3; g = p; b = q; break;
                    default: r = g = b = v3; break;
                }
            }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        public static Color ColorFromHSV360(double v1, double v2, double v3) {
            //converts hsv360 to rgb
            return ColorFromHSV(v1 / 360, v2 / 100, v3 / 100);
        }
    }
}