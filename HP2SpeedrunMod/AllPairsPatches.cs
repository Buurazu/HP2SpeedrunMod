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
    public class AllPairsPatches
    {
        //make pairs above the normal amount default to LOVERS
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveFileGirlPair), "Reset")]
        public static void EveryoneBeLovin(SaveFileGirlPair __instance, ref int id)
        {
            if (HP2SR.AllPairsEnabled.Value && id >= 27)
                __instance.relationshipLevel = (int)GirlPairRelationshipType.LOVERS;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveFileGirl), "Reset")]
        public static void SpecialsBeMet(SaveFileGirl __instance, ref int id)
        {
            if (HP2SR.AllPairsEnabled.Value && id >= 13)
            {
                __instance.playerMet = true;
                if (__instance.learnedBaggage.Count == 0)
                {
                    __instance.learnedBaggage.Add(0);
                    __instance.learnedBaggage.Add(1);
                    __instance.learnedBaggage.Add(2);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiAppHeadSlot), "Refresh")]
        public static void SpecialCharactersSmallHead(UiAppHeadSlot __instance, GirlDefinition ____girlDefinition)
        {
            //what a stupid way to resize Moxie and Jewn
            if (____girlDefinition.id >= 14)
            {
                __instance.itemIcon.sprite = Game.Data.Girls.Get(1).GetHead(false);
                __instance.itemIcon.SetNativeSize();
                __instance.itemIcon.sprite = ____girlDefinition.GetHead(false);
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiAppPairSlot), "Refresh")]
        public static void SpecialCharactersSmallHead2(UiAppPairSlot __instance, PlayerFileGirlPair ____playerFileGirlPair)
        {
            GirlDefinition one, two;
            if (____playerFileGirlPair != null)
            {
                one = ____playerFileGirlPair.girlPairDefinition.girlDefinitionOne;
                two = ____playerFileGirlPair.girlPairDefinition.girlDefinitionTwo;

                //don't need to check sides flipped; it's always false for special girl pairs?
                if (one.id >= 13)
                {
                    __instance.girlHeadOne.sprite = Game.Data.Girls.Get(1).GetHead(true);
                    __instance.girlHeadOne.SetNativeSize();
                    __instance.girlHeadOne.sprite = one.GetHead(false);
                }

                if (two.id >= 13)
                {
                    __instance.girlHeadTwo.sprite = Game.Data.Girls.Get(1).GetHead(true);
                    __instance.girlHeadTwo.SetNativeSize();
                    __instance.girlHeadTwo.sprite = two.GetHead(false);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationTransition), "Arrive")]
        public static void ShuffleSpecialBaggage(ref bool arriveWithGirls)
        {
            if (Game.Session.Location.currentLocation.id <= 8)
            {
                GirlDefinition[] gs = new GirlDefinition[] { Game.Session.Location.currentGirlLeft, Game.Session.Location.currentGirlRight };
                foreach (GirlDefinition g in gs)
                {
                    if (g.id >= 13)
                    {
                        g.baggageItemDefs.Clear();
                        g.baggageItemDefs.Add(Game.Data.Ailments.Get(UnityEngine.Random.Range(1, 37)).itemDefinition);
                        g.baggageItemDefs.Add(Game.Data.Ailments.Get(UnityEngine.Random.Range(1, 37)).itemDefinition);
                        g.baggageItemDefs.Add(Game.Data.Ailments.Get(UnityEngine.Random.Range(1, 37)).itemDefinition);
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TalkManager), "TalkWith")]
        public static bool NoSoftlocky(TalkManager __instance, ref int dollIndex)
        {
            if (HP2SR.AllPairsEnabled.Value)
            {
                PuzzleStatusGirl psg = Game.Session.Puzzle.puzzleStatus.GetStatusGirl(dollIndex > 0);

                if (psg.girlDefinition.id > 12)
                {
                    Game.Manager.Audio.Play(AudioCategory.SOUND, Game.Manager.Ui.sfxReject, Game.Session.gameCanvas.GetDoll(dollIndex > 0).pauseDefinition);
                    if (Game.Manager.Windows.IsWindowActive(null, true, false))
                    {
                        Game.Manager.Windows.ShowWindow(Game.Session.Location.actionBubblesWindow, true);
                        Game.Manager.Windows.HideWindow();
                    }
                    return false;
                }
            }
            return true;
        }
    }
}
