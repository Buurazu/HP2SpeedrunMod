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

        //for the autosplitter
        public static void InitSearchForMe()
        {
            searchForMe = 123456789;
        }

        public static void Update()
        {

        }

        //make pairs above the normal amount default to LOVERS
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveFileGirlPair), "Reset")]
        public static void EveryoneBeLovin(SaveFileGirlPair __instance, ref int id)
        {
            if (HP2SR.AllPairsEnabled.Value && id >= 27) __instance.relationshipLevel = (int)GirlPairRelationshipType.LOVERS;
        }

        //FOR PAIRS CHEAT ONLY
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerFile), "ReadData")]
        public static void AddExtraPairs(PlayerFile __instance, ref SaveFile saveFile)
        {
            if (HP2SR.AllPairsEnabled.Value && Game.Data.GirlPairs.GetAll().Count < 40) Datamining.ExperimentalAllPairsMod();
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
            while (Game.Persistence.playerData.unlockedCodes.Contains(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS))) {
                Game.Persistence.playerData.unlockedCodes.Remove(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS));
            }
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
                Game.Persistence.playerData.unlockedCodes.Remove(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS));
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
            else if (Game.Persistence.playerData.unlockedCodes.Contains(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS))) HP2SR.ShowThreeNotif("Quick Transitions are on, somehow");
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
    }
}
