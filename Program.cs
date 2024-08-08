using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Vic3MapCSharp
{
    internal class Program
    {
        private static void Main() {
            //check if .net framework 6.0 is installed
            if (Environment.Version.Major < 6) {
                Console.WriteLine("This program requires .NET Framework 6.0 or higher. Please install it and try again.");
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
            Dictionary<string, State> states = Parser.ParseStateFiles(provinces, localDir);
            Dictionary<string, Nation> nations = Parser.ParseNationFiles(provinces, states, localDir);
            Dictionary<string, Region> regions = Parser.ParseRegionFiles(states, localDir);
            Parser.ParseDefaultMap(provinces, localDir);
            Dictionary<string, Culture> cultures = Parser.ParseCultureFiles(states, localDir);

            Directory.CreateDirectory(localDir + "/_Output/ColorMap");
            Directory.CreateDirectory(localDir + "/_Output/BorderFrame");
            Directory.CreateDirectory(localDir + "/_Output/BlankMap");
            Directory.CreateDirectory(localDir + "/_Output/Homeland");
            Directory.CreateDirectory(localDir + "/_Output/Debug");

            (int x, int y) waterRecCenter = (0, 0);
            (int w, int h) waterRecSize = (0, 0);

            if (provinces.Values.Any(p => p.isSea || p.isLake)) {
                (waterRecCenter, waterRecSize) = MaximumRectangle.Center(provinces.Values.Where(p => p.isSea || p.isLake).SelectMany(p => p.Coords).ToList(), false);
            }


            //Province
            Bitmap provinceBorders = Drawer.DrawBorders(localDir + "/_Input/map_data/provinces.png", Color.Black);
            provinceBorders.Save(localDir + "/_Output/BorderFrame/province_border.png");
            Bitmap waterMap = Drawer.DrawWaterMap(provinces.Values.ToList());
            waterMap.Save(localDir + "/_Output/ColorMap/water_map.png");

            Bitmap whiteBitmap = new(waterMap.Width, waterMap.Height);
            using (Graphics g = Graphics.FromImage(whiteBitmap)) {
                g.Clear(Color.White);
            }
            Drawer.MergeImages(new List<Bitmap>() { whiteBitmap, waterMap, provinceBorders }).Save(localDir + "/_Output/BlankMap/Province_Blank.png");


            //States
            foreach (var state in states.Values) {
                state.GetCenter();
            }
            Bitmap stateColors = Drawer.DrawColorMap(states.Values.Cast<IDrawable>().ToList());
            stateColors.Save(localDir + "/_Output/ColorMap/state_colors.png");
            Bitmap stateBorders = Drawer.DrawBorders(stateColors, Color.Black, (bool)configs["DrawCoastalBordersStates"]);
            stateBorders.Save(localDir + "/_Output/BorderFrame/state_border.png");
            Drawer.MergeImages(new List<Bitmap>() { waterMap, stateColors, stateBorders }).Save(localDir + "/_Output/State_Map.png");


            //Regions
            foreach (var region in regions.Values) {
                region.GetCenter(true);
            }
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

                //compare rectangle and square sizes and choose the one that will work better with longer names
                if (region.MaxRectangleSize.h >= region.MaxRectangleSize.w * 2 && region.MaxRectangleSize.w > region.MaxSquareSize.w) {
                    Drawer.WriteText(mergedRegionMap, SplitAndCapitalize(region.Name.Replace("region_", "")), region.SquareCenter, region.MaxSquareSize, 25, OppositeExtremeColor(region.Color), new(privateFontCollection.Families[0], 8));
                    Drawer.DrawDebugRectangle(blankRegionMap, region.RectangleCenter, region.MaxRectangleSize, region.Color);
                }
                else {
                    Drawer.WriteText(mergedRegionMap, SplitAndCapitalize(region.Name.Replace("region_", "")), region.RectangleCenter, region.MaxRectangleSize, 25, OppositeExtremeColor(region.Color), new(privateFontCollection.Families[0], 8));
                    Drawer.DrawDebugRectangle(blankRegionMap, region.RectangleCenter, region.MaxRectangleSize, region.Color);
                }
            }
            Drawer.DrawDebugRectangle(blankRegionMap, waterRecCenter, waterRecSize, Color.Blue);
            mergedRegionMap.Save(localDir + "/_Output/Region_Map_Names.png");

            //in blankRegionMap write text to the center of the water rectangle
            blankRegionMap = Drawer.WriteText(blankRegionMap, "Water", waterRecCenter, waterRecSize, 25, Color.Black, Color.Red, new(privateFontCollection.Families[0], 8));

            blankRegionMap.Save(localDir + "/_Output/Debug/Region_Rectangles.png");
            
            



            //Hubs
            Dictionary<string, Color> hubColor = new() {
                { "city", Color.Purple },
                { "port", Color.DarkCyan },
                { "mine", Color.Red },
                { "farm", Color.Yellow },
                { "wood", Color.DarkGreen }
            };
            Bitmap hubMap = new(Drawer.MapSize.w, Drawer.MapSize.h);
            Graphics hubGrahics = Graphics.FromImage(hubMap);
            foreach (var province in provinces.Values) {
                if (hubColor.ContainsKey(province.hubName)) {
                    Drawer.DrawColorMap(hubGrahics, province, hubColor[province.hubName]);
                }
            }
            hubMap.Save(localDir + "/_Output/ColorMap/hub_map.png");


            //Impassable/Prime Land
            Bitmap impassablePrimeMap = new(Drawer.MapSize.w, Drawer.MapSize.h);
            Graphics impassablePrimeGraphics = Graphics.FromImage(impassablePrimeMap);
            foreach (var province in provinces.Values) {
                if (province.isImpassible) {
                    if (province.isSea || province.isLake) Drawer.DrawColorMap(impassablePrimeGraphics, province, Color.Blue);
                    else Drawer.DrawColorMap(impassablePrimeGraphics, province, Color.Gray);

                }
                else if (province.isPrimeLand) {
                    Drawer.DrawColorMap(impassablePrimeGraphics, province, Color.Green);
                }
            }
            impassablePrimeMap.Save(localDir + "/_Output/ColorMap/impassable_prime_map.png");

            //Homeland
            Dictionary<Culture, bool> usedHomelands = new();
            foreach (var culture in cultures.Values) {
                culture.GetCenter(true);
                if (culture.Coords.Count == 0) continue;
                usedHomelands[culture] = false;
            }

            //merge as many cultures as possible into one list such that their coords do not overlap, starting with the largest
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
            foreach (var cultureList in cultureLists) {
                Bitmap cultureMap = Drawer.DrawColorMap(new List<IDrawable>(cultureList));
                cultureMap = Drawer.MergeImages(new List<Bitmap>() { whiteBitmap, waterMap, cultureMap, Drawer.DrawBorders(cultureMap, Color.Black) });

                foreach (var culture in cultureList) {
                    //compare rectangle and square sizes and choose the one that will work better with longer names
                    if (culture.MaxRectangleSize.h >= culture.MaxRectangleSize.w * 2 && culture.MaxRectangleSize.w > culture.MaxSquareSize.w) {
                        Drawer.WriteText(cultureMap, SplitAndCapitalize(culture.Name), culture.SquareCenter, culture.MaxSquareSize, 8, OppositeExtremeColor(culture.Color), new(privateFontCollection.Families[0], 8));
                    }
                    else {
                        Drawer.WriteText(cultureMap, SplitAndCapitalize(culture.Name), culture.RectangleCenter, culture.MaxRectangleSize, 8, OppositeExtremeColor(culture.Color), new(privateFontCollection.Families[0], 8));
                    }
                }
                cultureMap.Save(localDir + $"/_Output/Homeland/{cultureList.First().Name}.png");
            }






            //print memory usage
            Console.WriteLine("Memory Usage: " + GC.GetTotalMemory(true) / 1024 / 1024 + "MB");
            //TODO this is the end
            return;

            if ((bool)configs["UseRGOsCSV"]) Parser.ParseRGOsCSV(regions, localDir);

            writeRGOs(regions);
            if ((bool)configs["DrawDebug"]) debugStateProv(regions);

            parseProvMap(regions, provinces);

            //pares province png
            void parseProvMap(Dictionary<string, Region> regions, Dictionary<Color, Province> ProvinceDict) {
                Bitmap image = new(localDir + "/_Input/map_data/provinces.png");
                Bitmap provBorder = new(image.Width, image.Height);

                Console.WriteLine("Parsing Map");
                //parse image and get coords of each color and add them to the state and draw borders
                for (int i = 0; i < image.Width; i++) {
                    for (int j = 0; j < image.Height; j++) {
                        Color c = image.GetPixel(i, j);
                        if (ProvinceDict.ContainsKey(c)) ProvinceDict[c].Coords.Add((i, j));

                        //check left and above pixel for border
                        if ((i > 0 && image.GetPixel(i - 1, j) != c) || (j > 0 && image.GetPixel(i, j - 1) != c)) {
                            provBorder.SetPixel(i, j, Color.Black);
                        }
                    }
                    //print progress every 25%
                    if (i % (image.Width / 4) == 0) {
                        Console.WriteLine("\t" + (i / (image.Width / 100)) + "%");
                    }

                }
                //update coords in each state
                foreach (Region r in regions.Values) {
                    foreach (State s in r.states) {
                        foreach (KeyValuePair<Color, Province> kvp in s.provDict) {
                            if (ProvinceDict.ContainsKey(kvp.Key)) {
                                kvp.Value.Coords = ProvinceDict[kvp.Key].Coords;
                            }
                        }
                        s.SetCoords();
                    }
                }

                //check if /_Output/BorderFrame exists if not add it
                if (!Directory.Exists(localDir + "/_Output/BorderFrame")) {
                    Directory.CreateDirectory(localDir + "/_Output/BorderFrame");
                }

                //save map
                provBorder.Save(localDir + "/_Output/BorderFrame/prov_border.png");

                drawStateImages(regions, image);
                List<(int, int)> waterCoordList = provinces.Values.Where(p => p.isSea || p.isLake).SelectMany(p => p.Coords).ToList();
                drawRegionImages(regions, image);


                if ((bool)configs["DrawRGOs"]) {
                    ((int, int) waterRecCenter, (int, int) waterRecSize) = drawRGOMaps(regions, waterCoordList);

                    mergeMaps();
                    namedMapes(regions);

                    debugDrawRectangle(regions, waterRecCenter, waterRecSize);

                    drawHubs(regions, image);
                    drawImpassablePrime(regions, image);
                    drawTerrain(provinces, image);
                }

                if ((bool)configs["DrawStartingNations"] || (bool)configs["DrawSaves"]) {
                    Dictionary<string, Nation> nationDict = Parser.ParseNationFiles(provinces, states, localDir);
                    if (nationDict == null) {
                        return;
                    }

                    if ((bool)configs["DrawStartingNations"]) {
                        drawNationsMap(nationDict, "Starting_National", (bool)configs["DrawDecentralized"]);
                    }
                    if ((bool)configs["DrawSaves"]) {

                        //for every file in _Input/Saves
                        foreach (string file in Directory.GetFiles(localDir + "/_Input/Saves")) {
                            //if file is a .txt file
                            if (file.EndsWith(".v3")) {
                                Parser.ParseSave(regions, nationDict, file);

                                //separate name from file path
                                string[] split = file.Split('\\');
                                string fileName = split[^1].Split(".")[0];

                                drawNationsMap(nationDict, fileName, (bool)configs["DrawDecentralized"]);
                            }
                        }
                    }
                }

                Console.WriteLine(sw.Elapsed);
            }

            //draw state images
            void drawStateImages(Dictionary<string, Region> regions, Bitmap image) {
                Bitmap stateImage = new(image.Width, image.Height);
                Console.WriteLine("Drawing State Maps");
                foreach (Region r in regions.Values) {
                    //Console.WriteLine(r.name);
                    foreach (State s in r.states) {
                        //catch if a state has no hubs but is not water to apply a color
                        //if the state color alpha is 0 and atleast 1 province is not a sea or lake then set color to first province color
                        if (s.Color.A == 0 && s.provDict.Count > 0) {
                            foreach (KeyValuePair<Color, Province> kvp in s.provDict) {
                                if (!kvp.Value.isSea && !kvp.Value.isLake) {
                                    s.Color = kvp.Key;
                                    break;
                                }
                            }
                        }

                        //Console.WriteLine("\t" + s.name + " " + s.color + " " + s.provIDList.Count);

                        foreach (var (x, y) in s.Coords) {
                            stateImage.SetPixel(x, y, s.Color);
                        }
                    }
                }
                //check if /_Output/ColorMap exists if not add it
                if (!Directory.Exists(localDir + "/_Output/ColorMap")) {
                    Directory.CreateDirectory(localDir + "/_Output/ColorMap");
                }

                //save state images
                stateImage.Save(localDir + "/_Output/ColorMap/state_colors.png");

                Drawer.DrawBorders(stateImage, Color.Black, (bool)configs["DrawCoastalBordersStates"]).Save(localDir + "/_Output/BorderFrame/state_border.png");

            }

            //draw region images
            void drawRegionImages(Dictionary<string, Region> regions, Bitmap image) {
                Console.WriteLine("Drawing Region Maps");
                Drawer.DrawWaterMap(provinces.Values.ToList()).Save(localDir + "/_Output/ColorMap/water_map.png");

                foreach (Region r in regions.Values) {
                    r.GetCenter();
                }

                Drawer.DrawColorMap(regions.Values.Cast<IDrawable>().ToList()).Save(localDir + "/_Output/ColorMap/region_colors.png");
                Drawer.DrawBorders(localDir + "/_Output/ColorMap/region_colors.png", Color.Black, (bool)configs["DrawCoastalBordersRegions"]).Save(localDir + "/_Output/BorderFrame/region_border.png");
            }

            //drawRGOMaps
            ((int, int) waterCenter, (int, int) waterMaxSize) drawRGOMaps(Dictionary<string, Region> regions, List<(int, int)> waterCoordList) {
                //if Output/RGOs/ does not exist, create it
                if (!Directory.Exists(localDir + "/_Output/RGOs/")) {
                    Directory.CreateDirectory(localDir + "/_Output/RGOs/");
                }

                HashSet<string> rgoNames = SetRGOColors(regions);
                Bitmap image = new(localDir + "/_Input/map_data/provinces.png");
                Bitmap water = new(localDir + "/_Output/ColorMap/water_map.png");
                Bitmap stateBorder = new(localDir + "/_Output/BorderFrame/state_border.png");

                StringFormat stringFormat = new() {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                PrivateFontCollection privateFontCollection = new();
                privateFontCollection.AddFontFile(localDir + "/_Input/ParadoxVictorian-Condensed.otf"); //font for numbers and names

                //find the largest rectangle without holes in the water
                MaximumRectangle mr = new();
                ((int, int) waterCenter, (int, int) waterMaxRecSize) = ((0, 0), (0, 0));
                if (waterCoordList.Count > 0) {
                    (waterCenter, waterMaxRecSize) = MaximumRectangle.Center(waterCoordList, false);
                }

                foreach (string name in rgoNames) {
                    string wType = "";
                    Color resColor = Color.FromArgb(255, 255, 255, 255);
                    Color textColor = Color.FromArgb(255, 255, 255, 255);
                    Console.WriteLine(name + "\t" + sw.Elapsed);
                    Bitmap rgoMap = new(image.Width, image.Height);
                    Bitmap rgoName = new(image.Width, image.Height);
                    Graphics g = Graphics.FromImage(rgoMap);
                    g.Clear(Color.White);
                    g.DrawImage(water, Point.Empty);

                    foreach (Region r in regions.Values) {
                        foreach (State s in r.states) {
                            foreach (var resPair in s.resources) {
                                Resource res = resPair.Value;
                                if (res.name.Contains(name)) {
                                    foreach (var (x, y) in s.Coords) {
                                        rgoMap.SetPixel(x, y, res.color);
                                    }
                                }
                            }
                        }
                    }

                    g.DrawImage(stateBorder, Point.Empty);

                    foreach (Region r in regions.Values) {
                        foreach (State s in r.states) {
                            foreach (var resPair in s.resources) {
                                Resource res = resPair.Value;
                                if (res.name.Contains(name)) {
                                    wType = res.type;
                                    // write text  
                                    string val = "";
                                    if (res.type.Equals("agriculture")) {
                                        val = s.arableLand.ToString();
                                    }
                                    else {
                                        if (res.knownAmount > 0) {
                                            val += res.knownAmount;
                                        }
                                        if (res.discoverableAmount > 0) {
                                            if (res.knownAmount > 0) {
                                                val += "|";
                                            }
                                            val += "(" + res.discoverableAmount + ")";
                                        }
                                    }

                                    bool gotRectangularBox = false;
                                    if (val.Length > 4) { // for those cases where the number would look better in a long rectangle than a square
                                        s.GetCenter();
                                        gotRectangularBox = true;
                                        Console.WriteLine("\t" + res.name + " in " + s.Name + " switching to rectangle");
                                    }

                                    int numberFontSize = 8; // minimum font size for number
                                    Font font1 = new(privateFontCollection.Families[0], numberFontSize);

                                    // check pixel size of font1
                                    SizeF size1 = g.MeasureString(val, font1);
                                    // if size1 is smaller than state maxRecSize then increase font size to fit
                                    while (size1.Width < s.MaxRectangleSize.h && size1.Height < s.MaxRectangleSize.w) {
                                        numberFontSize++;
                                        font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                                        size1 = g.MeasureString(val, font1);
                                    }
                                    numberFontSize = (int)(numberFontSize * 1.3);
                                    font1 = new Font(privateFontCollection.Families[0], numberFontSize);

                                    if (numberFontSize < 20) {
                                        numberFontSize = 10;
                                        font1 = new Font("Verdana", numberFontSize);
                                        size1 = g.MeasureString(val, font1);
                                        while (size1.Width < s.MaxRectangleSize.h && size1.Height < s.MaxRectangleSize.w) {
                                            numberFontSize++;
                                            font1 = new Font("Verdana", numberFontSize);
                                            size1 = g.MeasureString(val, font1);
                                        }
                                        numberFontSize = (int)(numberFontSize * 1.2);
                                        font1 = new Font("Verdana", numberFontSize, FontStyle.Bold);
                                    }

                                    resColor = res.color;
                                    textColor = res.textColor;
                                    g.DrawString(val, font1, new SolidBrush(res.textColor), new Point(s.RectangleCenter.x, s.RectangleCenter.y), stringFormat);

                                    if (gotRectangularBox) { // revert back to square for the rest of the res in that state
                                        s.GetCenter(true);
                                    }
                                }
                            }
                        }
                    }

                    List<string> tmpName = name.Replace("bg_", "").Split("_").ToList();
                    string wName = "";
                    for (int j = 0; j < tmpName.Count; j++) {
                        string tmpWord = tmpName[j][0].ToString().ToUpper() + tmpName[j][1..];
                        wName += tmpWord + " ";
                    }
                    // wName += "("+wType+")";
                    // wNameList new list containing wName
                    List<string> wNameList = new() {
                        wName,
                        "(" + wType + ")"
                    };

                    // scale font2 to fit inside waterMaxRecSize
                    int fontSize = 200; // minimum font size for name
                    Font font2 = new(privateFontCollection.Families[0], fontSize);

                    // check pixel size of font2
                    SizeF size2 = g.MeasureString(wNameList[0], font2);
                    // if size2 is smaller than waterMaxRecSize then increase font size to fit
                    while (size2.Width < waterMaxRecSize.Item1 && size2.Height < (int)(waterMaxRecSize.Item2 * 0.8)) {
                        fontSize++;
                        font2 = new Font(privateFontCollection.Families[0], fontSize);
                        size2 = g.MeasureString(name, font2);
                    }

                    // check if single line wName would be bigger
                    string wName2 = wNameList[0] + " " + wNameList[1];
                    int fontSize2 = 200; // minimum font size for name
                    Font font3 = new(privateFontCollection.Families[0], fontSize2);
                    SizeF size3 = g.MeasureString(wName2, font3);
                    while (size3.Width < waterMaxRecSize.Item1 && size3.Height < (int)(waterMaxRecSize.Item2 * 1.3)) {
                        fontSize2++;
                        font3 = new Font(privateFontCollection.Families[0], fontSize2);
                        size3 = g.MeasureString(wName2, font3);
                    }

                    // if single line wName would be bigger then use 2 lines
                    if (fontSize > fontSize2) {
                        // draw all names in wNameList to rgoName image and move them down by Xpx each time
                        int y = waterCenter.Item2 - (int)(size2.Height * 0.15);
                        foreach (string s in wNameList) {
                            g.DrawString(s, font2, new SolidBrush(resColor), new Point(waterCenter.Item1, y), stringFormat);

                            // border outline
                            GraphicsPath p = new();
                            p.AddString(
                                s,             // text to draw
                                privateFontCollection.Families[0],  // or any other font family
                                (int)FontStyle.Regular,      // font style (bold, italic, etc.)
                                g.DpiY * font2.Size / 72,       // em size
                                new Point(waterCenter.Item1, y),              // location where to draw text
                                stringFormat);          // set options here (e.g. center alignment)
                            Pen p1 = new(textColor, 4);
                            g.DrawPath(p1, p);

                            y += (int)(size2.Height * 0.5);
                        }
                    }
                    else {
                        int y = waterCenter.Item2 + (int)(size3.Height * 0.1);

                        g.DrawString(wName2, font3, new SolidBrush(resColor), new Point(waterCenter.Item1, y), stringFormat);

                        // border outline
                        GraphicsPath p = new();
                        p.AddString(
                            wName2,             // text to draw
                            privateFontCollection.Families[0],  // or any other font family
                            (int)FontStyle.Regular,      // font style (bold, italic, etc.)
                            g.DpiY * font3.Size / 72,       // em size
                            new Point(waterCenter.Item1, y),              // location where to draw text
                            stringFormat);          // set options here (e.g. center alignment)
                        Pen p1 = new(textColor, 4);
                        g.DrawPath(p1, p);
                    }

                    rgoMap.Save(localDir + "/_Output/RGOs/" + name.Replace("bg_", "") + ".png");
                    rgoMap.Dispose();
                }
                return (waterCenter, waterMaxRecSize);
            }

            //set RGO Colors
            HashSet<string> SetRGOColors(Dictionary<string, Region> regions) {
                HashSet<string> resourceNames = new();
                var ignoreRgoNames = (List<string>)configs["IgnoreRGONames"];
                var rgoColors = (List<(List<string> rgoNames, Color hColor, Color tColor)>)configs["RgoColors"];

                foreach (Region r in regions.Values) {
                    foreach (State s in r.states) {
                        if (s.RectangleCenter == (0, 0)) {
                            s.GetCenter(true);
                        }

                        foreach (Resource res in s.resources.Values) {
                            // Set color for the resource
                            foreach (var (rgoNames, hColor, tColor) in rgoColors) {
                                if (rgoNames.Any(res.name.Contains)) {
                                    res.color = hColor;
                                    res.textColor = tColor;
                                    break;
                                }
                            }

                            bool isIgnored = ignoreRgoNames.Any(res.name.Contains);
                            if (isIgnored) {
                                Console.WriteLine($"\t\tIgnoring {res.name} in {s.Name}");
                            }
                            else {
                                resourceNames.Add(res.name);
                            }
                        }
                    }
                }
                return resourceNames;
            }

            //merge maps
            void mergeMaps() {
                //if Output/BlankMap/ does not exist create it
                if (!Directory.Exists(localDir + "/_Output/BlankMap/")) {
                    Directory.CreateDirectory(localDir + "/_Output/BlankMap/");
                }

                Bitmap waterColor = new(localDir + "/_Output/ColorMap/water_map.png");
                Bitmap regionColor = new(localDir + "/_Output/ColorMap/region_colors.png");
                Bitmap regionBorder = new(localDir + "/_Output/BorderFrame/region_border.png");

                Bitmap whiteBitmap = new Bitmap(waterColor.Width, waterColor.Height);
                using (Graphics g = Graphics.FromImage(whiteBitmap)) {
                    g.Clear(Color.White);
                }

                Drawer.MergeImages(new List<Bitmap>() { waterColor, regionColor, regionBorder }).Save(localDir + "/_Output/Region_Map.png");
                Drawer.MergeImages(new List<Bitmap>() { whiteBitmap, waterColor, regionBorder }).Save(localDir + "/_Output/BlankMap/Region_Blank.png");

                Bitmap regionNames = new(localDir + "/_Output/Region_Map.png");
                Bitmap regionDebug = new(localDir + "/_Output/BlankMap/Region_Blank.png");
                foreach (var region in regions.Values) {
                    if (region.Color.A == 0) continue; //no ocean/sea names
                    region.GetCenter();
                    if (region.RectangleCenter == (0, 0)) continue;
                    Drawer.WriteText(regionNames, region.Name.Replace("region_", "").Replace("_", " "), region.RectangleCenter, region.MaxRectangleSize, 8, Color.Black);
                    Drawer.DrawDebugRectangle(regionDebug, region.RectangleCenter, region.MaxRectangleSize, region.Color);
                }
                regionNames.Save(localDir + "/_Output/Region_Map_Names_test.png");
                regionDebug.Save(localDir + "/_Output/Region_Rectangles_test.png");


                regionColor.Dispose();
                regionBorder.Dispose();

                Bitmap stateColor = new(localDir + "/_Output/ColorMap/state_colors.png");
                Bitmap stateBorder = new(localDir + "/_Output/BorderFrame/state_border.png");

                Drawer.MergeImages(new List<Bitmap>() { waterColor, stateColor, stateBorder }).Save(localDir + "/_Output/State_Map.png");
                Drawer.MergeImages(new List<Bitmap>() { whiteBitmap, waterColor, stateBorder }).Save(localDir + "/_Output/BlankMap/State_Blank.png");
                stateColor.Dispose();
                stateBorder.Dispose();

                Bitmap provinceBorder = new(localDir + "/_Output/BorderFrame/prov_border.png");
                Drawer.MergeImages(new List<Bitmap>() { whiteBitmap, waterColor, provinceBorder }).Save(localDir + "/_Output/BlankMap/Province_Blank.png");

                Console.WriteLine("Merged Maps\t" + sw.Elapsed);
            }

            //write names on merged maps
            void namedMapes(Dictionary<string, Region> regions) {
                StringFormat stringFormat = new() {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                PrivateFontCollection privateFontCollection = new();
                privateFontCollection.AddFontFile(localDir + "/_Input/ParadoxVictorian-Condensed.otf"); //font for region names


                //region map
                Bitmap regionMap = new(localDir + "/_Output/Region_Map.png");
                Graphics g = Graphics.FromImage(regionMap);


                foreach (Region region in regions.Values) {
                    if (region.Color.A == 0) continue; //no ocean/sea names

                    region.GetCenter();

                    List<string> tmpName = region.Name.Replace("region_", "").Split("_").ToList();
                    List<string> wName = new();
                    for (int j = 0; j < tmpName.Count; j++) {
                        string tmpWord = tmpName[j][0].ToString().ToUpper() + tmpName[j][1..].ToLower();
                        wName.Add(tmpWord);
                    }

                    if (region.MaxRectangleSize.h >= region.MaxRectangleSize.w * 2) {
                        string tmp = "";
                        for (int j = 0; j < wName.Count; j++) {
                            tmp += wName[j] + " ";
                        }
                        tmp = tmp.Trim();
                        wName.Clear();
                        wName.Add(tmp);
                    }

                    int numberFontSize = 25; //minimum font size for region name
                    Font font1 = new(privateFontCollection.Families[0], numberFontSize);

                    //check pixel size of font1
                    //longest name in wName
                    string longestName = "";
                    for (int j = 0; j < wName.Count; j++) {
                        if (wName[j].Length > longestName.Length) {
                            longestName = wName[j];
                        }
                    }
                    double vertBias = 1.2;
                    if (wName.Count > 1) {
                        vertBias = 1.0;
                    }

                    SizeF size1 = g.MeasureString(longestName, font1);
                    //if size1 is smaller than region maxRecSize then increase font size to fit
                    while (size1.Width < region.MaxRectangleSize.h * 1.2 && size1.Height < region.MaxRectangleSize.w * wName.Count * vertBias) {
                        numberFontSize++;
                        font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                        size1 = g.MeasureString(longestName, font1);
                    }

                    //for each word in wName draw it on the map and move down by size1.Height/2
                    int y = 0;
                    if (wName.Count > 1) {
                        y = region.RectangleCenter.y - (int)(size1.Height * 0.35);
                    }
                    else {
                        y = region.RectangleCenter.y;
                    }
                    Color textColor = OppositeExtremeColor(region.Color);

                    for (int j = 0; j < wName.Count; j++) {
                        g.DrawString(wName[j], font1, new SolidBrush(textColor), new Point(region.RectangleCenter.x, y), stringFormat);
                        y += (int)(size1.Height * 0.6);
                    }
                }

                //save regionMap as png
                regionMap.Save(localDir + "/_Output/Region_Map_Names.png");

                //state map
                Bitmap stateMap = new(localDir + "/_Output/State_Map.png");


                g = Graphics.FromImage(stateMap);

                foreach (Region region in regions.Values) {
                    foreach (State state in region.states) {
                        state.GetCenter();
                        if (state.Color.A == 0) continue; //no ocean/sea names

                        List<string> tmpName = state.Name.Replace("STATE_", "").Split("_").ToList();
                        List<string> wName = new();
                        int wLength = -1;
                        for (int j = 0; j < tmpName.Count; j++) {
                            string tmpWord = tmpName[j][0].ToString().ToUpper() + tmpName[j][1..].ToLower();
                            wName.Add(tmpWord);
                            wLength += tmpWord.Length;
                            wLength++;
                        }

                        //if more than 2 words in wName, merge into one line
                        if (wName.Count > 2) {
                            string tmp = "";
                            for (int k = 0; k < wName.Count; k++) {
                                tmp += wName[k] + " ";
                            }
                            tmp = tmp.Trim();
                            wName.Clear();
                            wName.Add(tmp);
                            //split wName[0] into 2 lines on the space that is closest to the middle
                            int spaceIndex = (int)(wName[0].Length * 0.4);
                            int tmpSpaceIndex = spaceIndex;
                            while (wName[0][tmpSpaceIndex] != ' ') {
                                tmpSpaceIndex++;
                            }
                            if (tmpSpaceIndex - spaceIndex < spaceIndex) {
                                spaceIndex = tmpSpaceIndex;
                            }
                            tmpSpaceIndex = spaceIndex;
                            while (wName[0][tmpSpaceIndex] != ' ') {
                                tmpSpaceIndex--;
                            }
                            if (spaceIndex - tmpSpaceIndex < spaceIndex) {
                                spaceIndex = tmpSpaceIndex;
                            }
                            string tmp1 = wName[0][..spaceIndex];
                            string tmp2 = wName[0][(spaceIndex + 1)..];
                            wName.Clear();
                            wName.Add(tmp1);
                            wName.Add(tmp2);

                            Console.WriteLine(wName[0] + " " + wName[0].Length + "\t" + wName[1] + " " + wName[1].Length);
                        }

                        else if ((state.MaxRectangleSize.h >= state.MaxRectangleSize.w * 2.1 || wLength < 8) && wName.Count() > 0) {
                            string tmp = "";
                            for (int k = 0; k < wName.Count; k++) {
                                tmp += wName[k] + " ";
                            }
                            tmp = tmp.Trim();
                            wName.Clear();
                            wName.Add(tmp);

                            Console.WriteLine(wName[0] + " " + wName[0].Length);
                        }

                        //check pixel size of font1
                        //longest name in wName
                        string longestName = "";
                        for (int k = 0; k < wName.Count; k++) {
                            if (wName[k].Length > longestName.Length) {
                                longestName = wName[k];
                            }
                        }

                        int numberFontSize = 7; //minimum font size for state name
                        Font font2 = new("Verdna", numberFontSize);

                        SizeF size1 = g.MeasureString(longestName, font2);
                        double vertBias = 1.2;
                        if (wName.Count > 1) {
                            vertBias = 1.0;
                        }

                        while (size1.Width < state.MaxRectangleSize.h && size1.Height < state.MaxRectangleSize.w * wName.Count * vertBias) {
                            numberFontSize++;
                            font2 = new Font("Verdna", numberFontSize);
                            size1 = g.MeasureString(longestName, font2);
                        }

                        int y = 0;
                        if (wName.Count > 2) {
                            y = state.RectangleCenter.y - (int)(size1.Height * 3 / 4);
                        }
                        else if (wName.Count > 1) {
                            y = state.RectangleCenter.y - (int)(size1.Height * 3 / 8);
                        }
                        else {
                            y = state.RectangleCenter.y;
                        }

                        Color textColor = OppositeExtremeColor(state.Color);
                        foreach (string s in wName) {
                            g.DrawString(s, font2, new SolidBrush(textColor), new Point(state.RectangleCenter.x, y), stringFormat);
                            y += (int)(size1.Height * 0.7);
                        }

                    }
                }

                //save stateMap as png
                stateMap.Save(localDir + "/_Output/State_Map_Names.png");
            }

            //debug draw rectangle around each region and state
            void debugDrawRectangle(Dictionary<string, Region> regions, (int, int) waterRecCenter, (int, int) waterRecSize) {
                //if Output/Debug/ does not exist create it
                if (!Directory.Exists(localDir + "/_Output/Debug/")) {
                    Directory.CreateDirectory(localDir + "/_Output/Debug/");
                }

                //create a new blank region map
                Bitmap regionMap = new(localDir + "/_Output/BlankMap/Region_Blank.png");
                Graphics g = Graphics.FromImage(regionMap);

                foreach (var region in regions.Values) {
                    g.FillRectangle(
                        new SolidBrush(region.Color),
                        region.RectangleCenter.x - (region.MaxRectangleSize.h / 2),
                        region.RectangleCenter.y - (region.MaxRectangleSize.w / 2),
                        region.MaxRectangleSize.h,
                        region.MaxRectangleSize.w
                    );
                }
                //fill a solid rectangle of size for water
                g.FillRectangle(new SolidBrush(Color.Black), waterRecCenter.Item1 - (waterRecSize.Item1 / 2), waterRecCenter.Item2 - (waterRecSize.Item2 / 2), waterRecSize.Item1, waterRecSize.Item2);


                //save regionMap as png
                regionMap.Save(localDir + "/_Output/Debug/Region_Rectangles.png");
                regionMap.Dispose();

                //state map
                Bitmap stateMap = new(localDir + "/_Output/BlankMap/State_Blank.png");
                g = Graphics.FromImage(stateMap);

                foreach (Region region in regions.Values) {
                    foreach (State state in region.states) {
                        state.GetCenter();
                        g.FillRectangle(
                            new SolidBrush(state.Color),
                            state.RectangleCenter.x - (state.MaxRectangleSize.h / 2),
                            state.RectangleCenter.y - (state.MaxRectangleSize.w / 2),
                            state.MaxRectangleSize.h,
                            state.MaxRectangleSize.w
                        );
                    }
                }

                //fill a solid rectangle of size for water
                g.FillRectangle(new SolidBrush(Color.Black), waterRecCenter.Item1 - (waterRecSize.Item1 / 2), waterRecCenter.Item2 - (waterRecSize.Item2 / 2), waterRecSize.Item1, waterRecSize.Item2);

                //save stateMap as png
                stateMap.Save(localDir + "/_Output/Debug/State_Rectangles.png");
                stateMap.Dispose();


                //state square
                Bitmap stateSquareMap = new(localDir + "/_Output/BlankMap/State_Blank.png");
                g = Graphics.FromImage(stateSquareMap);
                foreach (Region region in regions.Values) {
                    foreach (State state in region.states) {
                        state.GetCenter(true);
                        //fill a solid rectangle of size maxRecSize and color regions[i].color centered on regions[i].state[j].center
                        g.FillRectangle(new SolidBrush(color: state.Color),
                                        x: state.RectangleCenter.x - (state.MaxRectangleSize.h / 2),
                                        y: state.RectangleCenter.y - (state.MaxRectangleSize.w / 2),
                                        width: state.MaxRectangleSize.h,
                                        height: state.MaxRectangleSize.w);
                    }
                }

                //fill a solid rectangle of size for water
                g.FillRectangle(new SolidBrush(Color.Black),
                    x: waterRecCenter.Item1 - (waterRecSize.Item1 / 2),
                    y: waterRecCenter.Item2 - (waterRecSize.Item2 / 2),
                    width: waterRecSize.Item1,
                    height: waterRecSize.Item2);

                //save stateMap as png
                stateSquareMap.Save(localDir + "/_Output/Debug/State_Square.png");
                stateSquareMap.Dispose();
            }

            void drawHubs(Dictionary<string, Region> regions, Image image) {
                //create a new blank image of size image
                Bitmap hubMap = new(image.Width, image.Height);

                //hub name color pairs
                Dictionary<string, Color> hubColor = new() {
                    { "city", Color.Purple },
                    { "port", Color.DarkCyan },
                    { "mine", Color.Red },
                    { "farm", Color.Yellow },
                    { "wood", Color.DarkGreen }
                };

                foreach (Region r in regions.Values) {
                    foreach (State s in r.states) {
                        //for each province in s.provinces
                        foreach (Province p in s.provDict.Values) {
                            if (hubColor.ContainsKey(p.hubName)) {
                                foreach (var (x, y) in p.Coords) {
                                    hubMap.SetPixel(x, y, hubColor[p.hubName]);
                                }
                            }
                        }
                    }
                }

                hubMap.Save(localDir + "/_Output/ColorMap/hub_map.png");
            }

            void drawImpassablePrime(Dictionary<string, Region> regions, Image image) {
                //create a new blank image of size image
                Bitmap impassablePrimeMap = new(image.Width, image.Height);

                foreach (Region r in regions.Values) {
                    foreach (State s in r.states) {
                        //for each province in s.provinces
                        foreach (Province p in s.provDict.Values) {
                            if (p.isImpassible) {
                                Color c = Color.Gray;
                                if (p.isLake || p.isSea) {
                                    c = Color.Blue;
                                }
                                foreach (var (x, y) in p.Coords) {
                                    impassablePrimeMap.SetPixel(x, y, c);
                                }
                            }
                            else if (p.isPrimeLand) {
                                Color c = Color.Green;
                                foreach (var (x, y) in p.Coords) {
                                    impassablePrimeMap.SetPixel(x, y, c);
                                }
                            }

                        }
                    }
                }

                impassablePrimeMap.Save(localDir + "/_Output/ColorMap/impassable_prime_map.png");
            }

            void drawTerrain(Dictionary<Color, Province> provDict, Image image) {

                //create a new blank image of size image
                Bitmap terrainMap = new(image.Width, image.Height);

                //dictionary of terrain names and colors
                Dictionary<string, Color> terrainColor = new() {
                    { "plains", Color.LawnGreen },
                    { "forest", Color.ForestGreen },
                    { "jungle", Color.DarkOliveGreen },
                    { "desert", Color.SandyBrown },
                    { "hills", Color.Red },
                    { "mountain", Color.Black },
                    { "savanna", Color.Orange },
                    { "snow", Color.Snow },
                    { "tundra", Color.MediumOrchid },
                    { "wetland", Color.Brown },
                    { "lakes", Color.Blue },
                    { "ocean", Color.Blue }
                };

                foreach (Province p in provDict.Values) {
                    if (terrainColor.ContainsKey(p.terrain)) {
                        foreach (var (x, y) in p.Coords) {
                            terrainMap.SetPixel(x, y, terrainColor[p.terrain]);
                        }
                    }
                    else {
                        foreach (var (x, y) in p.Coords) {
                            terrainMap.SetPixel(x, y, Color.LightGray);
                        }
                    }
                }

                terrainMap.Save(localDir + "/_Output/ColorMap/terrain_map.png");
            }

            void drawNationsMap(Dictionary<string, Nation> nationDict, string fileName, bool drawDecentralized) {
                Console.WriteLine("Drawing " + fileName);

                //if the output folder doesn't exist, create it
                if (!Directory.Exists(localDir + "/_Output/National")) {
                    Directory.CreateDirectory(localDir + "/_Output/National");
                }

                //create a new bitmap with the same size as the image
                Bitmap bitmap = new(localDir + "/_Output/ColorMap/water_map.png");

                //for each nation in nationDict
                foreach (Nation n in nationDict.Values) {
                    //draw decentralized nations?
                    if (n.type == "decentralized" && !drawDecentralized) {
                        continue;
                    }
                    //for each province in n.provinces
                    foreach (Province p in n.provinces.Values) {
                        //for each pixel in p set the pixel in bitmap to n.color
                        foreach (var (x, y) in p.Coords) {
                            bitmap.SetPixel(x, y, n.Color);
                        }
                    }
                }

                //save bitmap to _Output/nations.png
                bitmap.Save(localDir + "/_Output/National/" + fileName + ".png");

                Drawer.DrawBorders(localDir + "/_Output/National/" + fileName + ".png", Color.Black, (bool)configs["DrawCoastalBordersNations"]).Save(localDir + "/_Output/BorderFrame/" + fileName + "_border.png");

                StringFormat stringFormat = new() {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                PrivateFontCollection privateFontCollection = new();
                privateFontCollection.AddFontFile(localDir + "/_Input/ParadoxVictorian-Condensed.otf"); //font for nation names

                Graphics g = Graphics.FromImage(bitmap);

                //add name to bitmap for each nation
                foreach (Nation n in nationDict.Values) {

                    //draw decentralized nations?
                    if (n.type == "decentralized" && !drawDecentralized) {
                        continue;
                    }
                    Color textColor = OppositeExtremeColor(n.Color);

                    //get the center of the nation
                    n.GetCenter();

                    string text = n.Name.ToLower();

                    int numberFontSize = 8; //minimum font size for nation name
                    Font font1 = new(privateFontCollection.Families[0], numberFontSize);

                    //check pixel size of font1
                    SizeF textSize = g.MeasureString(text, font1);

                    //if size1 is smaller than state maxRecSize then increase font size to fit
                    while (textSize.Width < n.MaxRectangleSize.h && textSize.Height < n.MaxRectangleSize.w) {
                        numberFontSize++;
                        font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                        textSize = g.MeasureString(text, font1);
                    }
                    numberFontSize = (int)(numberFontSize * 1.3);


                    int recFontSize = numberFontSize;

                    //check if square getCenter2 would give a larger font size
                    n.GetCenter(true);
                    numberFontSize = 8; //minimum font size for nation name
                    font1 = new Font(privateFontCollection.Families[0], numberFontSize);

                    //check pixel size of font1
                    textSize = g.MeasureString(text, font1);

                    //if size1 is smaller than state maxRecSize then increase font size to fit
                    while (textSize.Width < n.MaxRectangleSize.h && textSize.Height < n.MaxRectangleSize.w) {
                        numberFontSize++;
                        font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                        textSize = g.MeasureString(text, font1);
                    }
                    numberFontSize = (int)(numberFontSize * 1.3);

                    if (recFontSize > numberFontSize) {
                        //Console.WriteLine(n.name + " would be larger as a rectangle by " + (recFontSize - numberFontSize) + " size");
                        n.GetCenter();
                        numberFontSize = recFontSize;
                    }

                    font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                    //draw the name on bitmap
                    //drawText(bitmap, text, center.X, center.Y, Color.White);
                    g.DrawString(text, font1, new SolidBrush(textColor), new Point(n.RectangleCenter.x, n.RectangleCenter.y), stringFormat);
                }

                bitmap.Save(localDir + "/_Output/National/" + fileName + "_tags.png");
            }

            void writeRGOs(Dictionary<string, Region> regions) {
                //check if _Output/TextFiles folder exists
                if (!Directory.Exists(localDir + "/_Output/TextFiles/")) {
                    Directory.CreateDirectory(localDir + "/_Output/TextFiles/");
                }

                //dictionary of resource type and resource name
                Dictionary<string, List<string>> resDict = new();

                //go through each region in regions
                foreach (Region r in regions.Values) {
                    foreach (State s in r.states) {
                        foreach (Resource res in s.resources.Values) {
                            if (!resDict.TryGetValue(res.type, out var resourceList)) {
                                resourceList = new List<string>();
                                resDict[res.type] = resourceList;
                            }
                            if (!resourceList.Contains(res.name)) {
                                resourceList.Add(res.name);
                            }
                        }
                    }
                }

                //create a new csv file for each RGOs
                StreamWriter streamWriter = new(localDir + "/_Output/TextFiles/RGOs.csv");

                //header
                streamWriter.Write("Region;State;");
                foreach (string resType in resDict.Keys) {
                    //resource
                    if (resType == "resource") {
                        foreach (string resName in resDict[resType]) {
                            streamWriter.Write(resName.Replace("bg_", "") + ";");
                        }
                    }
                }
                foreach (string resType in resDict.Keys) {
                    //discoverable
                    if (resType == "discoverable") {
                        foreach (string resName in resDict[resType]) {
                            streamWriter.Write("Known " + resName.Replace("bg_", "") + ";");
                            streamWriter.Write("Discoverable " + resName.Replace("bg_", "") + ";");
                        }
                    }
                }
                streamWriter.Write("Arable Land;Agricultural RGOs\n");


                foreach (Region r in regions.Values) {
                    if (r.states.Count < 2) continue; //don't write naval states/regions

                    foreach (State s in r.states) {
                        streamWriter.Write(r.Name.Replace("region_", "") + ";" + s.Name.ToLower().Replace("state_", "") + ";");
                        // Resource
                        if (resDict.TryGetValue("resource", out var resourceNames)) {
                            foreach (var resName in resourceNames) {
                                if (s.resources.TryGetValue(resName, out var resource)) {
                                    streamWriter.Write(resource.knownAmount + ";");
                                }
                                else {
                                    streamWriter.Write("0;");
                                }
                            }
                        }

                        // Discoverable
                        if (resDict.TryGetValue("discoverable", out var discoverableNames)) {
                            foreach (var resName in discoverableNames) {
                                if (s.resources.TryGetValue(resName, out var resource)) {
                                    streamWriter.Write(resource.knownAmount + ";");
                                    streamWriter.Write(resource.discoverableAmount + ";");
                                }
                                else {
                                    streamWriter.Write("0;");
                                    streamWriter.Write("0;");
                                }
                            }
                        }

                        // Arable
                        streamWriter.Write(s.arableLand + ";");
                        foreach (var resource in s.resources.Values) {
                            if (resource.type == "agriculture") {
                                streamWriter.Write(resource.name.Replace("bg_", "") + " ");
                            }
                        }

                        streamWriter.Write("\n");
                    }
                }
                streamWriter.Close();
            }

            void debugStateProv(Dictionary<string, Region> regions) {
                //group regions by number of states in each region
                var stateCount = regions.Values.GroupBy(x => x.states.Count).OrderBy(x => x.Key);

                List<string> lines = new() {
                    "Region;State;Provinces\n"
                };
                //print out number of states in each region
                foreach (var s in stateCount) {
                    lines.Add(s.Key + ";" + s.Count() + ";\n");
                    Console.WriteLine(s.Key + " states: " + s.Count());
                }
                Console.WriteLine();
                lines.Add("\n");

                //group states by number of provinces
                var provCount = regions.Values.SelectMany(x => x.states).GroupBy(x => x.provDict.Count).OrderBy(x => x.Key);

                //print out number of provinces in each state
                foreach (var p in provCount) {
                    lines.Add(";" + p.Key + ";" + p.Count() + "\n");
                    Console.WriteLine(p.Key + " provinces: " + p.Count());
                }

                //write to file
                StreamWriter streamWriter = new(localDir + "/_Output/Debug/StateProv.csv");

                //write lines
                foreach (string line in lines) {
                    streamWriter.Write(line);
                }
                streamWriter.Close();

            }

            Color OppositeExtremeColor(Color c) {
                //for each rgb value in color find if it is closer to 0 or 255 and set it to the opposite
                int r = c.R;
                int g = c.G;
                int b = c.B;

                if (r > 127) r = 0;
                else r = 255;

                if (g > 127) g = 0;
                else g = 255;

                if (b > 127) b = 0;
                else b = 255;

                return Color.FromArgb(r, g, b);
            }

            string SplitAndCapitalize(string s) {
                string[] words = s.Split('_');
                string result = "";
                foreach (string word in words) {
                    result += word[0].ToString().ToUpper() + word[1..] + " ";
                }
                return result.Trim();
            }
        }
    }
}