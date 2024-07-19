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

        //taken from Lounger's huniemod
        public static Texture2D ImageFileToTexture(string filename, bool isEmbeddedResource = false)
        {
            byte[] imageData;

            if (isEmbeddedResource)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                //Assembly assembly = typeof(SpriteUtil).Assembly; // Replace with the assembly that contains your images, if you want to use embedded ones...

                using (Stream stream = assembly.GetManifestResourceStream(filename))
                {
                    if (stream == null)
                    {
                        //Logger.LogInfo($"{nameof(SpriteUtil)}.{nameof(ImageFileToTexture)}: Resource does not exist: {filename}");
                        return null;
                    }
                    imageData = new byte[stream.Length];
                    stream.Read(imageData, 0, (int)stream.Length);
                }
            }
            else
            {
                if (!File.Exists(filename))
                {
                    //Logger.LogInfo($"{nameof(SpriteUtil)}.{nameof(ImageFileToTexture)}: File does not exist: {filename}");
                    return null;
                }
                using (FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    imageData = new byte[stream.Length];
                    stream.Read(imageData, 0, (int)stream.Length);
                }
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);

            if (imageData?.Length > 0)
            {
                texture.LoadImage(imageData);
            }

            return texture;
        }
        public static Sprite ImageFileToSprite(string filename, string spriteName, bool isEmbeddedResource = false)
        {
            Texture2D texture = ImageFileToTexture(filename, isEmbeddedResource);
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
            sprite.name = spriteName; // idk if this matters
            return sprite;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiDoll), "LoadGirl")]
        public static void testingload(UiDoll __instance, GirlDefinition girlDef, int expressionIndex, ref int hairstyleIndex, ref int outfitIndex, GirlDefinition soulGirlDef)
        {
            if (HP2SR.cheatsEnabled) return;
            if (Game.Session.Location.currentLocation.locationType == LocationType.DATE) return;
            PlayerFileGirl playerFileGirl = Game.Persistence.playerFile.GetPlayerFileGirl(girlDef);
            if (hairstyleIndex == -1 && playerFileGirl.hairstyleIndex == girlDef.defaultHairstyleIndex)
                hairstyleIndex = HP2SR.hairstylePreferences[girlDef.girlName].Value;
            if (outfitIndex == -1 && playerFileGirl.outfitIndex == girlDef.defaultOutfitIndex)
                outfitIndex = HP2SR.outfitPreferences[girlDef.girlName].Value;
        }
        
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
        [HarmonyPatch(typeof(PhotoDefinition), "GetThumbnailImage")]
        public static bool NoCGsPlease(PhotoDefinition __instance, ref Sprite __result)
        {
            string cgNum = __instance.bigPhotoImages[0].name.Substring(3, 2);

            if (HP2SR.customCG4.ContainsKey(cgNum))
            {
                __result = HP2SR.customCG4[cgNum];
                return false;
            }
            else if (!Game.Persistence.playerData.uncensored && HP2SR.CensorshipEnabled.Value)
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
