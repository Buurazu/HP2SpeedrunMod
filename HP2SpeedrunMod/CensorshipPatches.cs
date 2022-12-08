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
    public class CensorshipPatches
        //and also Outfit-related patches
    {
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
            if (____girlDefinition.girlName == "Kyu" && hairstyleIndex == -1)
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
            if (____girlDefinition.girlName == "Kyu" && outfitIndex == -1)
            {
                __state = HP2SR.KyuOutfit;
                outfitIndex = HP2SR.KyuOutfit;
            }
            if (!Game.Persistence.playerData.uncensored && HP2SR.CensorshipEnabled.Value && HP2SR.OutfitCensorshipEnabled.Value)
            {
                foreach (int i in lewdOutfits[____girlDefinition.id - 1])
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

        // mute sex SFX
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AudioManager), "Play", typeof(AudioCategory), typeof(AudioClip), typeof(PauseDefinition), typeof(float))]
        public static void SilenceTheMoans(AudioClip audioClip, ref float volume)
        {
            if (!Game.Persistence.playerData.uncensored && HP2SR.CensorshipEnabled.Value && HP2SR.SexSFXCensorshipEnabled.Value && audioClip != null)
            {
                if (audioClip.name.Contains("vo_sex") || audioClip.name == "vo_scene_opening_1_ashley")
                    volume = 0f;
            }
        }
    }
}
