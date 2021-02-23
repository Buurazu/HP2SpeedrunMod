﻿using System;
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
        public const string PluginVersion = "1.5";

        //no item list yet
        //public static Dictionary<string, int> ItemNameList = new Dictionary<string, int>();

        public static ConfigEntry<KeyboardShortcut> ResetKey { get; private set; }
        public static ConfigEntry<Boolean> CensorshipEnabled { get; private set; }
        public static ConfigEntry<Boolean> ReturnToMenuEnabled { get; private set; }
        public static ConfigEntry<Boolean> CheatHotkeyEnabled { get; private set; }
        public static ConfigEntry<Boolean> AllPairsEnabled { get; private set; }
        public static ConfigEntry<Boolean> MouseWheelEnabled { get; private set; }
        public static ConfigEntry<Boolean> KeyboardEnabled { get; private set; }
        public static ConfigEntry<Boolean> InputModsEnabled { get; private set; }
        public static ConfigEntry<int> AutoDeleteFile { get; private set; }
        public static ConfigEntry<Boolean> InGameTimer { get; private set; }
        public static ConfigEntry<int> SplitRules { get; private set; }
        public static ConfigEntry<Boolean> CapAt144 { get; private set; }

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
            CapAt144 = Config.Bind(
                "Settings", nameof(CapAt144),
                true,
                "Cap the game at 144 FPS. If false, it will cap at 60 FPS instead. 144 FPS could help mash speed, but the higher framerate could mean bonus round affection drains faster (especially on Hard)");
            InGameTimer = Config.Bind(
                "Settings", nameof(InGameTimer),
                true,
                "Enable or disable the built-in timer (shows your time on the affection meter after each date, read the readme for more info)");
            SplitRules = Config.Bind(
                "Settings", nameof(SplitRules),
                0,
                "0 = Split on every date/bonus, 1 = Split only after dates, 2 = Split only after bonus rounds\n(You may want to delete your run comparison/golds after changing this. 1 Wing is excluded from this option)");
            CensorshipEnabled = Config.Bind(
                "Settings", nameof(CensorshipEnabled),
                true,
                "Enable or disable the extra censorship mods (only active when the in-game setting is Bras & Panties)");
            ReturnToMenuEnabled = Config.Bind(
                "Settings", nameof(ReturnToMenuEnabled),
                true,
                "Enable or disable the return to main menu hotkey");
            ResetKey = Config.Bind(
                "Settings", nameof(ResetKey),
                new KeyboardShortcut(KeyCode.F4),
                "The hotkey to use for going back to the title");
            AutoDeleteFile = Config.Bind(
                "Settings", nameof(AutoDeleteFile),
                4,
                "The file that will be erased on new game if all files are full");
            MouseWheelEnabled = Config.Bind(
                "Settings", nameof(MouseWheelEnabled),
                true,
                "Enable or disable the mouse wheel being treated as a click");
            KeyboardEnabled = Config.Bind(
                "Settings", nameof(KeyboardEnabled),
                true,
                "Enable or disable keyboard keys being treated as a click");
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
            //allow max 144fps
            QualitySettings.vSyncCount = 0;
            if (CapAt144.Value)
                Application.targetFrameRate = 144;
            else
                Application.targetFrameRate = 60;

            Harmony.CreateAndPatchAll(typeof(BasePatches), null);
            Harmony.CreateAndPatchAll(typeof(CensorshipPatches), null);
            //initiate the variable used for autosplitting
            BasePatches.InitSearchForMe();

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

            if (InputModsEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(InputPatches), null);
            }
            if (InGameTimer.Value)
            {
                Harmony.CreateAndPatchAll(typeof(RunTimerPatches), null);
            }

        }

        public static int GameVersion()
        {
            switch (Game.Manager.buildVersion)
            {
                //eventually, a version update will break something
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
            if (ReturnToMenuEnabled.Value && ResetKey.Value.IsDown())
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
                    System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + "/splits");
                }
                //reset run on Ctrl+R
                if (Input.GetKeyDown(KeyCode.R) && run != null)
                {
                    run.reset(true);
                }
                //quit run on Ctrl+Q
                if (Input.GetKeyDown(KeyCode.Q) && run != null)
                {
                    run.reset(false);
                }
            }

            if (Input.GetKeyDown(KeyCode.S) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                
            }
            

                //if currently in a puzzle, and the status bar is up, and it's not nonstop mode, send data to autosplitters
                if (!Game.Manager.Ui.currentCanvas.titleCanvas && Game.Session && Game.Session.Puzzle.isPuzzleActive
                && !Game.Session.gameCanvas.cellphone.isOpen && Game.Session.Puzzle.puzzleStatus.statusType != PuzzleStatusType.NONSTOP)
            {
                UiCellphoneAppStatus status = (UiCellphoneAppStatus)AccessTools.Field(typeof(UiCellphone), "_currentApp").GetValue(Game.Session.gameCanvas.cellphone);
                bool isBonusRound = Game.Session.Puzzle.puzzleStatus.bonusRound;
                if (status.affectionMeter.currentValue == 0)
                {
                    startingRelationshipType = Game.Persistence.playerFile.GetPlayerFileGirlPair(Game.Session.Location.currentGirlPair).relationshipType;
                    startingCompletedPairs = Game.Persistence.playerFile.completedGirlPairs.Count;
                }
                if (status.affectionMeter.currentValue == status.affectionMeter.maxValue)
                {
                    if (!splitThisDate && run != null)
                    {
                        bool didSplit = false;
                        //always split for the two tutorial splits
                        if (Game.Session.Location.currentGirlPair.girlDefinitionOne.girlName == "Kyu")
                        {
                            didSplit = run.split();
                        }
                        //don't split for dates in 48 Shoes, or in postgame
                        else if (run.goal != 48 && Game.Persistence.playerFile.storyProgress < 13)
                        {
                            if (run.goal == 1 || SplitRules.Value <= 0) didSplit = run.split();
                            else if (SplitRules.Value == 1 && !isBonusRound) didSplit = run.split();
                            else if (SplitRules.Value == 2 && isBonusRound) didSplit = run.split();
                            //check for final split regardless of option
                            else if (isBonusRound && (run.goal == startingCompletedPairs + 1 ||
                                        (run.goal == 25 && Game.Session.Puzzle.puzzleStatus.statusType == PuzzleStatusType.BOSS)))
                                didSplit = run.split();
                        }
                        //this delay is both so the affection meter doesn't change instantly, and so that the variables can change as they need to
                        if (didSplit)
                        {
                            Task.Delay(1000).ContinueWith(t =>
                            {
                                status.affectionMeter.valueLabelPro.richText = true;
                                status.affectionMeter.valueLabelPro.text =
                                "<color=" + RunTimer.colors[(int)run.splitColor] + ">" + run.splitText + "</color>";
                                
                                Sequence seq = DOTween.Sequence();
                                seq.Insert(0f, status.canvasGroupDate.DOFade(1, 0.32f).SetEase(Ease.Linear));
                                seq.Insert(0f, status.canvasGroupLeft.DOFade(1, 0.32f).SetEase(Ease.Linear));
                                seq.Insert(0f, status.canvasGroupRight.DOFade(1, 0.32f).SetEase(Ease.Linear));
                                Game.Manager.Time.Play(seq, status.pauseBehavior.pauseDefinition, 0f);

                                if (run.prevColor == RunTimer.SplitColors.BLUE)
                                {
                                    status.sentimentRollerRight.valueName = "THIS SPLIT";
                                    status.sentimentRollerRight.maxName = "THIS SPLIT";
                                    status.sentimentRollerRight.nameLabel.text = "THIS SPLIT";
                                    status.sentimentRollerRight.valueLabelPro.text = run.prevText;
                                }
                                else if (run.prevColor == RunTimer.SplitColors.RED)
                                {
                                    status.passionRollerRight.valueName = "THIS SPLIT";
                                    status.passionRollerRight.maxName = "THIS SPLIT";
                                    status.passionRollerRight.nameLabel.text = "THIS SPLIT";
                                    status.passionRollerRight.valueLabelPro.text = run.prevText;
                                }

                                if (run.goldText != "")
                                {
                                    status.movesRoller.valueName = "GOLD";
                                    status.movesRoller.maxName = "GOLD";
                                    status.movesRoller.nameLabel.text = "GOLD";
                                    status.movesRoller.valueLabelPro.text = run.goldText;
                                }
                                //don't undo my changes if it's a bonus round, there's no stats to return to
                                if (!isBonusRound)
                                {
                                    Task.Delay(5000).ContinueWith(t2 =>
                                    {
                                        status.sentimentRollerRight.valueName = "SENTIMENT"; status.sentimentRollerRight.maxName = "SENTI... • MAX";
                                        AccessTools.Method(typeof(MeterRollerBehavior), "Refresh").Invoke(status.sentimentRollerRight, null);
                                        status.passionRollerRight.valueName = "PASSION"; status.passionRollerRight.maxName = "PASSION • MAX";
                                        AccessTools.Method(typeof(MeterRollerBehavior), "Refresh").Invoke(status.passionRollerRight, null);
                                        status.movesRoller.valueName = "MOVES"; status.movesRoller.maxName = "MOVES • MAX";
                                        AccessTools.Method(typeof(MeterRollerBehavior), "Refresh").Invoke(status.movesRoller, null);
                                    });
                                }

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
                                        run.save();
                                    }
                                }
                            });
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

                if (Input.GetKeyDown(KeyCode.A))
                {
                    CodeDefinition codeDefinition = Game.Data.Codes.Get(ABIAHAIR);
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
                if (CheatHotkeyEnabled.Value && cheatsEnabled == false && !isLoading && Input.GetKeyDown(KeyCode.C))
                {
                    Game.Manager.Audio.Play(AudioCategory.SOUND, Game.Manager.Ui.sfxReject);
                    PlayCheatLine();
                    Harmony.CreateAndPatchAll(typeof(CheatPatches), null);
                    CheatPatches.UnlockAllCodes();
                    ShowTooltip("Cheat Mode Activated!", 2000, 0, 30);
                    cheatsEnabled = true;
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
