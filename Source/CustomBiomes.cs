/************************************************************************************
 * Custom Biomes v1.7.0                                                             *
 * This is the core class for the CustomBiomes mod for Kerbal Space Program.        *
 * It is  licensed under a Creative Commons Attribution-NonCommercial-ShareAlike    *
 * 3.0 Unported License and based on the game produced and under copywrite to Squad.*
 *                                                                                  *                                                            
 * This mod uses Majiir's version compatability checker, found in:                  *
 * CompatabilityChecker.cs                                                          *
 * This mod uses toadicus and TriggerAu's toolbar wrapper to make use of blizzy's   *
 * toolbar optional. Found in ToolbarWrapper.cs                                     *
 *                                                                                  *
 * Aaron Port (Trueborn) December 2014                                              *
 * *********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.IO;

namespace CustomBiomes
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]      //Don't show anything unless in flight.
    public class CustomBiomes  : MonoBehaviour
    {
        private ToolbarButtonWrapper _biomeButton;       //toolbar button wrapper
        private ApplicationLauncherButton _appButton;    //launcher button
        private bool _useLauncher;          //Should we use Blizzy's toolbar, or the stock launcher?
        private bool _initialized = false;  //Track initialization so it only gets used once
        private bool _enableGUI;            //Is the GUI enabled at all?
        private bool _isMinimized;          //Is the window displayed?
        private bool _mapMinimized;         //Is the map window displayed?
        private bool _debug = false;        //Prints additional data to the console if true
        private Rect _mainWindow;           //Location and size of the main window
        private Rect _mapWindow;            //Location and size of the map window
        private bool _hasInitStyles;        //Just checks to see if the styles have been initialized
        private GUIStyle _windowStyle, _labelStyle, _boldStyle, _buttonStyle, _IconStyle, _toggleStyle;         //Styles for the windows and labels
        private int _currentBiome;          //The name of the biome the window is looking at right now
        private String _lastMainBody;       //Used for checking to see if we changed SOI's
        private String[] _biomes;           //All the biomes currently loaded in the game
        private bool _replaced=false;       //True if the biomes have been replaced, false if not
        private string[] _saveFiles;        //A list of the save game direcories
        private string[] _setFolders;       //A list of the biome sets detected in the directory
        private string _defaultSets;        //Which set of biomes should get loaded for each save?
        private Dictionary<String, String> _saveDictionary; //Fast lookup of default sets
        private List<sciresult> _resultList; //Storage for my expirament injects
        private int _windowTab;              //Which "tab" the main window is displaying
        private int _lastTab;                //Used to decide if we need to resize the menu window
        private int _lastBiome=-1;              //Used to decied if we need to recreate the biome texture
        private Texture2D _map;              //You mean I don't have to regenerate the texture each frame?
        private bool rdInjected=false;       //Have we succuessfully injected our results to R&D?
        private static String _versionString = "1.7.0";     //Current version of this plugin
        private static int _compatibleMajorVersion = 0;     //What major version of KSP does this plugin work on?
        private static int _compatibleMinorVersion = 90;    //What minor version of KSP does this plugin work on?
        private static int _compatibleRevisionVersion = 0;  //What revision of KSP does this plugin work on?
        //private static bool _disabled;      //If this is true, the entire plugin is disabled.
        
        public static String VersionString { get { return _versionString; } }
        public static int CompatibleMajorVersion { get { return _compatibleMajorVersion; } }
        public static int CompatibleMinorVersion { get { return _compatibleMinorVersion; } }
        public static int CompatibleRevisionVersion { get { return _compatibleRevisionVersion; } }

        //Applauncher instead of toolbar
        public ApplicationLauncherButton StockButton;


        //This runs upon entering the flight scene.
        public void Awake()
        {
            //only run once
            if (_initialized) return;
            _initialized = true;

            Debug.Log("(CB) Starting Custom Biomes v" + _versionString);
            //Check compatability first
            if (!CompatibilityChecker.IsCompatible())
            {
                Debug.Log("(CB) Custom Biomes may be incompatible with the installed version of KSP.  Please visit the KSP forums for more information.");
                string buf = "Version "+VersionString+" of Custom Biomes is not designed for with KSP version"+_compatibleMajorVersion+"."+_compatibleMinorVersion;
                ScreenMessage click = new ScreenMessage(buf, 10, ScreenMessageStyle.UPPER_CENTER);
                ScreenMessages.PostScreenMessage(click);
                //_disabled = true;
                //return;
            }
            //_disabled = false;
            //Load persistant configuration data
            if (_debug) Debug.Log("(CB) Loading configuration data...");
            LoadMe();
            if (_debug) Debug.Log("(CB) Loading complete.");
            //Get the save files in this installation
            char dsc = Path.DirectorySeparatorChar;
            char asc = Path.AltDirectorySeparatorChar;
            //Debug.Log("DSC: " + dsc + " ASC: " + asc);
            //Debug.Log("KSPUtil: "+KSPUtil.ApplicationRootPath);
            String currentsave = Application.dataPath;
            if(_debug) Debug.Log("(CB) DataPath: " + currentsave);
            currentsave = currentsave.Remove(currentsave.LastIndexOf(asc));
            if (_debug) Debug.Log("(CB) Trimmed: " + currentsave);
            if (currentsave.Substring(currentsave.LastIndexOf(asc)).Contains("KSP.app"))
            {
                currentsave = currentsave.Remove(currentsave.LastIndexOf(asc));
                //Debug.Log("Trimmed2: " + currentsave);
            }
            //currentsave = currentsave.Remove(currentsave.LastIndexOf(asc));
            currentsave = currentsave + asc+"saves";
            if (_debug) Debug.Log("(CB) Saves: " + currentsave);
            _saveFiles = Directory.GetDirectories(currentsave);
            //Strip the first part of the directory off the save names
            for (int i = 0; i < _saveFiles.Length; i++)
                _saveFiles[i] = _saveFiles[i].Substring(_saveFiles[i].LastIndexOf(dsc) + 1);
            //List the biome sets in this installation
            currentsave = Application.dataPath;
            currentsave = currentsave.Remove(currentsave.LastIndexOf(asc));
            if (currentsave.Substring(currentsave.LastIndexOf(asc)).Contains("KSP.app"))
            {
                currentsave = currentsave.Remove(currentsave.LastIndexOf(asc));
            }
            currentsave = currentsave + asc + "GameData" + asc + "CustomBiomes" + asc + "PluginData" + asc + "CustomBiomes";
            if (_debug) Debug.Log("(CB) Full Path: " + currentsave);
            //currentsave = Directory.GetCurrentDirectory() + dsc+"GameData"+dsc+"CustomBiomes"+dsc+"PluginData"+dsc+"CustomBiomes";
            _setFolders = Directory.GetDirectories(currentsave);
            currentsave = HighLogic.SaveFolder;
            //Strip the leading directory info off the set names
            for (int i = 0; i < _setFolders.Length; i++)
                _setFolders[i] = _setFolders[i].Substring(_setFolders[i].LastIndexOf(dsc) + 1);
            //Set up the defaults list
            String[] ds = null;
            if (!_defaultSets.Equals(""))
                ds = _defaultSets.Split(';');
            //_saveDefaults = new string[_saveFiles.Length, 2];
            _saveDictionary = new Dictionary<string,string>();
            _resultList = new List<sciresult>();
            string s, d;
            //Look at each save file in this install
            for (int i = 0; i < _saveFiles.Length; i++)
            {
                //_saveDefaults[i, 0] = _saveFiles[i];
                //look through the default sets string to see if we can find a match
                if (ds != null)
                {
                    bool found = false;
                    for (int j = 0; j < ds.Length; j++)
                    {
                        //Split the "save|biome set" combination into 2 strings
                        split(ds[j], out s, out d);
                        if (s.Equals(_saveFiles[i]))
                        {
                            if (_debug) Debug.Log("(CB) " + d + " is the default set for " + s);
                            found = true;
                            //_saveDefaults[i, 1] = d;
                            _saveDictionary.Add(_saveFiles[i], d);
                            break;
                        }
                    }
                    if (!found) //Set basic to default if this save doesn't have a default ie, new save detected
                    {
                        if (_debug) Debug.Log("(CB) Default set not found for" + _saveFiles[i]);
                        //_saveDefaults[i, 1] = "Basic";
                        _saveDictionary.Add(_saveFiles[i], "Basic");
                    }
                }
                else   //If no defaults were set, use the Basic pack as the default
                {
                    if (_debug) Debug.Log("(CB) No defaults found, setting all saves to 'Basic' set.");
                    _saveDictionary.Add(_saveFiles[i], "Basic");
                }
                //If this is the active save, load the default biome set
                if (_saveFiles[i].Equals(currentsave) && !_replaced)
                {
                    string def;
                    if (_saveDictionary.TryGetValue(_saveFiles[i], out def))
                        ReplaceBiomes(def);
                    else
                        ReplaceBiomes("Basic");
                }
            }
            _defaultSets = buildDefaultsString();
            SaveMe();            
            bool loaded = ToolbarDLL.Loaded;
            if ((_useLauncher || !loaded) && _enableGUI)
            {
                //Stock toolbar a.k.a App Launcher
                _appButton = ApplicationLauncher.Instance.AddModApplication(
                    () => {
                        if (_debug) Debug.Log("(CB) CustomBiomes menu toggled.");
                             _isMinimized = !_isMinimized;
                             SaveMe();
                          },
                    () => {
                        if (_debug) Debug.Log("(CB) CustomBiomes menu toggled.");
                            _isMinimized = !_isMinimized;
                            SaveMe();
                          },
                    null,
                    null,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.FLIGHT,
                    GameDatabase.Instance.GetTexture("CustomBiomes/BiomeIcon", false)
                    );
            }
            else if (_enableGUI)
            {
                //Blizzy's toolbar buton setup, using wrapper
                if (_debug) Debug.Log("(CB) Setting up Custom Biomes toolbar button...");
                 _biomeButton = new ToolbarButtonWrapper("CustomBiomes", "Biomes1");
                _biomeButton.TexturePath = "CustomBiomes/biome";
                _biomeButton.ToolTip = "CustomBiomes Menu";
                _biomeButton.SetButtonVisibility(GameScenes.FLIGHT);
                _biomeButton.AddButtonClickHandler((e) =>
                    {
                        if (_debug) Debug.Log("(CB) CustomBiomes menu toggled.");
                        _isMinimized = !_isMinimized;
                        SaveMe();
                    });
            }
            //This needs to get drawn from here on out
            if (_debug) Debug.Log("(CB) Adding to post draw queue...");
            RenderingManager.AddToPostDrawQueue(3, OnDraw);
            _mainWindow.height = 300;    //Reset height since we don't save which biome was active
            _mainWindow.width = 350;
            _mapWindow.height = 10;
            _mapWindow.width = 50;

            if (_debug) Debug.Log("(CB) Initializing GUI styles...");
            //Initialize styles, if not already done
            if (!_hasInitStyles) initStyles();

            if (_debug) Debug.Log("(CB) Determining current biome...");
            //Now load the names of all the biomes into an array
            String _lastMainBody = Planetarium.fetch.CurrentMainBody.name;
            for (int i = 0; i < _biomes.Length; i++)
            {
                if (_biomes[i].Equals(_lastMainBody))
                {
                    _currentBiome = i;
                    break;
                }
            }
            _lastTab = -1;
            Debug.Log("(CB) Biome replacement complete. Custom Biomes initialization complete.");
        }

        //Cleans up buttons on scene change
        private void OnDestroy()
        {
            if (_appButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(_appButton);
            _biomeButton.Destroy();
            if (_debug) Debug.Log("(CB) Custom Biomes button destroyed.");
        }

        //This initializes some styles for drawing the GUI
        private void initStyles()
        {
            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.stretchHeight = true;
            _windowStyle.stretchWidth = true;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _labelStyle.stretchWidth = true;
            _toggleStyle = new GUIStyle(HighLogic.Skin.toggle);
            _boldStyle = new GUIStyle(HighLogic.Skin.label);
            _boldStyle.fontStyle = FontStyle.Bold;
            _boldStyle.fontSize = 16;                          
            _boldStyle.stretchWidth = true;
            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _hasInitStyles = true;
            _IconStyle = new GUIStyle();                       
        }

        //Splits the source string into the save and set parts
        private void split(String source, out String save, out String set)
        {
            try
            {
                int index = source.IndexOf('|');
                save = source.Remove(index);
                set = source.Substring(index + 1);
            }
            catch
            {
                if (_debug) Debug.Log("(CB) Could not split " + source);
                save = "";
                set = "";
            }
        }

        //Attempts to inject the stuff that should have been in sciencedefs to begin with
        private void rdInject()
        {
            ScienceExperiment temp = null;
            try
            {
                List<String> ids = ResearchAndDevelopment.GetExperimentIDs();
                if (_debug) Debug.Log("(CB) Injecting experimental results...");
                ScienceExperiment exp;
                foreach (String id in ids)
                {
                    exp = ResearchAndDevelopment.GetExperiment(id);
                    if (id.Equals("temperatureScan"))
                        temp = exp;
                    foreach (sciresult res in _resultList)
                    {
                        if (res.expid.Equals(id) && !exp.Results.ContainsKey(res.biome))
                        {
                            exp.Results.Add(res.biome, res.res);
                        }
                       // else
                            //if (_debug) Debug.Log(res.biome + " already exists for " + exp.experimentTitle);
                    }
                    if (_debug) Debug.Log("(CB) "+exp.experimentTitle);
                    //foreach (String key in exp.Results.Keys)
                        //Debug.Log(key + " | " + exp.Results[key]);
                }
                Debug.Log("(CB) R&D injection complete.");
                rdInjected = true;

            }
            catch
            {
                Debug.Log("(CB) Error attempting R&D injection.");
            }
        }

        //This gets called every frame, even though I really only need it once or twice
        private void Update()
        {
            //if (!rdInjected && !_disabled)
            if (!rdInjected)
                rdInject();
        }

        //This method handles the OnDraw call, as well as screen clamping
        private void OnDraw()
        {
            //Draw the main window, if not minimized
            if (!_isMinimized && _enableGUI)
            {
                //Keep main window on screen
                if ((_mainWindow.xMin + _mainWindow.width) < 20) _mainWindow.xMin = 20 - _mainWindow.width; //left limit
                if (_mainWindow.yMin + _mainWindow.height < 20) _mainWindow.yMin = 20 - _mainWindow.height; //top limit
                if (_mainWindow.xMin > Screen.width - 20) _mainWindow.xMin = Screen.width - 20;   //right limit
                if (_mainWindow.yMin > Screen.height - 20) _mainWindow.yMin = Screen.height - 20; //bottom limit
                String title = "CustomBiomes " + _versionString;
                _mainWindow = GUILayout.Window(8930, _mainWindow, OnWindow, title, _windowStyle);
            }
            //Draw map window, if not minimized, disabled, or main window minimized
            if (!_mapMinimized && !_isMinimized && _enableGUI)
            {
                //Keep map window on screen
                if ((_mapWindow.xMin + _mapWindow.width) < 20) _mapWindow.xMin = 20 - _mapWindow.width; //left limit
                if (_mapWindow.yMin + _mapWindow.height < 20) _mapWindow.yMin = 20 - _mapWindow.height; //top limit
                if (_mapWindow.xMin > Screen.width - 20) _mapWindow.xMin = Screen.width - 20;   //right limit
                if (_mapWindow.yMin > Screen.height - 20) _mapWindow.yMin = Screen.height - 20; //bottom limit
                _mapWindow = GUILayout.Window(8931, _mapWindow, OnMap, _biomes[_currentBiome]+" Biome Map", _windowStyle);
            }
        }

        //Draws and drags the actual window
        private void OnWindow(int WindowID)
        {
            //Draw "tab" selection buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_windowTab == 0, "Biome List", _buttonStyle))
            {
                _windowTab = 0;
                if (_windowTab != _lastTab)
                {
                    _lastTab = 0;
                    _mainWindow.height = 10;    //Reset becuase different tabs are different heights
                    SaveMe();
                }
            }
            if (GUILayout.Toggle(_windowTab == 1, "Default Sets", _buttonStyle))
            {
                _windowTab = 1;
                if (_windowTab != _lastTab)
                {
                    _lastTab = 1;
                    _mainWindow.height = 10;
                    SaveMe();
                }
            }
            if (GUILayout.Toggle(_windowTab == 2, "Biome Sets", _buttonStyle))
            {
                _windowTab = 2;
                if (_windowTab != _lastTab)
                {
                    _lastTab = 2;
                    _mainWindow.height = 10;
                    SaveMe();
                }
            }
            GUILayout.EndHorizontal();
            //Draw the appropriate "tab"
            if (_windowTab == 0)
            {
                drawAtributeTab();
            }
            else if (_windowTab == 1)
            {
                drawSaveTab();
            }
            else if (_windowTab == 2)
                drawSetTab();
            //Add dragability
            GUI.DragWindow();
            //Save if we moved
            if (GUI.changed)
                SaveMe();
        }

        //Draws the tab that displays the list of biomes and attributes for this body
        private void drawAtributeTab()
        {
            int last = _currentBiome;
            //Check for SOI change
            if (!FlightGlobals.currentMainBody.name.Equals(_lastMainBody))
            {
                _lastMainBody = FlightGlobals.currentMainBody.name;
                for (int i = 0; i < _biomes.Length; i++)
                {
                    if (_biomes[i].Equals(_lastMainBody))
                    {
                        _currentBiome = i;
                        break;
                    }
                }
            }
            //Load the current biome
            if (_debug) Debug.Log("(CB) Loading biome attributes for attributes tab...");
            CBAttributeMapSO biome = LoadBiome(_biomes[_currentBiome]);
            
            GUILayout.BeginHorizontal();
            //Do the next and prev buttons
            if (GUILayout.Button("<<", _buttonStyle))
            {
                _currentBiome--;
                if (_currentBiome < 0) _currentBiome = _biomes.Length - 1;
                //_mainWindow.height = 300;    //Reset window height so it resizes correctly for each biome
                _mapWindow.height = 10;
                //_mapWindow.width = 2000;
                if (_debug) Debug.Log("(CB) Current biome is now " + _biomes[_currentBiome]);
            }
            if (GUILayout.Button(">>", _buttonStyle))
            {
                _currentBiome++;
                if (_currentBiome >= _biomes.Length) _currentBiome = 0;
                //_mainWindow.height = 300;    //Reset window height so it resizes correctly for each biome
                _mapWindow.height = 10;
                //_mapWindow.width = 200;
                if (_debug) Debug.Log("(CB) Current biome is now " + _biomes[_currentBiome]);
            }
            //If we changed biomes, reset window height, width
            if (_currentBiome != last)
            {
                _mainWindow.height = 10;
                _mainWindow.width = 10;
            }
            GUILayout.EndHorizontal();
            if (biome != null)
            {
                GUILayout.Label("Biome Attributes for " + _biomes[_currentBiome], _labelStyle);
                //_mainWindow.height = 100+biome.Attributes.Length * 20;
                foreach (CBAttributeMapSO.MapAttribute att in biome.Attributes)
                {
                    if (att != null)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(att.name + " | " + att.value + " | " + att.mapColor.ToString());
                        Color tmp = GUI.color;
                        GUI.color = att.mapColor;
                        GUILayout.Box("  ", _buttonStyle, GUILayout.Width(20));
                        GUI.color = tmp;
                        GUILayout.EndHorizontal();
                        //Debug.Log(att.name);
                    }
                }
                if (biome.exactSearch)
                    GUILayout.Label("Exact Threshold: " + biome.nonExactThreshold);
                if (_biomes[_currentBiome].Equals(FlightGlobals.currentMainBody.name))
                {
                    double lat, lon;
                    lat = FlightGlobals.ship_latitude;
                    lon = FlightGlobals.ship_longitude;
                    CBAttributeMapSO.MapAttribute ma = FlightGlobals.currentMainBody.BiomeMap.GetAtt(lat * Mathf.Deg2Rad, lon * Mathf.Deg2Rad);
                    GUILayout.Label("Current Biome: " + ma.name, _labelStyle);
                }
            }
            else
                GUILayout.Label("Blank biome at " + _biomes[_currentBiome], _labelStyle);
            if (GUILayout.Button("Toggle Map", _buttonStyle))
            {
                SaveMe();
                _mapMinimized = !_mapMinimized;
            }
        }

        //Draws the tab that displays the list of save files in this install, and allows them to be disabled
        private void drawSaveTab()
        {
            //Display a line for each save file with the save name and a disable checkbox
            for (int i = 0; i < _saveFiles.Length; i++)
            {
                GUILayout.Label(_saveFiles[i], _labelStyle, GUILayout.Width(200));
                GUILayout.BeginHorizontal();
                foreach (String s in _setFolders)
                {
                    if (GUILayout.Toggle(s.Equals(_saveDictionary[_saveFiles[i]]),s, _buttonStyle, GUILayout.Width(60)))
                    {
                        _saveDictionary.Remove(_saveFiles[i]);
                        _saveDictionary.Add(_saveFiles[i], s);
                        _defaultSets = buildDefaultsString();
                    }
                }
                GUILayout.EndHorizontal();
            }
            //Draws the button to toggle between blizzy or stock buttons
            GUILayout.Label("Use stock toolbar?  This will take affect after a scene change.", _labelStyle);
            String str;
            if (_useLauncher)
                str = "Use Stock Toolbar";
            else
                str = "Use Blizzy's Toolbar";
            if(GUILayout.Button(str, _buttonStyle, GUILayout.Width(140)))
            {
                _useLauncher = !_useLauncher;
                SaveMe();
            }
        }

        //Builds a string representing all the save files and their default sets
        private string buildDefaultsString()
        {
            string temp = "";
            foreach (var pair in _saveDictionary)
            {
                temp = temp + pair.Key + "|" + pair.Value + ";";
            }
            return temp;
        }

        //Draws the tab that displays a list of available sets, and allows them to be selected
        private void drawSetTab()
        {
            foreach (String s in _setFolders)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(s, _labelStyle);
                if (GUILayout.Button("Load Biome Set", _buttonStyle, GUILayout.Width(110)))
                {
                    ReplaceBiomes(s);
                }
                GUILayout.EndHorizontal();
            }
        }

        //Returns a biome matching the provided name, or null if not found
        private CBAttributeMapSO LoadBiome(String name)
        {
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.name.Equals(name))
                    return body.BiomeMap;
            }
            return null;
        }

        //Loads and replaces biomes
        private void ReplaceBiomes(String set)
        {
            if (_debug) Debug.Log("(CB) Searching for replacement biomes in "+set);
            _replaced = true;
            _biomes = new String[FlightGlobals.Bodies.Count];
            int i = 0;
            char asc = Path.AltDirectorySeparatorChar;
            char dsc = Path.AltDirectorySeparatorChar;
            String app = Application.dataPath;
            //if (_debug) Debug.Log("(CB) App datapath: "+app);
            app = app.Remove(app.LastIndexOf(dsc)+1);
            if (_debug) Debug.Log("(CB) App: " + app);
            if (app.Contains("KSP.app"))
            {
                if (_debug) Debug.Log("(CB) KSP.app detected, attempting OSX fix.");
                app = app.Remove(app.LastIndexOf(dsc));
                app = app.Remove(app.LastIndexOf(dsc)+1);
                if (_debug) Debug.Log("(CB) App: " + app);
            }
            string path = app + "GameData" + asc + "CustomBiomes" + asc + "PluginData" + asc + "CustomBiomes" + asc + set + asc;
            if (_debug) Debug.Log("(CB) Path: " + path);
            //String path = Directory.GetCurrentDirectory() + dsc + "GameData" + dsc + "CustomBiomes" + dsc + "PluginData" + dsc + "CustomBiomes" + dsc + set + dsc;
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                String name = body.name;
                /*if (name.Length > 3)
                {
                    if (name.Substring(0,3).Contains("the"))
                    {
                        name = name.Remove(0, 4);
                        if (_debug) Debug.Log("(CB) 'the' removed from "+name);
                    }
                } */
                if (_debug) Debug.Log("(CB) Looking at "+name);
                _biomes[i] = name;
                i++;
                CBAttributeMapSO biome = body.BiomeMap;
                
                //Try loading a texture file
                try
                {
                    byte[] bytes;
                    if (_debug) Debug.Log("(CB) Looking for " + name + ".png...");
                    System.IO.FileStream file = new System.IO.FileStream(path + name + ".png", System.IO.FileMode.Open);
                    System.IO.BinaryReader reader = new System.IO.BinaryReader(file);
                    bytes = reader.ReadBytes((int)file.Length);
                    if (_debug) Debug.Log("(CB) Replacing " + name + "'s biome map.");
                    Texture2D tex = new Texture2D(1024, 512);
                    tex.LoadImage(bytes);
                    biome.CreateMap(MapSO.MapDepth.RGB, tex);
                    reader.Close();
                    file.Close();
                }
                catch
                {
                    //Don't actually care...
                    if (_debug) Debug.Log("(CB) No map found.");
                }
                //Try loading an attribute file
                try
                {
                    if (_debug) Debug.Log("(CB) Looking for "+name+".att ...");
                    System.IO.FileStream file = new System.IO.FileStream(path + name + ".att", System.IO.FileMode.Open);
                    System.IO.StreamReader reader = new StreamReader(file);
                    if (_debug) Debug.Log("(CB) Replacing " + name + "'s biome attributes.");
                    String locale;
                    float a, r, g, b,e;
                    e = float.Parse(reader.ReadLine());
                    if (e != 0.0)
                    {
                        biome.nonExactThreshold = e;
                        biome.exactSearch = true;
                    }
                    else
                        biome.exactSearch = false;
                    CBAttributeMapSO.MapAttribute[] attributes = new CBAttributeMapSO.MapAttribute[0];
                    while (!reader.EndOfStream)
                    {
                        locale = reader.ReadLine();
                        CBAttributeMapSO.MapAttribute[] old = new CBAttributeMapSO.MapAttribute[0];
                        old = attributes;
                        attributes = new CBAttributeMapSO.MapAttribute[old.Length + 1];
                        old.CopyTo(attributes, 0);
                        a = float.Parse(reader.ReadLine());
                        if (a > 1) a = a / 255f;
                        r = float.Parse(reader.ReadLine());
                        if (r > 1) r = r / 255f;
                        g = float.Parse(reader.ReadLine());
                        if (g > 1) g = g / 255f;
                        b = float.Parse(reader.ReadLine());
                        if (b > 1) b = b / 255f;
                        CBAttributeMapSO.MapAttribute temp = new CBAttributeMapSO.MapAttribute();
                        temp.name = locale;
                        temp.mapColor = new Color(r, g, b, a);
                        temp.value = a;
                        attributes[attributes.Length - 1] = temp;
                    }
                    biome.Attributes = attributes;
                    //biome.defaultAttribute = attributes[0]; //Seems as good as any
                    reader.Close(); //Close my files
                    file.Close();
                }
                catch
                {
                    if (_debug) Debug.Log("(CB) No attributes found.");
                }
            }
            try //to read the science results
            {
                if (_debug) Debug.Log("(CB) Looking for " + "ScienceResults.txt ...");
                System.IO.FileStream file = new System.IO.FileStream(path +"ScienceResults.txt", System.IO.FileMode.Open);
                System.IO.StreamReader reader = new StreamReader(file);
                if (_debug) Debug.Log("(CB) Parsing Science Results...");
                readResults(reader);
            }
            catch
            { //No science results file.
            }
        }

        //A simple struct for my "thre dimensional" list
        struct sciresult {
            public String expid, biome, res;
            public sciresult(String e, String b, String r)
            {
                expid = e;
                biome = b;
                res = r;
            }
        };

        //Gets the experiment results from the attribute file, and stores them for later injection
        private void readResults(System.IO.StreamReader reader)
        {
            string buff;
            string expid = "";
            while(!reader.EndOfStream)
            {
                buff = reader.ReadLine();
                if (buff.Contains("id = "))
                {
                    expid = buff.Substring(buff.IndexOf('=') + 2);
                }
                else
                {
                    String[] splt = buff.Split('=');
                    sciresult r = new sciresult(expid, splt[0].Trim(),splt[1].Trim());
                    _resultList.Add(r);
                }
            }
        }
        
        //Draws and drags the map window
        private void OnMap(int WindowID)
        {
            if ((_lastBiome == -1) || (_lastBiome != _currentBiome) || _map == null)
            {
                if (_debug) Debug.Log("(CB) Compiling to texture()");
                _map = LoadBiome(_biomes[_currentBiome]).CompileToTexture();
                _lastBiome = _currentBiome;
            }
            
            if (_map != null)
            {
                GUILayout.Box(_map,GUILayout.MaxWidth(800),GUILayout.MaxHeight(600));    //resizes, sloppily
                if (_biomes[_currentBiome].Equals(FlightGlobals.currentMainBody.name))
                {
                    float lat, lon;
                    lat = (float)FlightGlobals.ship_latitude;
                    lon = (float)FlightGlobals.ship_longitude;
                    //Debug.Log("Lat/Long " + Math.Round(lat, 2) + ", " + Math.Round(lon, 2));
                    //conert lat,long into x,y
                    //Lat goes from 90 to -90
                    lat += 90;
                    lat /= 180;
                    lat *= 512;
                    lat = 512 - lat;
                    lat += 20;  //Roughly accounts for the window title
                    //Lon goes from 0 to 360...except it doesn't, so normalize it first
                    lon -= 90;  //This corrects for something
                    while (lon > 360) lon -= 360;
                    while (lon < 0) lon += 360;
                    lon /= 360;
                    lon *= 1024;
                    lon += 10;  //Roughly accounts for window border
                    //Debug.Log("(X,Y) (" + Math.Round(lon, 2) + ", " + Math.Round(lat, 2) + ")");
                    GUI.Label(new Rect(lon, lat, 10, 10), "X", _labelStyle);
                }
            }
            else
            {
                _mapWindow.width = 100;
                GUILayout.Label("There is no biome map for " + _biomes[_currentBiome]);
            }
            GUI.DragWindow();
            if (GUI.changed)
                SaveMe();
        }

        //Save persistant data to the config file
        public void SaveMe()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<CustomBiomes>();
            config.SetValue("WindowPosition", _mainWindow);
            config.SetValue("WindowMinimized", _isMinimized);
            config.SetValue("MapPosition", _mapWindow);
            config.SetValue("MapMinimized", _mapMinimized);
            config.SetValue("WindowTab", _windowTab);
            config.SetValue("DefaultSets", _defaultSets);
            config.SetValue("GUIEnabled", _enableGUI);
            config.SetValue("UseLauncher", _useLauncher);
            config.save();
        }

        //Load persistant data from the config file
        public void LoadMe()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<CustomBiomes>();
            config.load();
            _mainWindow = config.GetValue<Rect>("WindowPosition", new Rect(50, 100, 350, 100));
            _mapWindow = config.GetValue<Rect>("MapPosition", new Rect(450, 100, 512, 256));
            _isMinimized = config.GetValue<bool>("WindowMinimized", true);
            _mapMinimized = config.GetValue<bool>("MapMinimized", true);
            _windowTab = config.GetValue<int>("WindowTab", 0);
            _defaultSets = config.GetValue<string>("DefaultSets", "");
            _enableGUI = config.GetValue<bool>("GUIEnabled", true);
            _useLauncher = config.GetValue<bool>("UseLauncher", true);
        }
    }
}
