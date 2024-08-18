using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vic3MapCSharp.DataObjects;
using Region = Vic3MapCSharp.DataObjects.Region;

namespace Vic3MapCSharp
{
    public class Parser
    {
        private static readonly Random rand = new();
        private static readonly string[] hubTypes = ["city", "port", "farm", "mine", "wood"];

        /// <summary>
        /// Parses state files and returns a dictionary of states.
        /// </summary>
        /// <param name="provDict">Dictionary of provinces keyed by color.</param>
        /// <param name="localDir">Local directory path.</param>
        /// <returns>Dictionary of states keyed by state name.</returns>
        public static Dictionary<string, State> ParseStateFiles(Dictionary<Color, Province> provDict, string localDir) {
            var stateDict = new Dictionary<string, State>();
            var statesWithDuplicateProvincesBetween = new HashSet<(State, State)>();
            var colorsAlreadyUsedForStates = new HashSet<Color>();
            int count = 0;

            Console.WriteLine($"provinces count {provDict.Count}");

            string[] files = Directory.GetFiles(Path.Combine(localDir, "_Input", "map_data", "state_regions"), "*.txt");

            foreach (string file in files) {
                string[] lines = File.ReadAllLines(file);
                State s = new();
                Resource dr = new();
                bool cappedResourceFound = false, discoverableResourceFound = false, traitsFound = false, provincesFound = false;

                foreach (string line in lines) {
                    string cl = CleanLine(line);
                    string[] parts = cl.Split('=');
                    string key = parts[0].Trim();
                    string value = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                    switch (key) {
                        case string stateName when stateName.StartsWith("STATE_", StringComparison.OrdinalIgnoreCase):
                            s = new State(stateName);
                            stateDict[s.Name] = s;
                            break;

                        case "id":
                            if (int.TryParse(Regex.Match(cl, @"\d+").Value, out int stateID)) s.StateID = stateID;
                            else Console.WriteLine($"No valid state ID found in line: {cl}");
                            break;

                        case "subsistence_building":
                            s.SubsistenceBuilding = value.Replace("\"", "").Trim();
                            break;

                        case "provinces":
                            provincesFound = true;
                            break;

                        case "impassable":
                        case "prime_land":
                            var colorCodes = value.Split().Where(code => code.StartsWith("\"x") || code.StartsWith("x"))
                                .Select(code => code.Replace("\"", "").Replace("x", ""));
                            foreach (var colorValue in colorCodes) {
                                Color color = ColorTranslator.FromHtml("#" + colorValue);
                                if (provDict.TryGetValue(color, out var province)) {
                                    if (key == "impassable") province.IsImpassible = true;
                                    else province.IsPrimeLand = true;
                                }
                            }
                            break;

                        case "Traits":
                            traitsFound = true;
                            break;

                        case "arable_land":
                            s.ArableLand = int.Parse(value);
                            count++;
                            break;

                        case "arable_resources":
                            foreach (var res in value.Replace("\"", "").Split().Where(r => r.StartsWith("bg_"))) {
                                s.Resources[res] = new Resource(res) {
                                    KnownAmount = s.ArableLand,
                                    Type = "agriculture"
                                };
                            }
                            break;

                        case "capped_resources":
                            cappedResourceFound = true;
                            break;

                        case "resource":
                            discoverableResourceFound = true;
                            break;

                        case "naval_exit_id":
                            s.NavalID = int.Parse(value);
                            break;

                        default:
                            break;
                    }
                    if (provincesFound) {
                        foreach (var prov in cl.Replace("\"", "").Split().Where(p => p.StartsWith("x", StringComparison.OrdinalIgnoreCase))) {
                            Color c = ColorTranslator.FromHtml(prov.Trim().Replace("x", "#", StringComparison.OrdinalIgnoreCase));
                            if (provDict.TryGetValue(c, out var province)) {
                                if (!s.Provinces.TryAdd(c, province)) {
                                    Console.WriteLine($"Error: duplicate province color {c} in state {s.Name} line {cl}");
                                }
                            }
                        }

                        var statesToUpdate = stateDict.Values.Where(state => state.Name != s.Name).ToList();
                        Parallel.ForEach(statesToUpdate, state => {
                            var overlappingProvinces = state.Provinces.Keys.Intersect(s.Provinces.Keys).ToList();
                            foreach (var prov in overlappingProvinces) {
                                if (state.Provinces.Remove(prov)) statesWithDuplicateProvincesBetween.Add((s, state));
                            }
                        });
                    }

                    if (traitsFound) {
                        foreach (var trait in cl.Split().Where(t => t.StartsWith("\""))) {
                            s.Traits.Add(trait.Replace("\"", ""));
                        }
                    }

                    if (cappedResourceFound && cl.TrimStart().StartsWith("bg_")) {
                        var l2 = cl.Replace("\"", "").Split("=");
                        s.Resources[l2[0].Trim()] = new Resource(l2[0].Trim()) {
                            KnownAmount = int.Parse(l2[1].Trim()),
                            Type = "resource"
                        };
                    }

                    if (discoverableResourceFound) {
                        var l2 = cl.Split("=");
                        if (cl.TrimStart().StartsWith("type")) {
                            dr = new Resource(l2[1].Trim().Replace("\"", "")) {
                                Type = "discoverable"
                            };
                            s.Resources[dr.Name] = dr;
                        }
                        else if (cl.TrimStart().StartsWith("undiscovered_amount")) {
                            dr.DiscoverableAmount = int.Parse(l2[1].Trim());
                        }
                        else if (cl.TrimStart().StartsWith("amount") || cl.TrimStart().StartsWith("discovered_amount")) {
                            dr.KnownAmount = int.Parse(l2[1].Trim());
                        }
                    }

                    if (hubTypes.Any(type => cl.TrimStart().StartsWith(type)) && cl.Contains('x')) {
                        try {
                            Color hubC = ColorTranslator.FromHtml(value.Replace("\"", "").Replace("x", "#").Trim());
                            if (provDict.TryGetValue(hubC, out var p)) {
                                p.HubName = key;
                                if (s.Color.A == 0 && !colorsAlreadyUsedForStates.Contains(hubC)) {
                                    s.Color = Color.FromArgb(255, hubC);
                                }
                            }
                        }
                        catch {
                            Console.WriteLine($"Error: cannot parse color for hub {cl} in state {s.Name}");
                        }
                    }

                    if (cl.Contains('}')) {
                        cappedResourceFound = false;
                        discoverableResourceFound = false;
                        traitsFound = false;
                        provincesFound = false;
                    }
                }
            }

            var statesWithTransparentColor = stateDict.Values.Where(state => state.Color.A == 0).ToList();
            foreach (var state in statesWithTransparentColor) {
                var availableColor = state.Provinces.Values.Select(p => p.Color).FirstOrDefault(color => !colorsAlreadyUsedForStates.Contains(color));
                if (availableColor != default) {
                    state.Color = availableColor;
                    lock (colorsAlreadyUsedForStates) {
                        colorsAlreadyUsedForStates.Add(availableColor);
                    }
                }
                else {
                    int hash = state.Name.GetHashCode();
                    Color defaultColor = Color.FromArgb(255, (hash & 0xFF0000) >> 16, (hash & 0x00FF00) >> 8, hash & 0x0000FF);
                    state.Color = defaultColor;
                    lock (colorsAlreadyUsedForStates) {
                        colorsAlreadyUsedForStates.Add(defaultColor);
                    }
                }
            }

            var statesToUpdate2 = stateDict.Values.Where(state => state.Provinces.Values.All(p => p.IsSea || p.IsLake)).ToList();
            Parallel.ForEach(statesToUpdate2, state => {
                state.Color = Color.FromArgb(0, state.Color);
            });

            Console.WriteLine("States: " + count + " | " + stateDict.Count);
            return stateDict;
        }

        /// <summary>
        /// Parses region files and returns a dictionary of regions.
        /// </summary>
        /// <param name="stateDict">Dictionary of states keyed by state name.</param>
        /// <param name="localDir">Local directory path.</param>
        /// <returns>Dictionary of regions keyed by region name.</returns>
        public static Dictionary<string, Region> ParseRegionFiles(Dictionary<string, State> stateDict, string localDir) {
            var regions = new Dictionary<string, Region>();
            string[] files = Directory.GetFiles(Path.Combine(localDir, "_Input", "common", "strategic_regions"), "*.txt");
            int count = 0;

            foreach (string file in files) {
                string[] lines = File.ReadAllLines(file);
                bool stateStart = false;
                int indentation = 0;
                Region? r = null;

                foreach (string line in lines) {
                    string cl = CleanLine(line);
                    string[] parts = cl.Split('=');
                    string key = parts[0].Trim();
                    string value = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                    if (key.StartsWith("region_")) {
                        string regionName = key;
                        r = new Region(regionName);
                        regions[regionName] = r;
                    }
                    if (r == null) continue;

                    switch (key) {
                        case "states":
                            stateStart = true;
                            break;
                        case "map_color":
                            count++;
                            r.Color = LineToColor(cl);
                            break;
                        case "graphical_culture":
                            r.GfxCulture = value.Replace("\"", "").Trim();
                            break;
                        default:
                            break;
                    }

                    if (stateStart) {
                        var states = cl.Split()
                            .Where(state => state.StartsWith("STATE_") && stateDict.TryGetValue(state, out var stateObj))
                            .Select(state => stateDict[state]);
                        r.States.AddRange(states);
                    }

                    if (cl.Contains('{') || cl.Contains('}')) {
                        foreach (char c in cl) {
                            if (c == '{') indentation++;
                            else if (c == '}') {
                                indentation--;
                                if (indentation == 1) stateStart = false;
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Regions: " + count + " | " + regions.Count);
            return regions;
        }

        /// <summary>
        /// Parses terrain files and returns a dictionary of provinces.
        /// </summary>
        /// <param name="localDir">Local directory path.</param>
        /// <returns>Dictionary of provinces keyed by color.</returns>
        public static Dictionary<Color, Province> ParseTerrain(string localDir) {
            var provDict = new Dictionary<Color, Province>();
            string[] lines = File.ReadAllLines(Path.Combine(localDir, "_Input", "map_data", "province_terrains.txt"));

            int count = 1;
            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

                if (line.Contains('=')) {
                    string[] parts = line.Replace("\"", "").Trim().Split('=');
                    Color color = ColorTranslator.FromHtml(parts[0].ToLower().Replace("x", "#"));

                    if (!provDict.ContainsKey(color)) {
                        provDict[color] = new Province(color);
                    }

                    provDict[color].Terrain = parts[1];
                    provDict[color].ID = count;
                }
                count++;
            }
            return provDict;
        }

        /// <summary>
        /// Parses the default map and updates the provided dictionary of provinces.
        /// </summary>
        /// <param name="colorToProvDic">Dictionary of provinces keyed by color.</param>
        /// <param name="localDir">Local directory path.</param>
        public static void ParseDefaultMap(Dictionary<Color, Province> colorToProvDic, string localDir) {
            string[] lines = File.ReadAllLines(Path.Combine(localDir, "_Input", "map_data", "default.map"));
            bool seaStart = false, lakeStart = false;

            foreach (string line in lines) {
                string cleanLine = CleanLine(line);
                if (cleanLine.StartsWith("sea_starts")) seaStart = true;
                else if (cleanLine.StartsWith("lakes")) lakeStart = true;

                if (seaStart || lakeStart) {
                    string[] parts = cleanLine.Split();
                    foreach (string part in parts) {
                        if (part.StartsWith('x')) {
                            Color color = ColorTranslator.FromHtml(part.Replace("x", "#").Trim());
                            if (colorToProvDic.TryGetValue(color, out var province)) {
                                if (seaStart) province.IsSea = true;
                                else if (lakeStart) province.IsLake = true;
                            }
                        }
                        else if (part.StartsWith('}')) {
                            seaStart = lakeStart = false;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parses the province map and updates the provided dictionary of provinces.
        /// </summary>
        /// <param name="provinceDict">Dictionary of provinces keyed by color.</param>
        /// <param name="localDir">Local directory path.</param>
        public static void ParseProvMap(Dictionary<Color, Province> provinceDict, string localDir) {
            using Bitmap image = new(Path.Combine(localDir, "_Input", "map_data", "Provinces.png"));

            Console.WriteLine("Parsing Map");

            // Lock the bitmap's bits
            Rectangle rect = new(0, 0, image.Width, image.Height);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);

            try {
                // Get the address of the first line
                IntPtr ptr = bmpData.Scan0;

                // Declare an array to hold the bytes of the bitmap
                int bytes = Math.Abs(bmpData.Stride) * image.Height;
                byte[] rgbValues = new byte[bytes];

                // Copy the RGB values into the array
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

                // Process the pixel data
                int pixelSize = Image.GetPixelFormatSize(image.PixelFormat) / 8;
                for (int y = 0; y < image.Height; y++) {
                    int yOffset = y * bmpData.Stride;
                    for (int x = 0; x < image.Width; x++) {
                        int index = yOffset + (x * pixelSize);
                        if (index + 2 < rgbValues.Length) {
                            Color c = Color.FromArgb(
                                255,                  // A
                                rgbValues[index + 2], // R
                                rgbValues[index + 1], // G
                                rgbValues[index]      // B
                            );

                            if (provinceDict.TryGetValue(c, out Province? value)) {
                                value.Coords.Add((x, y));
                            }
                        }
                    }
                }
            }
            finally {
                // Unlock the bits
                image.UnlockBits(bmpData);
            }
        }

        /// <summary>
        /// Parses nation files and returns a dictionary of nations.
        /// </summary>
        /// <param name="provinces">Dictionary of provinces keyed by color.</param>
        /// <param name="states">Dictionary of states keyed by state name.</param>
        /// <param name="localDir">Local directory path.</param>
        /// <returns>Dictionary of nations keyed by nation name.</returns>
        public static Dictionary<string, Nation> ParseNationFiles(Dictionary<Color, Province> provinces, Dictionary<string, State> states, string localDir) {
            ArgumentNullException.ThrowIfNull(provinces);

            var nations = new Dictionary<string, Nation>();
            string[] files = Directory.GetFiles(Path.Combine(localDir, "_Input", "common", "country_definitions"), "*.txt");

            foreach (string file in files) {
                string[] lines = File.ReadAllLines(file);
                int indent = 0;
                Nation n = new();
                foreach (string line in lines) {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

                    string cl = CleanLine(line);

                    if (indent == 0 && cl.Contains('=')) {
                        n = new Nation(cl.Split('=')[0].Trim());
                        nations.TryAdd(n.Name, n);
                    }
                    else if (indent == 1) {
                        if (cl.StartsWith("color")) n.Color = LineToColor(cl);
                        else if (cl.StartsWith("country_type")) n.Type = cl.Split('=')[1].Trim();
                        else if (cl.StartsWith("Tier")) n.Tier = cl.Split('=')[1].Trim();
                        else if (cl.StartsWith("Cultures")) n.Cultures = cl.Replace("{", "").Replace("}", "").Split('=')[1].Trim().Split().ToList();
                        else if (cl.StartsWith("Capital")) {
                            string capital = cl.Split('=')[1].Trim();
                            n.Capital = states.TryGetValue(capital, out State? value) ? value : null;
                        }
                    }

                    if (cl.Contains('{') || cl.Contains('}')) {
                        foreach (char c in cl) {
                            if (c == '{') indent++;
                            else if (c == '}') indent--;
                        }
                    }
                }
            }

            files = Directory.GetFiles(Path.Combine(localDir, "_Input", "common", "history", "States"), "*.txt");

            if (files.Length == 0) {
                Console.WriteLine("No country definitions found");
                throw new FileNotFoundException("No country definitions found");
            }

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
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

                    string cl = CleanLine(line);

                    if (indent == 1 && cl.StartsWith("s:")) {
                        string stateName = cl.Split('=')[0].Split("s:")[1].Trim();
                        if (states.TryGetValue(stateName, out State? value)) s = value;
                    }
                    if (s is null) continue;

                    if (indent >= 2) {
                        if (cl.StartsWith("add_homeland")) s.HomeLandList.Add(cl.Split('=')[1].Replace("cu:", "").Trim());
                        else if (cl.StartsWith("create_state")) {
                            createStateFound = true;
                            stateIndent = indent;
                        }
                        else if (cl.StartsWith("add_claim")) {
                            string stateTag = cl.Split('=')[1].Replace("c:", "", StringComparison.OrdinalIgnoreCase).Trim();
                            if (nations.TryGetValue(stateTag, out Nation? value)) value.ClaimList.Add(s);
                        }
                    }

                    if (createStateFound) {
                        if (cl.StartsWith("country")) {
                            string tag = cl.Split('=')[1].Replace("c:", "", StringComparison.OrdinalIgnoreCase).Trim();
                            if (nations.TryGetValue(tag, out Nation? value)) n = value;
                        }
                        else if (cl.StartsWith("owned_provinces")) {
                            stateProvsFound = true;
                            stateProvsIndent = indent;
                        }
                        else if (cl.StartsWith("state_type")) n.Type = cl.Split('=')[1].Trim();
                    }

                    if (stateProvsFound) {
                        string[] provincesNames = cl.Replace("\"", " ").Trim()
                            .Split()
                            .Where(p => p.StartsWith("x"))
                            .Select(p => p.Replace("x", "#"))
                            .ToArray();

                        foreach (string p in provincesNames) {
                            try {
                                Color c = ColorTranslator.FromHtml(p.Trim());
                                if (provinces.TryGetValue(c, out Province? value)) n.Provinces.TryAdd(c, value);
                            }
                            catch { }
                        }
                    }

                    if (cl.Contains('{') || cl.Contains('}')) {
                        foreach (char c in cl) {
                            if (c == '{') indent++;
                            else if (c == '}') {
                                indent--;
                                if (indent == stateIndent) createStateFound = false;
                                if (indent == stateProvsIndent) stateProvsFound = false;
                            }
                        }
                    }
                }
            }
            return nations;
        }

        /// <summary>
        /// Parses culture files and returns a dictionary of cultures.
        /// </summary>
        /// <param name="states">Dictionary of states keyed by state name.</param>
        /// <param name="localDir">Local directory path.</param>
        public static Dictionary<string, Culture> ParseCultureFiles(Dictionary<string, State> states, string localDir) {
            Dictionary<string, Culture> cultures = [];
            string[] files = Directory.GetFiles(Path.Combine(localDir, "_Input", "common", "Cultures"), "*.txt");

            foreach (string file in files) {
                string[] lines = File.ReadAllLines(file);
                int indent = 0;
                bool traitsFound = false;
                Culture c = new();
                foreach (string line in lines) {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
                    string cl = CleanLine(line);

                    if (indent == 0 && cl.Contains('=')) {
                        c = new Culture(cl.Split('=')[0].Trim());
                        cultures.TryAdd(c.Name, c);
                        foreach (var state in states.Values) {
                            if (state.HomeLandList.Contains(c.Name)) c.States.Add(state);
                        }
                    }

                    if (indent == 1) {
                        if (cl.StartsWith("color")) c.Color = LineToColor(cl);
                        else if (cl.StartsWith("graphical_culture")) c.Graphics = cl.Split('=')[1].Trim();
                        else if (cl.StartsWith("Religion")) c.Religion = cl.Split('=')[1].Trim();
                        else if (cl.StartsWith("Traits")) traitsFound = true;
                    }

                    if (traitsFound) {
                        string[] parts = cl.Split();
                        if (cl.Contains('{')) {
                            parts = cl.Split('{')[1].Split();
                        }

                        foreach (string part in parts) {
                            if (part.Contains('}')) break;
                            c.Traits.Add(part.Replace("\"", ""));
                        }
                    }

                    if (cl.Contains('{') || cl.Contains('}')) {
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

        /// <summary>
        /// Parses the RGOs CSV file and updates the provided dictionary of regions.
        /// </summary>
        /// <param name="regions">Dictionary of regions keyed by region name.</param>
        /// <param name="localDir">Local directory path.</param>
        public static void ParseRGOsCSV(Dictionary<string, State> states, string localDir) {
            string filePath = Path.Combine(localDir, "_Input", "TextFiles", "RGOs.csv");
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException("RGOs.csv not found");
            }

            string[] lines = File.ReadAllLines(filePath);
            string[] header = lines[0].Split(';');

            for (int i = 1; i < lines.Length; i++) {
                string[] fields = lines[i].Split(';');
                if (fields.Length == 0) continue;

                string stateName = $"STATE_{fields[1].ToUpper()}";
                if (!states.TryGetValue(stateName, out var state)) {
                    Console.WriteLine($"State not found: {fields[1]}");
                    continue;
                }

                state.Resources.Clear();
                for (int j = 2; j < header.Length - 2; j++) {
                    string resourceName = "bg_" + header[j].Replace("Known ", "");
                    int knownAmount = int.Parse(fields[j]);
                    int discoverableAmount = header[j].StartsWith("Known ") ? int.Parse(fields[++j]) : 0;

                    if (knownAmount > 0 || discoverableAmount > 0) {
                        state.Resources[resourceName] = new Resource(resourceName) {
                            Type = header[j].StartsWith("Known ") ? "discoverable" : "resource",
                            KnownAmount = knownAmount,
                            DiscoverableAmount = discoverableAmount
                        };
                    }
                }

                state.ArableLand = int.Parse(fields[^2]);
                foreach (string ar in fields[^1].Split().Where(ar => !string.IsNullOrWhiteSpace(ar))) {
                    string resourceName = "bg_" + ar;
                    state.Resources[resourceName] = new Resource(resourceName) {
                        KnownAmount = state.ArableLand,
                        Type = "arable"
                    };
                }
            }
        }

        /// <summary>
        /// Cleans a line of text by removing unwanted characters.
        /// </summary>
        /// <param name="line">The line of text to clean.</param>
        /// <returns>The cleaned line of text.</returns>
        public static string CleanLine(string line) {
            int commentIndex = line.IndexOf('#');
            if (commentIndex >= 0) {
                line = line[..commentIndex];
            }

            StringBuilder sb = new(line.Length);
            bool lastWasSpace = false;

            foreach (char c in line) {
                if (c == '=') {
                    sb.Append(" = ");
                    lastWasSpace = true;
                }
                else if (c == '{') {
                    sb.Append(" { ");
                    lastWasSpace = true;
                }
                else if (c == '}') {
                    sb.Append(" } ");
                    lastWasSpace = true;
                }
                else if (char.IsWhiteSpace(c)) {
                    if (!lastWasSpace) {
                        sb.Append(' ');
                        lastWasSpace = true;
                    }
                }
                else {
                    sb.Append(c);
                    lastWasSpace = false;
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Parses the configuration files and returns a dictionary of configuration settings.
        /// </summary>
        /// <param name="localDir">Local directory path.</param>
        /// <returns>Dictionary of configuration settings.</returns>
        public static Dictionary<string, object> ParseConfig(string localDir) {
            var configDict = SetDefaultConfig();
            string filePath = Path.Combine(localDir, "_Input", "config.cfg");

            if (!File.Exists(filePath)) {
                Console.WriteLine("Configuration file not found: " + filePath);
                return configDict;
            }

            string[] lines = File.ReadAllLines(filePath);
            bool colorFound = false, ignoreFound = false;
            var ignoreRGONames = new List<string>();
            var rgoColors = new List<(List<string> rgoNames, Color hColor, Color tColor)>();

            foreach (var line in lines) {
                var cleanedLine = CleanLine(line);
                if (string.IsNullOrWhiteSpace(cleanedLine) || cleanedLine.StartsWith("#")) continue;

                var parts = cleanedLine.Split('=');
                var key = parts[0].Trim();
                var value = parts.Length > 1 ? parts[1].Trim() : "";

                switch (key) {
                    case "Color":
                        colorFound = true;
                        break;
                    case "IgnoreRGO":
                        ignoreFound = true;
                        break;
                    default:
                        configDict[key] = ParseValue(value);
                        break;
                }

                if (colorFound && cleanedLine.Contains('(')) {
                    var buildingSubstring = key.Split(',').Select(s => s.Trim()).ToList();
                    var color = value.Split(new[] { '(', ')', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => int.TryParse(s, out int i) ? Math.Clamp(i, 0, 255) : 128)
                                     .ToList();

                    while (color.Count < 3) color.Add(128);

                    var hColor = Color.FromArgb(color[0], color[1], color[2]);
                    var tColor = color.Count < 6 ? Drawer.OppositeExtremeColor(hColor) : Color.FromArgb(color[3], color[4], color[5]);

                    rgoColors.Add((buildingSubstring, hColor, tColor));
                }
                else if (ignoreFound && cleanedLine.Contains('"')) {
                    var ignoreSubstring = cleanedLine.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries)
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

        /// <summary>
        /// Parses a value from a string.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <returns>The parsed value as an object.</returns>
        private static object ParseValue(string value) {
            if (bool.TryParse(value, out bool boolResult)) return boolResult;
            if (int.TryParse(value, out int intResult)) return intResult;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleResult)) return doubleResult;
            return value;
        }

        /// <summary>
        /// Sets the default configuration settings.
        /// </summary>
        /// <returns>Dictionary of default configuration settings.</returns>
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

            return new Dictionary<string, object> {
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
        }

        /// <summary>
        /// Parses a save file and updates the provided dictionaries of regions and nations.
        /// </summary>
        /// <param name="regions">Dictionary of regions keyed by region name.</param>
        /// <param name="nations">Dictionary of nations keyed by nation name.</param>
        /// <param name="filePath">Path to the save file.</param>
        public static void ParseSave(Dictionary<string, Region> regions, Dictionary<string, Nation> nations, string filePath) {
            ArgumentNullException.ThrowIfNull(regions);
            ArgumentNullException.ThrowIfNull(nations);
            Dictionary<int, Province> provDict = [];

            foreach (var region in regions.Values) {
                foreach (var state in region.States) {
                    foreach (var province in state.Provinces.Values) {
                        if (province.ID == -1) {
                            Console.WriteLine($"Error: {province.Name} has no ID");
                            continue;
                        }
                        provDict[province.ID] = province;
                    }
                }
            }

            //sort on ID
            provDict = provDict.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

            //reset all Provinces in nations
            foreach (Nation nat in nations.Values) {
                nat.Provinces = [];
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
                            if (nations.TryGetValue(tag, out Nation? value) && !civilWarFound) {
                                n = value;
                                n.ID = potentialID;
                            }
                            else if (civilWarFound) {
                                try {
                                    n = new Nation(tag + "_cw") {
                                        ID = potentialID,
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
                            n = nations.Values.First(x => x.ID == potentialID);
                        }
                        catch {
                            // Handle potentialID not found in nations error
                        }
                    }
                }
                if (indentation > 1 && !foundSeaNodes && cl.StartsWith("Provinces=")) {
                    try {
                        n = nations.Values.First(x => x.ID == potentialID);

                        string[] l2 = cl.Split("=")[1].Replace("{", "").Replace("}", "").Trim().Split();
                        var idList = l2.Select(s => int.TryParse(s, out int id) ? id : (int?)null)
                                       .Where(id => id.HasValue)
                                       .Select(id => id.Value)
                                       .ToList();

                        for (int i = 0; i < idList.Count; i += 2) {
                            for (int j = idList[i]; j <= idList[i] + idList[i + 1]; j++) {
                                if (provDict.ContainsKey(j)) {
                                    n.Provinces[provDict[j].Color] = provDict[j];
                                }
                                else {
                                    // Handle j not found in Provinces error
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

        /// <summary>
        /// Converts a line of text to a Color object.
        /// </summary>
        /// <param name="line">The line of text representing a color.</param>
        /// <returns>The Color object.</returns>
        public static Color LineToColor(string line) {
            var rgbValues = new List<double>();
            var e = line.Trim().Split('=').Last().Trim().Split();

            foreach (var s in e) {
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double d)) {
                    d = Math.Clamp(d * (d <= 1.01 ? 255 : 1), 0, 255);
                    rgbValues.Add(d);
                }
            }

            while (rgbValues.Count < 3) {
                rgbValues.Add(128);
            }

            return line.Contains("hsv360") ?
                ColorFromHSV360(rgbValues[0], rgbValues[1], rgbValues[2]) :
                line.Contains("hvs") ?
                ColorFromHSV(rgbValues[0], rgbValues[1], rgbValues[2]) :
                Color.FromArgb((int)rgbValues[0], (int)rgbValues[1], (int)rgbValues[2]);
        }

        /// <summary>
        /// Creates a Color object from HSV values.
        /// </summary>
        /// <param name="hue">Hue value (0-360).</param>
        /// <param name="saturation">Saturation value.</param>
        /// <param name="value">Value (brightness) value.</param>
        /// <returns>The Color object.</returns>
        public static Color ColorFromHSV(double hue, double saturation, double value) {
            //convert hsv to rgb
            double r, g, b;
            if (value == 0) {
                r = g = b = 0;
            }
            else {
                if (saturation == -1) saturation = 1;
                int i = (int)Math.Floor(hue * 6);
                double f = hue * 6 - i;
                double p = value * (1 - saturation);
                double q = value * (1 - f * saturation);
                double t = value * (1 - (1 - f) * saturation);
                switch (i % 6) {
                    case 0: r = value; g = t; b = p; break;
                    case 1: r = q; g = value; b = p; break;
                    case 2: r = p; g = value; b = t; break;
                    case 3: r = p; g = q; b = value; break;
                    case 4: r = t; g = p; b = value; break;
                    case 5: r = value; g = p; b = q; break;
                    default: r = g = b = value; break;
                }
            }
            r = Math.Max(0, Math.Min(255, r * 255));
            g = Math.Max(0, Math.Min(255, g * 255));
            b = Math.Max(0, Math.Min(255, b * 255));
            return Color.FromArgb((int)r, (int)g, (int)b);
        }

        /// <summary>
        /// Creates a Color object from HSV values in the 360-degree range.
        /// </summary>
        /// <param name="hue">Hue value (0-360).</param>
        /// <param name="saturation">Saturation value.</param>
        /// <param name="value">Value (brightness) value.</param>
        /// <returns>The Color object.</returns>
        public static Color ColorFromHSV360(double hue, double saturation, double value) {
            return ColorFromHSV(hue / 360, saturation / 100, value / 100);
        }
    }
}