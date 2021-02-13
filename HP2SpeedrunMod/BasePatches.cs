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

        //define inappropriate outfits for each character
        //Girl IDs start at 1, be careful
        public static int[][] lewdOutfits =
        {
            //Lola
            new int[] { 4, 5 },
            //Jessie
            new int[] { 4, 5, 8, 9 },
            //honestly Lillian's lingerie is tame, but fuck it
            new int[] { 4, 5 },
            //Zoey
            new int[] { 2, 3, 4, 5, 7 },
            //Sarah
            new int[] { 0, 4, 5, 8, 9 },
            //Lailani is pretty pure
            new int[] { 4 },
            //Candace rockin the pasties on 3, jeez
            new int[] { 0, 3, 4, 5 },
            //Nora
            new int[] { 4, 7, 8 },
            //Brooke
            new int[] { 0, 4, 5, 6 },
            //Ashley
            new int[] { 4, 5, 7 },
            //Abia
            new int[] { 4, 5, 6, 9 },
            //Polly
            new int[] { 5, 9 },
            //Kyu (6,7,8,9 are all treated as 5)
            new int[] { 5, 6, 7, 8, 9 },
            //Moxie
            new int[] { 5 },
            //Jewn
            new int[] { 5 }
        };

        //custom Kyu hairstyle
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiDoll), "ChangeHairstyle")]
        public static void ForGarrett(UiDoll __instance, ref int hairstyleIndex, ref int ____currentHairstyleIndex, ref GirlDefinition ____girlDefinition, ref int __state)
        {
            if (____girlDefinition == null) return;
            if (____girlDefinition.girlName == "Kyu")
            {
                hairstyleIndex = HP2SR.KyuHairstyle;
            }
        }

        //censor outfits, plus custom Kyu outfit
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiDoll), "ChangeOutfit")]
        public static void CensorLewdOutfits(UiDoll __instance, ref int outfitIndex, ref int ____currentOutfitIndex, ref GirlDefinition ____girlDefinition, ref int __state)
        {
            if (____girlDefinition == null) return;

            //set state to the intended outfit index, and change outfitIndex if lewd
            __state = outfitIndex;
            if (outfitIndex == -1) __state = Game.Persistence.playerFile.GetPlayerFileGirl(____girlDefinition).outfitIndex;
            if (____girlDefinition.girlName == "Kyu")
            {
                __state = HP2SR.KyuOutfit;
                outfitIndex = HP2SR.KyuOutfit;
            }
            if (!Game.Persistence.playerData.uncensored && HP2SR.CensorshipEnabled.Value)
            {
                foreach (int i in lewdOutfits[____girlDefinition.id-1])
                {
                    if (i == __state)
                    {
                        outfitIndex = ____girlDefinition.defaultOutfitIndex;
                        break;
                    }
                }
            }
            //the ChangeOutfit function should now change the outfit to the girl's default outfit, and then the postfix allows lewd outfits to still be unlocked
        }

        //censorship postfix, plus nude mod
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiDoll), "ChangeOutfit")]
        public static void Nippy(UiDoll __instance, ref int outfitIndex, ref int ____currentOutfitIndex, ref GirlDefinition ____girlDefinition, ref int __state)
        {
            if (____girlDefinition == null) return;
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
            //set outfit index to whatever it was supposed to be, for outfit unlocking purposes
            if (__state != -1)
            {
                __state = Mathf.Clamp(__state, 0, ____girlDefinition.outfits.Count - 1);
                ____currentOutfitIndex = __state;
            }
        }

        //censorship of large photos, replace with locations
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotoDefinition), "GetBigPhotoImage")]
        public static bool NoCGsPlease(PhotoDefinition __instance, ref Sprite __result)
        {
            if (!Game.Persistence.playerData.uncensored && HP2SR.CensorshipEnabled.Value)
            {
                __result = Game.Session.Location.currentLocation.backgrounds[0];
                return false;
            }
            else return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotoDefinition), "GetThumbnailImage")]
        public static bool NoThumbnailsEither(PhotoDefinition __instance, ref Sprite __result)
        {
            if (!Game.Persistence.playerData.uncensored && HP2SR.CensorshipEnabled.Value)
            {
                //__result = Game.Session.Location.currentLocation.finderLocationIcon;
                __result = Game.Session.Location.currentLocation.backgrounds[0];
                return false;
            }
            else return true;
        }

        //rip Kyu's butt :(
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiWindowKyuButt), "Show")]
        public static void KissMyBigFatVoidBIIIIIITCH(UiWindowKyuButt __instance)
        {
            if (!Game.Persistence.playerData.uncensored && HP2SR.CensorshipEnabled.Value)
            {
                __instance.buttImage.color = new Color(0, 0, 0, 1);
            }
        }

        //make pairs above the normal amount default to LOVERS
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveFileGirlPair), "Reset")]
        public static void EveryoneBeLovin(SaveFileGirlPair __instance, ref int id)
        {
            if (id >= 27) __instance.relationshipLevel = (int)GirlPairRelationshipType.LOVERS;
        }

        //FOR PAIRS CHEAT ONLY
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerFile), "ReadData")]
        public static void AddExtraPairs(PlayerFile __instance, ref SaveFile saveFile)
        {
            if (HP2SR.AllPairsEnabled.Value && Game.Data.GirlPairs.GetAll().Count < 30) Datamining.ExperimentalAllPairsMod();
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
                HP2SR.ShowTooltip("File #" + (____newSaveFileIndex + 1) + " will be deleted!", 4000, 0, 30);
            }
            //all girl heads available
            for (int j = 0; j < __instance.fileIconSlots.Count; j++)
            {
                __instance.fileIconSlots[j].Populate(false);
                __instance.fileIconSlots[j].canvasGroup.blocksRaycasts = true;
            }
            //randomly pick a girl head
            System.Random rand = new System.Random();
            int r = rand.Next(__instance.fileIconSlots.Count);
            ____selectedFileIconSlot = __instance.fileIconSlots[r];
            ____selectedFileIconSlot.button.Disable();
            //default Easy difficulty
            __instance.settingSelectorDifficulty.Populate(Game.Manager.Settings.GetSettingValueNames("difficulty", MathUtils.IntToBool(__instance.settingSelectorGender.selectedIndex), 0), 0, false);
            __instance.settingSelectorDifficulty.PopDescriptions(Game.Manager.Settings.GetSettingValueDescs("difficulty", MathUtils.IntToBool(__instance.settingSelectorGender.selectedIndex), 0));
            //give the people what they want
            //__instance.settingSelectorPolly.Populate(Game.Manager.Settings.GetSettingValueNames("polly", false, 0), 1, false);
            //actually, I don't think the bulge helps youtube friendliness at all

            __instance.Refresh();
        }

        //New game started = run is legitimate even if we returned to menu
        //Also, erase the file we're about to start
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "OnStartButtonPressed")]
        public static void ClearReturnFlag(UiCellphoneAppNew __instance, ref int ____newSaveFileIndex)
        {
            Game.Persistence.playerData.files[____newSaveFileIndex] = new PlayerFile(new SaveFile());
            Game.Persistence.Apply(____newSaveFileIndex);
            Game.Persistence.SaveGame();

            HP2SR.hasReturned = false;
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

        //during the custom tooltip time, prevent the tooltip text from changing or being respawned
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
