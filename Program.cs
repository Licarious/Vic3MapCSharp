using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using Vic3MapCSharp.DataObjects;
using Region = Vic3MapCSharp.DataObjects.Region;

namespace Vic3MapCSharp
{
    internal class Program
    {
        private static void Main() {
            if (Environment.Version.Major < 8) {
                Console.WriteLine("This program requires .NET Framework 8.0 or higher. Please install it and try again.");
                Console.ReadKey();
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();

            string localDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\"));
            Console.WriteLine(localDir);

            PrivateFontCollection privateFontCollection = new();
            privateFontCollection.AddFontFile(localDir + "/_Input/ParadoxVictorian-Condensed.otf"); //font for numbers and names

            Dictionary<string, object> configs = Parser.ParseConfig(localDir);

            Dictionary<Color, Province> provinces = Parser.ParseTerrain(localDir);
            Parser.ParseProvMap(provinces, localDir);

            Parser.ParseDefaultMap(provinces, localDir);
            Dictionary<string, State> states = Parser.ParseStateFiles(provinces, localDir);
            Dictionary<string, Nation> nations = Parser.ParseNationFiles(provinces, states, localDir);
            Dictionary<string, Region> regions = Parser.ParseRegionFiles(states, localDir);
            Dictionary<string, Culture> cultures = Parser.ParseCultureFiles(states, localDir);

            if (configs.TryGetValue("UseRGOsCSV", out object? value) && value is true) {
                Parser.ParseRGOsCSV(states, localDir);
            }

            string[] directories = ["ColorMap", "BorderFrame", "BlankMap", "Debug", "Homeland", "National", "RGOs"];
            foreach (var dir in directories) {
                Directory.CreateDirectory(Path.Combine(localDir, "_Output", dir));
            }

            Console.WriteLine("Calculating Water Center");
            List<(int x, int y, int h, int w)> waterRectangles = provinces.Values.Any(p => p.IsSea || p.IsLake)
                ? MaximumRectangle.Center(provinces.Values.Where(p => p.IsSea || p.IsLake).SelectMany(p => p.Coords).ToList(), false)
                : [];

            //Province
            Bitmap provinceBorders = Drawer.DrawBorders(localDir + "/_Input/map_data/Provinces.png", Color.Black);
            Drawer.MapSize = (provinceBorders.Width, provinceBorders.Height);
            provinceBorders.Save(localDir + "/_Output/BorderFrame/province_border.png");
            Bitmap waterMap = Drawer.DrawMap(provinces.Values.Where(p => p.IsSea || p.IsLake).Cast<IDrawable>().ToList(), Color.LightBlue);
            waterMap.Save(localDir + "/_Output/ColorMap/water_map.png");

            Bitmap whiteBitmap = new(waterMap.Width, waterMap.Height);
            using (Graphics g = Graphics.FromImage(whiteBitmap)) {
                g.Clear(Color.White);
            }
            Drawer.MergeImages(new List<Bitmap>() { whiteBitmap, waterMap, provinceBorders }).Save(localDir + "/_Output/BlankMap/Province_Blank.png");

            Console.WriteLine("Parsed Provinces\t" + sw.Elapsed);
            Parallel.ForEach(states.Values, state => {
                state.GetCenter(true);
            });
            Console.WriteLine("State Centers\t" + sw.Elapsed);
            Parallel.ForEach(regions.Values, region => {
                region.GetCenter(true);
            });
            Console.WriteLine("Region Centers\t" + sw.Elapsed);

            List<Task> tasks = [
                Task.Run(() => DrawStates(localDir, states, configs, waterMap))
                    .ContinueWith(_ => DrawRGOs(localDir, states, configs)),
                Task.Run(() => DrawRegions(localDir, regions, configs, waterMap)),
                Task.Run(() => DrawHubs(localDir, provinces)),
                Task.Run(() => DrawImpassablePrime(localDir, provinces)),
                Task.Run(() => DrawHomeland(localDir, cultures))
            ];

            if (configs.TryGetValue("DrawStartingNations", out object? drawStarting) && drawStarting is true) {
                tasks.Add(Task.Run(() => DrawNations(localDir, nations, configs)));
            }
            /*
            if (configs.TryGetValue("DrawSaves", out object? drawSaves) && (bool)drawSaves) {
                string[] saveFiles = Directory.GetFiles(Path.Combine(localDir, "_Input", "save"), "*.vic3");
                foreach (string saveFile in saveFiles) {
                    Dictionary<string, Nation> save = nations.ToDictionary(x => x.Key, x => new Nation(x.Value));
                    Parser.ParseSave(regions, save, localDir);
                    tasks.Add(Task.Run(() => DrawNations(localDir, save, configs, saveFile)));
                }
            }
            */

            // Wait for all tasks to complete
            Task.WaitAll([.. tasks]);

            //print memory usage
            Console.WriteLine("Memory Usage: " + GC.GetTotalMemory(true) / 1024 / 1024 + "MB");
            Console.WriteLine("Total Time: " + sw.Elapsed);

            void DrawStates(string localDir, Dictionary<string, State> states, Dictionary<string, object> configs, Bitmap waterMap) {
                Bitmap stateColors = Drawer.DrawColorMap(states.Values.Cast<IDrawable>().ToList());
                stateColors.Save(localDir + "/_Output/ColorMap/state_colors.png");
                Bitmap stateBorders = Drawer.DrawBorders(stateColors, Color.Black, (bool)configs["DrawCoastalBordersStates"]);
                stateBorders.Save(localDir + "/_Output/BorderFrame/state_border.png");
                Bitmap mergedStateMap = Drawer.MergeImages(new List<Bitmap> { waterMap, stateColors, stateBorders });
                mergedStateMap.Save(localDir + "/_Output/State_Map.png");
                Bitmap blankStateMap = Drawer.MergeImages(new List<Bitmap> { whiteBitmap, waterMap, stateBorders });
                blankStateMap.Save(localDir + "/_Output/BlankMap/State_Blank.png");

                foreach (var state in states.Values) {
                    if (state.Color.A == 0 || state.Coords.Count == 0) continue; //no ocean/sea names
                    Drawer.WriteText(
                        mergedStateMap,
                        SplitAndCapitalize(Regex.Replace(state.Name, "state_", "", RegexOptions.IgnoreCase)),
                        state.MaximumRectangles,
                        6,
                        Drawer.OppositeExtremeColor(state.Color),
                        new(privateFontCollection.Families[0], 8)
                    );
                }
                mergedStateMap.Save(localDir + "/_Output/State_Map_Names.png");

                if (configs.TryGetValue("DrawDebug", out object? value) && (bool)value) {
                    var debugActions = new List<(Bitmap map, (int x, int y, int h, int w) rect, string fileName)>();

                    if (waterRectangles.Count > 0) {
                        debugActions.Add(((Bitmap)blankStateMap.Clone(), waterRectangles[0], "/_Output/Debug/State_Squares.png"));
                    }
                    if (waterRectangles.Count > 1) {
                        debugActions.Add(((Bitmap)blankStateMap.Clone(), waterRectangles[1], "/_Output/Debug/State_Rectangles.png"));
                    }

                    foreach (var (map, rect, fileName) in debugActions) {
                        Console.WriteLine(fileName);
                        Drawer.DrawDebugRectangle(map, rect, Color.Blue);
                        int index = debugActions.IndexOf((map, rect, fileName));
                        DrawDebugRectanglesAndText(map, states.Values, r => r.MaximumRectangles.Count > index ? r.MaximumRectangles[index] : r.MaximumRectangles[0], "STATE_", 6, fileName);
                    }
                }
            }

            void DrawRegions(string localDir, Dictionary<string, Region> regions, Dictionary<string, object> configs, Bitmap waterMap) {
                Bitmap regionColors = Drawer.DrawColorMap(regions.Values.Cast<IDrawable>().ToList());
                regionColors.Save(localDir + "/_Output/ColorMap/region_colors.png");
                Bitmap regionBorders = Drawer.DrawBorders(regionColors, Color.Black, (bool)configs["DrawCoastalBordersRegions"]);
                regionBorders.Save(localDir + "/_Output/BorderFrame/region_border.png");
                Bitmap mergedRegionMap = Drawer.MergeImages(new List<Bitmap> { waterMap, regionColors, regionBorders });
                mergedRegionMap.Save(localDir + "/_Output/Region_Map.png");
                Bitmap blankRegionMap = Drawer.MergeImages(new List<Bitmap> { whiteBitmap, waterMap, regionBorders });
                blankRegionMap.Save(localDir + "/_Output/BlankMap/Region_Blank.png");

                foreach (var region in regions.Values) {
                    if (region.Color.A == 0 || region.Coords.Count == 0) continue; //no ocean/sea names
                    Drawer.WriteText(
                        mergedRegionMap,
                        SplitAndCapitalize(Regex.Replace(region.Name, "region_", "", RegexOptions.IgnoreCase)),
                        region.MaximumRectangles,
                        20,
                        Drawer.OppositeExtremeColor(region.Color),
                        new(privateFontCollection.Families[0], 8)
                    );
                }

                mergedRegionMap.Save(localDir + "/_Output/Region_Map_Names.png");

                if (configs.TryGetValue("DrawDebug", out object? value) && (bool)value) {
                    var debugActions = new List<(Bitmap map, (int x, int y, int h, int w) rect, string fileName)>();

                    if (waterRectangles.Count > 0) {
                        debugActions.Add(((Bitmap)blankRegionMap.Clone(), waterRectangles[0], "/_Output/Debug/Region_Squares.png"));
                    }
                    if (waterRectangles.Count > 1) {
                        debugActions.Add(((Bitmap)blankRegionMap.Clone(), waterRectangles[1], "/_Output/Debug/Region_Rectangles.png"));
                    }

                    foreach (var (map, rect, fileName) in debugActions) {
                        Drawer.DrawDebugRectangle(map, rect, Color.Blue);
                        int index = debugActions.IndexOf((map, rect, fileName));
                        DrawDebugRectanglesAndText(map, regions.Values, r => r.MaximumRectangles.Count > index ? r.MaximumRectangles[index] : r.MaximumRectangles[0], "region_", 20, fileName);
                    }
                }
            }

            void DrawHubs(string localDir, Dictionary<Color, Province> provinces) {
                var hubColor = new Dictionary<string, Color> {
                    { "city", Color.Purple },
                    { "port", Color.DarkCyan },
                    { "mine", Color.Red },
                    { "farm", Color.Yellow },
                    { "wood", Color.DarkGreen }
                };

                using Bitmap hubMap = new(Drawer.MapSize.w, Drawer.MapSize.h);
                using Graphics hubGraphics = Graphics.FromImage(hubMap);

                foreach (var province in provinces.Values) {
                    if (hubColor.TryGetValue(province.HubName, out var value)) {
                        Drawer.DrawColorMap(hubGraphics, province, value);
                    }
                }

                hubMap.Save(Path.Combine(localDir, "_Output", "ColorMap", "hub_map.png"));
            }

            void DrawImpassablePrime(string localDir, Dictionary<Color, Province> provinces) {
                using Bitmap impassablePrimeMap = new(Drawer.MapSize.w, Drawer.MapSize.h);
                using Graphics impassablePrimeGraphics = Graphics.FromImage(impassablePrimeMap);

                foreach (var province in provinces.Values) {
                    Color color = province.IsImpassible
                        ? (province.IsSea || province.IsLake ? Color.Blue : Color.Gray)
                        : (province.IsPrimeLand ? Color.Green : Color.Transparent);

                    if (color != Color.Transparent) {
                        Drawer.DrawColorMap(impassablePrimeGraphics, province, color);
                    }
                }

                impassablePrimeMap.Save(Path.Combine(localDir, "_Output", "ColorMap", "impassable_prime_map.png"));
            }

            void DrawHomeland(string localDir, Dictionary<string, Culture> cultures) {
                var usedHomelands = new ConcurrentDictionary<Culture, bool>();
                Parallel.ForEach(cultures.Values, culture => {
                    culture.GetCenter(true);
                    if (culture.Coords.Count > 0) {
                        usedHomelands[culture] = false;
                    }
                });
                Console.WriteLine($"Homeland Centers\t{sw.Elapsed}");

                // Merge cultures into lists without overlapping coordinates
                var cultureLists = new List<List<Culture>>();
                foreach (var culture in cultures.Values.OrderByDescending(c => c.Coords.Count)) {
                    if (culture.Coords.Count == 0 || usedHomelands[culture]) continue;
                    usedHomelands[culture] = true;
                    var cultureList = new List<Culture> { culture };
                    foreach (var otherCulture in cultures.Values.OrderByDescending(c => c.Coords.Count)) {
                        if (otherCulture.Coords.Count == 0 || usedHomelands[otherCulture] || culture == otherCulture) continue;
                        if (cultureList.Any(c => c.Coords.Any(coord => otherCulture.Coords.Contains(coord)))) continue;
                        cultureList.Add(otherCulture);
                        usedHomelands[otherCulture] = true;
                    }
                    cultureLists.Add(cultureList);
                }

                // Draw each culture list
                Parallel.ForEach(cultureLists, cultureList => {
                    Bitmap localWhiteBitmap, localWaterMap;
                    lock (whiteBitmap) {
                        localWhiteBitmap = (Bitmap)whiteBitmap.Clone();
                    }
                    lock (waterMap) {
                        localWaterMap = (Bitmap)waterMap.Clone();
                    }

                    Bitmap cultureMap;
                    lock (cultureList) {
                        cultureMap = Drawer.DrawColorMap(new List<IDrawable>(cultureList));
                        cultureMap = Drawer.MergeImages(new List<Bitmap> { localWhiteBitmap, localWaterMap, cultureMap, Drawer.DrawBorders(cultureMap, Color.Black) });
                    }

                    foreach (var culture in cultureList) {
                        Drawer.WriteText(
                            cultureMap,
                            SplitAndCapitalize(culture.Name),
                            culture.MaximumRectangles,
                            8,
                            Drawer.OppositeExtremeColor(culture.Color),
                            new(privateFontCollection.Families[0], 8)
                        );
                    }

                    cultureMap.Save(Path.Combine(localDir, "_Output", "Homeland", $"Homelands_{cultureLists.IndexOf(cultureList)}.png"));
                });
            }

            void DrawNations(string localDir, Dictionary<string, Nation> nations, Dictionary<string, object> configs, string name = "National") {
                // Get centers of nations
                Parallel.ForEach(nations.Values, nation => nation.GetCenter());
                Console.WriteLine("Nation Centers\t\t" + sw.Elapsed);

                // Sort nations by size, and remove Type == "decentralized" nations if config["DrawDecentralized"] is false
                bool drawDecentralized = (bool)configs["DrawDecentralized"];
                var nationList = nations.Values
                    .Where(n => drawDecentralized || n.Type != "decentralized")
                    .OrderByDescending(n => n.Coords.Count)
                    .Cast<IDrawable>()
                    .ToList();

                // Draw nations and borders
                Bitmap nationMap = Drawer.DrawColorMap(nationList);
                Bitmap nationBorders = Drawer.DrawBorders(nationMap, Color.Black, (bool)configs["DrawCoastalBordersNations"]);

                // Merge images
                Bitmap mergedNationMap = Drawer.MergeImages(new List<Bitmap> { whiteBitmap, waterMap, nationMap, nationBorders });
                mergedNationMap.Save(localDir + $"/_Output/National/{name}_Map.png");

                // Blank map
                Bitmap blankNationMap = Drawer.MergeImages(new List<Bitmap> { whiteBitmap, waterMap, nationBorders });
                blankNationMap.Save(localDir + $"/_Output/BlankMap/{name}_Blank.png");

                // Write names
                foreach (var nation in nationList) {
                    if (nation.Color.A == 0 || nation.Coords.Count == 0) continue; // No ocean/sea names

                    Drawer.WriteText(
                        mergedNationMap,
                        SplitAndCapitalize(nation.Name),
                        nation.MaximumRectangles,
                        8,
                        Drawer.OppositeExtremeColor(nation.Color),
                        new Font(privateFontCollection.Families[0], 4)
                    );
                }

                mergedNationMap.Save(localDir + $"/_Output/National/{name}_Map_Tags.png");

                if (configs.TryGetValue("DrawDebug", out object? value) && (bool)value) {
                    var debugActions = new List<(Bitmap map, (int x, int y, int h, int w) rect, string fileName)>();

                    if (waterRectangles.Count > 0) {
                        debugActions.Add(((Bitmap)blankNationMap.Clone(), waterRectangles[0], $"/_Output/Debug/{name}_Squares.png"));
                    }
                    if (waterRectangles.Count > 1) {
                        debugActions.Add(((Bitmap)blankNationMap.Clone(), waterRectangles[1], $"/_Output/Debug/{name}_Rectangles.png"));
                    }

                    foreach (var (map, rect, fileName) in debugActions) {
                        Console.WriteLine(fileName);
                        Drawer.DrawDebugRectangle(map, rect, Color.Blue);
                        int index = debugActions.IndexOf((map, rect, fileName));
                        DrawDebugRectanglesAndText(map, nations.Values, r => r.MaximumRectangles.Count > index ? r.MaximumRectangles[index] : r.MaximumRectangles[0], "", 8, fileName);
                    }
                }
            }

            void DrawRGOs(string localDir, Dictionary<string, State> states, Dictionary<string, object> configs) {
                if(configs.TryGetValue("DrawRGOs", out object? draw) && draw is false) return;

                var rgos = new Dictionary<string, (Color hColor, Color tColor, string type)>();

                foreach (var state in states.Values) {
                    foreach (var resource in state.Resources.Values) {
                        if (((IEnumerable<string>)configs["IgnoreRGONames"]).Contains(resource.Name) || rgos.ContainsKey(resource.Name)) continue;

                        var matchingConfig = ((IEnumerable<(List<string> rgoNames, Color hColor, Color tColor)>)configs["RgoColors"])
                            .FirstOrDefault(config => config.rgoNames.Any(rgoName => resource.Name.Contains(rgoName)));

                        rgos[resource.Name] = matchingConfig == default
                            ? (Color.HotPink, Color.DarkBlue, resource.Type)
                            : (matchingConfig.hColor, matchingConfig.tColor, resource.Type);
                    }
                }

                foreach (var rgo in rgos) {
                    Bitmap localWhiteBitmap, localWaterMap;
                    lock (whiteBitmap) localWhiteBitmap = (Bitmap)whiteBitmap.Clone();
                    lock (waterMap) localWaterMap = (Bitmap)waterMap.Clone();

                    var statesWithRGO = states.Values
                        .Where(state => state.Resources.ContainsKey(rgo.Key))
                        .Cast<IDrawable>()
                        .ToList();

                    Bitmap rgoMap = Drawer.DrawMap(statesWithRGO, rgo.Value.hColor);

                    foreach (State state in states.Values) {
                        if (state.Color.A == 0 || state.Coords.Count == 0) continue; // no ocean/sea names
                        if (state.Resources.TryGetValue(rgo.Key, out Resource? value)) {
                            Drawer.WriteText(
                                rgoMap,
                                SplitAndCapitalize(value.AmountString()),
                                state.MaximumRectangles,
                                8,
                                rgo.Value.tColor,
                                new(privateFontCollection.Families[0], 8)
                            );
                        }
                    }

                    Bitmap stateBorders = new(localDir + "/_Output/BorderFrame/state_border.png");

                    Bitmap mergedRgoMap = Drawer.MergeImages(new List<Bitmap>() { localWhiteBitmap, localWaterMap, rgoMap, stateBorders });

                    string rgoName = SplitAndCapitalize(rgo.Key.Replace("bg_", "")) + $" ({rgo.Value.type})";

                    Drawer.WriteText(
                        mergedRgoMap,
                        rgoName,
                        waterRectangles,
                        100,
                        rgo.Value.hColor,
                        rgo.Value.tColor,
                        new(privateFontCollection.Families[0], 100),
                        false
                    );

                    mergedRgoMap.Save(localDir + $"/_Output/RGOs/{rgo.Key.Replace("bg_", "")}.png");
                }
            }

            // Draw debug rectangles and write text on blankRegionMap and blankMapSquare
            void DrawDebugRectanglesAndText(Bitmap map, IEnumerable<IDrawable> drawable, Func<IDrawable, (int x, int y, int h, int w)> getRectangle, string substringToRemove, int startingTextSize, string fileName) {
                foreach (var draw in drawable) {
                    if (draw.Color.A == 0 || draw.Coords.Count == 0) continue; // no ocean/sea names
                    map = Drawer.DrawDebugRectangle(map, getRectangle(draw), draw.Color);
                    Drawer.WriteText(
                        map,
                        SplitAndCapitalize(Regex.Replace(draw.Name, substringToRemove, "", RegexOptions.IgnoreCase)),
                        [getRectangle(draw)],
                        startingTextSize,
                        Drawer.OppositeExtremeColor(draw.Color),
                        new(privateFontCollection.Families[0], 8)
                    );
                }
                Drawer.WriteText(
                    map,
                    "Water1 Water2 Water3 Water4 Water5",
                    waterRectangles,
                    startingTextSize,
                    Color.Black,
                    Color.White,
                    new(privateFontCollection.Families[0], 8)
                );
                map.Save(localDir + fileName);
            }

            string SplitAndCapitalize(string s) {
                string[] words = s.ToLower().Split('_'); // Convert to lowercase first
                string result = "";
                foreach (string word in words) {
                    result += char.ToUpper(word[0]) + word[1..] + " "; // Capitalize the first letter
                }
                return result.Trim();
            }
        }
    }
}