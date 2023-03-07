using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using DG.Tweening;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace HP2SpeedrunMod
{
    public class BasePatches
    {
        public static int searchForMe;
        public static int ASLPairID;
        public static int ASLDateNum;

        //for the autosplitter
        public static void InitSearchForMe()
        {
            searchForMe = 123456789;
            ASLPairID = 1;
            ASLDateNum = 1;
        }

        static string temp = "";
        public static void Update()
        {
            //pewter mod
            /*
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (!Game.Manager.Ui.currentCanvas.titleCanvas)
                {
                    HP2SR.ShowNotif("Store Refreshed!", 2);
                    Game.Persistence.playerFile.PopulateStoreProducts();
                    Game.Session.gameCanvas.cellphone.LoadOpenApp();
                }
            }
            */

            /*if (Game.Session && Game.Session.Puzzle.puzzleStatus.tokenStatus[5].GetCurrentWeight() < 30)
            {
                Game.Session.Puzzle.puzzleStatus.tokenStatus[5].AdjustCurrentWeight(1);
                Datamining.Logger.LogDebug(Game.Session.Puzzle.puzzleStatus.tokenStatus[5].GetCurrentWeight());
            }*/
            /*
            if (Game.Session)
            {
                foreach (PuzzleStatusToken pst in Game.Session.Puzzle.puzzleStatus.tokenStatus)
                {
                    //Datamining.Logger.LogDebug(pst.tokenDefinition.tokenName + ": " + pst.GetCurrentWeight());
                }
                PuzzleSet set = Game.Session.Puzzle.puzzleGrid.moveMatchSet;
                if (set != null)
                {
                    string newmatch = "";
                    List<UiPuzzleSlot> list = null;
                    List<UiPuzzleSlot> list2 = new List<UiPuzzleSlot>();
                    foreach (PuzzleMatch match in set.matches)
                    {
                        list = (from slot in ListUtils.CopyList<UiPuzzleSlot>(match.slots)
                                                   orderby slot.row + slot.col, UnityEngine.Random.Range(0f, 1f)
                                                   select slot).ToList<UiPuzzleSlot>();
                        while (list.Count > 0)
                        {
                            float f = (float)(list.Count - 1) * 0.5f;
                            int index = Mathf.Clamp(MathUtils.RandomBool() ? Mathf.FloorToInt(f) : Mathf.CeilToInt(f), 0, list.Count - 1);
                            list2.Add(list[index]);
                            list.RemoveAt(index);
                        }
                        foreach (UiPuzzleSlot slot in match.slots)
                        {
                            newmatch += slot.row + "," + slot.col + "; ";
                        }
                    }
                    if (newmatch != temp)
                    {
                        temp = newmatch;
                        Datamining.Logger.LogDebug(newmatch);
                        newmatch = "";
                        foreach (UiPuzzleSlot slot in list2)
                        {
                            newmatch += slot.row + "," + slot.col + "; ";
                        }
                        Datamining.Logger.LogDebug(newmatch);
                    }
                }
            }*/
            /*
            if (Game.Session)
            {
                foreach (PuzzleStatusToken pst in Game.Session.Puzzle.puzzleStatus.tokenStatus)
                {
                    Datamining.Logger.LogDebug(pst.tokenDefinition.tokenName + ": " + pst.GetCurrentWeight());
                }
            }
            */
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppCode), "Start")]
        public static void DisableClicksOnCodeScreen()
        {
            InputPatches.codeScreen = true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppCode), "OnDestroy")]
        public static void ReenableClicksOffCodeScreen()
        {
            InputPatches.codeScreen = false;
        }

        //add the all pairs code, and also remove it from the save if needed
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerFile), "ReadData")]
        public static void AddExtraPairs(PlayerFile __instance, ref SaveFile saveFile)
        {
            if (HP2SR.AllPairsEnabled.Value && Game.Data.GirlPairs.GetAll().Count < 27)
            {
                Datamining.ExperimentalAllPairsMod(false);
                if (HP2SR.SpecialPairsEnabled.Value) Datamining.ExperimentalAllPairsMod(true);
            }
            if ((!HP2SR.AllPairsEnabled.Value && saveFile.girlPairs.Count >= 27) || (!HP2SR.SpecialPairsEnabled.Value && saveFile.girlPairs.Count >= 69))
            {
                //fix an allpairs save file before it gets read
                if (Game.Data.GirlPairs.Get(saveFile.girlPairId) == null)
                {
                    saveFile.girlPairId = UnityEngine.Random.Range(1, 25);
                }
                for (int i = saveFile.girlPairs.Count - 1; i >= 0; i--)
                {
                    if (Game.Data.GirlPairs.Get(saveFile.girlPairs[i].girlPairId) == null)
                        saveFile.girlPairs.RemoveAt(i);
                }
                for (int i = saveFile.metGirlPairs.Count - 1; i >= 0; i--)
                {
                    if (Game.Data.GirlPairs.Get(saveFile.metGirlPairs[i]) == null)
                        saveFile.metGirlPairs.RemoveAt(i);
                }
                for (int i = saveFile.completedGirlPairs.Count - 1; i >= 0; i--)
                {
                    if (Game.Data.GirlPairs.Get(saveFile.completedGirlPairs[i]) == null)
                        saveFile.completedGirlPairs.RemoveAt(i);
                }
                for (int i = saveFile.finderSlots.Count - 1; i >= 0; i--)
                {
                    if (Game.Data.GirlPairs.Get(saveFile.finderSlots[i].girlPairId) == null)
                        saveFile.finderSlots[i].girlPairId = 0;
                }
            }
        }

        //allow the disabled toggles (quick transitions, Abia's hair) to be added to the code list
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SettingManager), "Start")]
        public static bool CodeEnabler(ref Dictionary<string, CodeDefinition> ____unlockCodes)
        {
            //enable the disabled codes muahaha
            List<CodeDefinition> all = Game.Data.Codes.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                if (!StringUtils.IsEmpty(all[i].codeHash))
                {
                    ____unlockCodes.Add(all[i].codeHash, all[i]);
                }
            }
            //remove all copies of the quick transitions code on boot
            /*while (Game.Persistence.playerData.unlockedCodes.Contains(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS))) {
                Game.Persistence.playerData.unlockedCodes.Remove(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS));
            }*/
            return false;
        }

        //make multiple changes to the new file menu to help with speedrunning
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "Start")]
        public static void NewGameMenuFixer(UiCellphoneAppNew __instance, ref int ____newSaveFileIndex, ref UiAppFileIconSlot ____selectedFileIconSlot)
        {
            //no save file was available, so use last file (or user's preference)
            if (____newSaveFileIndex < 0)
            {
                __instance.cellphone.phoneErrorMsg.ClearMessage();
                int fileToDelete = HP2SR.AutoDeleteFile.Value - 1;
                int lastFile = Game.Persistence.playerData.files.Count - 1;
                if (fileToDelete >= 0 && fileToDelete < lastFile)
                    ____newSaveFileIndex = fileToDelete;
                else
                    ____newSaveFileIndex = lastFile;
                HP2SR.ShowTooltip("File #" + (____newSaveFileIndex + 1) + " will be deleted!", 3000, 0, 30);
            }
            //all girl heads available
            for (int j = 0; j < __instance.fileIconSlots.Count; j++)
            {
                __instance.fileIconSlots[j].Populate(false);
                __instance.fileIconSlots[j].canvasGroup.blocksRaycasts = true;
            }

            System.Random rand = new System.Random();
            int r = rand.Next(__instance.fileIconSlots.Count);
            ____selectedFileIconSlot = __instance.fileIconSlots[r];
            ____selectedFileIconSlot.button.Disable();

            //default Easy difficulty
            __instance.settingSelectorDifficulty.Populate(Game.Manager.Settings.GetSettingValueNames("difficulty", MathUtils.IntToBool(__instance.settingSelectorGender.selectedIndex), 0), HP2SR.lastChosenDifficulty, false);
            __instance.settingSelectorDifficulty.PopDescriptions(Game.Manager.Settings.GetSettingValueDescs("difficulty", MathUtils.IntToBool(__instance.settingSelectorGender.selectedIndex), 0));

            __instance.Refresh();
        }

        //New game started = run is legitimate even if we returned to menu
        //Also, erase the file we're about to start
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "OnStartButtonPressed")]
        public static void ClearReturnFlag(UiCellphoneAppNew __instance, ref int ____newSaveFileIndex, ref UiAppFileIconSlot ____selectedFileIconSlot)
        {
            Game.Persistence.playerData.files[____newSaveFileIndex] = new PlayerFile(new SaveFile());
            Game.Persistence.Apply(____newSaveFileIndex);
            Game.Persistence.SaveGame();

            HP2SR.hasReturned = false;
            //alert the autosplitter
            if (!HP2SR.cheatsEnabled && !HP2SR.AllPairsEnabled.Value)
            {
                searchForMe = 111;
            }

        }

        //keep track of the difficulty selection
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "OnSettingSelectorChanged")]
        public static void JustCheckingDifficulty(UiCellphoneAppNew __instance)
        {
            HP2SR.lastChosenDifficulty = __instance.settingSelectorDifficulty.selectedIndex;
        }

        //alert the autosplitter of a legitimate Return to Menu
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppSettings), "OnReturnButtonPressed")]
        public static void LegitReturn()
        {
            searchForMe = -112;
        }

        //Runs aren't legitimate with cheat mode or after returning to menu with the hotkey
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "OnArrivalComplete")]
        public static void PostReturnNotification(LocationManager __instance)
        {
            if (HP2SR.cheatsEnabled) HP2SR.ShowThreeNotif("CHEATS ARE ENABLED");
            else if (HP2SR.hasReturned) HP2SR.ShowThreeNotif("This is for practice purposes only");
            //only display the warning on the Your Apartment location
            else if (HP2SR.AllPairsEnabled.Value && __instance.currentLocation == Game.Data.Locations.Get(22)) HP2SR.ShowThreeNotif("ALL PAIRS MODE IS ON");
        }

        //Check for new updates by opening the Drive link, and exe location, and quitting
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppCredits), "Start")]
        public static bool UpdateButton()
        {
            if (!HP2SR.newVersionAvailable) return true;

            System.Diagnostics.Process.Start("https://drive.google.com/u/0/uc?id=110dLsFRVXVr2I-jV3s6TDuJVfcKUjE1o&export=download");

            System.Diagnostics.Process.Start(Directory.GetCurrentDirectory());
            Application.Quit();

            return false;
        }

        //during custom tooltip time, prevent the tooltip text from changing or being respawned
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiTooltipSimple), "Populate")]
        public static bool PreventFileHover()
        {
            if (HP2SR.tooltipTimer.IsRunning) return false;
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiTooltip), "ShowInner")]
        public static bool PreventFileHover2()
        {
            if (HP2SR.tooltipTimer.IsRunning) return false;
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiTooltip), "Hide")]
        public static bool PreventFileHover3()
        {
            if (HP2SR.tooltipTimer.IsRunning) return false;
            return true;
        }

        //quicker return to menu hotkey
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiGameCanvas), "UnloadStep")]
        public static bool InstantReturn(UiGameCanvas __instance, ref Tweener ____titleTweener)
        {
            if (!HP2SR.unloadingByHotkey) return true;

            HP2SR.unloadingByHotkey = false;
            __instance.header.isLocked = true;
            __instance.cellphone.isLocked = true;
            __instance.overlayCanvasGroup.blocksRaycasts = true;
            Game.Manager.Audio.FadeOutCategory(AudioCategory.SOUND, 0f);
            Game.Manager.Audio.FadeOutCategory(AudioCategory.VOICE, 0f);
            Game.Manager.Audio.FadeOutCategory(AudioCategory.MUSIC, 0f);
            Game.Manager.Time.KillTween(____titleTweener, false, true);

            Game.Manager.ClearSession();
            SceneManager.LoadScene("TitleScene", LoadSceneMode.Single);

            return false;
        }

        //replace the title screen copyright/version info with speedrun mod version + game version + vsync info
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiCoverArt), "Refresh")]
        public static void ShowModSettingsInfo(UiCoverArt __instance)
        {
            string newText1 = "Speedrun Mod " + HP2SR.PluginVersion + " (Game Version " + Game.Manager.buildVersion + ")";
            string newText2 = "";
            if (HP2SR.VsyncEnabled.Value)
                newText2 += "Vsync On (" + Screen.currentResolution.refreshRate + ")";
            else
                newText2 += HP2SR.FramerateCap.Value + " FPS Lock";

            __instance.versionLabel.text = newText1;
            __instance.copyrightLabel.text = newText2;
        }

        //prevent being considered unfocused
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameManager), "OnApplicationFocus")]
        public static bool StopBeingUnfocused()
        {
            if (Application.runInBackground) return false;
            else return true;
        }

        //allow any resolution to be picked by typing it as a code (undocumented feature)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppCode), "OnSubmitButtonPressed")]
        public static bool CustomResolutions(UiCellphoneAppCode __instance)
        {
            string input = __instance.inputField.text.ToUpper().Trim();
            string[] res = input.Split(new string[] { "X" }, StringSplitOptions.RemoveEmptyEntries);
            int width = 0; int height = 0;
            if (res.Length == 2 && int.TryParse(res[0].Trim(), out width) && int.TryParse(res[1].Trim(), out height))
            {
                Screen.SetResolution(width, height, false);
                //messing with resolution stuff
                //Datamining.Logger.LogDebug(Game.Manager.Ui.currentCanvas.canvasScaler.screenMatchMode);
                //Game.Manager.Ui.currentCanvas.canvasScaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                //Game.Manager.Ui.currentCanvas.canvasScaler.matchWidthOrHeight = 0.5f;
                //Game.Manager.Ui.currentCanvas.canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;

                return false;
            }
            else return true;
        }


        //Choosing pairs manually
        //Some code by Lounger

        public static GirlDefinition currentGirl = null;
        public static AudioKlip click = null, woosh = null;
        public static UiTooltipSimple tooltip = null;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiCellphoneAppProfile), "Start")]
        public static void MakeGirlHeadClickable(UiCellphoneAppProfile __instance, UiTooltipSimple ____tooltip)
        {
            //enable the Girl Profile head to choose pairs, but only when cheating / using all pairs mod
            if (HP2SR.cheatsEnabled || HP2SR.AllPairsEnabled.Value)
            {
                click = (Game.Session.gameCanvas.cellphone.appPrefabs.FirstOrDefault((UiCellphoneApp a) => a is UiCellphoneAppFinder) as UiCellphoneAppFinder).sfxProfilePressed;
                woosh = (Game.Session.gameCanvas.cellphone.appPrefabs.FirstOrDefault((UiCellphoneApp a) => a is UiCellphoneAppFinder) as UiCellphoneAppFinder).sfxLocationSelect;
                __instance.girlHeadButton.Enable();
                __instance.girlHeadButton.ButtonPressedEvent += BasePatches.OnGirlHeadPressed;
                tooltip = ____tooltip;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppProfile), "OnGirlHeadEnter")]
        public static bool ChangeTooltipText(UiCellphoneAppProfile __instance, UiTooltipSimple ____tooltip, PlayerFileGirl ____playerFileGirl)
        {
            //enable the Girl Profile head to choose pairs, but only when cheating / using all pairs mod
            if (HP2SR.cheatsEnabled || HP2SR.AllPairsEnabled.Value)
            {
                string text = ____playerFileGirl.girlDefinition.girlName + "'s Profile";
                text = text + "\nAge: " + ____playerFileGirl.girlDefinition.girlAge.ToString();
                text = text + "\nClick to visit any\nof this girl's pairs!";
                ____tooltip.Populate(text, 0, 1f, 1920f);
                ____tooltip.Show(Vector2.up * 50f, false);
                return false;
            }
            return true;
        }

        public static void OnGirlHeadPressed(ButtonBehavior b)
        {
            tooltip.Hide();
            Game.Manager.Audio.Play(AudioCategory.SOUND, click, Game.Session.gameCanvas.cellphone.pauseBehavior.pauseDefinition);

            currentGirl = Game.Data.Girls.Get(Game.Session.gameCanvas.cellphone.GetCellFlag("profile_girl_id"));
            UiCellphoneAppPairs p = (Game.Session.gameCanvas.cellphone.appPrefabs.FirstOrDefault((UiCellphoneApp a) => a is UiCellphoneAppPairs) as UiCellphoneAppPairs);
            Game.Session.gameCanvas.cellphone.LoadApp(Game.Session.gameCanvas.cellphone.appPrefabs.IndexOf(p));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppPairs), "Start")]
        public static bool FindAllHerPairs(UiCellphoneAppPairs __instance)
        {
            if (currentGirl != null)
            {
                int num = 0;

                List<GirlPairDefinition> stockPairs = Game.Data.GirlPairs.GetAllBySpecial(false);
                for (int i = 0; i < stockPairs.Count; i++)
                {
                    if (stockPairs[i].HasGirlDef(currentGirl))
                    {
                        __instance.pairSlots[num].rectTransform.anchoredPosition = new Vector2((float)(num % 4) * 256f, (float)Mathf.FloorToInt((float)num / 4f) * -90f);
                        __instance.pairSlots[num].Populate(stockPairs[i], null);
                        __instance.pairSlots[num].profileLinked = true;
                        __instance.pairSlots[num].PairSlotPressedEvent += BasePatches.OnPairSlotPressed;
                        __instance.pairSlots[num].button.Enable();
                        num++;
                    }
                }
                for (int i = num; i < __instance.pairSlots.Length; i++)
                {
                    __instance.pairSlots[i].Populate(null, null);
                }
                __instance.pairSlotsContainer.anchoredPosition += new Vector2((float)Mathf.Min(num - 1, 3) * -128f, (float)Mathf.Max(Mathf.CeilToInt((float)num / 4f) - 1, 0) * 45f);

                currentGirl = null;
                return false;
            }
            return true;
        }

        public static void OnPairSlotPressed(UiAppPairSlot pairSlot)
        {
            //pewter mod
            //return;
            Game.Manager.Audio.Play(AudioCategory.SOUND, woosh, Game.Session.gameCanvas.cellphone.pauseBehavior.pauseDefinition);

            IEnumerable<LocationDefinition> source = from loc in Game.Data.Locations.GetAllByLocationType(LocationType.SIM)
                                                     where loc != Game.Session.Location.currentLocation
                                                     select loc;
            LocationDefinition locationDef = source.ElementAt(UnityEngine.Random.Range(0, source.Count<LocationDefinition>()));
            Game.Persistence.playerFile.daytimeElapsed++;
            Game.Session.Location.Depart(locationDef, pairSlot.playerFileGirlPair.girlPairDefinition, false);
        }
    }
}
