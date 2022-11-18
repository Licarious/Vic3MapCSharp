
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

internal class Program
{
    private static void Main(string[] args) {
        Stopwatch sw = Stopwatch.StartNew();

        //move up 3 directorys from local
        string localDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
        
        List<State> stateList = new List<State>();
        parseStateFiles(stateList);
        List<Region> regionList = new List<Region>();
        parseRegionFiles(regionList);
        mergeStateRegion(stateList, regionList);

        List<string> seaList = new List<string>();
        List<string> lakeList = new List<string>();
        parseDefaultMap(seaList, lakeList);

        //drawMap(regionList, seaList, lakeList);
        parseProvMap(regionList, seaList, lakeList);


        //method to parse state files
        void parseStateFiles(List<State> stateList) {
            //read all files in localDir/_Input/state_regions
            string[] files = Directory.GetFiles(localDir + "/_Input/state_regions");
            //for each file
            int count = 0;

            foreach (string file in files) {
                if (file.EndsWith(".txt")) {
                    //read file
                    string[] lines = File.ReadAllLines(file);
                    //for each line
                    //Console.WriteLine(file);
                    State s = new State();
                    Resource dr = new Resource();
                    bool cappedResourseFound = false;
                    bool discoverableResourseFound = false;
                    bool traitsfound = false;
                    foreach (string l1 in lines) {
                        string line = l1.Replace("=", " = ").Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Replace("  ", " ").Trim();

                        //get STATE_NAME
                        if (line.StartsWith("STATE_")) {
                            //Console.WriteLine("\t"+line.Split()[0]);
                            s = new State(line.Split()[0]);

                            //incase people are orverriding states in latter files
                            //check if state with same name already exists in stateList and if so, delete it
                            foreach (State state in stateList) {
                                if (state.name == s.name) {
                                    stateList.Remove(state);
                                    break;
                                }
                            }

                            stateList.Add(s);
                        }
                        //get stateID
                        if (line.StartsWith("id")) {
                            s.stateID = int.Parse(line.Split()[2]);
                        }
                        if (line.StartsWith("subsistence_building")) {
                            s.subsistanceBuilding = line.Split("=")[1].Replace("\"", "").Trim();
                        }

                        //get provinces
                        if (line.TrimStart().StartsWith("provinces")) {
                            string[] l2 = line.Split("=")[1].Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                                    string n = l2[i].Replace("\"", "").Replace("x", "");
                                    s.provIDList.Add(n);
                                }
                            }
                        }
                        //get impassable colors
                        if (line.TrimStart().StartsWith("impassable")) {
                            string[] l2 = line.Split("=")[1].Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                                    string n = l2[i].Replace("\"", "").Replace("x", "");
                                    Color c = ColorTranslator.FromHtml("#" + n);
                                    s.impassables.Add(c);
                                }
                            }
                        }
                        //get prime_land colors
                        if (line.TrimStart().StartsWith("prime_land")) {
                            string[] l2 = line.Split("=")[1].Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                                    string n = l2[i].Replace("\"", "").Replace("x", "");
                                    Color c = ColorTranslator.FromHtml("#" + n);
                                    s.primeLand.Add(c);
                                }
                            }
                        }

                        //get traits
                        if (line.Trim().StartsWith("traits")) {
                            traitsfound = true;
                        }
                        if (traitsfound) {
                            string[] l2 = line.Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"")) {
                                    s.traits.Add(l2[i].Replace("\"", ""));
                                }
                            }
                        }

                        //get arable_land
                        if (line.TrimStart().StartsWith("arable_land")) {
                            s.arableLand = int.Parse(line.Split("=")[1].Trim());
                            count++;
                        }
                        //get arable_resources
                        if (line.TrimStart().StartsWith("arable_resources")) {
                            string[] resList = line.Split("=")[1].Replace("\"", "").Split(" ");
                            for (int i = 0; i < resList.Length; i++) {
                                if (resList[i].StartsWith("bg_")) {
                                    Resource r = new Resource(resList[i]);
                                    r.knownAmmount = s.arableLand;
                                    r.type = "agriculture";
                                    s.resoures.Add(r);
                                }
                            }
                        }
                        //get capped_resources
                        if (line.TrimStart().StartsWith("capped_resources")) {
                            cappedResourseFound = true;
                        }
                        if (cappedResourseFound) {
                            if (line.TrimStart().StartsWith("bg_")) {
                                string[] l2 = line.Replace("\"", "").Split("=");
                                Resource r = new Resource(l2[0].Trim());
                                r.knownAmmount = int.Parse(l2[1].Trim());
                                r.type = "resource";
                                s.resoures.Add(r);
                            }
                        }
                        //get discvorable resources
                        if (line.TrimStart().StartsWith("resource")) {
                            discoverableResourseFound = true;
                        }
                        if (discoverableResourseFound) {

                            if (line.TrimStart().StartsWith("type")) {
                                string[] l2 = line.Split("=");
                                dr = new Resource(l2[1].Trim().Replace("\"", ""));
                                dr.type = "discoverable";
                                s.resoures.Add(dr);
                            }
                            else if (line.TrimStart().StartsWith("undiscovered_amount")) {
                                string[] l2 = line.Split("=");
                                dr.discoverableAmmount = int.Parse(l2[1].Trim());
                            }
                            else if (line.TrimStart().StartsWith("amount") || line.TrimStart().StartsWith("discovered_amount")) {
                                string[] l2 = line.Split("=");
                                dr.knownAmmount = int.Parse(l2[1].Trim());
                            }
                        }
                        //get naval id
                        if (line.TrimStart().StartsWith("naval_exit_id")) {
                            string[] l2 = line.Split("=");
                            s.navalID = int.Parse(l2[1].Trim());
                        }

                        //get city color
                        if (line.TrimStart().StartsWith("city") || line.TrimStart().StartsWith("port") || line.TrimStart().StartsWith("farm") || line.TrimStart().StartsWith("mine") || line.TrimStart().StartsWith("wood")) {
                            string[] l2 = line.Split("=");
                            s.hubs.Add((l2[0].Trim(), ColorTranslator.FromHtml("#" + l2[1].Replace("\"", "").Replace("x", "").Trim())));
                            s.color = s.hubs[0].color;
                        }
                        //reset cappedResourseFound and discoverableResourseFound
                        if (line.Trim().StartsWith("}")) {
                            cappedResourseFound = false;
                            discoverableResourseFound = false;
                            traitsfound = false;
                        }

                    }
                }
            }

            Console.WriteLine("States: " + count);

        }
        //parse all region files
        void parseRegionFiles(List<Region> regionList) {
            string[] files = Directory.GetFiles(localDir + "/_Input/strategic_regions");

            int count = 0;
            foreach (string file in files) {
                if (file.EndsWith(".txt")) {
                    string[] lines = File.ReadAllLines(file);
                    Region r = new Region();
                    //Console.WriteLine(file);
                    foreach (string l1 in lines) {
                        string line = l1.Replace("=", " = ").Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Replace("  ", " ").Trim();

                        if (line.Trim().StartsWith("region_")) {
                            r = new Region(line.Split("=")[0].Trim());

                            //incase people are orverriding regions in latter files
                            //check if region with same name already exists in regionList and if so, delete it
                            foreach (Region region in regionList) {
                                if (region.name == r.name) {
                                    regionList.Remove(region);
                                    break;
                                }
                            }

                            regionList.Add(r);
                        }
                        else if (line.Trim().StartsWith("states")) {
                            string[] states = line.Split("=")[1].Replace("{", "").Replace("}", "").Split(" ");
                            for (int i = 0; i < states.Length; i++) {
                                if (states[i].StartsWith("STATE_")) {
                                    r.stateNames.Add(states[i]);
                                }
                            }
                        }
                        else if (line.Trim().StartsWith("map_color")) {
                            count++;
                            string[] e = line.Split("=")[1].Split(" ");

                            List<double> rgbValues = new List<double>();
                            
                            foreach (string s in e) {
                                //try parse float
                                if (double.TryParse(s, out double d)) {
                                    //if f is outsied of 0-255 range, then set it to 0 or 255
                                    if (d < 0) {
                                        d = 0;
                                    }
                                    else if (d > 255) {
                                        d = 255;
                                    }
                                    //if d is between 0 and 1.1 then multiply it by 255
                                    else if (d > 0 && d < 1.1) {
                                        d = d * 255;
                                    }
                                    rgbValues.Add(d);
                                }
                            }
                            //if rgbValues has less than 3 values, then add 128 to make it 3
                            while (rgbValues.Count < 3) {
                                rgbValues.Add(128);
                            }
                            
                            r.color = Color.FromArgb((int)rgbValues[0], (int)rgbValues[1], (int)rgbValues[2]);

                        }
                        else if (line.StartsWith("graphical_culture")){
                            r.gfxCulture = line.Split("=")[1].Replace("\"", "").Trim();
                        }
                    }
                }
            }
            Console.WriteLine("Regions: " + count);
        }


        //merge state into regions
        void mergeStateRegion(List<State> stateList, List<Region> regionList) {
            foreach (Region r in regionList) {
                foreach (State s in stateList) {
                    if (r.stateNames.Contains(s.name)) {
                        r.states.Add(s);
                        s.hexToColor();
                    }
                }
            }
        }

        //parse default.map
        void parseDefaultMap(List<string> seaList, List<string> lakeList) {
            string[] lines = File.ReadAllLines(localDir + "/_Input/default.map");
            bool seaStart = false;
            bool lakeStart = false;
            foreach (string line in lines) {
                if (line.Trim().StartsWith("sea_starts")) {
                    seaStart = true;
                }
                else if (line.Trim().StartsWith("lakes")) {
                    lakeStart = true;
                }
                if (seaStart) {
                    string[] l2 = line.Trim().Split(" ");
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("#")) {
                            break;
                        }
                        else if (l2[i].StartsWith("x")) {
                            seaList.Add(l2[i].Replace("x", ""));
                        }
                        else if (l2[i].StartsWith("}")) {
                            seaStart = false;
                            break;
                        }
                    }
                }
                if (lakeStart) {
                    string[] l2 = line.Trim().Split(" ");
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("#")) {
                            break;
                        }
                        else if (l2[i].StartsWith("x")) {
                            lakeList.Add(l2[i].Replace("x", ""));
                        }
                        else if (l2[i].StartsWith("}")) {
                            seaStart = false;
                            break;
                        }
                    }
                }
            }
        }

        //pares province png
        void parseProvMap(List<Region> regionList, List<string> seaList, List<string> lakeList) {
            //dictionary prov color to state object
            Dictionary<Color, State> provColorToState = new Dictionary<Color, State>();

            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    foreach (Color c in s.provColors) {
                        provColorToState.Add(c, s);
                    }
                }
            }
            
            Bitmap image = new Bitmap(localDir + "/_Input/provinces.png");
            Bitmap provBorder = new Bitmap(image.Width, image.Height);

            Console.WriteLine("Parsing Map");
            //parse image and get coords of each color and add them to the state and draw borders
            for (int i = 0; i < image.Width; i++) {                
                Color lastColor = image.GetPixel(i, 0);
                for (int j = 0; j < image.Height; j++) {
                    Color c = image.GetPixel(i, j);
                    //if c is in provColorToState add coord to state
                    if (provColorToState.ContainsKey(c)) provColorToState[c].coordList.Add((i, j));
                    if (c != lastColor) {
                        provBorder.SetPixel(i, j, Color.Black);
                        lastColor = c;
                    }
                }

                //progress bar every 25% with 0% and 100% mapping to 0% and 50% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + i * 100 / image.Width / 2 + "%");
                }

            }

            //draw vertical borders
            //Console.WriteLine("Drawing Vertical Borders for Prov Map");
            for (int i = 0; i < image.Height; i++) {
                Color lastColor = image.GetPixel(0, i);
                for (int j = 1; j < image.Width; j++) {
                    Color c = image.GetPixel(j, i);
                    if (c != lastColor) {
                        provBorder.SetPixel(j, i, Color.Black);
                        lastColor = c;
                    }
                }
                //progress bar every 25% with 0% and 100% mapping to 50% and 100% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + (i * 100 / image.Width / 2 + 50) + "%");
                }

            }

            //check if /_Output/BorderFrame exists if not add it
            if (!Directory.Exists(localDir + "/_Output/BorderFrame")) {
                Directory.CreateDirectory(localDir + "/_Output/BorderFrame");
            }

            //save map
            provBorder.Save(localDir + "/_Output/BorderFrame/prov_border.png");

            drawStateImages(regionList, image);
            List<(int, int)> waterCoordList = darwRegionImages(regionList, image);

            ((int, int) waterRecCenter, (int, int) waterRecSize) = drawRGOMaps(regionList, waterCoordList);

            mergeMaps();

            namedMapes(regionList);

            Console.WriteLine(sw.Elapsed);

            debugDrawRectangle(regionList, waterRecCenter, waterRecSize);

        }
        
        //draw state images
        void drawStateImages(List<Region> regionList, Bitmap image) {
            Bitmap stateImage = new Bitmap(image.Width, image.Height);
            Bitmap stateBorder = new Bitmap(image.Width, image.Height);
            Console.WriteLine("Drawing State Maps");
            foreach (Region r in regionList) {                               
                foreach (State s in r.states) {
                    foreach ((int, int) c in s.coordList) {
                        stateImage.SetPixel(c.Item1, c.Item2, s.color);
                    }
                }
            }

            //draw horizontal borders for state map            
            for (int i = 0; i < image.Width; i++) {
                Color lastColor = stateImage.GetPixel(i, 0);
                for (int j = 0; j < image.Height; j++) {
                    Color c = stateImage.GetPixel(i, j);
                    if (c != lastColor) {
                        stateBorder.SetPixel(i, j, Color.Black);
                        lastColor = c;
                    }
                }

                //progress bar every 25% with 0% and 100% mapping to 0% and 50% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + i * 100 / image.Width / 2 + "%");
                }
            }

            //draw vertical borders for state map
            for (int i = 0; i < image.Height; i++) {
                Color lastColor = stateImage.GetPixel(0, i);
                for (int j = 1; j < image.Width; j++) {
                    Color c = stateImage.GetPixel(j, i);
                    if (c != lastColor) {
                        stateBorder.SetPixel(j, i, Color.Black);
                        lastColor = c;
                    }
                }

                //progress bar every 25% with 0% and 100% mapping to 50% and 100% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + (i * 100 / image.Width / 2 + 50) + "%");
                }
            }

            //check if /_Output/ColorMap exists if not add it
            if (!Directory.Exists(localDir + "/_Output/ColorMap")) {
                Directory.CreateDirectory(localDir + "/_Output/ColorMap");
            }

            //save state images
            stateImage.Save(localDir + "/_Output/ColorMap/state_colors.png");
            stateBorder.Save(localDir + "/_Output/BorderFrame/state_border.png");


        }

        //draw region images
        List<(int, int)> darwRegionImages(List<Region> regionList, Bitmap image) {
            Bitmap regionImage = new Bitmap(image.Width, image.Height);
            Bitmap regionBorder = new Bitmap(image.Width, image.Height);
            Bitmap waterImage = new Bitmap(image.Width, image.Height);

            List<(int, int)> waterCoordList = new List<(int, int)>();


            Console.WriteLine("Drawing Region Maps");
            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    foreach ((int, int) c in s.coordList) {
                        regionImage.SetPixel(c.Item1, c.Item2, r.color);
                    }
                }
            }

            //draw horizontal borders for region map            
            for (int i = 0; i < image.Width; i++) {
                Color lastColor = regionImage.GetPixel(i, 0);
                for (int j = 0; j < image.Height; j++) {
                    Color c = regionImage.GetPixel(i, j);
                    if (c.A == 0) {
                        waterCoordList.Add((i, j));
                        waterImage.SetPixel(i, j, Color.LightBlue);
                    }
                    if (c != lastColor) {
                        regionBorder.SetPixel(i, j, Color.Black);
                        lastColor = c;
                    }
                }

                //progress bar every 25% with 0% and 100% mapping to 0% and 50% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + i * 100 / image.Width / 2 + "%");
                }
            }

            //draw vertical borders for region map
            for (int i = 0; i < image.Height; i++) {
                Color lastColor = regionImage.GetPixel(0, i);
                for (int j = 1; j < image.Width; j++) {
                    Color c = regionImage.GetPixel(j, i);
                    if (c != lastColor) {
                        regionBorder.SetPixel(j, i, Color.Black);
                        lastColor = c;
                    }
                }

                //progress bar every 25% with 0% and 100% mapping to 50% and 100% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + (i * 100 / image.Width / 2 + 50) + "%");
                }
            }

            //save region images
            regionImage.Save(localDir + "/_Output/ColorMap/region_colors.png");
            regionBorder.Save(localDir + "/_Output/BorderFrame/region_border.png");
            waterImage.Save(localDir + "/_Output/ColorMap/water_map.png");



            return waterCoordList;

        }

        //drawRGOMaps
        ((int, int) waterCenter, (int, int) waterMaxSize) drawRGOMaps(List<Region> regionList, List<(int, int)> waterCoordList) {
            //if Output/RGOs/ does not exist, create it
            if (!Directory.Exists(localDir + "/_Output/RGOs/")) {
                Directory.CreateDirectory(localDir + "/_Output/RGOs/");
            }

            List<string> rgoNames = setRGOColors(regionList);
            Bitmap image = new Bitmap(localDir + "/_Input/provinces.png");
            Bitmap water = new Bitmap(localDir + "/_Output/ColorMap/water_map.png");
            Bitmap stateBorder = new Bitmap(localDir + "/_Output/BorderFrame/state_border.png");

            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            PrivateFontCollection privateFontCollection = new PrivateFontCollection();
            privateFontCollection.AddFontFile(localDir + "/_Input/ParadoxVictorian-Condensed.otf"); //font for numbers and names

            //find the largest rectangle without holes in the water
            MaximumRectangle mr = new MaximumRectangle();
            ((int, int) waterCenter, (int, int) waterMaxRecSize) = ((0, 0), (0, 0));
            if (waterCoordList.Count > 0) {
                ( waterCenter, waterMaxRecSize) = mr.center(waterCoordList, false);
            }
            

            for (int i = 0; i < rgoNames.Count; i++) {
                string name = rgoNames[i];
                string wType = "";
                Color resColor = Color.FromArgb(255, 255, 255, 255);
                Color textColor = Color.FromArgb(255, 255, 255, 255);
                Console.WriteLine(name + "\t" + sw.Elapsed);
                Bitmap rgoMap = new Bitmap(image.Width, image.Height);
                Bitmap rgoName = new Bitmap(image.Width, image.Height);
                Graphics g = Graphics.FromImage(rgoMap);
                g.Clear(Color.White);
                g.DrawImage(water, Point.Empty);

                foreach (Region r in regionList) {
                    foreach (State s in r.states) {
                        foreach (Resource res in s.resoures) {
                            if (res.name.Contains(name)) {
                                //setpixel for each s.coords in rgoMap
                                foreach ((int, int) c in s.coordList) {
                                    rgoMap.SetPixel(c.Item1, c.Item2, res.color);
                                }
                            }
                        }
                    }
                }

                g.DrawImage(stateBorder, Point.Empty);

                foreach (Region r in regionList) {
                    foreach (State s in r.states) {
                        foreach (Resource res in s.resoures) {
                            if (res.name.Contains(name)) {
                                wType = res.type;
                                //write text  
                                string val = "";
                                if (res.type.Equals("agriculture")) {
                                    val = s.arableLand.ToString();
                                }
                                else {
                                    if (res.knownAmmount > 0) {
                                        val += res.knownAmmount;
                                    }
                                    if (res.discoverableAmmount > 0) {
                                        if (res.knownAmmount > 0) {
                                            val += "|";
                                        }
                                        val += "(" + res.discoverableAmmount + ")";
                                    }
                                }


                                bool gotRectangularBox = false;
                                if (val.Length > 4) { //for those cases where the number would look better in a long rectangle than a square
                                    s.getCenter2();
                                    gotRectangularBox = true;
                                    Console.WriteLine("\t" + res.name + " in " + s.name + " switching to rectange");
                                }


                                int numberFontSize = 8; //minimum font size for number
                                Font font1 = new Font(privateFontCollection.Families[0], numberFontSize);

                                //check pixel size of font1
                                SizeF size1 = g.MeasureString(val, font1);
                                //if size1 is smaller than state maxRecSize then increase font size to fit
                                while (size1.Width < s.maxRecSize.Item1 && size1.Height < s.maxRecSize.Item2) {
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
                                    while (size1.Width < s.maxRecSize.Item1 && size1.Height < s.maxRecSize.Item2) {
                                        numberFontSize++;
                                        font1 = new Font("Verdana", numberFontSize);
                                        size1 = g.MeasureString(val, font1);
                                    }
                                    numberFontSize = (int)(numberFontSize * 1.2);
                                    font1 = new Font("Verdana", numberFontSize, FontStyle.Bold);

                                }

                                resColor = res.color;
                                textColor = res.textColor;
                                g.DrawString(val, font1, new SolidBrush(res.textColor), new Point(s.center.Item1, s.center.Item2), stringFormat);

                                if (gotRectangularBox) { //revert back to square for the rest of the res in that state
                                    s.getCenter2(true);
                                }

                            }
                        }
                    }
                }

                List<string> tmpName = name.Replace("bg_", "").Split("_").ToList();
                string wName = "";
                for (int j = 0; j < tmpName.Count; j++) {
                    string tmpWord = tmpName[j][0].ToString().ToUpper() + tmpName[j].Substring(1);
                    wName += tmpWord + " ";
                }
                //wName += "("+wType+")";
                //wNameList new list containing wName
                List<string> wNameList = new List<string>();
                wNameList.Add(wName);
                wNameList.Add("(" + wType + ")");

                //draw solid rectangle centered on waterCenter with size of waterMaxRecSize and color of Black (DEBUG)
                //g.FillRectangle(new SolidBrush(Color.Black), waterCenter.Item1 - waterMaxRecSize.Item1 / 2, waterCenter.Item2 - waterMaxRecSize.Item2 / 2, waterMaxRecSize.Item1, waterMaxRecSize.Item2);

                //scale font2 to fit inside waterMaxRecSize
                int fontSize = 200; //minimum font size for name
                Font font2 = new Font(privateFontCollection.Families[0], fontSize);

                //check pixel size of font2
                SizeF size2 = g.MeasureString(wNameList[0], font2);
                //if size2 is smaller than waterMaxRecSize then increase font size to fit
                while (size2.Width < waterMaxRecSize.Item1 && size2.Height < (int)(waterMaxRecSize.Item2 * 0.8)) {
                    fontSize++;
                    font2 = new Font(privateFontCollection.Families[0], fontSize);
                    size2 = g.MeasureString(name, font2);
                }

                //check if single line wName would be bigger
                string wName2 = wNameList[0] + " " + wNameList[1];
                int fontSize2 = 200; //minimum font size for name
                Font font3 = new Font(privateFontCollection.Families[0], fontSize2);
                SizeF size3 = g.MeasureString(wName2, font3);
                while (size3.Width < waterMaxRecSize.Item1 && size3.Height < (int)(waterMaxRecSize.Item2 * 1.3)) {
                    fontSize2++;
                    font3 = new Font(privateFontCollection.Families[0], fontSize2);
                    size3 = g.MeasureString(wName2, font3);
                }

                //if single line wName would be bigger then use 2 lines
                if (fontSize > fontSize2) {

                    //draw all names in wNameList to rgoName image and move them down by Xpx each time
                    int y = waterCenter.Item2 - (int)(size2.Height * 0.15);
                    foreach (string s in wNameList) {
                        g.DrawString(s, font2, new SolidBrush(resColor), new Point(waterCenter.Item1, y), stringFormat);

                        //border outline
                        GraphicsPath p = new GraphicsPath();
                        p.AddString(
                            s,             // text to draw
                            privateFontCollection.Families[0],  // or any other font family
                            (int)FontStyle.Regular,      // font style (bold, italic, etc.)
                            g.DpiY * font2.Size / 72,       // em size
                            new Point(waterCenter.Item1, y),              // location where to draw text
                            stringFormat);          // set options here (e.g. center alignment)
                        Pen p1 = new Pen(textColor, 4);
                        g.DrawPath(p1, p);

                        y += (int)(size2.Height * 0.5);
                    }
                }
                else {
                    int y = waterCenter.Item2 + (int)(size3.Height * 0.1);

                    g.DrawString(wName2, font3, new SolidBrush(resColor), new Point(waterCenter.Item1, y), stringFormat);

                    //border outline
                    GraphicsPath p = new GraphicsPath();
                    p.AddString(
                        wName2,             // text to draw
                        privateFontCollection.Families[0],  // or any other font family
                        (int)FontStyle.Regular,      // font style (bold, italic, etc.)
                        g.DpiY * font3.Size / 72,       // em size
                        new Point(waterCenter.Item1, y),              // location where to draw text
                        stringFormat);          // set options here (e.g. center alignment)
                    Pen p1 = new Pen(textColor, 4);
                    g.DrawPath(p1, p);

                }



                rgoMap.Save(localDir + "/_Output/RGOs/" + name.Replace("bg_", "") + ".png");
                rgoMap.Dispose();
            }

            return (waterCenter, waterMaxRecSize);

        }

        //set RGO Colors
        List<string> setRGOColors(List<Region> regionList) {
            List<string> rgoList = new List<string>();

            List<string> ignoreList = new List<string>();
            ignoreList.Add("bg_monuments");
            ignoreList.Add("bg_skyscraper");

            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    foreach (Resource res in s.resoures) {
                        if (s.center == (0, 0)) {
                            s.getCenter2(true);
                        }
                        ColorList(res);
                        if (!rgoList.Contains(res.name) && !ignoreList.Contains(res.name)) {
                            rgoList.Add(res.name);
                        }
                        else if (ignoreList.Contains(res.name)) {
                            Console.WriteLine("\t\tIgnoring " + res.name + " in " + s.name);
                        }
                    }
                }
            }
            return rgoList;
        }

        //RGO Colors
        void ColorList(Resource res) {
            if (res.name.Contains("gold") || res.name.Contains("sulfur")) {
                res.color = Color.Gold;
                res.textColor = Color.DarkBlue;
            }
            else if (res.name.Contains("farms") || res.name.Contains("banana")) {
                res.color = Color.Yellow;
                res.textColor = Color.Brown;
            }
            else if (res.name.Contains("oil_") || res.name.Contains("coal_")) {
                res.color = Color.FromArgb(255, 37, 37, 37);
                res.textColor = Color.Red;
            }
            else if (res.name.Contains("coffee_") || res.name.Contains("ranches")) {
                res.color = Color.SaddleBrown;
                res.textColor = Color.LimeGreen;
            }
            else if (res.name.Contains("cotton") || res.name.Contains("sugar")) {
                res.color = Color.FromArgb(255, 85, 188, 187);
                res.textColor = Color.DarkViolet;
            }
            else if (res.name.Contains("dye_") || res.name.Contains("silk_")) {
                res.color = Color.DarkViolet;
                res.textColor = Color.FromArgb(255, 85, 188, 187);
            }
            else if (res.name.Contains("logging") || res.name.Contains("rubber")) {
                res.color = Color.BurlyWood;
                res.textColor = Color.DarkGreen;
            }
            else if (res.name.Contains("plantation")) {
                res.color = Color.Green;
                res.textColor = Color.Purple;
            }
            else if (res.name.Contains("mining")) {
                res.color = Color.SlateGray;
                res.textColor = Color.Brown;
            }
            else if (res.name.Contains("fish") || res.name.Contains("whal")) {
                res.color = Color.DarkCyan;
                res.textColor = Color.FromArgb(255, 0, 0, 64);
            }
        }

        //merge maps
        void mergeMaps() {
            //if Output/BlankMap/ does not exist create it
            if (!Directory.Exists(localDir + "/_Output/BlankMap/")) {
                Directory.CreateDirectory(localDir + "/_Output/BlankMap/");
            }

            Bitmap waterColor = new Bitmap(localDir + "/_Output/ColorMap/water_map.png");
            Bitmap regionColor = new Bitmap(localDir + "/_Output/ColorMap/region_colors.png");
            Bitmap regionBorder = new Bitmap(localDir + "/_Output/BorderFrame/region_border.png");

            //merge 3 maps together into new image
            for (int i = 0; i < regionColor.Height; i++) {
                for (int j = 0; j < regionColor.Width; j++) {
                    if (waterColor.GetPixel(j, i).A != 0) {
                        regionColor.SetPixel(j, i, waterColor.GetPixel(j, i));
                    }
                    if (regionBorder.GetPixel(j, i).A != 0) {
                        regionColor.SetPixel(j, i, regionBorder.GetPixel(j, i));
                    }
                }
            }
            regionColor.Save(localDir + "/_Output/Region_Map.png");
            regionColor.Dispose();
            regionBorder.Dispose();

            Console.WriteLine("Merged Region Map\t" + sw.Elapsed);

            Bitmap stateColor = new Bitmap(localDir + "/_Output/ColorMap/state_colors.png");
            Bitmap stateBorder = new Bitmap(localDir + "/_Output/BorderFrame/state_border.png");

            //merge 3 maps together into new image
            for (int i = 0; i < stateColor.Height; i++) {
                for (int j = 0; j < stateColor.Width; j++) {
                    if (waterColor.GetPixel(j, i).A != 0) {
                        stateColor.SetPixel(j, i, waterColor.GetPixel(j, i));
                    }
                    if (stateBorder.GetPixel(j, i).A != 0) {
                        stateColor.SetPixel(j, i, stateBorder.GetPixel(j, i));
                    }
                }
            }
            stateColor.Save(localDir + "/_Output/State_Map.png");
            stateColor.Dispose();

            Console.WriteLine("Merged State Map\t" + sw.Elapsed);

            Bitmap blankProv = new Bitmap(waterColor.Width, waterColor.Height);
            Bitmap bp = new Bitmap(localDir + "/_Output/BorderFrame/prov_border.png");
            Graphics g = Graphics.FromImage(blankProv);
            g.Clear(Color.White);
            g.DrawImage(waterColor, Point.Empty);
            g.DrawImage(bp, Point.Empty);
            blankProv.Save(localDir + "/_Output/BlankMap/Province_Blank.png");
            blankProv.Dispose();
            bp.Dispose();
            Console.WriteLine("Merged Blank Prov Map\t" + sw.Elapsed);

            Bitmap blankState = new Bitmap(waterColor.Width, waterColor.Height);
            Bitmap bs = new Bitmap(localDir + "/_Output/BorderFrame/state_border.png");
            g = Graphics.FromImage(blankState);
            g.Clear(Color.White);
            g.DrawImage(waterColor, Point.Empty);
            g.DrawImage(bs, Point.Empty);
            blankState.Save(localDir + "/_Output/BlankMap/State_Blank.png");
            blankState.Dispose();
            bs.Dispose();
            Console.WriteLine("Merged Blank State Map\t" + sw.Elapsed);

            Bitmap blankRegion = new Bitmap(waterColor.Width, waterColor.Height);
            Bitmap br = new Bitmap(localDir + "/_Output/BorderFrame/region_border.png");
            g = Graphics.FromImage(blankRegion);
            g.Clear(Color.White);
            g.DrawImage(waterColor, Point.Empty);
            g.DrawImage(br, Point.Empty);
            blankRegion.Save(localDir + "/_Output/BlankMap/Region_Blank.png");
            blankRegion.Dispose();
            br.Dispose();
            g.Dispose();
            Console.WriteLine("Merged Blank Region Map\t" + sw.Elapsed);
        }

        //write names on merged maps
        void namedMapes(List<Region> regionList) {
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;
            PrivateFontCollection privateFontCollection = new PrivateFontCollection();
            privateFontCollection.AddFontFile(localDir + "/_Input/ParadoxVictorian-Condensed.otf"); //font for region names


            //region map
            Bitmap regionMap = new Bitmap(localDir + "/_Output/Region_Map.png");
            Graphics g = Graphics.FromImage(regionMap);


            for (int i = 0; i < regionList.Count; i++) {
                if (regionList[i].color != Color.FromArgb(0, 0, 0, 0)) {    //no ocean/sea names

                    regionList[i].getCenter2();

                    List<string> tmpName = regionList[i].name.Replace("region_", "").Split("_").ToList();
                    List<string> wName = new List<string>();
                    for (int j = 0; j < tmpName.Count; j++) {
                        string tmpWord = tmpName[j][0].ToString().ToUpper() + tmpName[j].Substring(1).ToLower();
                        wName.Add(tmpWord);
                    }

                    //if region maxRecSize width is atleast 2.5x the height merge wName into one line
                    if (regionList[i].maxRecSize.Item1 >= regionList[i].maxRecSize.Item2 * 2) {
                        string tmp = "";
                        for (int j = 0; j < wName.Count; j++) {
                            tmp += wName[j] + " ";
                        }
                        tmp = tmp.Trim();
                        wName.Clear();
                        wName.Add(tmp);
                    }

                    int numberFontSize = 25; //minimum font size for region name
                    Font font1 = new Font(privateFontCollection.Families[0], numberFontSize);

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

                    bool fontTooSmall = false;
                    SizeF size1 = g.MeasureString(longestName, font1);
                    //if size1 is smaller than region maxRecSize then increase font size to fit
                    while (size1.Width < regionList[i].maxRecSize.Item1 * 1.2 && size1.Height < regionList[i].maxRecSize.Item2 * wName.Count * vertBias) {
                        numberFontSize++;
                        font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                        size1 = g.MeasureString(longestName, font1);
                    }

                    //for each word in wName draw it on the map and move down by size1.Height/2
                    int y = 0;
                    if (wName.Count > 1) {
                        y = regionList[i].center.Item2 - (int)(size1.Height * 0.35);
                    }
                    else {
                        y = regionList[i].center.Item2;
                    }

                    //if all 3 colors are less than 100, make text white
                    Color textColor = Color.Black;

                    if (rgbToYIQ(regionList[i].color) < 90) {
                        textColor = Color.White;
                    }

                    for (int j = 0; j < wName.Count; j++) {
                        g.DrawString(wName[j], font1, new SolidBrush(textColor), new Point(regionList[i].center.Item1, y), stringFormat);
                        y += (int)(size1.Height * 0.6);
                    }


                }
            }

            //save regionMap as png
            regionMap.Save(localDir + "/_Output/Region_Map_Names.png");



            //state map
            Bitmap stateMap = new Bitmap(localDir + "/_Output/State_Map.png");


            g = Graphics.FromImage(stateMap);


            for (int i = 0; i < regionList.Count; i++) {
                for (int j = 0; j < regionList[i].states.Count; j++) {
                    regionList[i].states[j].getCenter2();
                    if (regionList[i].color != Color.FromArgb(0, 0, 0, 0)) {    //no ocean/sea names
                        List<string> tmpName = regionList[i].states[j].name.Replace("STATE_", "").Split("_").ToList();
                        List<string> wName = new List<string>();
                        int wLength = -1;
                        for (int k = 0; k < tmpName.Count; k++) {
                            string tmpWord = tmpName[k][0].ToString().ToUpper() + tmpName[k].Substring(1).ToLower();
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
                            string tmp1 = wName[0].Substring(0, spaceIndex);
                            string tmp2 = wName[0].Substring(spaceIndex + 1);
                            wName.Clear();
                            wName.Add(tmp1);
                            wName.Add(tmp2);

                            Console.WriteLine(wName[0] + " " + wName[0].Length + "\t" + wName[1] + " " + wName[1].Length);
                        }


                        //else if region maxRecSize width is atleast 2.1x the height merge wName into one line
                        else if ((regionList[i].states[j].maxRecSize.Item1 >= regionList[i].states[j].maxRecSize.Item2 * 2.1 || wLength < 8) && wName.Count > 1) {
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
                        Font font2 = new Font("Verdna", numberFontSize);

                        SizeF size1 = g.MeasureString(longestName, font2);
                        double vertBias = 1.2;
                        if (wName.Count > 1) {
                            vertBias = 1.0;
                        }
                        bool fontTooSmall = false;
                        //if size1 is smaller than region maxRecSize then increase font size to fit
                        while (size1.Width < regionList[i].states[j].maxRecSize.Item1 && size1.Height < regionList[i].states[j].maxRecSize.Item2 * wName.Count * vertBias) {
                            numberFontSize++;
                            font2 = new Font("Verdna", numberFontSize);
                            size1 = g.MeasureString(longestName, font2);
                        }

                        int y = 0;
                        if (wName.Count > 2) {
                            y = regionList[i].states[j].center.Item2 - (int)(size1.Height * 3 / 4);
                        }
                        else if (wName.Count > 1) {
                            y = regionList[i].states[j].center.Item2 - (int)(size1.Height * 3 / 8);
                        }
                        else {
                            y = regionList[i].states[j].center.Item2;
                        }

                        //if all 3 colors are less than 100, make text white
                        Color textColor = Color.DarkBlue;

                        //check color of state with rgbToYIQ if less than 128 make text white
                        if (rgbToYIQ(regionList[i].states[j].color) < 90) {
                            textColor = Color.White;
                        }


                        for (int k = 0; k < wName.Count; k++) {
                            g.DrawString(wName[k], font2, new SolidBrush(textColor), new Point(regionList[i].states[j].center.Item1, y), stringFormat);
                            y += (int)(size1.Height * 0.7);
                        }
                    }
                }
            }

            //save stateMap as png
            stateMap.Save(localDir + "/_Output/State_Map_Names.png");
        }

        float rgbToYIQ(Color c) {
            return (c.R * 299 + c.G * 587 + c.B * 114) / 1000;
        }

        //debug draw rectange around each region and state
        void debugDrawRectangle(List<Region> regionList, (int, int) waterRecCenter, (int, int) waterRecSize) {
            //if Output/Debug/ does not exist create it
            if (!Directory.Exists(localDir + "/_Output/Debug/")) {
                Directory.CreateDirectory(localDir + "/_Output/Debug/");
            }

            //creat a new blank region map
            Bitmap regionMap = new Bitmap(localDir + "/_Output/BlankMap/Region_Blank.png");
            Graphics g = Graphics.FromImage(regionMap);

            for (int i = 0; i < regionList.Count; i++) {
                //fill a solid rectangle of size maxRecSize and color regionList[i].color centered on regionList[i].center
                g.FillRectangle(new SolidBrush(regionList[i].color), regionList[i].center.Item1 - regionList[i].maxRecSize.Item1 / 2, regionList[i].center.Item2 - regionList[i].maxRecSize.Item2 / 2, regionList[i].maxRecSize.Item1, regionList[i].maxRecSize.Item2);

            }
            //fill a solid rectangle of size for water
            g.FillRectangle(new SolidBrush(Color.Black), waterRecCenter.Item1 - waterRecSize.Item1 / 2, waterRecCenter.Item2 - waterRecSize.Item2 / 2, waterRecSize.Item1, waterRecSize.Item2);


            //save regionMap as png
            regionMap.Save(localDir + "/_Output/Debug/Region_Rectangles.png");
            regionMap.Dispose();

            //state map
            Bitmap stateMap = new Bitmap(localDir + "/_Output/BlankMap/State_Blank.png");
            g = Graphics.FromImage(stateMap);

            for (int i = 0; i < regionList.Count; i++) {
                for (int j = 0; j < regionList[i].states.Count; j++) {
                    //fill a solid rectangle of size maxRecSize and color regionList[i].color centered on regionList[i].state[j].center
                    g.FillRectangle(new SolidBrush(regionList[i].states[j].color), regionList[i].states[j].center.Item1 - regionList[i].states[j].maxRecSize.Item1 / 2, regionList[i].states[j].center.Item2 - regionList[i].states[j].maxRecSize.Item2 / 2, regionList[i].states[j].maxRecSize.Item1, regionList[i].states[j].maxRecSize.Item2);
                }
            }
            //fill a solid rectangle of size for water
            g.FillRectangle(new SolidBrush(Color.Black), waterRecCenter.Item1 - waterRecSize.Item1 / 2, waterRecCenter.Item2 - waterRecSize.Item2 / 2, waterRecSize.Item1, waterRecSize.Item2);

            //save stateMap as png
            stateMap.Save(localDir + "/_Output/Debug/State_Rectangles.png");
            stateMap.Dispose();


            //state square
            Bitmap stateSquareMap = new Bitmap(localDir + "/_Output/BlankMap/State_Blank.png");
            g = Graphics.FromImage(stateSquareMap);
            for (int i = 0; i < regionList.Count; i++) {
                for (int j = 0; j < regionList[i].states.Count; j++) {
                    regionList[i].states[j].getCenter2(true);
                    //fill a solid rectangle of size maxRecSize and color regionList[i].color centered on regionList[i].state[j].center
                    g.FillRectangle(new SolidBrush(regionList[i].states[j].color), regionList[i].states[j].center.Item1 - regionList[i].states[j].maxRecSize.Item1 / 2, regionList[i].states[j].center.Item2 - regionList[i].states[j].maxRecSize.Item2 / 2, regionList[i].states[j].maxRecSize.Item1, regionList[i].states[j].maxRecSize.Item2);
                }
            }
            //fill a solid rectangle of size for water
            g.FillRectangle(new SolidBrush(Color.Black), waterRecCenter.Item1 - waterRecSize.Item1 / 2, waterRecCenter.Item2 - waterRecSize.Item2 / 2, waterRecSize.Item1, waterRecSize.Item2);

            //save stateMap as png
            stateSquareMap.Save(localDir + "/_Output/Debug/State_Square.png");
            stateSquareMap.Dispose();

        }

        
    }
}