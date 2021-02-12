using System;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Net;

namespace HP2SpeedrunMod
{
    public class Datamining
    {
        public static BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("Datamining");

        public static void GetAllDialogTriggers()
        {
            string huge = "";
            List<DialogTriggerDefinition> lines = Game.Data.DialogTriggers.GetAll();
            for (int a = 0; a < lines.Count; a++)
            {
                for (int b = 0; b < lines[a].dialogLineSets.Count; b++)
                {
                    for (int c = 0; c < lines[a].dialogLineSets[b].dialogLines.Count; c++)
                    {
                        huge += a + "," + b + "," + c + ": " + PurifyDialogText(lines[a].dialogLineSets[b].dialogLines[c].dialogText) + "\n";
                    }
                }
            }
            Logger.LogDebug(huge);
        }

        public static void LocationInfo()
        {
            string huge = "";
            LocationManager lm = Game.Session.Location;
            foreach (DateLocationsInfo dli in lm.dateLocationsInfos)
            {
                huge += dli.daytimeType + ": ";
                foreach (LocationDefinition l in dli.locationDefinitions)
                {
                    huge += l.locationName + " (" + (int)l.dateGirlStyleType + "), ";
                }
                huge += "\n";
            }
            Logger.LogDebug(huge);
        }

        public static void EndlessFruits()
        {
            Console.WriteLine("Normal going rate post-game is 20 Fruits per 5000 Affection: 1/" + (5000 / 20));
            for (int j = 0; j < 100; j++)
            {
                int num = 0;
                int num2 = 2;
                int affectionEarned = 0;
                for (int i = 0; i < j; i++)
                {
                    if (i % 2 == 0 && i > 0)
                    {
                        num2 += 2;
                    }
                    num += num2;
                    affectionEarned += ((i + 1) * 250);
                }
                if (num > 0)
                {
                    Console.WriteLine("After " + j + " rounds: " + num + " Fruits, " + affectionEarned + " Affection: 1/" + affectionEarned / num);
                }
            }
        }
        public static void GetGirlData()
        {
            Logger.LogDebug(Game.Persistence.playerFile.girls.Count + " girls");

            List<GirlDefinition> allBySpecial = Game.Data.Girls.GetAllBySpecial(false);

            List<GirlPairDefinition> allBySpecial2 = Game.Data.GirlPairs.GetAllBySpecial(false);

            string data = "";
            Logger.LogDebug("test");
            foreach (GirlDefinition g in allBySpecial)
            {
                data += g.girlName + ": Loves " + g.favoriteAffectionType + ", Hates " + g.leastFavoriteAffectionType + "\n";
                data += g.uniqueType + " (" + g.uniqueAdj + "), " + g.shoesType + " (" + g.shoesAdj + ")\n\n";

            }
            foreach (GirlPairDefinition g in allBySpecial2)
            {
                GirlDefinition g1 = g.girlDefinitionOne; GirlDefinition g2 = g.girlDefinitionTwo;
                data += g1.girlName + " + " + g2.girlName + "; " + g2.girlName + " + " + g1.girlName +
                    " (" + g1.favoriteAffectionType + ", " + g2.favoriteAffectionType + ")\n";
                if (g.introductionPair) data += "Introduction Pair: YES, ";
                data += "Meeting Location: " + g.meetingLocationDefinition.locationName + ", Sex Day Phase: " + g.sexDaytime + "\n";
                data += "Favorite Questions: ";
                foreach (GirlPairFavQuestionSubDefinition q in g.favQuestions)
                {
                    data += q.questionDefinition.questionName + ", ";
                }
                data += "\n\n";
            }
            Logger.LogDebug(data);

            float num = (float)Mathf.Clamp(Game.Persistence.playerFile.storyProgress - 2, 0, 12) * 0.5f;
            Logger.LogDebug(num);
            for (int i = 0; i < 4; i++)
            {
                num += (float)Game.Persistence.playerFile.GetAffectionLevel((PuzzleAffectionType)i, true) * 0.625f;
                Logger.LogDebug(num);
            }
            Logger.LogDebug(num);

            for (int j = 0; j < Game.Persistence.playerFile.girls.Count; j++)
            {
                PlayerFileGirl playerFileGirl = Game.Persistence.playerFile.girls[j];
                if (!playerFileGirl.girlDefinition.specialCharacter && playerFileGirl.playerMet)
                {
                    num += (float)playerFileGirl.learnedBaggage.Count * 0.25f;
                    num += (float)playerFileGirl.receivedUniques.Count * 0.125f;
                    num += (float)playerFileGirl.receivedShoes.Count * 0.125f;
                    for (int k = 0; k < playerFileGirl.unlockedHairstyles.Count; k++)
                    {
                        if (k != playerFileGirl.girlDefinition.defaultHairstyleIndex && k != 6)
                        {
                            num += 0.046875f;
                        }
                    }
                    for (int l = 0; l < playerFileGirl.unlockedOutfits.Count; l++)
                    {
                        if (l != playerFileGirl.girlDefinition.defaultOutfitIndex && l != 6)
                        {
                            num += 0.046875f;
                        }
                    }
                }
                Logger.LogDebug(num);
            }
            for (int m = 0; m < Game.Persistence.playerFile.girlPairs.Count; m++)
            {
                PlayerFileGirlPair playerFileGirlPair = Game.Persistence.playerFile.girlPairs[m];
                if (!playerFileGirlPair.girlPairDefinition.specialPair)
                {
                    GirlPairRelationshipType relationshipType = playerFileGirlPair.relationshipType;
                    if (relationshipType >= GirlPairRelationshipType.COMPATIBLE)
                    {
                        num += 0.25f;
                    }
                    if (relationshipType >= GirlPairRelationshipType.ATTRACTED)
                    {
                        num += 1f;
                    }
                    if (relationshipType >= GirlPairRelationshipType.LOVERS)
                    {
                        num += 1f;
                    }
                }
            }
            Logger.LogDebug(num);
        }

        public static string PurifyDialogText(string text)
        {
            string INLINE_STYLER_START = "<color=#FFFFFF00>";
            while (text.Contains("["))
            {
                int num = text.IndexOf("[", StringComparison.CurrentCulture);
                int length = text.IndexOf("]", StringComparison.CurrentCulture) - num + 1;
                string text2 = text.Substring(num, length);
                text = text.Replace(text2, text2.Contains(INLINE_STYLER_START) ? INLINE_STYLER_START : "");
            }
            return text.Replace("▸", "");
        }

        public static void GetAllCutsceneLines()
        {
            string huge = "";
            List<CutsceneDefinition> lines = Game.Data.Cutscenes.GetAll();
            for (int a = 0; a < lines.Count; a++)
            {
                for (int b = 0; b < lines[a].steps.Count; b++)
                {
                    for (int c = 0; c < lines[a].steps[b].dialogOptions.Count; c++)
                    {
                        huge += a + "," + b + "," + c + ": " + PurifyDialogText(lines[a].steps[b].dialogOptions[c].dialogOptionText) +
                            " (" + lines[a].steps[b].dialogOptions[c].steps.Count + ")\n";
                        for (int d = 0; d < lines[a].steps[b].dialogOptions[c].steps.Count; d++)
                        {
                            huge += a + "," + b + "," + c + "," + d + ": " + PurifyDialogText(lines[a].steps[b].dialogOptions[c].steps[d].dialogLine.dialogText) +
                               " (" + lines[a].steps[b].dialogOptions[c].steps[d].dialogOptions.Count + ")\n";
                        }
                    }
                    //Game.Manager.Audio.Play(AudioCategory.VOICE, lines[a].steps[b].audioKlip);
                    huge += a + "," + b + ": " + PurifyDialogText(lines[a].steps[b].dialogLine.dialogText) + "\n";
                }
            }
            Logger.LogDebug(huge);
        }

        public static void GetAllItems()
        {
            string huge = "";
            List<ItemDefinition> items = Game.Data.Items.GetAll();
            for (int a = 0; a < items.Count; a++)
            {
                huge += items[a].itemName + "\n" + items[a].itemDescription + "\n" + items[a].costDescription + "\n" + items[a].categoryDescription + "\n" + items[a].storeCost + " Seeds\n" + items[a].useCost + "\n";
            }
            Logger.LogDebug(huge);
        }

        enum GetLaidPairs
        {
            LA, LN, AP, AL, SL, JL
        }

        public static void TestAllPermutations()
        {
            int[] options = new int[] { 0, 1, 2, 3, 4, 5 };
            IList<IList<int>> permutations = Permute(options);
            int totalSuccess = 0;
            int totalRuns = 0;
            foreach (IList<int> i in permutations)
            {
                totalRuns++;
                string temp = "";
                bool success = true;
                foreach (int j in i)
                {
                    temp += (GetLaidPairs)j + ",";
                }
                bool Lfound = false;
                bool Hotelfound = false;
                bool LAfound = false;
                foreach (int j in i)
                {
                    GetLaidPairs g = (GetLaidPairs)j;
                    //Ashley + Lillian
                    if (g == GetLaidPairs.AL)
                    {
                        break;
                    }
                    //Ashley + Polly
                    if (g == GetLaidPairs.AP)
                    {
                        success = false; break;
                    }
                    //Lola + Abia, before finding Lola + Nora
                    if (g == GetLaidPairs.LA && Lfound == false)
                    {
                        success = false; break;
                    }
                    if (g == GetLaidPairs.LN && Lfound == false && Hotelfound == false)
                    {
                        Hotelfound = true;
                        Lfound = true;
                    }
                    if (g == GetLaidPairs.SL && LAfound == false)
                    {
                        LAfound = true;
                    }
                    if (g == GetLaidPairs.JL && LAfound == false && Hotelfound == false)
                    {
                        Hotelfound = true;
                        LAfound = true;
                    }
                }
                if (success) totalSuccess++;
                temp += " " + totalSuccess + "/" + totalRuns;
                Logger.LogDebug(temp);
            }
        }

        static IList<IList<int>> Permute(int[] nums)
        {
            var list = new List<IList<int>>();
            return DoPermute(nums, 0, nums.Length - 1, list);
        }

        static IList<IList<int>> DoPermute(int[] nums, int start, int end, IList<IList<int>> list)
        {
            if (start == end)
            {
                // We have one of our possible n! solutions,
                // add it to the list.
                list.Add(new List<int>(nums));
            }
            else
            {
                for (var i = start; i <= end; i++)
                {
                    Swap(ref nums[start], ref nums[i]);
                    DoPermute(nums, start + 1, end, list);
                    Swap(ref nums[start], ref nums[i]);
                }
            }

            return list;
        }

        static void Swap(ref int a, ref int b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        public static void GetAllCutsceneLinesSimplified()
        {
            string huge = "";
            List<CutsceneDefinition> lines = Game.Data.Cutscenes.GetAll();
            for (int a = 0; a < lines.Count; a++)
            {
                for (int b = 0; b < lines[a].steps.Count; b++)
                {
                    if (lines[a].steps[b].dialogOptions.Count > 0)
                    {
                        huge += "\n" + a + "," + b + ": " + PurifyDialogText(lines[a].steps[b - 1].dialogLine.dialogText) + "\n";
                        int lowest = lines[a].steps[b].dialogOptions[0].steps.Count;
                        int highest = lines[a].steps[b].dialogOptions[0].steps.Count;
                        int fastest = 0;

                        for (int c = 0; c < lines[a].steps[b].dialogOptions.Count; c++)
                        {
                            int stepCount = lines[a].steps[b].dialogOptions[c].steps.Count;
                            if (stepCount > highest) highest = stepCount;
                            if (stepCount < lowest) { lowest = stepCount; fastest = c; }

                            huge += PurifyDialogText(lines[a].steps[b].dialogOptions[c].dialogOptionText) +
                                " (" + lines[a].steps[b].dialogOptions[c].steps.Count + ")\n";
                            for (int d = 0; d < lines[a].steps[b].dialogOptions[c].steps.Count; d++)
                            {
                                huge += d + ": " + PurifyDialogText(lines[a].steps[b].dialogOptions[c].steps[d].dialogLine.dialogText) +
                                   "\n";
                            }
                        }
                        if (lowest == highest) huge += "ALL OPTIONS EQUAL";
                        else if (fastest == 0) huge += "TOP OPTION FASTEST";
                        else if (fastest == 1) huge += "MIDDLE OPTION FASTEST";
                        else if (fastest == 2) huge += "BOTTOM OPTION FASTEST";
                        else huge += "FOURTH OPTION FASTEST?";
                        huge += "\n";

                    }
                    //Game.Manager.Audio.Play(AudioCategory.VOICE, lines[a].steps[b].audioKlip);
                    //huge += a + "," + b + ": " + PurifyDialogText(lines[a].steps[b].dialogLine.dialogText) + "\n";
                }
            }
            Logger.LogDebug(huge);
        }
    }
}
