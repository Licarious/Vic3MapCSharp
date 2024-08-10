using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text.RegularExpressions;

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


            //TODO change other stuff so they take state/region dictionaries
            Parser.ParseDefaultMap(provinces, localDir);
            Dictionary<string, State> states = Parser.ParseStateFiles(provinces, localDir);
            Dictionary<string, Nation> nations = Parser.ParseNationFiles(provinces, states, localDir);
            Dictionary<string, Region> regions = Parser.ParseRegionFiles(states, localDir);
            Dictionary<string, Culture> cultures = Parser.ParseCultureFiles(states, localDir);

            string[] directories = { "ColorMap", "BorderFrame", "BlankMap", "Homeland", "Debug", "National", "RGOs" };
            foreach (var dir in directories) {
                Directory.CreateDirectory(Path.Combine(localDir, "_Output", dir));
            }

            (int x, int y) waterRecCenter = (0, 0);
            (int w, int h) waterRecSize = (0, 0);

            if (provinces.Values.Any(p => p.IsSea || p.IsLake)) {
                (waterRecCenter, waterRecSize) = MaximumRectangle.Center(provinces.Values.Where(p => p.IsSea || p.IsLake).SelectMany(p => p.Coords).ToList(), false);
            }


            //Province
            Bitmap provinceBorders = Drawer.DrawBorders(localDir + "/_Input/map_data/Provinces.png", Color.Black);
            provinceBorders.Save(localDir + "/_Output/BorderFrame/province_border.png");
            Bitmap waterMap = Drawer.DrawWaterMap(provinces.Values.ToList());
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

            List<Task> tasks = new() {
                Task.Run(() => DrawStates(localDir, states, configs, waterMap))
                    .ContinueWith(_ => DrawRGOs(localDir, states, configs)),
                Task.Run(() => DrawRegions(localDir, regions, configs, waterMap)),
                Task.Run(() => DrawHubs(localDir, provinces)),
                Task.Run(() => DrawImpassablePrime(localDir, provinces)),
                Task.Run(() => DrawHomeland(localDir, cultures)),
                Task.Run(() => DrawNations(localDir, nations, configs))
            };

            // Wait for all tasks to complete
            Task.WaitAll(tasks.ToArray());


            //print memory usage
            Console.WriteLine("Memory Usage: " + GC.GetTotalMemory(true) / 1024 / 1024 + "MB");
            Console.WriteLine("Total Time: " + sw.Elapsed);

            void DrawStates(string localDir, Dictionary<string, State> states, Dictionary<string, object> configs, Bitmap waterMap) {
                //States

                Bitmap stateColors = Drawer.DrawColorMap(states.Values.Cast<IDrawable>().ToList());
                stateColors.Save(localDir + "/_Output/ColorMap/state_colors.png");
                Bitmap stateBorders = Drawer.DrawBorders(stateColors, Color.Black, (bool)configs["DrawCoastalBordersStates"]);
                stateBorders.Save(localDir + "/_Output/BorderFrame/state_border.png");
                Bitmap mergedStateMap = Drawer.MergeImages(new List<Bitmap>() { waterMap, stateColors, stateBorders });
                mergedStateMap.Save(localDir + "/_Output/State_Map.png");
                Bitmap blankStateMap = Drawer.MergeImages(new List<Bitmap>() { whiteBitmap, waterMap, stateBorders });
                blankStateMap.Save(localDir + "/_Output/BlankMap/State_Blank.png");

                foreach (var state in states.Values) {
                    if (state.Color.A == 0 || state.Provinces.Count == 0) continue; //no ocean/sea names
                    Drawer.WriteText(
                        mergedStateMap,
                        SplitAndCapitalize(Regex.Replace(state.Name, "state_", "", RegexOptions.IgnoreCase)),
                        new List<(int x, int y)> { state.SquareCenter, state.RectangleCenter },
                        new List<(int x, int y)> { state.MaxSquareSize, state.MaxRectangleSize },
                        6,
                        Drawer.OppositeExtremeColor(state.Color),
                        new(privateFontCollection.Families[0], 8)
                    );
                }
                mergedStateMap.Save(localDir + "/_Output/State_Map_Names.png");

                //draw debug rectangles on blankStateMap
                foreach (var state in states.Values) {
                    blankStateMap = Drawer.DrawDebugRectangle(blankStateMap, state.RectangleCenter, state.MaxRectangleSize, state.Color);
                }

                blankStateMap.Save(localDir + "/_Output/Debug/State_Rectangles.png");

            }

            void DrawRegions(string localDir, Dictionary<string, Region> regions, Dictionary<string, object> configs, Bitmap waterMap) {
                //Regions

                Bitmap regionColors = Drawer.DrawColorMap(regions.Values.Cast<IDrawable>().ToList());
                regionColors.Save(localDir + "/_Output/ColorMap/region_colors.png");
                Bitmap regionBorders = Drawer.DrawBorders(regionColors, Color.Black, (bool)configs["DrawCoastalBordersRegions"]);
                regionBorders.Save(localDir + "/_Output/BorderFrame/region_border.png");
                Bitmap mergedRegionMap = Drawer.MergeImages(new List<Bitmap>() { waterMap, regionColors, regionBorders });
                mergedRegionMap.Save(localDir + "/_Output/Region_Map.png");
                Bitmap blankRegionMap = Drawer.MergeImages(new List<Bitmap>() { whiteBitmap, waterMap, regionBorders });
                blankRegionMap.Save(localDir + "/_Output/BlankMap/Region_Blank.png");

                foreach (var region in regions.Values) {
                    if (region.Color.A == 0 || region.Coords.Count == 0) continue; //no ocean/sea names

                    Drawer.WriteText(
                        mergedRegionMap,
                        SplitAndCapitalize(Regex.Replace(region.Name, "region_", "", RegexOptions.IgnoreCase)),
                        new List<(int x, int y)> { region.SquareCenter, region.RectangleCenter },
                        new List<(int x, int y)> { region.MaxSquareSize, region.MaxRectangleSize },
                        20,
                        Drawer.OppositeExtremeColor(region.Color),
                        new(privateFontCollection.Families[0], 8)
                    );
                }
                Drawer.DrawDebugRectangle(blankRegionMap, waterRecCenter, waterRecSize, Color.Blue);
                mergedRegionMap.Save(localDir + "/_Output/Region_Map_Names.png");

                //in blankStateMap write text to the center of the water rectangle
                //Drawer.WriteText(blankRegionMap, "Water", waterRecCenter, waterRecSize, 25, Color.Black, Color.Red, new Font(privateFontCollection.Families[0], 8));
                

                Bitmap blankRegionMapSquare = (Bitmap)blankRegionMap.Clone();

                //draw debug rectangles on blankRegionMap
                foreach (var region in regions.Values) {
                    if (region.Color.A == 0 || region.Coords.Count == 0) continue; //no ocean/sea names
                    blankRegionMap = Drawer.DrawDebugRectangle(blankRegionMap, region.RectangleCenter, region.MaxRectangleSize, region.Color);
                    Drawer.WriteText(
                        blankRegionMap,
                        SplitAndCapitalize(Regex.Replace(region.Name, "region_", "", RegexOptions.IgnoreCase)),
                        new List<(int x, int y)> { region.RectangleCenter },
                        new List<(int x, int y)> { region.MaxRectangleSize },
                        20,
                        Drawer.OppositeExtremeColor(region.Color),
                        new(privateFontCollection.Families[0], 8)
                    );

                }
                Drawer.WriteText(
                    blankRegionMap,
                    "Water1\nWater2\nWater3\nWater4",
                    new List<(int x, int y)> { waterRecCenter },
                    new List<(int x, int y)> { waterRecSize },
                    25,
                    Color.Black,
                    Color.White,
                    new(privateFontCollection.Families[0], 8)
                );
                blankRegionMap.Save(localDir + "/_Output/Debug/Region_Rectangles.png");

                //draw debug rectangles on blankRegionMapSquare
                foreach (var region in regions.Values) {
                    if (region.Color.A == 0 || region.Coords.Count == 0) continue; //no ocean/sea names
                    blankRegionMapSquare = Drawer.DrawDebugRectangle(blankRegionMapSquare, region.SquareCenter, region.MaxSquareSize, region.Color);
                    Drawer.WriteText(
                        blankRegionMapSquare,
                        SplitAndCapitalize(Regex.Replace(region.Name, "region_", "", RegexOptions.IgnoreCase)),
                        new List<(int x, int y)> { region.SquareCenter },
                        new List<(int x, int y)> { region.MaxSquareSize },
                        20,
                        Drawer.OppositeExtremeColor(region.Color),
                        new(privateFontCollection.Families[0], 8)
                    );
                }
                Drawer.WriteText(
                    blankRegionMapSquare,
                    "Water",
                    new List<(int x, int y)> { waterRecCenter },
                    new List<(int x, int y)> { waterRecSize },
                    25,
                    Color.Black,
                    Color.White,
                    new(privateFontCollection.Families[0], 8)
                );
                blankRegionMapSquare.Save(localDir + "/_Output/Debug/Region_Rectangles_Square.png");
            }

            void DrawHubs(string localDir, Dictionary<Color, Province> provinces) {
                //Hubs
                Dictionary<string, Color> hubColor = new() {
                    { "city", Color.Purple },
                    { "port", Color.DarkCyan },
                    { "mine", Color.Red },
                    { "farm", Color.Yellow },
                    { "wood", Color.DarkGreen }
                };
                Bitmap hubMap = new(Drawer.MapSize.w, Drawer.MapSize.h);
                Graphics hubGraphics = Graphics.FromImage(hubMap);
                foreach (var province in provinces.Values) {
                    if (hubColor.ContainsKey(province.HubName)) {
                        Drawer.DrawColorMap(hubGraphics, province, hubColor[province.HubName]);
                    }
                }
                hubMap.Save(localDir + "/_Output/ColorMap/hub_map.png");
            }

            void DrawImpassablePrime(string localDir, Dictionary<Color, Province> provinces) {
                //Impassable/Prime Land
                Bitmap impassablePrimeMap = new(Drawer.MapSize.w, Drawer.MapSize.h);
                Graphics impassablePrimeGraphics = Graphics.FromImage(impassablePrimeMap);
                foreach (var province in provinces.Values) {
                    if (province.IsImpassible) {
                        if (province.IsSea || province.IsLake) Drawer.DrawColorMap(impassablePrimeGraphics, province, Color.Blue);
                        else Drawer.DrawColorMap(impassablePrimeGraphics, province, Color.Gray);

                    }
                    else if (province.IsPrimeLand) {
                        Drawer.DrawColorMap(impassablePrimeGraphics, province, Color.Green);
                    }
                }
                impassablePrimeMap.Save(localDir + "/_Output/ColorMap/impassable_prime_map.png");
            }

            void DrawHomeland(string localDir, Dictionary<string, Culture> cultures) {
                //Homeland
                ConcurrentDictionary<Culture, bool> usedHomelands = new();
                Parallel.ForEach(cultures.Values, culture => {
                    culture.GetCenter(true);
                    if (culture.Coords.Count == 0) return;
                    usedHomelands[culture] = false;
                });
                Console.WriteLine($"Homeland Centers\t{sw.Elapsed}");

                //merge as many Cultures as possible into one list such that their coords do not overlap, starting with the largest
                List<List<Culture>> cultureLists = new();
                foreach (var culture in cultures.Values.OrderByDescending(c => c.Coords.Count)) {
                    if (culture.Coords.Count == 0 || usedHomelands[culture]) continue;
                    usedHomelands[culture] = true;
                    List<Culture> cultureList = new() { culture };
                    foreach (var otherCulture in cultures.Values.OrderByDescending(c => c.Coords.Count)) {
                        if (otherCulture.Coords.Count == 0 || usedHomelands[otherCulture]) continue;
                        if (culture == otherCulture) continue;
                        if (cultureList.Any(c => c.Coords.Any(coord => otherCulture.Coords.Contains(coord)))) continue;
                        cultureList.Add(otherCulture);
                        usedHomelands[otherCulture] = true;
                    }
                    cultureLists.Add(cultureList);
                }

                //draw each culture list
                Parallel.ForEach(cultureLists, cultureList => {
                    // Clone the whiteBitmap and waterMap for each iteration
                    Bitmap localWhiteBitmap;
                    Bitmap localWaterMap;
                    lock (whiteBitmap) {
                        localWhiteBitmap = (Bitmap)whiteBitmap.Clone();
                    }
                    lock (waterMap) {
                        localWaterMap = (Bitmap)waterMap.Clone();
                    }

                    Bitmap cultureMap;
                    lock (cultureList) {
                        cultureMap = Drawer.DrawColorMap(new List<IDrawable>(cultureList));
                        cultureMap = Drawer.MergeImages(new List<Bitmap>() { localWhiteBitmap, localWaterMap, cultureMap, Drawer.DrawBorders(cultureMap, Color.Black) });
                    }

                    foreach (var culture in cultureList) {
                        // Compare rectangle and square sizes and choose the one that will work better with longer names
                        Drawer.WriteText(
                            cultureMap,
                            SplitAndCapitalize(culture.Name),
                            new List<(int x, int y)> { culture.SquareCenter, culture.RectangleCenter },
                            new List<(int x, int y)> { culture.MaxSquareSize, culture.MaxRectangleSize },
                            8,
                            Drawer.OppositeExtremeColor(culture.Color),
                            new(privateFontCollection.Families[0], 8)
                        );
                    }

                    cultureMap.Save(localDir + $"/_Output/Homeland/Homelands_{cultureLists.IndexOf(cultureList)}.png");
                });
            }

            void DrawNations(string localDir, Dictionary<string, Nation> nations, Dictionary<string, object> configs, string name = "National") {
                //get centers of nations
                Parallel.ForEach(nations.Values, nation => {
                    nation.GetCenter();
                });
                Console.WriteLine("Nation Centers\t\t" + sw.Elapsed);

                // Sort nations by size, and remove Type == "decentralized" nations if config["DrawDecentralized"] is false
                bool drawDecentralized = (bool)configs["DrawDecentralized"];
                List<Nation> nationList = nations.Values
                    .Where(n => drawDecentralized || n.Type != "decentralized")
                    .OrderByDescending(n => n.Coords.Count)
                    .ToList();

                //draw nations using drawer.DrawColorMap
                Bitmap nationMap = Drawer.DrawColorMap(nationList.Cast<IDrawable>().ToList());
                //draw borders
                Bitmap nationBorders = Drawer.DrawBorders(nationMap, Color.Black, (bool)configs["DrawCoastalBordersNations"]);

                //merge images
                Bitmap mergedNationMap = Drawer.MergeImages(new List<Bitmap>() { whiteBitmap, waterMap, nationMap, nationBorders });
                mergedNationMap.Save(localDir + $"/_Output/National/{name}_Map.png");

                //blank map
                Bitmap blankNationMap = Drawer.MergeImages(new List<Bitmap>() { whiteBitmap, waterMap, nationBorders });
                blankNationMap.Save(localDir + $"/_Output/BlankMap/{name}_Blank.png");

                //write names
                foreach (var nation in nationList) {
                    if (nation.Color.A == 0 || nation.Coords.Count == 0) continue; //no ocean/sea names

                    Drawer.WriteText(
                        mergedNationMap,
                        SplitAndCapitalize(nation.Name),
                        new List<(int x, int y)> { nation.SquareCenter, nation.RectangleCenter },
                        new List<(int w, int h)> { nation.MaxSquareSize, nation.MaxRectangleSize },
                        10,
                        Drawer.OppositeExtremeColor(nation.Color),
                        new Font(privateFontCollection.Families[0], 4)
                    );
                    Drawer.DrawDebugRectangle(blankNationMap, nation.SquareCenter, nation.MaxSquareSize, nation.Color);
                }

                mergedNationMap.Save(localDir + $"/_Output/National/{name}_Map_Tags.png");
                blankNationMap.Save(localDir + $"/_Output/Debug/{name}_Rectangles.png");


            }

            void DrawRGOs(string localDir, Dictionary<string, State> states, Dictionary<string, object> configs) {
                Dictionary<string, (Color hColor, Color tColor, string type)> rgos = new();
                foreach (var state in states.Values) {
                    foreach (var resource in state.Resources.Values) {
                        if (((IEnumerable<string>)configs["IgnoreRGONames"]).Contains(resource.Name)) continue;
                        if (rgos.ContainsKey(resource.Name)) continue;

                        var matchingConfig = ((IEnumerable<(List<string> rgoNames, Color hColor, Color tColor)>)configs["RgoColors"])
                            .FirstOrDefault(config => config.rgoNames.Any(rgoName => resource.Name.Contains(rgoName)));

                        if (matchingConfig == default) {
                            rgos.Add(resource.Name, (Color.HotPink, Color.DarkBlue, resource.Type));
                        }
                        else {
                            rgos.Add(resource.Name, (matchingConfig.hColor, matchingConfig.tColor, resource.Type));
                        }

                    }
                }

                foreach (var rgo in rgos) {
                    Bitmap localWhiteBitmap;
                    Bitmap localWaterMap;
                    lock (whiteBitmap) {
                        localWhiteBitmap = (Bitmap)whiteBitmap.Clone();
                    }
                    lock (waterMap) {
                        localWaterMap = (Bitmap)waterMap.Clone();
                    }

                    //list all states with the rgo
                    List<IDrawable> statesWithRGO = states.Values
                        .Where(state => state.Resources.ContainsKey(rgo.Key))
                        .Cast<IDrawable>()
                        .ToList();

                    Bitmap rgoMap = Drawer.DrawMap(statesWithRGO, rgo.Value.hColor);

                    foreach (State state in states.Values) {
                        if (state.Resources.ContainsKey(rgo.Key)) {
                            Drawer.WriteText(
                                rgoMap,
                                SplitAndCapitalize(state.Resources[rgo.Key].AmountString()),
                                new List<(int x, int y)> { state.SquareCenter, state.RectangleCenter },
                                new List<(int x, int y)> { state.MaxSquareSize, state.MaxRectangleSize },
                                8,
                                rgo.Value.tColor,
                                new(privateFontCollection.Families[0], 8)
                            );
                        }
                    }
                    // Create the Bitmap object once the file exists
                    Bitmap stateBorders = new(localDir + "/_Output/BorderFrame/state_border.png");

                    //merge localWhiteBitmap, localWaterMap, rgoMap, and stateBorders
                    Bitmap mergedRgoMap = Drawer.MergeImages(new List<Bitmap>() { localWhiteBitmap, localWaterMap, rgoMap, stateBorders });

                    //write RGO name
                    string rgoName = SplitAndCapitalize(rgo.Key.Replace("bg_", "")) + $" ({rgo.Value.type})";

                    Drawer.WriteText(
                        mergedRgoMap,
                        rgoName,
                        new List<(int x, int y)> { waterRecCenter },
                        new List<(int x, int y)> { waterRecSize },
                        100,
                        rgo.Value.hColor,
                        rgo.Value.tColor,
                        new(privateFontCollection.Families[0], 100),
                        false
                    );


                    mergedRgoMap.Save(localDir + $"/_Output/RGOs/{rgo.Key.Replace("bg_", "")}.png");
                }
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