using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HP2SpeedrunMod
{
    // Token: 0x02000002 RID: 2
    public class BasePatches
    {
        public static int searchForMe;

        //for the autosplitter
        public static void InitSearchForMe()
        {
            searchForMe = 123456789;
        }

        //nip time
        //this could be part of censorship mod later on
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiDoll), "ChangeOutfit")]
        public static void Nippy(UiDoll __instance, int outfitIndex)
        {
            //Datamining.Logger.LogDebug("outfit change to " + outfitIndex + "; changed to " + __instance.currentOutfitIndex);
            //FileLog.Log("outfit changed to " + outfitIndex);
            if (HP2SR.nudePatch)
            {
                __instance.partNipples.Show();
                __instance.partOutfit.Hide();
            }
            else
            {
                __instance.partOutfit.Show();
            }
        }

        //allow the disabled toggles (quick transitions, Abia's hair) to be added to the code list
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SettingManager), "Start")]
        public static bool CodeEnabler(ref Dictionary<string, CodeDefinition> ____unlockCodes)
        {
            List<CodeDefinition> all = Game.Data.Codes.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                if (!StringUtils.IsEmpty(all[i].codeHash))
                {
                    ____unlockCodes.Add(all[i].codeHash, all[i]);
                }
            }
            return false;
        }

        //New game started = run is legitimate even if we returned to menu
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "OnStartButtonPressed")]
        public static void ClearReturnFlag()
        {
            HP2SR.hasReturned = false;
        }

        //Runs aren't legitimate with cheat mode or after returning to menu with the hotkey
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "OnArrivalComplete")]
        public static void PostReturnNotification()
        {
            if (HP2SR.cheatsEnabled) HP2SR.ShowThreeNotif("CHEATS ARE ENABLED");
            else if (HP2SR.hasReturned) HP2SR.ShowThreeNotif("This is for practice purposes only");
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

        //during the update notification time, prevent the tooltip text from changing to Save File #s
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiTooltipSimple), "Populate")]
        public static bool PreventFileHover()
        {
            if (!HP2SR.newVersionAvailable) return true;
            if (HP2SR.alertedOfUpdate.IsRunning) return false;
            return true;
        }


        //Autosplitter related; coming soon
        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "UpdateAffectionMeterDisplay")]
        public static void AutosplitHelp(PuzzleGame __instance, ref int ____goalAffection)
        {
            if (HP2SR.cheatsEnabled || HP2SR.hasReturned) return;
            if (__instance.currentDisplayAffection == ____goalAffection)
                searchForMe = 100;
            else searchForMe = 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnStartGameMale")]
        public static void MaleStart()
        {
            if (!HP2SR.cheatsEnabled)
                searchForMe = 111;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnStartGameFemale")]
        public static void FemaleStart()
        {
            if (!HP2SR.cheatsEnabled)
                searchForMe = 111;
        }

        */
    }
}
