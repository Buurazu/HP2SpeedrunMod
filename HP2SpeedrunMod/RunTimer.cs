using System;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using DG.Tweening.Core;

namespace HP2SpeedrunMod
{
    public class RunTimer
    {
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
        public static string[] colors = new string[] { "#ffffff", "#d6e9ff", "#ffcccc", "#ddaf4c" };

        public Stopwatch runTimer;
        public int runFile;
        public string category;
        public int goal;
        public List<TimeSpan> splits = new List<TimeSpan>();
        public List<TimeSpan> comparison = new List<TimeSpan>();
        public List<TimeSpan> golds = new List<TimeSpan>();
        public string splitText;
        public string goldText;
        public SplitColors splitColor;
        public SplitColors goldColor;

        public string finalRunDisplay = "";

        public static string GetAll(int cat, int difficulty)
        {
            if (cat >= categories.Length || difficulty >= difficulties.Length) return "N/A";
            string val = categories[cat] + " " + difficulties[difficulty] + "\nPB: " + GetPB(cat, difficulty) + "\nSoB: " + GetGolds(cat, difficulty);
            return val;
        }
        public static string GetPB(int cat, int difficulty)
        {
            string val = "N/A";
            string target = "splits/data/" + categories[cat] + " " + difficulties[difficulty] + ".txt";
            if (File.Exists(target))
            {
                string[] textFile = File.ReadAllLines(target);
                //saved comparison is longer than our new one
                TimeSpan s = TimeSpan.Parse(textFile[textFile.Length - 1]);
                if (s.Hours > 0)
                    val = s.ToString(@"h\:mm\:ss\.f");
                else
                    val = s.ToString(@"m\:ss\.f");
            }
            return val;
        }
        public static string GetGolds(int cat, int difficulty)
        {
            string val = "N/A";
            if (GetPB(cat, difficulty) == "N/A") return val;
            string target = "splits/data/" + categories[cat] + " " + difficulties[difficulty] + " Golds.txt";
            if (File.Exists(target))
            {
                string[] textFile = File.ReadAllLines(target);
                TimeSpan s = new TimeSpan();
                foreach (string line in textFile)
                {
                    s += TimeSpan.Parse(line);
                }
                if (s.Hours > 0)
                    val = s.ToString(@"h\:mm\:ss\.f");
                else
                    val = s.ToString(@"m\:ss\.f");
            }
            return val;
        }

        public RunTimer()
        {
            //no new file, so it's just practice
            runFile = -1;
            category = "";
            goal = -1;
            runTimer = new Stopwatch();
            runTimer.Start();
        }
        public RunTimer(int newFile, int cat, int difficulty) : this()
        {
            //beginning a new run
            runFile = newFile;
            if (cat < categories.Length)
            {
                category = categories[cat] + " " + difficulties[difficulty];
                goal = goals[cat];

                Datamining.Logger.LogDebug("new run: save file #" + runFile + " ," + category + " ," + goal);
                //search for comparison splits
                string target = "splits/data/" + category + ".txt";
                if (File.Exists(target))
                {
                    string[] textFile = File.ReadAllLines(target);
                    for (int j = 0; j < textFile.Length; j++)
                    {
                        comparison.Add(TimeSpan.Parse(textFile[j]));
                    }
                }
                //search for gold splits
                target = "splits/data/" + category + " Golds.txt";
                if (File.Exists(target))
                {
                    string[] textFile = File.ReadAllLines(target);
                    for (int j = 0; j < textFile.Length; j++)
                    {
                        golds.Add(TimeSpan.Parse(textFile[j]));
                    }
                }
            }
            else
            {
                Datamining.Logger.LogDebug("invalid category");
            }
        }

        public bool split()
        {
            splits.Add(runTimer.Elapsed);
            Datamining.Logger.LogDebug(runTimer.Elapsed.ToString());
            splitColor = SplitColors.WHITE;
            goldColor = SplitColors.WHITE;
            goldText = "";

            string val = "";
            TimeSpan s = splits[splits.Count - 1];
            //having no minutes in your split time is literally never possible in this game
            if (s.Hours > 0)
                val = s.ToString(@"h\:mm\:ss\.f");
            else
                val = s.ToString(@"m\:ss\.f");

            if (category != "")
            {
                if (comparison.Count >= splits.Count)
                {
                    val += " [";
                    TimeSpan diff = s - comparison[splits.Count - 1];
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

                    if (diff.Hours != 0)
                        val += diff.ToString(@"h\:mm\:ss\.f");
                    else if (diff.Minutes != 0)
                        val += diff.ToString(@"m\:ss\.f");
                    else
                        val += diff.ToString(@"s\.f");

                    val += "]";
                }

                if (golds.Count >= splits.Count)
                {
                    goldText += "(";
                    //get segment length
                    if (splits.Count > 1) s = s - splits[splits.Count - 2];
                    TimeSpan diff = s - golds[splits.Count - 1];
                    if (diff.TotalSeconds < 0)
                    {
                        //new gold
                        goldText += "-";
                        splitColor = SplitColors.GOLD;
                        goldColor = SplitColors.GOLD;
                        golds[splits.Count - 1] = s;
                    }
                    else
                        goldText += "+";
                    if (diff.Hours != 0)
                        goldText += diff.ToString(@"h\:mm\:ss\.f");
                    else if (diff.Minutes != 0)
                        goldText += diff.ToString(@"m\:ss\.f");
                    else
                        goldText += diff.ToString(@"s\.f");
                    goldText += ")";
                }
                //no gold to compare with, or no category defined
                else
                {
                    if (splits.Count > 1) s = s - splits[splits.Count - 2];
                    golds.Add(s);
                }
            }

            splitText = val;
            Datamining.Logger.LogDebug(splitText + " " + goldText);
            return true;
        }

        //aka "save golds"
        public void reset()
        {
            //save golds on reset of a category
            if (category != "")
            {
                string target = "splits/data/" + category + " Golds.txt";
                if (File.Exists(target))
                {
                    //merge the two golds lists
                    string[] textFile = File.ReadAllLines(target);
                    List<TimeSpan> prevGolds = new List<TimeSpan>();
                    List<TimeSpan> newGolds = new List<TimeSpan>();
                    for (int j = 0; j < textFile.Length; j++)
                    {
                        prevGolds.Add(TimeSpan.Parse(textFile[j]));
                    }
                    for (int j = 0; j < prevGolds.Count; j++)
                    {
                        //make sure our golds isn't too short to compare
                        if (golds.Count-1 < j) { newGolds.Add(prevGolds[j]); }
                        else
                        {
                            if (golds[j] < prevGolds[j]) newGolds.Add(golds[j]);
                            else newGolds.Add(prevGolds[j]);
                        }
                    }
                    //make sure the file's golds isn't too short to compare
                    if (golds.Count > prevGolds.Count)
                    {
                        for (int j = prevGolds.Count; j < golds.Count; j++)
                        {
                            newGolds.Add(golds[j]);
                        }
                    }
                    File.WriteAllLines(target, spansToStrings(newGolds));
                }
                else
                {
                    //create a new file with our current golds list
                    string[] goldsString = new string[golds.Count];
                    for (int i = 0; i < golds.Count; i++)
                    {
                        goldsString[i] = golds[i].ToString("g");
                    }
                    File.WriteAllLines(target, spansToStrings(golds));
                }
                Datamining.Logger.LogDebug("writing PB Attempt.txt");
                File.WriteAllText("splits/" + category + " Last Attempt.txt", finalRunDisplay);
            }
            category = "";
            goal = -1;
        }

        //a run has finished; is it faster than our comparison?
        public void save()
        {
            if (category != "")
            {
                string target = "splits/data/" + category + ".txt";
                if (File.Exists(target))
                {
                    string[] textFile = File.ReadAllLines(target);
                    //saved comparison is longer than our new one?
                    if (TimeSpan.Parse(textFile[textFile.Length-1]) > splits[splits.Count-1])
                    {
                        File.WriteAllLines(target, spansToStrings(splits));
                        File.WriteAllText("splits/" + category + " PB.txt", finalRunDisplay);
                    }
                }
                else
                {
                    File.WriteAllLines(target, spansToStrings(splits));
                    File.WriteAllText("splits/" + category + " PB.txt", finalRunDisplay);
                }
                //run is over, so we're no longer on a category
                reset();
            }
        }

        public void push(string s)
        {
            finalRunDisplay += s;
            Datamining.Logger.LogDebug(finalRunDisplay);
        }

        private string[] spansToStrings(List<TimeSpan> list)
        {
            string[] array = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = list[i].ToString("g");
            }
            return array;
        }
    }
}
