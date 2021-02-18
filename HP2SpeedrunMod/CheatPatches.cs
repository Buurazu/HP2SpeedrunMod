﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HP2SpeedrunMod
{
    public class CheatPatches
    {

        public static void Update()
        {
            if (!HP2SR.cheatsEnabled) return;
            //add the quick transitions code if cheat mode is on
            if (!Game.Persistence.playerData.unlockedCodes.Contains(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS)))
                Game.Persistence.playerData.unlockedCodes.Add(Game.Data.Codes.Get(HP2SR.QUICKTRANSITIONS));

            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (Game.Session.Location.currentLocation.locationType == LocationType.DATE)
                {
                    HP2SR.ShowNotif("Affection Filled!", 2);
                    Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.AFFECTION, 9999999, false);
                    Game.Session.Puzzle.puzzleStatus.CheckChanges();
                }
                else
                {
                    HP2SR.ShowNotif("Fruit Given!", 2);
                    Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.FLIRTATION, 100);
                    Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.ROMANCE, 100);
                    Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.SEXUALITY, 100);
                    Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.TALENT, 100);
                }
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (!Game.Manager.Ui.currentCanvas.titleCanvas)
                {
                    HP2SR.ShowNotif("Stamina Gained!", 2);
                    Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.STAMINA, 6, false);
                    Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.STAMINA, 6, true);
                    Game.Session.Puzzle.puzzleStatus.CheckChanges();
                }
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.A) && !Game.Manager.Ui.currentCanvas.titleCanvas)
                {
                    CodeDefinition abiaHair = Game.Data.Codes.Get(HP2SR.ABIAHAIR);
                    if (!Game.Persistence.playerData.unlockedCodes.Contains(abiaHair))
                    {
                        Game.Persistence.playerData.unlockedCodes.Add(abiaHair);
                        HP2SR.ShowNotif("Abia's hair enabled!", 0);
                    }
                    else
                    {
                        Game.Persistence.playerData.unlockedCodes.Remove(abiaHair);
                        HP2SR.ShowNotif("Abia's hair disabled!", 0);
                    }
                    foreach (UiDoll doll in Game.Session.gameCanvas.dolls)
                    {
                        if (doll.girlDefinition && doll.girlDefinition.girlName == "Abia") doll.ChangeHairstyle(doll.currentHairstyleIndex);
                    }
                }

                for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha9; i++)
                {
                    //Alpha0 = 48, Keypad0 = 256
                    int num = i - 48;

                    if (!Game.Manager.Ui.currentCanvas.titleCanvas && (Input.GetKeyDown((KeyCode)i) || Input.GetKeyDown((KeyCode)(i + 208))))
                    {
                        foreach (UiDoll doll in Game.Session.gameCanvas.dolls)
                        {
                            HP2SR.ShowThreeNotif("Changed to Outfit #" + num);
                            doll.ChangeHairstyle(num);
                            doll.ChangeOutfit(num);
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.N))
                {
                    HP2SR.nudePatch = !HP2SR.nudePatch;
                    if (HP2SR.nudePatch)
                    {
                        HP2SR.ShowNotif("AWOOOOOOOOOOGA", 2);
                        foreach (UiDoll doll in Game.Session.gameCanvas.dolls)
                        {
                            doll.partNipples.Show();
                            doll.partOutfit.Hide();
                        }
                    }
                    else
                    {
                        foreach (UiDoll doll in Game.Session.gameCanvas.dolls)
                        {
                            doll.partNipples.Hide();
                            doll.partOutfit.Show();
                        }
                    }

                }

                if (Input.GetKeyDown(KeyCode.M))
                {
                    if (!InputPatches.mashCheat) HP2SR.ShowThreeNotif("MASH POWER ACTIVATED");
                    else HP2SR.ShowThreeNotif("Mash power deactivated");
                    InputPatches.mashCheat = !InputPatches.mashCheat;
                }

                if (Input.GetKeyDown(KeyCode.L))
                {
                    //Datamining.LocationInfo();
                    CodeDefinition codeDefinition = Game.Data.Codes.Get(16);
                    if (!Game.Persistence.playerData.unlockedCodes.Contains(codeDefinition))
                    {
                        Game.Persistence.playerData.unlockedCodes.Add(codeDefinition);
                        HP2SR.ShowTooltip("Letterbox Code Enabled! (not that it does anything yet)", 2000);
                    }
                    else
                    {
                        Game.Persistence.playerData.unlockedCodes.Remove(codeDefinition);
                        HP2SR.ShowTooltip("Letterbox Code Disabled!", 2000);
                    }
                }

                if (Input.GetKeyDown(KeyCode.G))
                {
                    Datamining.GetGirlData();
                }

                if (Input.GetKeyDown(KeyCode.D))
                {
                    Datamining.GetAllDialogTriggers();
                    Datamining.GetAllCutsceneLines();
                }
            }

            /*
            i should still make a save toggle, on F3?
            if (cheatsEnabled)
            {

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    if (savingDisabled)
                    {
                        savingDisabled = false;
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Saving has been enabled");
                    }
                    else
                    {
                        savingDisabled = true;
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Saving has been disabled");
                    }
                }

            }
            */
        }

        //unlock all 12 outfits. don't mess with toggle codes here
        public static void UnlockAllCodes()
        {
            List<CodeDefinition> codes = Game.Data.Codes.GetAll();
            foreach (CodeDefinition code in codes)
            {
                //if (Game.Manager.Settings.unlockCodes.ContainsKey(code.codeHash))
                {
                    CodeDefinition codeDefinition = code;
                    CodeType codeType = codeDefinition.codeType;
                    if (codeType == CodeType.UNLOCK)
                    {
                        if (!Game.Persistence.playerData.unlockedCodes.Contains(codeDefinition))
                        {
                            Game.Persistence.playerData.unlockedCodes.Add(codeDefinition);
                        }
                    }
                }
            }
        }

        //credit to RShields
        //show relationship points on the girl list

        public static string[] shoes = new string[] { "Winter Boots", "Peep Toes", "Booties", "Cyber Boots", "Platforms", "Flip Flops", "Stripper Heels", "Sneakers", "Wedges", "Gladiators", "Flats", "Pumps" };
        public static string[] uniques = new string[] { "Tailoring", "Alcohol", "Occult", "Spiritual", "Weeaboo", "Spa", "Toddler Toys", "Baby Boy", "Handbags", "Band", "Kinky", "Antiques" };

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiGirlSlot), "Populate")]
        private static void AddNumber(UiGirlSlot __instance)
        {
            GirlDefinition girlDefinition = __instance.girlDefinition;
            PlayerFileGirl girlFile = Game.Persistence.playerFile.GetPlayerFileGirl(girlDefinition);
            __instance.nameLabel.richText = true;
            //__instance.nameLabel.text = "<size=75%><color=#FFFFFF>" + uniques[(int)girlFile.girlDefinition.shoesType] + "<line-height=75%>\n</line-height>" + shoes[(int)girlFile.girlDefinition.shoesType] + "</color></size>\n\n<voffset=3.25em>    " + girlDefinition.GetNickName().ToUpper() + "</voffset>";
            __instance.nameLabel.text = "<size=230%>" + girlFile.relationshipPoints + "</size>\n<voffset=3em>" + girlDefinition.GetNickName().ToUpper() + "</voffset>";
            //__instance.nameLabel.text = "    " + girlDefinition.GetNickName().ToUpper();
        }

        //item cheats and tutorial skip and more coming soon
        /*
        public static void AddItem(string theItem, InventoryItemPlayerData[] target = null)
        {
            if (target == null) target = GameManager.System.Player.inventory;
            if (!GameManager.System.Player.HasItem(GameManager.Data.Items.Get(BaseHunieModPlugin.ItemNameList[theItem])))
                GameManager.System.Player.AddItem(GameManager.Data.Items.Get(BaseHunieModPlugin.ItemNameList[theItem]), target, false, false);
        }
        public static void AddItem(int theItem, InventoryItemPlayerData[] target = null)
        {
            if (target == null) target = GameManager.System.Player.inventory;
            if (!GameManager.System.Player.HasItem(GameManager.Data.Items.Get(theItem)))
                GameManager.System.Player.AddItem(GameManager.Data.Items.Get(theItem), target, false, false);
        }

        public static void AddGreatGiftsToInventory()
        {
            //perfumes
            CheatPatches.AddItem(151); CheatPatches.AddItem(152); CheatPatches.AddItem(153); CheatPatches.AddItem(154);
            //flowers
            CheatPatches.AddItem(139); CheatPatches.AddItem(140); CheatPatches.AddItem(141);
            CheatPatches.AddItem(142); CheatPatches.AddItem(143); CheatPatches.AddItem(144);

            CheatPatches.AddItem("Suede Ankle Booties"); CheatPatches.AddItem("Leopard Print Pumps"); CheatPatches.AddItem("Shiny Lipstick");
            CheatPatches.AddItem("Pearl Necklace"); CheatPatches.AddItem("Stuffed Penguin"); CheatPatches.AddItem("Stuffed Whale");
            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Fantastic date gifts added to inventory");
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GirlPlayerData), "ReadGirlSaveData")]
        public static void MakeUsMet(GirlPlayerData __instance)
        {
            if (__instance.metStatus < GirlMetStatus.UNKNOWN) __instance.metStatus = GirlMetStatus.UNKNOWN;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameManager), "SaveGame")]
        public static bool SaveDisabler() {
            if (BaseHunieModPlugin.savingDisabled) return false;
            else return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LocationManager), "OnLocationSettled")]
        public static void SkipTutorialOnArrival()
        {
            if (GameManager.System.Player.tutorialStep < 2)
            {
                GameManager.System.Player.tutorialStep = 2;
                GameManager.System.Player.money = 1000;
                CheatPatches.AddItem("Stuffed Bear",GameManager.System.Player.dateGifts);
                AddGreatGiftsToInventory();

            }
        }
        */
    }
}
