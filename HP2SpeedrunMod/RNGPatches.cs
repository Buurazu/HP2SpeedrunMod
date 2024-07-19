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
    public class RNGPatches
    {
        // seed modes needed in huniepop 2: seed pairs, seed specific pair (allow any of the intro pairs to be chosen as the specific seed?), seed shop contents, seed some puzzle stuff, seed talks, seed seed rewards after dates


        public static System.Random curRandom = null; //temporarily set before a key function is done
        public static System.Random dateRandom = null; //advanced per date start
        public static System.Random curDateRandom = null; //handles the RNG mid-date
        public static System.Random powerTokenRandom = null; //handles power token RNG

        public static System.Random seedRandom = null; //for the seeds given after dates
        public static System.Random nonstopRandom = null; //for nympho/nonstop girl choosing
        public static System.Random finderRandom = null;
        public static System.Random storeRandom = null;
        public static System.Random talkRandom = null; //used for what kind of talk you get

        public static bool inPuzzleBegin = false;
        public static bool ignore = false;

        public static int ourSeed;
        public static int currentDateSeed, currentPowerSeed;
        public static string[] firstPairs = { "None", "Ashley+Lillian", "Ashley+Polly", "Lailani+Sarah", "Lailani+Jessie",  "Lola+Nora", "Lola+Abia" };
        public static int[] firstPairToInt = { 0, 5, 4, 7, 8, 2, 1 };
        public static int[] firstPairToLoc = { 0, 6, 4, 2, 7, 7, 6 };


        public static void InitializeRNG(int seed)
        {
            ourSeed = seed;
            BasePatches.Logger.LogMessage("Seed: " + seed);
            curRandom = null;
            if (HP2SR.seedDates)
            {
                dateRandom = new System.Random(seed);
                seedRandom = new System.Random(seed - 500);
                nonstopRandom = new System.Random(seed - 750);
            }
            curDateRandom = null;
            powerTokenRandom = null;
            if (HP2SR.seedFinder) finderRandom = new System.Random(seed - 1000);
            if (HP2SR.seedStore) storeRandom = new System.Random(seed - 2000);
            if (HP2SR.seedTalks) talkRandom = new System.Random(seed - 3000);
        }
        public static void WipeRNG()
        {
            ourSeed = 0;
            curRandom = null;
            dateRandom = null;
            curDateRandom = null;
            powerTokenRandom = null;
            seedRandom = null;
            finderRandom = null;
            storeRandom = null;
            talkRandom = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnityEngine.Random), "Range", new Type[] { typeof(int), typeof(int) })]
        public static void SpoofRandomRange(int min, int max, ref int __result)
        {
            if (HP2SR.seedMode && curRandom != null && !ignore)
            {
                int prevResult = __result;
                __result = curRandom.Next(min, max);

                string upOneLevel = (new System.Diagnostics.StackTrace()).GetFrame(2).GetMethod().Name;
                string upTwoLevels = (new System.Diagnostics.StackTrace()).GetFrame(3).GetMethod().Name;
                BasePatches.Logger.LogMessage(upTwoLevels + " called " + upOneLevel + " called RandomInt: " + min + "," + max + "," + __result + ", orig result: " + prevResult);
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnityEngine.Random), "Range", new Type[] { typeof(float), typeof(float) })]
        public static void SpoofRandomRangeFloat(float min, float max, ref float __result)
        {
            //NextDouble doesn't have nextdouble range, but, it defaults to between 0 and 1
            //That's fine for now, this should only be used for power token chance which is 0-1
            if (HP2SR.seedMode && curRandom != null && !ignore)
            {
                float prevResult = __result;
                __result = (float)curRandom.NextDouble();

                string upOneLevel = (new System.Diagnostics.StackTrace()).GetFrame(2).GetMethod().Name;
                string upTwoLevels = (new System.Diagnostics.StackTrace()).GetFrame(3).GetMethod().Name;
                BasePatches.Logger.LogMessage(upTwoLevels + " called " + upOneLevel + " called RandomFloat: " + min + "," + max + "," + __result + ", orig result: " + prevResult);
            }
        }

        //Continue game = check if file is same as the run
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiTitleCanvas), "LoadGame")]
        public static void CheckFileID(ref int saveFileIndex)
        {
            if (HP2SR.seedMode)
            {
                if (HP2SR.seedString != "") RNGPatches.InitializeRNG(int.Parse(HP2SR.seedString));
                else RNGPatches.InitializeRNG(int.Parse(HP2SR.defaultSeed));
            }
            else
            {
                RNGPatches.WipeRNG();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CutsceneStepSpecialPostRewards), "Start")]
        public static void SeededNotif()
        {
            if (HP2SR.seedMode)
                HP2SR.ShowThreeNotif("Seeded Run: " + ourSeed);
        }

        [HarmonyPrefix]
        //[HarmonyPatch(typeof(GiftManager), "GetRandomFruit", typeof(PuzzleAffectionType))] this just randomizes the fruit's sprite
        [HarmonyPatch(typeof(GiftManager), "GetRandomFruit", typeof(GirlDefinition))]
        [HarmonyPatch(typeof(GiftManager), "GetRandomFruit", new Type[] { })]
        [HarmonyPatch(typeof(CutsceneStepSpecialPostRewards), "Start")]
        public static void FruitRNG()
        {
            curRandom = seedRandom;
        }
        [HarmonyPostfix]
        //[HarmonyPatch(typeof(GiftManager), "GetRandomFruit", typeof(PuzzleAffectionType))]
        [HarmonyPatch(typeof(GiftManager), "GetRandomFruit", typeof(GirlDefinition))]
        [HarmonyPatch(typeof(GiftManager), "GetRandomFruit", new Type[] { })]
        [HarmonyPatch(typeof(CutsceneStepSpecialPostRewards), "Start")]
        public static void EndFruitRNG()
        {
            curRandom = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleStatus), "NextRound")]
        public static void NonstopRNG()
        {
            curRandom = nonstopRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleStatus), "NextRound")]
        public static void EndNonstopRNG()
        {
            curRandom = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleStatusGirl), "PopulateAilments")]
        public static void AilmentRNG(bool random)
        {
            if (random) curRandom = nonstopRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleStatusGirl), "PopulateAilments")]
        public static void EndAilmentRNG(bool random)
        {
            if (random) curRandom = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleStatusGirl), "AlterAilment")]
        [HarmonyPatch(typeof(AilmentTrigger), "Trigger")]
        [HarmonyPatch(typeof(AbilityManager), "DefineValue")]
        public static void AlterAilmentRNG()
        {
            curRandom = dateRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleStatusGirl), "AlterAilment")]
        [HarmonyPatch(typeof(AilmentTrigger), "Trigger")]
        [HarmonyPatch(typeof(AbilityManager), "DefineValue")]
        public static void EndAlterAilmentRNG()
        {
            curRandom = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "StartPuzzle")]
        public static void PerDateRNG()
        {
            foreach (PuzzleStatusToken token in Game.Session.Puzzle.puzzleStatus.tokenStatus)
            {
                Datamining.Logger.LogMessage(token.tokenDefinition.tokenName + ": " + token.GetCurrentWeight());
            }

            if (!HP2SR.seedMode || dateRandom == null) return;
            if (CheatPatches.refreshingPuzzle)
            {
                
            }
            else if (dateRandom != null)
            {
                currentDateSeed = dateRandom.Next();
                currentPowerSeed = dateRandom.Next();
            }
            CheatPatches.refreshingPuzzle = false;
            curDateRandom = new System.Random(currentDateSeed);
            powerTokenRandom = new System.Random(currentPowerSeed);
            inPuzzleBegin = true;
            curRandom = curDateRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "StartPuzzle")]
        public static void EndInitialRNG()
        {
            inPuzzleBegin = false;
            curRandom = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "EndPuzzle")]
        public static void EndRNG()
        {
            curDateRandom = null;
            powerTokenRandom = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "CreateToken")]
        public static void TokenRNGPre()
        {
            curRandom = curDateRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "CreateToken")]
        public static void TokenRNGPost()
        {
            if (!inPuzzleBegin) curRandom = null;
        }
        // start manipping power token RNG when the power token chance is looked at
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleManager), "GetPuzzleOffset")]
        public static void PowerRNGPre(string offsetName)
        {
            if (offsetName == "power_token_chance") curRandom = powerTokenRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleSet), "GetMatchRewards")]
        public static void PowerRNGPost()
        {
            curRandom = null;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "ChangeSlotTokens")]
        public static void CompactRNGPre()
        {
            curRandom = curDateRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "ChangeSlotTokens")]
        public static void CompactRNGPost()
        {
            curRandom = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TalkManager), "TalkWith")]
        [HarmonyPatch(typeof(TalkManager), "TalkStep")]
        public static void TalkPre()
        {
            curRandom = talkRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TalkManager), "TalkWith")]
        [HarmonyPatch(typeof(TalkManager), "TalkStep")]
        public static void TalkPost()
        {
            curRandom = null;
        }

        public static void ListUtilsShuffleList<T>(List<T> list, System.Random random)
        {
            int i = list.Count;
            while (i > 1)
            {
                i--;
                int index = random.Next(i + 1);
                Datamining.Logger.LogMessage("advanced RNG in ListUtilsShuffleList");
                T value = list[index];
                list[index] = list[i];
                list[i] = value;
            }
        }


        //ListUtils.Shuffle isn't really patchable so I have to do this...

        //nymphojinn girl list unrandomizer
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleStatus), "Reset", typeof(List<GirlDefinition>), typeof(bool))]
        public static void SeedNymphos(List<GirlDefinition> girlList, bool nonstop)
        {
            if (!nonstop && girlList.Count > 2)
            {
                List<GirlDefinition> allBySpecial = Game.Data.Girls.GetAllBySpecial(false);
                ListUtilsShuffleList<GirlDefinition>(allBySpecial, nonstopRandom);
                while (allBySpecial.Count > 8)
                {
                    allBySpecial.RemoveAt(allBySpecial.Count - 1);
                }
                allBySpecial.Add(Game.Session.Puzzle.bossGirlPairDefinition.girlDefinitionOne);
                allBySpecial.Add(Game.Session.Puzzle.bossGirlPairDefinition.girlDefinitionTwo);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerFile), "PopulateFinderSlots")]
        public static bool SeedGirlFinder(PlayerFile __instance)
        {
            Datamining.Logger.LogMessage("In PopulateFinderSlots");

            if (!HP2SR.seedMode || finderRandom == null) return true;
            curRandom = new System.Random(finderRandom.Next());

            List<GirlDefinition> allBySpecial = Game.Data.Girls.GetAllBySpecial(false);
            Dictionary<GirlDefinition, PlayerFileGirl> dictionary = new Dictionary<GirlDefinition, PlayerFileGirl>();
            for (int i = 0; i < allBySpecial.Count; i++)
            {
                dictionary.Add(allBySpecial[i], __instance.GetPlayerFileGirl(allBySpecial[i]));
            }
            List<GirlPairDefinition> allBySpecial2 = Game.Data.GirlPairs.GetAllBySpecial(false);
            List<PlayerFileGirlPair> list = new List<PlayerFileGirlPair>();
            for (int j = 0; j < allBySpecial2.Count; j++)
            {
                list.Add(__instance.GetPlayerFileGirlPair(allBySpecial2[j]));
            }
            ListUtilsShuffleList<PlayerFileGirlPair>(list, curRandom);
            if (__instance.girlPairDefinition != null)
            {
                AccessTools.Method(typeof(PlayerFile), "AddToFinderLists").Invoke(__instance, new object[] { null, null, __instance.girlPairDefinition, null, list });
                //__instance.AddToFinderLists(null, null, __instance.girlPairDefinition, null, list);
            }
            for (int k = 0; k < list.Count; k++)
            {
                if (!dictionary[list[k].girlPairDefinition.girlDefinitionOne].playerMet && !dictionary[list[k].girlPairDefinition.girlDefinitionTwo].playerMet)
                {
                    list.RemoveAt(k);
                    k--;
                }
            }
            List<GirlPairDefinition> list2 = new List<GirlPairDefinition>();
            List<LocationDefinition> list3 = new List<LocationDefinition>();
            for (int l = 0; l < list.Count; l++)
            {
                if (list[l].relationshipType == GirlPairRelationshipType.ATTRACTED && (ClockDaytimeType)((__instance.daytimeElapsed + (int)ClockDaytimeType.AFTERNOON) % 4) == list[l].girlPairDefinition.sexDaytime)
                {
                    l = (int)AccessTools.Method(typeof(PlayerFile), "AddToFinderLists").Invoke(__instance, new object[] { list2, list3, list[l].girlPairDefinition, null, list });
                    //l = __instance.AddToFinderLists(list2, list3, list[l].girlPairDefinition, null, list);
                }
            }
            for (int m = 0; m < list.Count; m++)
            {
                if (list[m].relationshipType == GirlPairRelationshipType.UNKNOWN && list[m].girlPairDefinition.introductionPair && list[m].girlPairDefinition.meetingLocationDefinition != __instance.locationDefinition && !list3.Contains(list[m].girlPairDefinition.meetingLocationDefinition))
                {
                    m = (int)AccessTools.Method(typeof(PlayerFile), "AddToFinderLists").Invoke(__instance, new object[] { list2, list3, list[m].girlPairDefinition, list[m].girlPairDefinition.meetingLocationDefinition, list });
                    //m = __instance.AddToFinderLists(list2, list3, list[m].girlPairDefinition, list[m].girlPairDefinition.meetingLocationDefinition, list);
                }
            }
            for (int n = 0; n < list.Count; n++)
            {
                if ((list[n].relationshipType == GirlPairRelationshipType.UNKNOWN && !list[n].girlPairDefinition.introductionPair && list[n].girlPairDefinition.meetingLocationDefinition != __instance.locationDefinition && !list3.Contains(list[n].girlPairDefinition.meetingLocationDefinition) && dictionary[list[n].girlPairDefinition.girlDefinitionOne].playerMet && dictionary[list[n].girlPairDefinition.girlDefinitionTwo].playerMet) || list[n].relationshipType == GirlPairRelationshipType.COMPATIBLE)
                {
                    n = (int)AccessTools.Method(typeof(PlayerFile), "AddToFinderLists").Invoke(__instance, new object[] { list2, list3, list[n].girlPairDefinition, (list[n].relationshipType == GirlPairRelationshipType.UNKNOWN) ? list[n].girlPairDefinition.meetingLocationDefinition : null, list });
                    //n = __instance.AddToFinderLists(list2, list3, list[n].girlPairDefinition, (list[n].relationshipType == GirlPairRelationshipType.UNKNOWN) ? list[n].girlPairDefinition.meetingLocationDefinition : null, list);
                }
            }
            for (int num = 0; num < list.Count; num++)
            {
                if (list[num].relationshipType == GirlPairRelationshipType.LOVERS)
                {
                    num = (int)AccessTools.Method(typeof(PlayerFile), "AddToFinderLists").Invoke(__instance, new object[] { list2, list3, list[num].girlPairDefinition, null, list });
                    //num = __instance.AddToFinderLists(list2, list3, list[num].girlPairDefinition, null, list);
                }
            }
            if (list2.Count <= 0)
            {
                for (int num2 = 0; num2 < list.Count; num2++)
                {
                    if (list[num2].relationshipType == GirlPairRelationshipType.ATTRACTED)
                    {
                        num2 = (int)AccessTools.Method(typeof(PlayerFile), "AddToFinderLists").Invoke(__instance, new object[] { list2, list3, list[num2].girlPairDefinition, null, list });
                        //num2 = __instance.AddToFinderLists(list2, list3, list[num2].girlPairDefinition, null, list);
                    }
                }
            }
            List<LocationDefinition> allByLocationType = Game.Data.Locations.GetAllByLocationType(LocationType.SIM);
            Dictionary<LocationDefinition, PlayerFileFinderSlot> dictionary2 = new Dictionary<LocationDefinition, PlayerFileFinderSlot>();
            for (int num3 = 0; num3 < allByLocationType.Count; num3++)
            {
                PlayerFileFinderSlot playerFileFinderSlot = __instance.GetPlayerFileFinderSlot(allByLocationType[num3]);
                playerFileFinderSlot.Clear();
                dictionary2.Add(allByLocationType[num3], playerFileFinderSlot);
            }
            if (allByLocationType.Contains(__instance.locationDefinition))
            {
                allByLocationType.Remove(__instance.locationDefinition);
            }
            ListUtils.RemoveRangeUnique<LocationDefinition>(allByLocationType, list3);
            for (int num4 = 0; num4 < list2.Count; num4++)
            {
                LocationDefinition locationDefinition = list3[num4];
                if (locationDefinition == null)
                {
                    locationDefinition = allByLocationType[UnityEngine.Random.Range(0, allByLocationType.Count)];
                    allByLocationType.Remove(locationDefinition);
                }
                bool flipped = (!list2[num4].specialPair && list2[num4].introductionPair && __instance.GetPlayerFileGirlPair(list2[num4]).relationshipType == GirlPairRelationshipType.UNKNOWN) ? list2[num4].introSidesFlipped : MathUtils.RandomBool();
                dictionary2[locationDefinition].Populate(list2[num4], flipped);
            }

            curRandom = null;
            return false;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerFile), "PopulateFinderSlots")]
        public static void LillianOrWhateverCheat(PlayerFile __instance)
        {
            Datamining.Logger.LogMessage(__instance.daytimeElapsed);
            if (__instance.daytimeElapsed != 8 || !HP2SR.seedMode || HP2SR.seedPair == 0)
            {
                return;
            }

            PlayerFileFinderSlot theLocation = __instance.GetPlayerFileFinderSlot(Game.Data.Locations.Get(firstPairToLoc[HP2SR.seedPair]));
            if (theLocation.girlPairDefinition == null || theLocation.girlPairDefinition.id != firstPairToInt[HP2SR.seedPair])
            {
                Datamining.Logger.LogMessage("Rerolling the Girl Finder slots for fixed pair!");
                __instance.PopulateFinderSlots(); //this will recursively end up calling this same prefix/postfix until the pair is there
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerFile), "PopulateStoreProducts")]
        public static bool SeedStoreContents(PlayerFile __instance)
        {
            if (!HP2SR.seedMode || storeRandom == null) return true;
            curRandom = storeRandom;

            List<PlayerFileStoreProduct> list = new List<PlayerFileStoreProduct>();
            for (int i = 0; i < 32; i++)
            {
                list.Add(__instance.GetPlayerFileStoreProduct(i));
            }
            List<ItemDefinition> list2 = new List<ItemDefinition>();
            List<int> list3 = new List<int>();
            Dictionary<ItemDefinition, int> dictionary = new Dictionary<ItemDefinition, int>();
            Dictionary<ItemDefinition, int> dictionary2 = new Dictionary<ItemDefinition, int>();
            Dictionary<ItemDefinition, int> dictionary3 = new Dictionary<ItemDefinition, int>();
            Dictionary<ItemDefinition, int> dictionary4 = new Dictionary<ItemDefinition, int>();
            List<ItemDefinition> list4 = new List<ItemDefinition>();
            List<ItemDefinition> allOfTypes = Game.Data.Items.GetAllOfTypes(new ItemType[]
            {
        ItemType.SMOOTHIE
            });
            List<ItemDefinition> list5 = new List<ItemDefinition>();
            for (int j = 0; j < allOfTypes.Count; j++)
            {
                int num = Mathf.Clamp(6 + __instance.GetBaggageCountByAffectionType(allOfTypes[j].affectionType, true) * 2, 0, 24);
                int affectionLevelExp = __instance.GetAffectionLevelExp(allOfTypes[j].affectionType, false);
                if (affectionLevelExp < num)
                {
                    ListUtils.AddManyOf<ItemDefinition>(list5, allOfTypes[j], num - affectionLevelExp);
                }
            }
            int num2 = 4;
            while (list2.Count < num2 && list5.Count > 0)
            {
                ItemDefinition itemDefinition = list5[UnityEngine.Random.Range(0, list5.Count)];
                if (!list2.Contains(itemDefinition))
                {
                    ListUtils.RemovePercentOf<ItemDefinition>(list5, itemDefinition, 0.5f);
                }
                else
                {
                    ListUtils.RemoveAllOf<ItemDefinition>(list5, itemDefinition);
                }
                list2.Add(itemDefinition);
                list3.Add(5);
            }
            List<ItemDefinition> allOfTypes2 = Game.Data.Items.GetAllOfTypes(new ItemType[]
            {
        ItemType.DATE_GIFT
            });
            List<ItemDefinition> list6 = new List<ItemDefinition>();
            list6.AddRange(allOfTypes2);
            int num3 = 12;
            while (dictionary.Count < num3 && list6.Count > 0)
            {
                ItemDefinition itemDefinition = list6[UnityEngine.Random.Range(0, list6.Count)];
                ListUtils.RemoveAllOf<ItemDefinition>(list6, itemDefinition);
                if (!__instance.IsItemInInventory(itemDefinition, true, 2) && (!itemDefinition.difficultyExclusive || itemDefinition.difficulty == __instance.settingDifficulty))
                {
                    dictionary.Add(itemDefinition, 6);
                }
            }
            List<GirlDefinition> allBySpecial = Game.Data.Girls.GetAllBySpecial(false);
            List<GirlDefinition> list7 = new List<GirlDefinition>();
            for (int k = 0; k < allBySpecial.Count; k++)
            {
                PlayerFileGirl playerFileGirl = __instance.GetPlayerFileGirl(allBySpecial[k]);
                int num4 = Mathf.Clamp(playerFileGirl.receivedShoes.Count + __instance.GetInventoryItemsCount(allBySpecial[k].shoesItemDefs, false), 0, allBySpecial[k].shoesItemDefs.Count);
                int num5 = allBySpecial[k].shoesItemDefs.Count - num4;
                if (num5 > 0 && num4 < playerFileGirl.learnedBaggage.Count + 1)
                {
                    ListUtils.AddManyOf<GirlDefinition>(list7, allBySpecial[k], num5 * num5);
                }
            }
            int num6 = 4;
            while (dictionary2.Count < num6 && list7.Count > 0)
            {
                GirlDefinition girlDefinition = list7[UnityEngine.Random.Range(0, list7.Count)];
                ListUtils.RemoveAllOf<GirlDefinition>(list7, girlDefinition);
                PlayerFileGirl playerFileGirl = __instance.GetPlayerFileGirl(girlDefinition);
                list4.Clear();
                list4.AddRange(girlDefinition.shoesItemDefs);
                for (int l = 0; l < list4.Count; l++)
                {
                    if (__instance.IsItemInInventory(list4[l], false, 1) || playerFileGirl.HasShoes(list4[l]))
                    {
                        list4.RemoveAt(l);
                        l--;
                    }
                }
                if (list4.Count > 0)
                {
                    ItemDefinition itemDefinition = list4[UnityEngine.Random.Range(0, list4.Count)];
                    dictionary2.Add(itemDefinition, 4);
                }
            }
            List<GirlDefinition> allBySpecial2 = Game.Data.Girls.GetAllBySpecial(false);
            List<GirlDefinition> list8 = new List<GirlDefinition>();
            for (int m = 0; m < allBySpecial2.Count; m++)
            {
                PlayerFileGirl playerFileGirl = __instance.GetPlayerFileGirl(allBySpecial2[m]);
                int num7 = Mathf.Clamp(playerFileGirl.receivedUniques.Count + __instance.GetInventoryItemsCount(allBySpecial2[m].uniqueItemDefs, false), 0, allBySpecial2[m].uniqueItemDefs.Count);
                int num8 = allBySpecial2[m].uniqueItemDefs.Count - num7;
                if (num8 > 0 && num7 < playerFileGirl.learnedBaggage.Count + 1)
                {
                    ListUtils.AddManyOf<GirlDefinition>(list8, allBySpecial2[m], num8 * num8);
                }
            }
            int num9 = 4;
            while (dictionary3.Count < num9 && list8.Count > 0)
            {
                GirlDefinition girlDefinition = list8[UnityEngine.Random.Range(0, list8.Count)];
                ListUtils.RemoveAllOf<GirlDefinition>(list8, girlDefinition);
                PlayerFileGirl playerFileGirl = __instance.GetPlayerFileGirl(girlDefinition);
                list4.Clear();
                list4.AddRange(girlDefinition.uniqueItemDefs);
                for (int n = 0; n < list4.Count; n++)
                {
                    if (__instance.IsItemInInventory(list4[n], false, 1) || playerFileGirl.HasUnique(list4[n]))
                    {
                        list4.RemoveAt(n);
                        n--;
                    }
                }
                if (list4.Count > 0)
                {
                    ItemDefinition itemDefinition = list4[UnityEngine.Random.Range(0, list4.Count)];
                    dictionary3.Add(itemDefinition, 4);
                }
            }
            List<ItemDefinition> allOfTypes3 = Game.Data.Items.GetAllOfTypes(new ItemType[]
            {
        ItemType.FOOD
            });
            ListUtilsShuffleList<ItemDefinition>(allOfTypes3, storeRandom);
            List<ItemDefinition> list9 = new List<ItemDefinition>();
            for (int num10 = 0; num10 < allOfTypes3.Count; num10++)
            {
                if (allOfTypes3[num10].noStaminaCost)
                {
                    list9.Add(allOfTypes3[num10]);
                    allOfTypes3.RemoveAt(num10);
                    num10--;
                }
            }
            __instance.staminaFoodLimit = 4;
            __instance.staminaFoodLimit = Mathf.Clamp(__instance.staminaFoodLimit, 0, 4);
            for (int num11 = 0; num11 < __instance.staminaFoodLimit; num11++)
            {
                allOfTypes3.Insert(0, list9[num11]);
            }
            int num12 = 32 - (list2.Count + dictionary.Count + dictionary2.Count + dictionary3.Count);
            while (dictionary4.Count < num12 && allOfTypes3.Count > 0)
            {
                ItemDefinition itemDefinition = allOfTypes3[0];
                allOfTypes3.Remove(itemDefinition);
                dictionary4.Add(itemDefinition, itemDefinition.storeCost);
            }
            Dictionary<ItemDefinition, int> dictionary5 = new Dictionary<ItemDefinition, int>();
            ListUtils.DictionaryAddRangeUnique<ItemDefinition, int>(dictionary5, dictionary3);
            ListUtils.DictionaryAddRangeUnique<ItemDefinition, int>(dictionary5, dictionary2);
            ListUtils.DictionaryAddRangeUnique<ItemDefinition, int>(dictionary5, dictionary);
            ListUtils.DictionaryAddRangeUnique<ItemDefinition, int>(dictionary5, dictionary4);
            List<ItemDefinition> list10 = new List<ItemDefinition>(dictionary5.Keys);
            List<int> indexList = ListUtils.GetIndexList(32, 0);
            int num13 = 8;
            List<List<int>> list11 = new List<List<int>>
    {
        ListUtils.GetIndexList(num13, 0),
        ListUtils.GetIndexList(num13, num13),
        ListUtils.GetIndexList(num13, num13 * 2),
        ListUtils.GetIndexList(num13, num13 * 3)
    };
            for (int num14 = 0; num14 < list2.Count; num14++)
            {
                List<int> list12 = (list11[(int)list2[num14].affectionType].Count > 0) ? list11[(int)list2[num14].affectionType] : indexList;
                int num15 = list12[UnityEngine.Random.Range(0, list12.Count)];
                indexList.Remove(num15);
                for (int num16 = 0; num16 < list11.Count; num16++)
                {
                    if (list11[num16].Contains(num15))
                    {
                        list11[num16].Remove(num15);
                    }
                }
                list[num15].Populate(list2[num14], list3[num14]);
            }
            for (int num17 = 0; num17 < list10.Count; num17++)
            {
                List<int> list12 = indexList;
                switch (list10[num17].itemType)
                {
                    case ItemType.FOOD:
                    case ItemType.DATE_GIFT:
                        if (list10[num17].storeSectionPreference && list11[(int)list10[num17].affectionType].Count > 0)
                        {
                            list12 = list11[(int)list10[num17].affectionType];
                        }
                        break;
                    case ItemType.SHOES:
                    case ItemType.UNIQUE_GIFT:
                        if (list11[(int)list10[num17].girlDefinition.favoriteAffectionType].Count > 0)
                        {
                            list12 = list11[(int)list10[num17].girlDefinition.favoriteAffectionType];
                        }
                        break;
                }
                int num15 = list12[UnityEngine.Random.Range(0, list12.Count)];
                indexList.Remove(num15);
                for (int num18 = 0; num18 < list11.Count; num18++)
                {
                    if (list11[num18].Contains(num15))
                    {
                        list11[num18].Remove(num15);
                    }
                }
                list[num15].Populate(list10[num17], dictionary5[list10[num17]]);
            }

            curRandom = null;
            return false;
        }


        [HarmonyPrefix]
        //[HarmonyPatch(typeof(EnergyTrailBehavior), "Init", typeof(EnergyTrailFormat), typeof(PuzzleReward), typeof(UiPuzzleSlot))]
        //[HarmonyPatch(typeof(EnergyTrailBehavior), "Init", typeof(EnergyTrailFormat), typeof(EnergyDefinition), typeof(ItemDefinition), typeof(UiDoll), typeof(string))]
        //[HarmonyPatch(typeof(EnergyTrailBehavior), "Init", typeof(EnergyTrailFormat), typeof(EnergyDefinition), typeof(Vector3), typeof(UiDoll), typeof(string), typeof(string))]
        [HarmonyPatch(typeof(EmitterParticle), "Start")]
        [HarmonyPatch(typeof(EmitterBehavior), "Init")]
        [HarmonyPatch(typeof(EmitterBehavior), "Update")]
        [HarmonyPatch(typeof(EmitterBehavior), "RefreshSpawnDelay")]
        [HarmonyPatch(typeof(UiWindowActionBubbles), "OnActionBubbleEnter")]
        [HarmonyPatch(typeof(UiWindowDialogOptions), "OnOptionEnter")]
        public static void IgnorePre()
        {
            ignore = true;
        }

        [HarmonyPostfix]
        //[HarmonyPatch(typeof(EnergyTrailBehavior), "Init", typeof(EnergyTrailFormat), typeof(PuzzleReward), typeof(UiPuzzleSlot))]
        //[HarmonyPatch(typeof(EnergyTrailBehavior), "Init", typeof(EnergyTrailFormat), typeof(EnergyDefinition), typeof(ItemDefinition), typeof(UiDoll), typeof(string))]
        //[HarmonyPatch(typeof(EnergyTrailBehavior), "Init", typeof(EnergyTrailFormat), typeof(EnergyDefinition), typeof(Vector3), typeof(UiDoll), typeof(string), typeof(string))]
        [HarmonyPatch(typeof(EmitterParticle), "Start")]
        [HarmonyPatch(typeof(EmitterBehavior), "Init")]
        [HarmonyPatch(typeof(EmitterBehavior), "Update")]
        [HarmonyPatch(typeof(EmitterBehavior), "RefreshSpawnDelay")]
        [HarmonyPatch(typeof(UiWindowActionBubbles), "OnActionBubbleEnter")]
        [HarmonyPatch(typeof(UiWindowDialogOptions), "OnOptionEnter")]
        public static void IgnorePost()
        {
            ignore = false;
        }
        
    }
}
