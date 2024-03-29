﻿using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

namespace HP2SpeedrunMod
{
    public class RunTimer
    {
        public static BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("RunTimer");
        public enum SplitColors
        {
            WHITE, BLUE, RED, GOLD
        }
        public static string[] categories = new string[] { "1 Wing", "6 Wings", "12 Wings", "24 Wings", "48 Shoes", "100%" };
        public static int[] goals = new int[] { 1, 6, 12, 25, 48, 100 };
        public static string[] difficulties = new string[] { "Easy", "Normal", "Hard" };

        //white, blue, red, gold
        public static Color32[] outlineColors = new Color32[] { new Color32(98, 149, 252, 255), new Color(229, 36, 36, 255), new Color(240, 176, 18, 255) };
        //too dark
        //public static string[] darkColors = new string[] { "#ffffff", "#4bb7e8", "#e24c3c", "#ddaf4c" };
        //taken directly from the seed counts; too light
        //public static string[] colors = new string[] { "#ffffff", "#d6e9ff", "#ffcccc", "#ddaf4c" };
        public static string[] colors = new string[] { "#ffffff", "#b2d6ff", "#ffb2b2", "#ddaf4c" };

        public long runTimer;
        public int runFile;
        public string category;
        public int goal;
        public bool finishedRun;
        public List<TimeSpan> splits = new List<TimeSpan>();
        public List<bool> isBonus = new List<bool>();
        public List<TimeSpan> comparisonDates = new List<TimeSpan>();
        public List<TimeSpan> comparisonBonuses = new List<TimeSpan>();
        public List<TimeSpan> goldDates = new List<TimeSpan>();
        public List<TimeSpan> goldBonuses = new List<TimeSpan>();
        public string splitText;
        public string prevText;
        public string goldText;
        public SplitColors splitColor;
        public SplitColors prevColor;
        public SplitColors goldColor;
        public bool rerollOccurred;

        public string finalRunDisplay = "";

        //convert a timespan to the proper format string
        public static string convert(TimeSpan time)
        {
            string val = "";
            if (time.Hours != 0)
                val += time.ToString(@"h\:mm\:ss\.f");
            else if (time.Minutes != 0)
                val += time.ToString(@"m\:ss\.f");
            else
                val += time.ToString(@"s\.f");
            return val;
        }
        public static string GetAll(int cat, int difficulty)
        {
            if (cat >= categories.Length || difficulty >= difficulties.Length) return "N/A";
            string val = categories[cat] + " " + difficulties[difficulty] + "\nPB: " + GetPB(cat, difficulty) + "\nSoB: " + GetGolds(cat, difficulty);
            return val;
        }
        public static string GetPB(string category, bool chop = true)
        {
            string val = "N/A";
            string target = "splits/data/" + category + " Dates.txt";
            string target2 = "splits/data/" + category + " Bonuses.txt";
            if (File.Exists(target) && File.Exists(target2))
            {
                TimeSpan s1 = ReadFile(target); TimeSpan s2 = ReadFile(target2);
                if (s1 != TimeSpan.Zero && s2 != TimeSpan.Zero)
                {
                    if (chop) val = convert(s1 + s2);
                    else val = (s1 + s2).ToString();
                }
            }
            return val;
        }
        public static string GetPB(int cat, int difficulty)
        {
            return GetPB(categories[cat] + " " + difficulties[difficulty]);
        }
        public static string GetGolds(int cat, int difficulty)
        {
            string val = "N/A";
            if (GetPB(cat, difficulty) == "N/A") return val;
            string target = "splits/data/" + categories[cat] + " " + difficulties[difficulty] + " Dates Golds.txt";
            string target2 = "splits/data/" + categories[cat] + " " + difficulties[difficulty] + " Bonuses Golds.txt";
            if (File.Exists(target) && File.Exists(target2))
            {
                TimeSpan s1 = ReadFile(target); TimeSpan s2 = ReadFile(target2);
                if (s1 != TimeSpan.Zero && s2 != TimeSpan.Zero) val = convert(s1 + s2);
            }
            return val;
        }

        //adds contents of the file to the given list, if provided
        //returns the sum of all found timespans
        private static TimeSpan ReadFile(string target, List<TimeSpan> list = null)
        {
            TimeSpan sum = new TimeSpan();
            if (File.Exists(target))
            {
                string[] textFile = File.ReadAllLines(target);
                for (int j = 0; j < textFile.Length; j++)
                {
                    TimeSpan temp = TimeSpan.Parse(textFile[j]);
                    if (temp.Ticks > 0)
                    {
                        if (list != null) list.Add(temp);
                        sum += temp;
                    }
                    else
                    {
                        //a negative value was found in a saved file. this should be impossible but has happened before
                        if (list != null) list.Clear();
                        File.Delete(target);
                        return TimeSpan.Zero;
                    }
                }
            }
            return sum;
        }

        public RunTimer()
        {
            //no new file, so it's just practice
            runFile = -1;
            category = "";
            goal = -1;
            runTimer = DateTime.UtcNow.Ticks;
            rerollOccurred = false;
            finishedRun = false;
        }
        public RunTimer(int newFile, int cat, int difficulty) : this()
        {
            //beginning a new run
            runFile = newFile;
            if (cat < categories.Length)
            {
                category = categories[cat] + " " + difficulties[difficulty];
                goal = goals[cat];

                //search for comparison date splits
                string target = "splits/data/" + category + " Dates.txt"; string target2 = "splits/data/" + category + " Bonuses.txt";
                ReadFile(target, comparisonDates); ReadFile(target2, comparisonBonuses);

                //search for gold splits
                target = "splits/data/" + category + " Dates Golds.txt"; target2 = "splits/data/" + category + " Bonuses Golds.txt";
                ReadFile(target, goldDates); ReadFile(target2, goldBonuses);
            }
            else
            {
                Logger.LogMessage("invalid category, so no category loaded");
            }
        }
        public TimeSpan GetTimeAt(int numDates, int numBonuses)
        {
            TimeSpan s = new TimeSpan();
            for (int i = 0; i < numDates; i++)
                s += comparisonDates[i];
            for (int i = 0; i < numBonuses; i++)
                s += comparisonBonuses[i];
            return s;
        }

        public bool split(bool bonus = false)
        {
            long tickDiff = DateTime.UtcNow.Ticks - runTimer;
            //I hope this code never runs
            if (tickDiff < 0)
            {
                reset(false);
                HP2SR.ShowThreeNotif("Timer somehow became negative; run quit! Please report this!");
                return false;
            }
            splits.Add(new TimeSpan(tickDiff));
            isBonus.Add(bonus);
            splitColor = SplitColors.WHITE; prevColor = SplitColors.WHITE; goldColor = SplitColors.WHITE;
            splitText = ""; prevText = ""; goldText = "";

            TimeSpan s = splits[splits.Count - 1];
            string val = convert(s);

            int numDates = 0, numBonuses = 0;
            foreach (bool b in isBonus)
            {
                if (b) numBonuses++; else numDates++;
            }

            if (category != "")
            {
                //create the affection meter replacement text
                //time [+/-]
                if (comparisonDates.Count >= numDates && comparisonBonuses.Count >= numBonuses)
                {
                    TimeSpan elapsedC = GetTimeAt(numDates, numBonuses);
                    val += " [";
                    TimeSpan diff = s - elapsedC;
                    if (diff.TotalSeconds > 0)
                    {
                        val += "+";
                        splitColor = SplitColors.RED;
                    }
                    else
                    {
                        val += "-";
                        splitColor = SplitColors.BLUE;
                    }

                    val += convert(diff) + "]";

                    //create the this split text, which is just this split's diff minus the last split's diff
                    TimeSpan diff2;
                    if (splits.Count != 1)
                    {
                        TimeSpan s2 = splits[splits.Count - 2];
                        TimeSpan prevElapsedC;
                        if (bonus) prevElapsedC = GetTimeAt(numDates, numBonuses - 1);
                        else prevElapsedC = GetTimeAt(numDates - 1, numBonuses);
                        diff2 = s2 - prevElapsedC;
                        diff2 = diff - diff2;
                    }
                    else
                    {
                        diff2 = diff;
                    }
                    if (diff2.TotalSeconds > 0)
                    {
                        prevText += "+";
                        prevColor = SplitColors.RED;
                    }
                    else
                    {
                        prevText += "-";
                        prevColor = SplitColors.BLUE;
                    }
                    prevText += convert(diff2);
                }

                //create the gold diff text
                if (goldDates.Count >= numDates && goldBonuses.Count >= numBonuses)
                {
                    //get segment length
                    if (splits.Count > 1) s = s - splits[splits.Count - 2];
                    TimeSpan diff;
                    if (bonus) diff = s - goldBonuses[numBonuses - 1];
                    else diff = s - goldDates[numDates - 1];
                    if (diff.TotalSeconds < 0)
                    {
                        //new gold
                        goldText += "-";
                        splitColor = SplitColors.GOLD;
                        goldColor = SplitColors.GOLD;
                        if (bonus) goldBonuses[numBonuses - 1] = s;
                        else goldDates[numDates - 1] = s;
                    }
                    else
                        goldText += "+";
                    goldText += convert(diff);

                }
                //no gold to compare with, or no category defined
                else
                {
                    if (splits.Count > 1) s = s - splits[splits.Count - 2];
                    if (bonus) goldBonuses.Add(s);
                    else goldDates.Add(s);
                }
            }
            else if (category == "" && finishedRun == false)
            {
                //reset the timer for each split if we aren't in a category
                runTimer = DateTime.UtcNow.Ticks;
            }

            splitText = val;
            //Logger.LogMessage(splitText + " " + goldText);
            return true;
        }

        //aka "save golds"
        public void reset(bool saveGolds = true)
        {
            //save golds on reset of a category
            if (category != "" && saveGolds)
            {
                //save date and bonus golds separately, but without copying all the code twice lol
                string[] targets = { "splits/data/" + category + " Dates Golds.txt", "splits/data/" + category + " Bonuses Golds.txt" };
                List<TimeSpan>[] golds = { goldDates, goldBonuses };
                for (int i = 0; i < targets.Length; i++)
                {
                    string target = targets[i]; List<TimeSpan> gold = golds[i];

                    if (File.Exists(target))
                    {
                        //merge the two golds lists
                        List<TimeSpan> prevGolds = new List<TimeSpan>(); ReadFile(target, prevGolds);
                        List<TimeSpan> newGolds = new List<TimeSpan>();

                        for (int j = 0; j < prevGolds.Count; j++)
                        {
                            //make sure our golds isn't too short to compare
                            if (gold.Count - 1 < j) { newGolds.Add(prevGolds[j]); }
                            else
                            {
                                if (gold[j] < prevGolds[j]) newGolds.Add(gold[j]);
                                else newGolds.Add(prevGolds[j]);
                            }
                        }
                        //make sure the file's golds isn't too short to compare
                        if (gold.Count > prevGolds.Count)
                        {
                            for (int j = prevGolds.Count; j < gold.Count; j++)
                            {
                                newGolds.Add(gold[j]);
                            }
                        }
                        File.WriteAllLines(target, spansToStrings(newGolds));
                    }
                    else
                    {
                        //create a new file with our current golds list
                        File.WriteAllLines(target, spansToStrings(gold));
                    }
                }

                Logger.LogMessage("writing PB Attempt.txt");
                File.WriteAllText("splits/" + category + " Last Attempt.txt", finalRunDisplay);
            }
            category = "";
            goal = -1;
            //runTimer = DateTime.UtcNow.Ticks;
            finishedRun = true;
        }

        //a run has finished; is it faster than our comparison?
        public void save()
        {
            if (category != "")
            {
                string target = "splits/data/" + category + " Dates.txt"; string target2 = "splits/data/" + category + " Bonuses.txt";
                //due to the Tutorial, even 48 shoes would have a bonus file
                if (File.Exists(target) && File.Exists(target2))
                {
                    //saved comparison is longer than our new one (or zero, for some reason)
                    if (TimeSpan.Parse(GetPB(category,false)) > splits[splits.Count - 1] || TimeSpan.Parse(GetPB(category, false)) == TimeSpan.Zero)
                    {
                        File.WriteAllLines(target, splitsToStrings(false));
                        File.WriteAllLines(target2, splitsToStrings(true));
                        File.WriteAllText("splits/" + category + " PB.txt", finalRunDisplay);
                    }
                }
                //no PB file, so make one
                else
                {
                    File.WriteAllLines(target, splitsToStrings(false));
                    File.WriteAllLines(target2, splitsToStrings(true));
                    File.WriteAllText("splits/" + category + " PB.txt", finalRunDisplay);
                }
                //run is over, so we're no longer on a category
                reset();
            }
        }

        public void push(string s)
        {
            finalRunDisplay += s;
            Logger.LogMessage(finalRunDisplay);
        }

        private static string[] spansToStrings(List<TimeSpan> list)
        {
            string[] array = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = list[i].ToString("g");
            }
            return array;
        }

        private string[] splitsToStrings(bool countingBonuses)
        {
            int numDates = 0, numBonuses = 0;
            foreach (bool b in isBonus)
            {
                if (b) numBonuses++; else numDates++;
            }
            string[] array;
            if (countingBonuses) array = new string[numBonuses];
            else array = new string[numDates];
            int counter = 0;
            for (int i = 0; i < splits.Count; i++)
            {
                TimeSpan s = splits[i];
                if (i > 0) s = s - splits[i - 1];
                if (countingBonuses) Logger.LogMessage(splits[i].ToString("g"));
                if ((isBonus[i] && countingBonuses) || (!isBonus[i] && !countingBonuses))
                {
                    array[counter] = s.ToString("g");
                    counter++;
                }
            }
            return array;
        }
    }
}
