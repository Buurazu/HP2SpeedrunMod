using System;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Collections;

namespace HP2SpeedrunMod
{
    public class Datamining
    {
        public static BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("Datamining");

        public static void ExperimentalAllPairsMod(bool specialsToo = false)
        {
            Dictionary<int, GirlPairDefinition> allPairs = (Dictionary<int, GirlPairDefinition>)AccessTools.Field(typeof(GirlPairData), "_definitions").GetValue(Game.Data.GirlPairs);
            if (specialsToo)
            {
                foreach (GirlDefinition g in Game.Data.Girls.GetAll())
                {
                    g.specialCharacter = false;
                    g.baggageItemDefs.Add(Game.Data.Ailments.Get(UnityEngine.Random.Range(1, 37)).itemDefinition);
                    g.baggageItemDefs.Add(Game.Data.Ailments.Get(UnityEngine.Random.Range(1, 37)).itemDefinition);
                    g.baggageItemDefs.Add(Game.Data.Ailments.Get(UnityEngine.Random.Range(1, 37)).itemDefinition);
                    if (g.cellphoneHead == null) { g.cellphoneHead = g.cellphonePortrait; }
                }
            }
            List<GirlPairDefinition> stockPairs = Game.Data.GirlPairs.GetAllBySpecial(false);
            int startingID = 27;
            int maxGirls = 12;
            if (specialsToo)
            {
                startingID = 69;
                maxGirls = 15;
            }
            for (int i = 1; i <= maxGirls-1; i++)
            {
                for (int j = i+1; j <= maxGirls; j++)
                {
                    bool addThis = true;
                    for (int k = 0; k < stockPairs.Count; k++)
                    {
                        if (stockPairs[k].HasGirlDef(Game.Data.Girls.Get(i)) && stockPairs[k].HasGirlDef(Game.Data.Girls.Get(j)))
                        {
                            addThis = false;
                        }
                    }
                    if (addThis)
                    {
                        GirlPairDefinition gpd = ScriptableObject.CreateInstance<GirlPairDefinition>();
                        gpd.girlDefinitionOne = Game.Data.Girls.Get(i);
                        gpd.girlDefinitionTwo = Game.Data.Girls.Get(j);
                        gpd.id = startingID;
                        startingID++;
                        gpd.specialPair = false;
                        gpd.introductionPair = false;
                        gpd.introSidesFlipped = false;
                        gpd.photoDefinition = Game.Data.Photos.Get(1);
                        gpd.meetingLocationDefinition = Game.Data.Locations.GetAllByLocationType(LocationType.SIM)[0];
                        gpd.hasMeetingStyleOne = false;
                        gpd.meetingStyleTypeOne = GirlStyleType.SEXY;
                        gpd.meetingStyleTypeTwo = GirlStyleType.SEXY;
                        gpd.hasMeetingStyleTwo = false;
                        gpd.sexDaytime = ClockDaytimeType.AFTERNOON;
                        gpd.sexLocationDefinition = Game.Data.Locations.GetAllByLocationType(LocationType.DATE)[0];
                        gpd.sexStyleTypeOne = GirlStyleType.SEXY;
                        gpd.sexStyleTypeTwo = GirlStyleType.SEXY;
                        gpd.relationshipCutsceneDefinitions = Game.Data.GirlPairs.Get(1).relationshipCutsceneDefinitions;
                        List<GirlPairFavQuestionSubDefinition> Qs = new List<GirlPairFavQuestionSubDefinition>();
                        //oops all this code was actually useless, no unused pairs have any similarities
                        /*
                        for (int answer = 0; answer < gpd.girlDefinitionOne.favAnswers.Count && answer < gpd.girlDefinitionTwo.favAnswers.Count; answer++)
                        {
                            if (gpd.girlDefinitionOne.favAnswers[answer] == gpd.girlDefinitionTwo.favAnswers[answer])
                            {
                                GirlPairFavQuestionSubDefinition q = new GirlPairFavQuestionSubDefinition();
                                q.questionDefinition = Game.Data.Questions.Get(answer + 1);
                                q.girlResponseIndexOne = Qs.Count;
                                q.girlResponseIndexTwo = Qs.Count;
                                Qs.Add(q);
                            }
                        }*/
                        //add a "random" favorite question just so the game doesn't crash
                        if (Qs.Count == 0)
                        {
                            GirlPairFavQuestionSubDefinition q = new GirlPairFavQuestionSubDefinition();
                            q.questionDefinition = Game.Data.Questions.Get(gpd.id % 20 + 1);
                            q.girlResponseIndexOne = Qs.Count;
                            q.girlResponseIndexTwo = Qs.Count;
                            Qs.Add(q);
                        }
                        gpd.favQuestions = Qs;

                        allPairs.Add(gpd.id, gpd);
                    }
                }
            }
            
            GirlPairDefinition test = Game.Data.GirlPairs.GetAllBySpecial(false)[Game.Data.GirlPairs.GetAllBySpecial(false).Count - 1];
            Logger.LogDebug("pair count: " + Game.Data.GirlPairs.GetAllBySpecial(false).Count);
            foreach (GirlPairDefinition tester in Game.Data.GirlPairs.GetAllBySpecial(false))
            {
                Logger.LogDebug(tester.girlDefinitionOne.girlName + " " + tester.girlDefinitionTwo.girlName + " " + tester.id);
            }
        }

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
                        if (lines[a].dialogLineSets[b].dialogLines[c].yuri)
                            huge += a + "," + b + "," + c + " ALT: " + PurifyDialogText(lines[a].dialogLineSets[b].dialogLines[c].yuriDialogText) + "\n";
                    }
                    //is it always null?
                    if (lines[a].responseTriggerDefinition != null)
                    {
                            for (int c = 0; c < lines[a].responseTriggerDefinition.dialogLineSets[b].dialogLines.Count; c++)
                            {
                                huge += "  " + a + "," + b + "," + c + ": " + PurifyDialogText(lines[a].responseTriggerDefinition.dialogLineSets[b].dialogLines[c].dialogText) + "\n";
                                if (lines[a].responseTriggerDefinition.dialogLineSets[b].dialogLines[c].yuri)
                                    huge += a + "," + b + "," + c + " ALT: " + PurifyDialogText(lines[a].responseTriggerDefinition.dialogLineSets[b].dialogLines[c].yuriDialogText) + "\n";
                        }
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

        public static string GetQuestionQuizlet(GirlDefinition g)
        {
            string questions = "";

            List<DialogTriggerDefinition> lines = Game.Data.DialogTriggers.GetAll();
            List<DialogLine> herQuestions = lines[51].dialogLineSets[g.id].dialogLines;
            List<GirlQuestionSubDefinition> herAnswers = g.herQuestions;

            List<string> QandAs = new List<string>();
            for (int a = 0; a < herQuestions.Count; a++)
            {
                string tempq = PurifyDialogText(herQuestions[a].dialogText) + "\n    ";
                foreach (GirlQuestionAnswerSubDefinition gqa in g.herQuestions[a].answers)
                {
                    if (gqa.responseIndex == -1)
                    {
                        tempq += gqa.answerText;
                    }
                }
                QandAs.Add(tempq);
            }

            QandAs.Sort((x,y) => x.CompareTo(y));
            foreach (string s in QandAs)
            {
                questions += s + "\n";
            }

            return questions;
        }

        public static void GetGirlData()
        {
            //couldn't find a quick way to get these strings so i got them manually
            Dictionary<string, string> convertPairQuestions = new Dictionary<string, string>();
            convertPairQuestions.Add("Movie Genre", "movie genre");
            convertPairQuestions.Add("Phone App", "app on your phone");
            convertPairQuestions.Add("Theme Park Ride", "theme park ride");
            convertPairQuestions.Add("Holiday", "holiday");
            convertPairQuestions.Add("Outdoor Activity", "outdoor activity");
            convertPairQuestions.Add("Sunday Morning", "Sunday morning");
            convertPairQuestions.Add("Place to Shop", "place to go shopping");
            convertPairQuestions.Add("Trait in Partner", "trait to have in a partner");
            convertPairQuestions.Add("Own Body Part", "part about your own body");
            convertPairQuestions.Add("Porn Category", "category of porn");
            convertPairQuestions.Add("Ice Cream Flavor", "flavor of ice cream");
            convertPairQuestions.Add("Friday Night", "Friday night");
            convertPairQuestions.Add("Weather", "kind of weather");
            convertPairQuestions.Add("Pet", "type of pet");
            convertPairQuestions.Add("School Subject", "subject in high school");
            convertPairQuestions.Add("Drink", "thing to drink");
            convertPairQuestions.Add("Music Genre", "music genre");
            convertPairQuestions.Add("Online Activity", "thing to do online");
            convertPairQuestions.Add("Type of Exercise", "way to exercise");
            convertPairQuestions.Add("Sex Position", "sex position");

            Dictionary<int, string> girlIDToHotkey = new Dictionary<int, string>();
            for (int i = 1; i <= 9; i++) girlIDToHotkey.Add(i, i.ToString());
            girlIDToHotkey.Add(10, "0");
            girlIDToHotkey.Add(11, "-");
            girlIDToHotkey.Add(12, "=");

            Logger.LogDebug(Game.Persistence.playerFile.girls.Count + " girls");

            List<GirlDefinition> allBySpecial = Game.Data.Girls.GetAllBySpecial(false);

            List<GirlPairDefinition> allBySpecial2 = Game.Data.GirlPairs.GetAllBySpecial(false);

            foreach (GirlDefinition g in allBySpecial)
            {
                Logger.LogDebug(g.girlName + " " + g.id);
            }
            List<GirlDefinition> allSpecialGirls = Game.Data.Girls.GetAllBySpecial(true);
            foreach (GirlDefinition g in allSpecialGirls)
            {
                Logger.LogDebug(g.girlName + " " + g.id);
            }

            foreach (PlayerFileGirl pfg in Game.Persistence.playerFile.girls)
            {
                Logger.LogDebug(pfg.girlDefinition.girlName);
            }

            string data = "";

            //output unordered girl info list
            foreach (GirlDefinition g in allBySpecial)
            {
                data += g.girlName + " (" + girlIDToHotkey[g.id] + "): Loves " + g.favoriteAffectionType + ", Hates " + g.leastFavoriteAffectionType + "\n";
                data += g.uniqueType + " (" + g.uniqueAdj + "), " + g.shoesType + " (" + g.shoesAdj + ")\n";
                data += GetQuestionQuizlet(g) + "\n";
            }
            data += "\n";

            allBySpecial.Sort((x, y) => x.girlName.CompareTo(y.girlName));

            //output alphabetical girl info list
            foreach (GirlDefinition g in allBySpecial)
            {
                data += g.girlName + " (" + girlIDToHotkey[g.id] + "): Loves " + g.favoriteAffectionType + ", Hates " + g.leastFavoriteAffectionType + "\n";
                data += g.uniqueType + " (" + g.uniqueAdj + "), " + g.shoesType + " (" + g.shoesAdj + ")\n";
                data += GetQuestionQuizlet(g) + "\n";
            }

            //output unordered pair info list
            foreach (GirlPairDefinition g in allBySpecial2)
            {
                GirlDefinition g1 = g.girlDefinitionOne; GirlDefinition g2 = g.girlDefinitionTwo;
                data += g1.girlName + " + " + g2.girlName + "; " + g2.girlName + " + " + g1.girlName +
                    " (" + g1.favoriteAffectionType + ", " + g2.favoriteAffectionType + ")\n";
                if (g.introductionPair) data += "Introduction Pair: YES, ";
                data += "Meeting Location: " + g.meetingLocationDefinition.locationName + ", Sex Day Phase: " + g.sexDaytime + "\n";
                data += "Favorite Questions: ";
                List<GirlPairFavQuestionSubDefinition> qs = g.favQuestions;

                foreach (GirlPairFavQuestionSubDefinition q in qs) {
                    if (convertPairQuestions.ContainsKey(q.questionDefinition.questionName)) q.questionDefinition.questionName = convertPairQuestions[q.questionDefinition.questionName];
                }
                qs.Sort((x, y) => x.questionDefinition.questionName.CompareTo(y.questionDefinition.questionName));
                foreach (GirlPairFavQuestionSubDefinition q in qs)
                {
                    data += q.questionDefinition.questionName + ", ";
                }
                data = data.Substring(0, data.Length - 2);
                data += "\n\n";
            }
            data += "\n";
            foreach (GirlPairDefinition g in allBySpecial2)
            {
                //swap pairs so earlier name is always girldefinitionone
                if (g.girlDefinitionOne.girlName.CompareTo(g.girlDefinitionTwo.girlName) > 0)
                {
                    GirlDefinition temp = g.girlDefinitionOne;
                    g.girlDefinitionOne = g.girlDefinitionTwo;
                    g.girlDefinitionTwo = temp;
                }
            }

            allBySpecial2.Sort((x, y) => {
                int result = x.girlDefinitionOne.girlName.CompareTo(y.girlDefinitionOne.girlName);
                if (result == 0)
                {
                    result = x.girlDefinitionTwo.girlName.CompareTo(y.girlDefinitionTwo.girlName);
                }
                return result; }
            );
            //allBySpecial2.Sort((x, y) => { if (x.girlDefinitionOne.girlName == y.girlDefinitionOne.girlName) return x.girlDefinitionTwo.girlName.CompareTo(y.girlDefinitionTwo.girlName); else return 0; });
            
            foreach (GirlPairDefinition g in allBySpecial2)
            {
                GirlDefinition g1 = g.girlDefinitionOne; GirlDefinition g2 = g.girlDefinitionTwo;
                data += g1.girlName + " + " + g2.girlName + "; " + g2.girlName + " + " + g1.girlName +
                    " (" + g1.favoriteAffectionType + ", " + g2.favoriteAffectionType + ")\n";
                if (g.introductionPair) data += "Introduction Pair: YES, ";
                data += "Meeting Location: " + g.meetingLocationDefinition.locationName + ", Sex Day Phase: " + g.sexDaytime + "\n";
                data += "Favorite Questions: ";
                List<GirlPairFavQuestionSubDefinition> qs = g.favQuestions;

                foreach (GirlPairFavQuestionSubDefinition q in qs)
                {
                    if (convertPairQuestions.ContainsKey(q.questionDefinition.questionName)) q.questionDefinition.questionName = convertPairQuestions[q.questionDefinition.questionName];
                }
                qs.Sort((x, y) => x.questionDefinition.questionName.CompareTo(y.questionDefinition.questionName));
                foreach (GirlPairFavQuestionSubDefinition q in qs)
                {
                    data += q.questionDefinition.questionName + ", ";
                }
                data = data.Substring(0, data.Length - 2);
                data += "\n\n";
            }
            Logger.LogDebug(data);

            string huge = "";
            for (int a = 0; a < allBySpecial.Count; a++)
            {
                for (int b = 0; b < allBySpecial[a].herQuestions.Count; b++)
                {
                    for (int c = 0; c < allBySpecial[a].herQuestions[b].answers.Count; c++)
                    {
                        huge += a + "," + b + "," + c + ": " + PurifyDialogText(allBySpecial[a].herQuestions[b].answers[c].answerText) + " (" + allBySpecial[a].herQuestions[b].answers[c].responseIndex + ")\n";
                        if (allBySpecial[a].herQuestions[b].answers[c].hasAlt)
                            huge += a + "," + b + "," + c + " ALT: " + PurifyDialogText(allBySpecial[a].herQuestions[b].answers[c].answerTextAlt) + " (" + allBySpecial[a].herQuestions[b].answers[c].responseIndex + ")\n";

                    }
                }


            }


            //Logger.LogDebug(huge);

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
                        if (lines[a].steps[b].dialogOptions[c].yuri)
                            huge += a + "," + b + "," + c + " ALT: " + PurifyDialogText(lines[a].steps[b].dialogOptions[c].yuriDialogOptionText) +
                            " (" + lines[a].steps[b].dialogOptions[c].steps.Count + ")\n";
                        for (int d = 0; d < lines[a].steps[b].dialogOptions[c].steps.Count; d++)
                        {
                            huge += a + "," + b + "," + c + "," + d + ": " + PurifyDialogText(lines[a].steps[b].dialogOptions[c].steps[d].dialogLine.dialogText) +
                               " (" + lines[a].steps[b].dialogOptions[c].steps[d].dialogOptions.Count + ")\n";
                            if (lines[a].steps[b].dialogOptions[c].steps[d].dialogLine.yuri)
                                huge += a + "," + b + "," + c + "," + d + ": " + PurifyDialogText(lines[a].steps[b].dialogOptions[c].steps[d].dialogLine.yuriDialogText) +
                               " (" + lines[a].steps[b].dialogOptions[c].steps[d].dialogOptions.Count + ")\n";
                        }
                    }
                    //Game.Manager.Audio.Play(AudioCategory.VOICE, lines[a].steps[b].audioKlip);
                    huge += a + "," + b + ": " + PurifyDialogText(lines[a].steps[b].dialogLine.dialogText) + "\n";
                    if (lines[a].steps[b].dialogLine.yuri)
                        huge += a + "," + b + " ALT: " + PurifyDialogText(lines[a].steps[b].dialogLine.yuriDialogText) + "\n";
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

        private static int AddToFinderLists(List<GirlPairDefinition> girlPairList, List<LocationDefinition> locationList, GirlPairDefinition girlPairDef, LocationDefinition locationDef, List<PlayerFileGirlPair> fileGirlPairs)
        {
            if (girlPairList != null && !girlPairList.Contains(girlPairDef))
            {
                girlPairList.Add(girlPairDef);
                if (locationList != null && (locationDef == null || !locationList.Contains(locationDef)))
                {
                    locationList.Add(locationDef);
                }
            }
            for (int i = 0; i < fileGirlPairs.Count; i++)
            {
                if (fileGirlPairs[i].girlPairDefinition.girlDefinitionOne == girlPairDef.girlDefinitionOne || fileGirlPairs[i].girlPairDefinition.girlDefinitionTwo == girlPairDef.girlDefinitionOne || fileGirlPairs[i].girlPairDefinition.girlDefinitionOne == girlPairDef.girlDefinitionTwo || fileGirlPairs[i].girlPairDefinition.girlDefinitionTwo == girlPairDef.girlDefinitionTwo)
                {
                    fileGirlPairs.RemoveAt(i);
                    i--;
                }
            }
            return -1;
        }

        public static void TestAllPermutations()
        {
            PlayerFile file = Game.Persistence.playerFile;
            List<GirlPairDefinition> allPairs = Game.Data.GirlPairs.GetAllBySpecial(false);
            List<int> possiblePairs = new List<int>();
            Dictionary<int, int> occurences = new Dictionary<int, int>();
            Dictionary<int, int> phaseoccurences = new Dictionary<int, int>();
            phaseoccurences.Add((int)ClockDaytimeType.MORNING, 0);
            phaseoccurences.Add((int)ClockDaytimeType.AFTERNOON, 0);
            phaseoccurences.Add((int)ClockDaytimeType.EVENING, 0);
            phaseoccurences.Add((int)ClockDaytimeType.NIGHT, 0);
            Dictionary<GirlDefinition, bool> impossibleGirls = new Dictionary<GirlDefinition, bool>();
            foreach (GirlPairDefinition p in allPairs)
            {
                //ignore pairs that have a girl I'm currently with
                if (file.girlPairDefinition && (file.girlPairDefinition.girlDefinitionOne == p.girlDefinitionOne || file.girlPairDefinition.girlDefinitionTwo == p.girlDefinitionOne ||
                    file.girlPairDefinition.girlDefinitionOne == p.girlDefinitionTwo || file.girlPairDefinition.girlDefinitionTwo == p.girlDefinitionTwo))
                    continue;
                //ignore pairs who meet where i'm at currently
                if (file.GetPlayerFileGirlPair(p).relationshipType == GirlPairRelationshipType.UNKNOWN && p.meetingLocationDefinition == file.locationDefinition)
                    continue;
                //ignore pairs with no known girl
                if (!file.GetPlayerFileGirl(p.girlDefinitionOne).playerMet && !file.GetPlayerFileGirl(p.girlDefinitionTwo).playerMet)
                    continue;
                //ignore lovers, and attracted pairs that can't fuck
                if (file.GetPlayerFileGirlPair(p).relationshipType == GirlPairRelationshipType.LOVERS)
                    continue;
                if (file.GetPlayerFileGirlPair(p).relationshipType == GirlPairRelationshipType.ATTRACTED && (file.daytimeElapsed + 1) % 4 != (int)p.sexDaytime)
                    continue;
                //non-intro pairs require us to have met both girls
                if (!p.introductionPair && (!file.GetPlayerFileGirl(p.girlDefinitionOne).playerMet || !file.GetPlayerFileGirl(p.girlDefinitionTwo).playerMet))
                    continue;
                //in the end our pair list should have: DTF pairs, valid intro and new pairs, and compatible pairs
                possiblePairs.Add(p.id);
                occurences.Add(p.id, 0);
            }
            //we now have the shuffled list of every order of girl pairs that could possibly exist. (line 17 of populatefinderslots, but smaller)
            //now just check through each like the game does
            //11 pairs = 39,916,800 permutations. This is too much
            if (possiblePairs.Count >= 9)
            {
                Logger.LogMessage("Number of possible pairs is too large to permute! (" + possiblePairs.Count + ")");
                return;
            }
            IList<IList<int>> permutations = Permute(possiblePairs.ToArray());
            Logger.LogMessage("Total permutations: " + permutations.Count);
            string str = "\n";

            foreach (IList<int> i in permutations)
            {
                //convert the permutated ints back into a girl pair list
                List<PlayerFileGirlPair> list = new List<PlayerFileGirlPair>();
                foreach (int j in i) list.Add(file.GetPlayerFileGirlPair(Game.Data.GirlPairs.Get(j)));
                //blank lists used to keep track of selected pairs and definitions
                List<GirlPairDefinition> list2 = new List<GirlPairDefinition>();
                List<LocationDefinition> list3 = new List<LocationDefinition>();
                
                //this code has some extraneous checks removed, because we already handled those checks earlier
                //like if we've met either girl or if the attracted pair is DTF
                for (int l = 0; l < list.Count; l++)
                {
                    if (list[l].relationshipType == GirlPairRelationshipType.ATTRACTED)
                    {
                        l = AddToFinderLists(list2, list3, list[l].girlPairDefinition, null, list);
                    }
                }
                for (int m = 0; m < list.Count; m++)
                {
                    if (list[m].relationshipType == GirlPairRelationshipType.UNKNOWN && list[m].girlPairDefinition.introductionPair && !list3.Contains(list[m].girlPairDefinition.meetingLocationDefinition))
                    {
                        m = AddToFinderLists(list2, list3, list[m].girlPairDefinition, list[m].girlPairDefinition.meetingLocationDefinition, list);
                    }
                }
                for (int n = 0; n < list.Count; n++)
                {
                    if ((list[n].relationshipType == GirlPairRelationshipType.UNKNOWN && !list[n].girlPairDefinition.introductionPair && !list3.Contains(list[n].girlPairDefinition.meetingLocationDefinition)) || list[n].relationshipType == GirlPairRelationshipType.COMPATIBLE)
                    {
                        n = AddToFinderLists(list2, list3, list[n].girlPairDefinition, (list[n].relationshipType == GirlPairRelationshipType.UNKNOWN) ? list[n].girlPairDefinition.meetingLocationDefinition : null, list);
                    }
                }

                //list2 is now all the girls
                string testOutput = "";
                int[] foundtimes = new int[4];
                foreach (GirlPairDefinition gpd in list2) { 
                    testOutput += gpd.girlDefinitionOne.girlName + "&" + gpd.girlDefinitionTwo.girlName + ", ";
                    occurences[gpd.id] = occurences[gpd.id] + 1;
                    //i think the sex daytime info is only useful if it's not a DTF pair
                    if (file.GetPlayerFileGirlPair(gpd).relationshipType != GirlPairRelationshipType.ATTRACTED) foundtimes[(int)gpd.sexDaytime] += 1;
                }
                for (int t = 0; t <= 3; t++)
                {
                    if (foundtimes[t] > 0)
                    {
                        phaseoccurences[t] = phaseoccurences[t] + 1;
                    }
                }
                //Logger.LogDebug(testOutput);
            }
            //this is so lazy lol
            for (int i = 0; i < 30; i++)
            {
                if (occurences.ContainsKey(i) && occurences[i] > 0)
                    str += Game.Data.GirlPairs.Get(i).girlDefinitionOne.girlName + " & " + Game.Data.GirlPairs.Get(i).girlDefinitionTwo.girlName + 
                        //" (" + file.GetPlayerFileGirlPair(Game.Data.GirlPairs.Get(i)).relationshipType + ")" +
                        ": " + occurences[i] + "/" + permutations.Count + " (" + ((occurences[i] / (float)permutations.Count)*100).ToString("F") + "%)\n";
            }
            str += "\n";
            for (int i = 0; i <= 3; i++)
            {
                str += "New " + (ClockDaytimeType)i + " Sex Pair Chance: " +
                    phaseoccurences[i] + "/" + permutations.Count + " (" + ((phaseoccurences[i] / (float)permutations.Count) * 100).ToString("F") + "%)\n";
            }

            Logger.LogMessage(str);
        }

        public static void TestAllPermutationsOld()
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
