using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using BepInEx.Logging;
using DG.Tweening;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace HP2SpeedrunMod
{
    public class RunTimerPatches
    {
        public static int shoesProgress;
        public static bool justFinishedDate = false;
        public static bool itsRewindTime = false;
        public static bool justGaveShoe = false;
        public static bool noMoreShoe = false;
        public static Stopwatch initialTimerDelay = new Stopwatch();
        public static Stopwatch undoTimer = new Stopwatch();
        public static Stopwatch shoeTimer = new Stopwatch();
        public static void Update()
        {
            if (!HP2SR.InGameTimer.Value || HP2SR.run == null) return;
            RunTimer run = HP2SR.run;

            if (initialTimerDelay.IsRunning && initialTimerDelay.ElapsedMilliseconds > 1000)
            {
                initialTimerDelay.Reset();
                justFinishedDate = true;
            }
            if (undoTimer.IsRunning && undoTimer.ElapsedMilliseconds > 6000)
            {
                undoTimer.Reset();
                itsRewindTime = true;
            }
            if (shoeTimer.IsRunning && shoeTimer.ElapsedMilliseconds > 4000)
            {
                shoeTimer.Reset();
                noMoreShoe = true;
            }

            //checking criterias for non-Wing categories
            if (run.goal == 100 && Game.Persistence.playerFile.CalculateCompletePercent() == 100)
            {
                run.split();
                string newSplit = "100%\n      " + run.splitText + "\n";
                run.push(newSplit);
                run.save();
                HP2SR.ShowThreeNotif("100% completed at " + run.splitText);
            }
            else if (run.goal == 48 && Game.Persistence.playerFile.GetStyleLevelExp() > shoesProgress)
            {
                shoesProgress = Game.Persistence.playerFile.GetStyleLevelExp();
                //split every style level
                if (shoesProgress % 6 == 0)
                {
                    run.split();
                    justGaveShoe = true;
                    shoeTimer.Start();

                    string newSplit = "Shoe #" + shoesProgress + "\n      " + run.splitText + "\n";
                    run.push(newSplit);
                    if (shoesProgress == 48)
                    {
                        run.save();
                        //let you bask in the glory
                        shoeTimer.Reset();
                    }
                }
            }
        }

        //just in case, especially for the dumb 48 shoes timer, don't start timers on a different status and finish them on the new one
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppStatus), "OnDestroy")]
        public static void StopTimers(UiCellphoneAppStatus __instance)
        {
            initialTimerDelay.Reset();
            undoTimer.Reset();
            shoeTimer.Reset();
        }

        //modify the rollers when the date is finished
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiCellphoneAppStatus), "Update")]
        public static void DisplayOurInfo(UiCellphoneAppStatus __instance, ref PuzzleStatus ____puzzleStatus)
        {
            UiCellphoneAppStatus status = __instance;
            RunTimer run = HP2SR.run;
            if (justFinishedDate)
            {
                justFinishedDate = false;

                status.affectionMeter.valueLabelPro.richText = true;
                status.affectionMeter.valueLabelPro.text =
                "<color=" + RunTimer.colors[(int)run.splitColor] + ">" + run.splitText + "</color>";

                Sequence seq = DOTween.Sequence();
                seq.Insert(0f, status.canvasGroupDate.DOFade(1, 0.32f).SetEase(Ease.Linear));
                seq.Insert(0f, status.canvasGroupLeft.DOFade(1, 0.32f).SetEase(Ease.Linear));
                seq.Insert(0f, status.canvasGroupRight.DOFade(1, 0.32f).SetEase(Ease.Linear));
                Game.Manager.Time.Play(seq, status.pauseBehavior.pauseDefinition, 0f);

                if (run.prevColor == RunTimer.SplitColors.BLUE)
                {
                    status.sentimentRollerRight.valueName = "THIS SPLIT";
                    status.sentimentRollerRight.maxName = "THIS SPLIT";
                    status.sentimentRollerRight.nameLabel.text = "THIS SPLIT";
                    status.sentimentRollerRight.valueLabelPro.text = run.prevText;
                }
                else if (run.prevColor == RunTimer.SplitColors.RED)
                {
                    status.passionRollerRight.valueName = "THIS SPLIT";
                    status.passionRollerRight.maxName = "THIS SPLIT";
                    status.passionRollerRight.nameLabel.text = "THIS SPLIT";
                    status.passionRollerRight.valueLabelPro.text = run.prevText;
                }

                if (run.goldText != "")
                {
                    status.movesRoller.valueName = "GOLD";
                    status.movesRoller.maxName = "GOLD";
                    status.movesRoller.nameLabel.text = "GOLD";
                    status.movesRoller.valueLabelPro.text = run.goldText;
                }
            }
            if (itsRewindTime)
            {
                itsRewindTime = false;

                status.sentimentRollerRight.valueName = "SENTIMENT"; status.sentimentRollerRight.maxName = "SENTI... • MAX";
                status.sentimentRollerRight.Reset(____puzzleStatus.girlStatusRight.sentiment, 40);
                status.passionRollerRight.valueName = "PASSION"; status.passionRollerRight.maxName = "PASSION • MAX";
                status.passionRollerRight.Reset(____puzzleStatus.girlStatusRight.passion, 100);
                status.movesRoller.valueName = "MOVES"; status.movesRoller.maxName = "MOVES • MAX";
                status.movesRoller.Reset(____puzzleStatus.movesRemaining, ____puzzleStatus.maxMovesRemaining);
            }
            if (justGaveShoe)
            {
                justGaveShoe = false;

                //briefly display the affection meter
                for (int j = 0; j < status.simCanvasGroups.Length; j++) { status.simCanvasGroups[j].alpha = 0f; }
                status.affectionCanvasGroup.alpha = 1f;
                status.affectionMeter.valueLabelPro.richText = true;
                string shoeText = "<color=" + RunTimer.colors[(int)run.splitColor] + ">" + run.splitText + "</color>";
                if (run.goldText != "") shoeText += " <color=" + RunTimer.colors[(int)run.goldColor] + ">(" + run.goldText + ")</color>";
                status.affectionMeter.valueLabelPro.text = shoeText;
            }
            if (noMoreShoe)
            {
                noMoreShoe = false;

                status.affectionCanvasGroup.alpha = 0f;
                for (int j = 0; j < status.simCanvasGroups.Length; j++) { status.simCanvasGroups[j].alpha = 1f; }
            }
        }

        //default to last chosen category
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "Start")]
        public static void DefaultCategory(UiCellphoneAppNew __instance, ref int ____newSaveFileIndex, ref UiAppFileIconSlot ____selectedFileIconSlot)
        {
            if (HP2SR.cheatsEnabled || HP2SR.AllPairsEnabled.Value) return;

            //default girl head to the last chosen category, show their details on the mouse
            if (____selectedFileIconSlot != null)
                ____selectedFileIconSlot.button.Enable();
            ____selectedFileIconSlot = __instance.fileIconSlots[HP2SR.lastChosenCategory];
            ____selectedFileIconSlot.button.Disable();
            HP2SR.ShowTooltip(RunTimer.GetAll(HP2SR.lastChosenCategory, HP2SR.lastChosenDifficulty), 3000, 0, 50);

            __instance.Refresh();
        }

        //always start a new run, even if cheating
        //but only pass the category/difficulty when not cheating
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "OnStartButtonPressed")]
        public static void StartNewRun(UiCellphoneAppNew __instance, ref int ____newSaveFileIndex, ref UiAppFileIconSlot ____selectedFileIconSlot)
        {
            shoesProgress = 0;

            if (HP2SR.run != null)
            {
                HP2SR.run.reset();
                HP2SR.run = null;
            }

            if (!HP2SR.cheatsEnabled && !HP2SR.AllPairsEnabled.Value)
            {
                int catChoice = -1;
                for (int i = 0; i < __instance.fileIconSlots.Count; i++)
                {
                    if (__instance.fileIconSlots[i].girlDefinition.girlName == ____selectedFileIconSlot.girlDefinition.girlName)
                    {
                        catChoice = i;
                        HP2SR.lastChosenCategory = catChoice;
                        break;
                    }
                }
                HP2SR.run = new RunTimer(____newSaveFileIndex, catChoice, __instance.settingSelectorDifficulty.selectedIndex);
            }
            else
            {
                HP2SR.run = new RunTimer();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "OnFileIconSlotSelected")]
        public static void JustCheckingCategory(UiCellphoneAppNew __instance, ref int ____newSaveFileIndex, ref UiAppFileIconSlot ____selectedFileIconSlot)
        {
            if (HP2SR.cheatsEnabled || HP2SR.AllPairsEnabled.Value) return;
            if (____selectedFileIconSlot.girlDefinition.id - 1 < RunTimer.categories.Length)
                HP2SR.lastChosenCategory = ____selectedFileIconSlot.girlDefinition.id - 1;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "OnSettingSelectorChanged")]
        public static void JustCheckingDifficulty(UiCellphoneAppNew __instance)
        {
            if (HP2SR.lastChosenCategory < RunTimer.categories.Length)
                HP2SR.ShowTooltip(RunTimer.GetAll(HP2SR.lastChosenCategory, HP2SR.lastChosenDifficulty), 3000, 0, 50);
        }

        //Category tooltips
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiAppFileIconSlot), "ShowTooltip")]
        public static bool CategoryNameDisplay(UiAppFileIconSlot __instance, ref UiTooltipSimple ____tooltip)
        {
            if (HP2SR.cheatsEnabled || HP2SR.AllPairsEnabled.Value || !Game.Manager.Ui.currentCanvas.titleCanvas) return true;
            int cat = __instance.girlDefinition.id - 1;
            int diff = HP2SR.lastChosenDifficulty;
            if (cat < RunTimer.categories.Length)
            {
                HP2SR.ShowTooltip(RunTimer.GetAll(cat, diff), 3000, 0, 50);
                return false;
            }
            else
            {
                HP2SR.tooltip = null;
                HP2SR.tooltipTimer.Reset();
                return true;
            }
        }

        //Continue game = check if file is same as the run
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiTitleCanvas), "LoadGame")]
        public static void CheckFileID(ref int saveFileIndex)
        {
            //Reset the timer if loading a different file, or cheats got enabled, or return hotkey was used
            if (HP2SR.run != null && (HP2SR.run.runFile != saveFileIndex || HP2SR.cheatsEnabled || HP2SR.hasReturned))
            {
                HP2SR.run.reset();
                HP2SR.run = null;
            }
            //start a non-category timer on file load even if cheats are on or we just returned, for practice
            if (HP2SR.run == null)
            {
                HP2SR.run = new RunTimer();
            }
        }

    }
}
