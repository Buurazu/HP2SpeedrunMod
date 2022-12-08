using System;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using DG.Tweening.Core;
using System.Threading.Tasks;
using DG.Tweening;

namespace HP2SpeedrunMod
{
    /// <summary>
    /// The base plugin type that adds HuniePop-specific functionality over the default <see cref="BaseUnityPlugin"/>.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class HP2SR : BaseUnityPlugin
    {
        /// <summary>
        /// The version of this plugin.
        /// </summary>
        public const string PluginVersion = "2.6";

        //no item list yet
        //public static Dictionary<string, int> ItemNameList = new Dictionary<string, int>();

        public static ConfigEntry<String> MouseKeys { get; private set; }
        public static ConfigEntry<String> ControllerKeys { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ResetKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ResetKey2 { get; private set; }
        public static ConfigEntry<KeyboardShortcut> CheatHotkey { get; private set; }
        public static ConfigEntry<Boolean> CheatSpeedEnabled { get; private set; }
        public static ConfigEntry<Boolean> CensorshipEnabled { get; private set; }
        public static ConfigEntry<Boolean> OutfitCensorshipEnabled { get; private set; }
        //public static ConfigEntry<Boolean> BraPantiesCensorshipEnabled { get; private set; }
        public static ConfigEntry<Boolean> SexSFXCensorshipEnabled { get; private set; }
        public static ConfigEntry<Boolean> AllPairsEnabled { get; private set; }
        public static ConfigEntry<Boolean> SpecialPairsEnabled { get; private set; }
        public static ConfigEntry<Boolean> MouseWheelEnabled { get; private set; }
        public static ConfigEntry<Boolean> HorizVertEnabled { get; private set; }
        public static ConfigEntry<int> AutoDeleteFile { get; private set; }
        public static ConfigEntry<Boolean> InGameTimer { get; private set; }
        public static ConfigEntry<int> SplitRules { get; private set; }
        public static ConfigEntry<Boolean> VsyncEnabled { get; private set; }
        public static ConfigEntry<int> FramerateCap { get; private set; }
        //public static ConfigEntry<Boolean> CapAt144 { get; private set; }
        //public static ConfigEntry<Boolean> RerollForLillian { get; private set; }

        //hasReturned is used to display "This is for practice purposes" after a return to main menu, until you start a new file
        public static bool unloadingByHotkey = false;
        public static bool hasReturned = false;
        public static bool cheatsEnabled = false;
        public static bool savingDisabled = false;
        public static bool nudePatch = false;

        public const int UNKNOWN = 0;
        public const int ONE = 1;
        public const int TWO = 2;

        public const int QUICKTRANSITIONS = 13;
        public const int ABIAHAIR = 14;

        public static bool newVersionAvailable = false;
        public static bool alertedOfUpdate = false;

        public static UiTooltipSimple tooltip = null;
        public static Stopwatch tooltipTimer = new Stopwatch();
        public static int tooltipLength = 0;

        public static int KyuHairstyle = 1;
        public static int KyuOutfit = 1;

        public static RunTimer run;
        public static bool splitThisDate = false;
        GirlPairRelationshipType startingRelationshipType;
        int startingCompletedPairs;

        public static int lastChosenCategory = 0;
        public static int lastChosenDifficulty = 0;

        private void Awake()
        {
            VsyncEnabled = Config.Bind(
                "Settings", nameof(VsyncEnabled),
                true,
                "Enable or disable Vsync. The FPS cap below will only take effect with it disabled");
            FramerateCap = Config.Bind(
                "Settings", nameof(FramerateCap),
                144,
                "Set your framerate cap when Vsync is off. Valid values: 60, 120, 144, 170, 240, 300, 360. Higher FPS can help max mash speed, but it also means bonus round affection drains faster");

            MouseWheelEnabled = Config.Bind(
                "Settings", nameof(MouseWheelEnabled),
                true,
                "Enable or disable the mouse wheel being treated as a click");
            HorizVertEnabled = Config.Bind(
                "Settings", nameof(HorizVertEnabled),
                true,
                "Enable or disable Unity's Horizontal/Vertical axis being treated as a click (this includes WASD, Arrow Keys, and a controller's Left Control Stick)");
            MouseKeys = Config.Bind(
                "Settings", nameof(MouseKeys),
                "Q, E",
                "The keys that will be treated as a click (set to None for no keyboard clicks)\nNote: WASD/arrows are part of the Horizontal/Vertical Axis check");
            ControllerKeys = Config.Bind(
                "Settings", nameof(ControllerKeys),
                "JoystickButton0, JoystickButton1, JoystickButton2, JoystickButton3",
                "The controller buttons that will be treated as a click (set to None for no controller clicks)");

            ResetKey = Config.Bind(
                "Settings", nameof(ResetKey),
                new KeyboardShortcut(KeyCode.F4),
                "The hotkey to use for going back to the title (set to None to disable)");
            ResetKey2 = Config.Bind(
                "Settings", nameof(ResetKey2),
                new KeyboardShortcut(KeyCode.None),
                "Alternate hotkey to use for going back to the title (set to None to disable)");
            CheatHotkey = Config.Bind(
                "Settings", nameof(CheatHotkey),
                new KeyboardShortcut(KeyCode.C),
                "The hotkey to use for activating Cheat Mode on the title screen (set to None to disable)");
            CheatSpeedEnabled = Config.Bind(
                "Settings", nameof(CheatSpeedEnabled),
                true,
                "Enable or disable Cheat Mode skipping the tutorial and speeding up transitions");

            CensorshipEnabled = Config.Bind(
                "Settings", nameof(CensorshipEnabled),
                true,
                "Enable or disable the main censorship mods that make the game SFW (note: all censorship options are only active when the in-game setting is Bras & Panties)");
            OutfitCensorshipEnabled = Config.Bind(
                "Settings", nameof(OutfitCensorshipEnabled),
                true,
                "Enable or disable risque outfits being replaced with default outfit");
            SexSFXCensorshipEnabled = Config.Bind(
                "Settings", nameof(SexSFXCensorshipEnabled),
                false,
                "Enable or disable muting girls' moans during Bonus Round");

            InGameTimer = Config.Bind(
                "Settings", nameof(InGameTimer),
                true,
                "Enable or disable the built-in timer (shows your time on the affection meter after each date, read the readme for more info)");
            /*RerollForLillian = Config.Bind(
                "Settings", nameof(RerollForLillian),
                true,
                "Force Lillian+Ashley to appear on Afternoon 1 in 1 Wing runs (Requires the in-game timer, so it knows you're doing 1 Wing)");*/
            SplitRules = Config.Bind(
                "Settings", nameof(SplitRules),
                0,
                "0 = Split on every date/bonus, 1 = Split only after dates, 2 = Split only after bonus rounds\n(You may want to delete your run comparison/golds after changing this. 1 Wing is excluded from this option)");
            
            AutoDeleteFile = Config.Bind(
                "Settings", nameof(AutoDeleteFile),
                4,
                "The file that will be erased on new game if all files are full");

            AllPairsEnabled = Config.Bind(
                "Settings", nameof(AllPairsEnabled),
                false,
                "Enable or disable all 66 pairs showing up");
            SpecialPairsEnabled = Config.Bind(
                "Settings", nameof(SpecialPairsEnabled),
                false,
                "Include Kyu/Moxie/Jewn pairs");
        }

        void Start()
        {
            //I can't believe the game doesn't run in background by default
            Application.runInBackground = true;
            //allow max 144fps
            if (!VsyncEnabled.Value)
            {
                QualitySettings.vSyncCount = 0;
                if (FramerateCap.Value != 60 && FramerateCap.Value != 120 && FramerateCap.Value != 144 &&
                    FramerateCap.Value != 170 && FramerateCap.Value != 240 && FramerateCap.Value != 300 && FramerateCap.Value != 360)
                    FramerateCap.Value = 144;
                Application.targetFrameRate = FramerateCap.Value;
            }

            //Create the splits files for the first time if they don't exist
            if (!System.IO.Directory.Exists("splits"))
            {
                System.IO.Directory.CreateDirectory("splits");
                System.IO.Directory.CreateDirectory("splits/data");
            }

            //Check for a new update
            WebClient client = new WebClient();
            try
            {
                string reply = client.DownloadString("https://pastebin.com/raw/5z5PsCqr");

                if (reply != PluginVersion)
                    newVersionAvailable = true;
            }
            catch (Exception e) { Logger.LogMessage("Couldn't read the update pastebin! " + e.ToString()); }

            //initiate the variable used for autosplitting
            Harmony.CreateAndPatchAll(typeof(BasePatches), null); BasePatches.InitSearchForMe();
            Harmony.CreateAndPatchAll(typeof(CensorshipPatches), null);
            Harmony.CreateAndPatchAll(typeof(InputPatches), null);
            if (InGameTimer.Value) Harmony.CreateAndPatchAll(typeof(RunTimerPatches), null);
            if (AllPairsEnabled.Value) Harmony.CreateAndPatchAll(typeof(AllPairsPatches), null);

            string both = MouseKeys.Value + "," + ControllerKeys.Value;
            string[] keys = both.Split(',');
            string validKeycodes = "Mouse button bound to keys/buttons: ";
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = keys[i].Trim();
                KeyCode kc = KeyCode.None;
                try
                {
                    kc = (KeyCode)System.Enum.Parse(typeof(KeyCode), keys[i]);
                }
                catch { Logger.LogMessage(keys[i] + " is not a valid keycode name!"); }
                if (kc != KeyCode.None)
                {
                    InputPatches.mouseKeyboardKeys.Add(kc);
                    validKeycodes += keys[i] + ", ";
                }
            }
            Logger.LogMessage(validKeycodes);
        }

        public static int GameVersion()
        {
            switch (Game.Manager.buildVersion)
            {
                //eventually, a version update will break something
                //and i will be wholly unprepared
                case ("1.0.0"):
                case ("1.0.1"):
                case ("1.0.2"):
                case ("1.0.3"):
                    return ONE;
                default: return ONE;

            }
        }

        void PlayGirlAudio(int i, int j, int k = -1, int l = -1)
        {
            if (l >= 0) Game.Manager.Audio.Play(AudioCategory.VOICE, Game.Data.Cutscenes.GetAll()[i].steps[j].branches[k].steps[l].dialogLine.audioClip, null, 0.5f);
            else if (k >= 0) Game.Manager.Audio.Play(AudioCategory.VOICE, Game.Data.DialogTriggers.GetAll()[i].dialogLineSets[j].dialogLines[k].audioClip, null, 0.5f);
            else Game.Manager.Audio.Play(AudioCategory.VOICE, Game.Data.Cutscenes.GetAll()[i].steps[j].dialogLine.audioClip, null, 0.5f);
        }

        void PlayCheatLine(int line = -1)
        {
            int version = GameVersion(); //0 = jan23, 1 = valentines
            int r;
            if (line == -1)
            {
                System.Random rand = new System.Random();
                int max = 13;
                if (Game.Persistence.playerFile.storyProgress >= 14) max = 15;
                r = rand.Next(max);
            }
            else r = line;
            switch (r)
            {
                case 0: //Kyu: Busted!
                    if (version == ONE) PlayGirlAudio(78, 35);
                    break;
                case 1: //Lola: Cheat day
                    if (version == ONE) PlayGirlAudio(41, 1, 4);
                    break;
                case 2: //Jessie: Why am I not surprised
                    if (version == ONE) PlayGirlAudio(53, 2, 2);
                    break;
                case 3: //Lillian: That's kinda pathetic
                    if (version == ONE) PlayGirlAudio(53, 3, 1);
                    break;
                case 4: //Zoey: Just how is that helpful
                    if (version == ONE) PlayGirlAudio(9, 4, 0);
                    break;
                case 5: //Sarah: You stupid dummy head
                    if (version == ONE) PlayGirlAudio(9, 5, 1);
                    break;
                case 6: //Lailani: Can you please stop that
                    if (version == ONE) PlayGirlAudio(9, 6, 1);
                    break;
                case 7: //Candy: Quit being such a poop
                    if (version == ONE) PlayGirlAudio(9, 7, 2);
                    break;
                case 8: //Nora: If you're trying to impress me with that, no
                    if (version == ONE) PlayGirlAudio(26, 8, 1);
                    break;
                case 9: //Brooke: just like my husband
                    if (version == ONE) PlayGirlAudio(9, 9, 0);
                    break;
                case 10: //Ashley: What a fucking loser
                    if (version == ONE) PlayGirlAudio(53, 10, 3);
                    break;
                case 11: //Abia: gagging
                    if (version == ONE) PlayGirlAudio(42, 11, 0);
                    break;
                case 12: //Polly: Boys will be boys
                    if (version == ONE) PlayGirlAudio(53, 12, 5);
                    break;
                case 13: //Moxie: Is this your hero
                    if (version == ONE) PlayGirlAudio(9, 14, 1);
                    break;
                case 14: //Jewn: How much more disappointment are we to tolerate
                    if (version == ONE) PlayGirlAudio(24, 15, 0);
                    break;
                default:
                    break;
            }
        }

        void SetKyuOutfit(int num, bool hairstyle = false)
        {
            num = Mathf.Clamp(num, 0, 5);
            if (hairstyle) KyuHairstyle = num;
            else KyuOutfit = num;
        }
        public bool UnloadGame()
        {
            //exceptions to when we are allowed to unload
            if (Game.Manager.Ui.currentCanvas.titleCanvas) { Logger.LogDebug("titleCanvas = no unloading"); return false; }
            if (Game.Session.Location.isTraveling) { Logger.LogDebug("isTraveling = no unloading"); return false; }
            if ((bool)AccessTools.Field(typeof(UiGameCanvas), "_unloadingGame").GetValue(Game.Session.gameCanvas)) { Logger.LogDebug("_unloadingGame = no unloading"); return false; }

            unloadingByHotkey = true;
            Game.Session.gameCanvas.UnloadGame();
            return true;
        }

        public static void ShowThreeNotif(string s)
        {
            ShowNotif(s, 0, true);
            ShowNotif(s, 1, false);
            ShowNotif(s, 2, true);
        }
        public static void ShowNotif(string s, int pos = 1, bool silent = false)
        {
            if (Game.Manager.Ui.currentCanvas.titleCanvas)
            {
                ShowTooltip(s, 3000);
                return;
            }
            UiDoll doll = Game.Session.gameCanvas.dollLeft;
            if (pos == 1) doll = Game.Session.gameCanvas.dollMiddle;
            else if (pos == 2) doll = Game.Session.gameCanvas.dollRight;
            doll.notificationBox.Show(s, 0, silent);
        }

        public static void ShowTooltip(string s, int length, int xpos = 0, int ypos = 30) {
            tooltipTimer.Reset();
            tooltip = Game.Manager.Ui.GetTooltip<UiTooltipSimple>(TooltipType.SIMPLE);
            tooltip.Populate(s);
            tooltip.Show(new Vector2(xpos, ypos));
            tooltipLength = length;
            tooltipTimer.Start();
        }

        private void OnApplicationQuit()
        {
            //save golds on the way out
            if (run != null)
                run.reset();
        }
        private void Update() //called by Unity every frame
        {
            if (!Game.Manager) return; //don't run update code before Game.Manager exists
            BasePatches.Update(); CheatPatches.Update(); InputPatches.Update(); RunTimerPatches.Update();

            if (tooltip != null)
            {
                if (tooltipTimer.ElapsedMilliseconds > tooltipLength)
                {
                    tooltipTimer.Reset();
                    tooltip.Hide();
                    tooltip = null;
                }
            }
            //if tooltip became null, stop timer
            else tooltipTimer.Reset();

            //Check for the Return hotkey
            if (ResetKey.Value.IsDown() || ResetKey2.Value.IsDown())
            {
                if (UnloadGame())
                {
                    hasReturned = true;
                    BasePatches.searchForMe = -111;
                    if (run != null)
                    {
                        run.reset();
                        run = null;
                    }
                }
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                //display the splits folder on Ctrl+S
                if (Input.GetKeyDown(KeyCode.S))
                {
                    if (Game.Manager.Ui.currentCanvas.titleCanvas)
                        System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + "/splits");
                    /*
                    else if (run != null)
                    {
                        run.save();
                        ShowNotif("Run saved!", 2);
                    }*/
                    //don't allow saving midrun, could be done accidentally
                }
                //reset run on Ctrl+R
                if (Input.GetKeyDown(KeyCode.R) && run != null && run.category != "")
                {
                    run.reset(true);
                    ShowNotif("Run reset!", 2);
                }
                //quit run on Ctrl+Q
                if (Input.GetKeyDown(KeyCode.Q) && run != null && run.category != "")
                {
                    run.reset(false);
                    ShowNotif("Run quit!", 2);
                }
            }

                //if currently in a puzzle, and the status bar is up, and it's not nonstop mode, send data to autosplitters
                if (!Game.Manager.Ui.currentCanvas.titleCanvas && Game.Session && Game.Session.Puzzle.isPuzzleActive
                && !Game.Session.gameCanvas.cellphone.isOpen && Game.Session.Puzzle.puzzleStatus.statusType != PuzzleStatusType.NONSTOP)
            {
                //this is a possibly bold but necessary assumption to make about cellphone._currentApp
                UiCellphoneAppStatus status = (UiCellphoneAppStatus)AccessTools.Field(typeof(UiCellphone), "_currentApp").GetValue(Game.Session.gameCanvas.cellphone);
                bool isBonusRound = Game.Session.Puzzle.puzzleStatus.bonusRound;
                if (status.affectionMeter.currentValue == 0)
                {
                    startingRelationshipType = Game.Persistence.playerFile.GetPlayerFileGirlPair(Game.Session.Location.currentGirlPair).relationshipType;
                    startingCompletedPairs = Game.Persistence.playerFile.completedGirlPairs.Count;
                }
                if (status.affectionMeter.currentValue == status.affectionMeter.maxValue &&
                    (Game.Session.gameCanvas.puzzleGrid.roundState == PuzzleRoundState.SUCCESS || isBonusRound))
                {
                    if (!splitThisDate && run != null)
                    {
                        bool didSplit = false;
                        //always split for the two tutorial splits
                        if (Game.Session.Location.currentGirlPair.girlDefinitionOne.girlName == "Kyu")
                        {
                            didSplit = run.split(isBonusRound);
                        }
                        //don't split for dates in 48 Shoes, or in postgame
                        else if (run.goal != 48 && Game.Persistence.playerFile.storyProgress < 13)
                        {
                            if (run.goal == 1 || SplitRules.Value <= 0) didSplit = run.split(isBonusRound);
                            else if (SplitRules.Value == 1 && !isBonusRound) didSplit = run.split(isBonusRound);
                            else if (SplitRules.Value == 2 && isBonusRound) didSplit = run.split(isBonusRound);
                            //check for final split regardless of option
                            else if (isBonusRound && (run.goal == startingCompletedPairs + 1 ||
                                        (run.goal == 25 && Game.Session.Puzzle.puzzleStatus.statusType == PuzzleStatusType.BOSS)))
                                didSplit = run.split(isBonusRound);
                        }
                        if (didSplit)
                        {
                            //initiate the timers for displaying and removing our split times
                            RunTimerPatches.initialTimerDelay.Start();
                            if (!isBonusRound) RunTimerPatches.undoTimer.Start();

                            GirlPairDefinition pair = Game.Session.Location.currentGirlPair;
                            int dateNum = 1;
                            if (startingRelationshipType == GirlPairRelationshipType.ATTRACTED) dateNum = 2;
                            if (isBonusRound) dateNum = 3;
                            //Kyu pair starts at ATTRACTED (2) and her bonus should be 2, not 3, so this is the easiest way
                            if (pair.girlDefinitionOne.girlName == "Kyu") dateNum--;
                            if (Game.Session.Puzzle.puzzleStatus.statusType == PuzzleStatusType.BOSS)
                            {
                                dateNum = 5 - (Game.Session.Puzzle.puzzleStatus.girlListCount / 2);
                            }
                            string newSplit = pair.girlDefinitionOne.girlName + " & " + pair.girlDefinitionTwo.girlName;
                            //don't put a number on the date if they started as lovers, or it's nonstop mode? (for 100%)
                            //the storyProgress check probably makes this pointless
                            if (startingRelationshipType != GirlPairRelationshipType.LOVERS
                            && Game.Session.Puzzle.puzzleStatus.statusType != PuzzleStatusType.NONSTOP) newSplit += " #" + dateNum;
                            newSplit += "\n      " + run.splitText + "\n";
                            run.push(newSplit);

                            if (isBonusRound && pair.girlDefinitionOne.girlName != "Kyu")
                            {
                                //I think it's possible that, with a huge chain reaction, completedGirlPairs.Count might not have updated yet
                                //so use the number from before the date, +1
                                //funnily enough, that also makes the final boss's goal of "25" count
                                //but I'll leave the double-check there
                                if (run.goal == startingCompletedPairs + 1 ||
                                (run.goal == 25 && Game.Session.Puzzle.puzzleStatus.statusType == PuzzleStatusType.BOSS))
                                {
                                    Logger.LogMessage("initiating run.save");
                                    if (run.rerollOccurred)
                                    {
                                        run.push("\n(Rerolled for Lillian)\n");
                                    }
                                    //run.save();
                                    RunTimerPatches.savePBDelay.Start();
                                }
                            }
                        }
                    }
                    
                    if (isBonusRound)
                    {
                        BasePatches.searchForMe = 200;
                    }
                    else BasePatches.searchForMe = 100;

                    splitThisDate = true;
                }
                    
                else
                {
                    BasePatches.searchForMe = 0;
                    splitThisDate = false;
                }
                    
            }

            //title-screen only options, to prevent non-vanilla things happening midrun
            if (Game.Manager.Ui.currentCanvas.titleCanvas)
            {
                UiTitleCanvas tc = (UiTitleCanvas)Game.Manager.Ui.currentCanvas;
                bool isLoading = (bool)AccessTools.Field(typeof(UiTitleCanvas), "_loadingGame").GetValue(tc);
                //display New Version tooltip for 10 seconds
                if (newVersionAvailable && !alertedOfUpdate)
                {
                    alertedOfUpdate = true;
                    ShowTooltip("Update Available!\nClick on Credits!", 10000, 0, 45);
                }

                //check for Kyu outfits
                if (Input.GetKey(KeyCode.K))
                {
                    bool hairstyle = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
                    for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha5; i++)
                    {
                        //Alpha0 = 48, Keypad0 = 256
                        int num = i - 48;
                        if ((Input.GetKeyDown((KeyCode)i) || Input.GetKeyDown((KeyCode)(i + 208))))
                        {
                            SetKyuOutfit(num, hairstyle);
                            string s = "Kyu ";
                            if (hairstyle) s += "Hairstyle #";
                            else s += "Outfit #";
                            s += num + " Chosen!";
                            ShowTooltip(s, 2000);
                        }
                    }
                }

                //check for the Cheat Mode hotkey
                if (!InputPatches.codeScreen && cheatsEnabled == false && !isLoading && CheatHotkey.Value.IsDown())
                {
                    Game.Manager.Audio.Play(AudioCategory.SOUND, Game.Manager.Ui.sfxReject);
                    PlayCheatLine();
                    Harmony.CreateAndPatchAll(typeof(CheatPatches), null);
                    CheatPatches.UnlockAllCodes();
                    ShowTooltip("Cheat Mode Activated!", 2000, 0, 30);
                    cheatsEnabled = true;
                }

                //check for quick Quick Transitions toggle
                if (!InputPatches.codeScreen && !isLoading && Input.GetKeyDown(KeyCode.Q))
                {
                    Game.Manager.Audio.Play(AudioCategory.SOUND, Game.Manager.Ui.sfxCellphoneNotification);
                    if (Game.Persistence.playerData.unlockedCodes.Contains(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS)))
                    {
                        Game.Persistence.playerData.unlockedCodes.Remove(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS));
                        Game.Manager.Settings.SaveSettings();
                        ShowTooltip("Quick Transitions Disabled!", 1000, 0, 30);
                    }
                    else
                    {
                        Game.Persistence.playerData.unlockedCodes.Add(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS));
                        Game.Manager.Settings.SaveSettings();
                        ShowTooltip("Quick Transitions Enabled!", 1000, 0, 30);
                    }
                }
            }

        }

        /// <summary>
        /// The identifier of this plugin.
        /// </summary>
        public const string PluginGUID = "HP2SpeedrunMod";

        /// <summary>
        /// The name of this plugin.
        /// </summary>
        public const string PluginName = "HuniePop 2 Speedrun Mod";

        /// <summary>
        /// The directory where this plugin resides.
        /// </summary>
        public static readonly string PluginBaseDir = Path.GetDirectoryName(typeof(HP2SR).Assembly.Location);

    }
}
