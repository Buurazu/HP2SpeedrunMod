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
        public const string PluginVersion = "1.1.0";

        //no item list yet
        //public static Dictionary<string, int> ItemNameList = new Dictionary<string, int>();

        public static ConfigEntry<KeyboardShortcut> ResetKey { get; private set; }
        public static ConfigEntry<Boolean> CensorshipEnabled { get; private set; }
        public static ConfigEntry<Boolean> ReturnToMenuEnabled { get; private set; }
        public static ConfigEntry<Boolean> CheatHotkeyEnabled { get; private set; }
        public static ConfigEntry<Boolean> AllPairsEnabled { get; private set; }
        public static ConfigEntry<Boolean> MouseWheelEnabled { get; private set; }
        public static ConfigEntry<Boolean> InputModsEnabled { get; private set; }
        public static ConfigEntry<int> AutoDeleteFile { get; private set; }

        //hasReturned is used to display "This is for practice purposes" after a return to main menu, until you start a new file
        public static bool hasReturned = false;
        public static bool cheatsEnabled = false;
        public static bool savingDisabled = false;
        public static bool nudePatch = false;

        public const int AXISES = 13;

        public const int UNKNOWN = 0;
        public const int ONE = 1;
        public const int TWO = 2;

        public static bool newVersionAvailable = false;
        public static bool alertedOfUpdate = false;

        public static UiTooltipSimple tooltip = null;
        public static Stopwatch tooltipTimer = new Stopwatch();
        public static int tooltipLength = 0;

        public static int KyuHairstyle = 1;
        public static int KyuOutfit = 1;

        private void Awake()
        {
            ReturnToMenuEnabled = Config.Bind(
                "Settings", nameof(ReturnToMenuEnabled),
                true,
                "Enable or disable the return to main menu hotkey");
            ResetKey = Config.Bind(
                "Settings", nameof(ResetKey),
                new KeyboardShortcut(KeyCode.F4),
                "The hotkey to use for going back to the title");
            
            CensorshipEnabled = Config.Bind(
                "Settings", nameof(CensorshipEnabled),
                true,
                "Enable or disable the extra censorship mods (only active when the in-game setting is Bras & Panties)");

            AutoDeleteFile = Config.Bind(
                "Settings", nameof(AutoDeleteFile),
                4,
                "The file that will be erased on new game if all files are full");
            
            MouseWheelEnabled = Config.Bind(
                "Settings", nameof(MouseWheelEnabled),
                true,
                "Enable or disable the mouse wheel being treated as a click");
            InputModsEnabled = Config.Bind(
                "Settings", nameof(InputModsEnabled),
                true,
                "Enable or disable all fake clicks");

            CheatHotkeyEnabled = Config.Bind(
                "Settings", nameof(CheatHotkeyEnabled),
                true,
                "Enable or disable the cheat hotkey (C on main menu)");

            AllPairsEnabled = Config.Bind(
                "Settings", nameof(AllPairsEnabled),
                false,
                "Enable or disable all 66 pairs showing up (any save files used with this active will need to be erased if you go back to 24-pair mode)");
        }

        void Start()
        {
            //I can't believe the game doesn't run in background by default
            Application.runInBackground = true;

            Harmony.CreateAndPatchAll(typeof(BasePatches), null);
            //initiate the variable used for autosplitting
            BasePatches.InitSearchForMe();

            //Check for a new update
            WebClient client = new WebClient();
            try
            {
                string reply = client.DownloadString("https://pastebin.com/raw/5z5PsCqr");

                if (reply != PluginVersion)
                    newVersionAvailable = true;
            }
            catch (Exception e) { Logger.LogDebug("Couldn't read the pastebin! " + e.ToString()); }

            //coming soon
            /*
            //Create the item names dictionary for easier rewarding of specific items
            foreach (ItemDefinition item in HunieMod.Definitions.Items)
            {
                ItemNameList.Add(item.name, item.id);
            }

            if (CensorshipEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(CensorshipPatches), null);
            }
            */
            if (InputModsEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(InputPatches), null);
            }


        }

        public static int GameVersion()
        {
            switch (Game.Manager.buildVersion)
            {
                case ("1.0.0"):
                case ("1.0.1"):
                    return ONE;
                default: return ONE;

            }
        }

        void PlayGirlAudio(int i, int j, int k = -1, int l = -1)
        {
            if (l >= 0) Game.Manager.Audio.Play(AudioCategory.VOICE, Game.Data.Cutscenes.GetAll()[i].steps[j].branches[k].steps[l].dialogLine.audioClip);
            else if (k >= 0) Game.Manager.Audio.Play(AudioCategory.VOICE, Game.Data.DialogTriggers.GetAll()[i].dialogLineSets[j].dialogLines[k].audioClip);
            else Game.Manager.Audio.Play(AudioCategory.VOICE, Game.Data.Cutscenes.GetAll()[i].steps[j].dialogLine.audioClip);
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
            //(we still need more, because occasionally returning during a cutscene causes undefined tweens to lock the game up)
            if (Game.Manager.Ui.currentCanvas.titleCanvas) { Logger.LogDebug("titleCanvas = no unloading"); return false; }
            if (Game.Session.Location.isTraveling) { Logger.LogDebug("isTraveling = no unloading"); return false; }
            if ((bool)AccessTools.Field(typeof(UiGameCanvas), "_unloadingGame").GetValue(Game.Session.gameCanvas)) { Logger.LogDebug("_unloadingGame = no unloading"); return false; }

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

        private void Update() // Another Unity method
        {
            if (!Game.Manager) return; //don't run update code before Game.Manager exists
            //if (Game.Data.GirlPairs.GetAll().Count < 30) Datamining.ExperimentalAllPairsMod();
            InputPatches.prevHoriz = InputPatches.horiz; InputPatches.prevVert = InputPatches.vert;

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

            if (Game.Manager.Ui.currentCanvas.titleCanvas)
            {
                //display New Version tooltip for 10 seconds
                if (newVersionAvailable && !alertedOfUpdate)
                {
                    //disable this for his version
                    alertedOfUpdate = true;
                    ShowTooltip("Update Available!\nClick on Credits!", 10000, 0, 45);
                }

                if (Input.GetKeyDown(KeyCode.A))
                {
                    List<CodeDefinition> codes = Game.Data.Codes.GetAll();
                    CodeDefinition codeDefinition = codes[13];
                    if (!Game.Persistence.playerData.unlockedCodes.Contains(codeDefinition))
                    {
                        Game.Persistence.playerData.unlockedCodes.Add(codeDefinition);
                        ShowTooltip("Abia's Hair Enabled!", 2000);
                    }
                    else
                    {
                        Game.Persistence.playerData.unlockedCodes.Remove(codeDefinition);
                        ShowTooltip("Abia's Hair Disabled!", 2000);
                    }
                    Game.Manager.Ui.currentCanvas.GetComponent<UiTitleCanvas>().coverArt.Refresh();
                    
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
                if (CheatHotkeyEnabled.Value && cheatsEnabled == false && Input.GetKeyDown(KeyCode.C))
                {
                    Game.Manager.Audio.Play(AudioCategory.SOUND, Game.Manager.Ui.sfxReject);
                    PlayCheatLine();
                    Harmony.CreateAndPatchAll(typeof(CheatPatches), null);
                    //testMode bugs the final boss out, apply the quick transitions code in update instead
                    //if (Game.Manager.testMode == false) AccessTools.Field(typeof(GameManager), "_testMode").SetValue(Game.Manager, true);
                    CheatPatches.UnlockAllCodes();
                    ShowTooltip("Cheat Mode Activated!", 2000, 0, 30);
                    cheatsEnabled = true;
                }
            }

            if (ReturnToMenuEnabled.Value)
            {
                if (ResetKey.Value.IsDown())
                {
                    if (UnloadGame()) hasReturned = true;
                }
            }

            if (cheatsEnabled)
            {
                Game.Persistence.playerData.unlockedCodes.Add(Game.Data.Codes.GetAll()[12]);
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    if (Game.Session.Location.currentLocation.locationType == LocationType.DATE)
                    {
                        ShowNotif("Puzzle Cleared!", 2);
                        Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.AFFECTION, 9999999, false);
                        Game.Session.Puzzle.puzzleStatus.CheckChanges();
                    }
                    else
                    {
                        ShowNotif("Fruit Given!", 2);
                        Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.FLIRTATION, 100);
                        Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.ROMANCE, 100);
                        Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.SEXUALITY, 100);
                        Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.TALENT, 100);
                    }
                }

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    if (!Game.Manager.Ui.currentCanvas.titleCanvas) {
                        ShowNotif("Stamina Gained!", 2);
                        Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.STAMINA, 6, false);
                        Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.STAMINA, 6, true);
                        Game.Session.Puzzle.puzzleStatus.CheckChanges();
                    }
                }

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    if (Input.GetKeyDown(KeyCode.A) && !Game.Manager.Ui.currentCanvas.titleCanvas)
                    {
                        List<CodeDefinition> codes = Game.Data.Codes.GetAll();
                        CodeDefinition abiaHair = codes[13];
                        if (!Game.Persistence.playerData.unlockedCodes.Contains(abiaHair))
                        {
                            Game.Persistence.playerData.unlockedCodes.Add(abiaHair);
                            ShowNotif("Abia's hair enabled!", 0);
                        }
                        else
                        {
                            Game.Persistence.playerData.unlockedCodes.Remove(abiaHair);
                            ShowNotif("Abia's hair disabled!", 0);
                        }
                        foreach (UiDoll doll in Game.Session.gameCanvas.dolls)
                        {
                            if (doll.girlDefinition && doll.girlDefinition.girlName == "Abia") doll.ChangeHairstyle(doll.currentHairstyleIndex);
                        }
                    }

                    for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha9; i++)
                    {
                        //Alpha0 = 48, Keypad0 = 256
                        int num = i - 48;

                        if (!Game.Manager.Ui.currentCanvas.titleCanvas && (Input.GetKeyDown((KeyCode)i) || Input.GetKeyDown((KeyCode)(i+208))))
                        {
                            foreach (UiDoll doll in Game.Session.gameCanvas.dolls)
                            {
                                ShowThreeNotif("Changed to Outfit #" + num);
                                doll.ChangeHairstyle(num);
                                doll.ChangeOutfit(num);
                            }
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.N))
                    {
                        nudePatch = !nudePatch;
                        if (nudePatch)
                        {
                            ShowNotif("AWOOOOOOOOOOGA", 2);
                            foreach (UiDoll doll in Game.Session.gameCanvas.dolls)
                            {
                                doll.partNipples.Show();
                                doll.partOutfit.Hide();
                            }
                        }
                        else
                        {
                            foreach (UiDoll doll in Game.Session.gameCanvas.dolls)
                            {
                                doll.partNipples.Hide();
                                doll.partOutfit.Show();
                            }
                        }
                            
                    }

                    if (Input.GetKeyDown(KeyCode.M))
                    {
                        if (!InputPatches.mashCheat) ShowThreeNotif("MASH POWER ACTIVATED");
                        else ShowThreeNotif("Mash power deactivated");
                        InputPatches.mashCheat = !InputPatches.mashCheat;
                    }
                    
                    if (Input.GetKeyDown(KeyCode.L))
                    {
                        Datamining.LocationInfo();
                    }

                    if (Input.GetKeyDown(KeyCode.G))
                    {
                        Datamining.GetGirlData();
                    }

                    if (Input.GetKeyDown(KeyCode.P))
                    {

                    }
                }

                
            }
            /*
            Logger.LogDebug(Game.Data.Codes.GetAll();
            //Test if we should send the "Venus unlocked" signal
            //All Panties routes would have met Momo or Celeste by now
            if (GameManager.System.GameState == GameState.SIM
                && GameManager.System.Player.GetGirlData(GameManager.Stage.uiGirl.alienGirlDef).metStatus != GirlMetStatus.MET
                && GameManager.System.Player.GetGirlData(GameManager.Stage.uiGirl.catGirlDef).metStatus != GirlMetStatus.MET
                && !BaseHunieModPlugin.cheatsEnabled && GameManager.Stage.girl.definition.firstName == "Venus"
                && GameManager.System.Player.GetGirlData(GameManager.Stage.girl.definition).metStatus != GirlMetStatus.MET
                && GameManager.Stage.girl.girlPieceContainers.localX < 520)
            {
                BasePatches.searchForMe = 500;
            }

            if (GameManager.System.GameState == GameState.TITLE)
            {

                if (CheatHotkeyEnabled.Value && cheatsEnabled == false && Input.GetKeyDown(KeyCode.C))
                {
                    GameManager.System.Audio.Play(AudioCategory.SOUND, GameManager.Stage.uiPuzzle.puzzleGrid.failureSound, false, 2f, false);
                    GameManager.System.Audio.Play(AudioCategory.SOUND, GameManager.Stage.uiPuzzle.puzzleGrid.badMoveSound, false, 2f, false);
                    PlayCheatLine();
                    Harmony.CreateAndPatchAll(typeof(CheatPatches), null);
                    cheatsEnabled = true;
                }
            }
            if (cheatsEnabled)
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    if (GameManager.System.GameState == GameState.PUZZLE)
                    {
                        if (GameManager.System.Puzzle.Game.puzzleGameState == PuzzleGameState.WAITING)
                        {
                            GameManager.System.Puzzle.Game.SetResourceValue(PuzzleGameResourceType.AFFECTION, 999999, false);
                            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Puzzle cleared!");
                        }
                    }
                    else if (GameManager.System.GameState == GameState.SIM)
                    {
                        CheatPatches.AddGreatGiftsToInventory();
                        GameManager.System.Player.money = 69420;
                        GameManager.System.Player.hunie = 69420;
                    }
                }

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    if (savingDisabled)
                    {
                        savingDisabled = false;
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Saving has been enabled");
                    }
                    else
                    {
                        savingDisabled = true;
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Saving has been disabled");
                    }
                }

                if (Input.GetKeyDown(KeyCode.M))
                {
                    InputPatches.mashCheat = !InputPatches.mashCheat;
                    if (InputPatches.mashCheat)
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "MASH POWER ACTIVATED!!!!!");
                    else
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Mash power disabled");

                }
            }
            if (ReturnToMenuEnabled.Value)
            {
                if (ResetKey.Value.IsDown() || ResetKey2.Value.IsDown())
                {
                    if (GameManager.System.GameState == GameState.TITLE)
                    {
                        //GameUtil.QuitGame();
                    }
                    else
                    {
                        if (GameUtil.EndGameSession(false, false, false))
                        {
                            hasReturned = true;
                            BasePatches.searchForMe = -111;
                        }
                    }
                }
            }
            */
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
