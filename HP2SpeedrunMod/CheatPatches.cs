using HarmonyLib;
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
                    Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.AFFECTION, -1, false);
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

                if (Input.GetKeyDown(KeyCode.R))
                {
                    PlayerFileGirlPair thePair = Game.Persistence.playerFile.GetPlayerFileGirlPair(Game.Persistence.playerFile.girlPairDefinition);
                    if (thePair.relationshipType != GirlPairRelationshipType.LOVERS)
                    {
                        thePair.relationshipType++;
                        HP2SR.ShowThreeNotif("Relationship Leveled Up to " + thePair.relationshipType + "!");
                        UiCellphoneAppStatus status = (UiCellphoneAppStatus)AccessTools.Field(typeof(UiCellphone), "_currentApp").GetValue(Game.Session.gameCanvas.cellphone);
                        status.Refresh();
                    }
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

        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "OnStartButtonPressed")]
        public static void SkipTutorialOnArrival()
        {
            //skip to inside the airplane, it has special properties
            Game.Persistence.playerFile.locationDefinition = Game.Data.Locations.Get(25);
            //give our first date fruits
            Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.FLIRTATION, 5);
            Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.ROMANCE, 5);
            Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.SEXUALITY, 5);
            Game.Persistence.playerFile.AddFruitCount(PuzzleAffectionType.TALENT, 5);
            //ashley is added first, not that it matters probably
            Game.Persistence.playerFile.GetPlayerFileGirl(Game.Data.Girls.Get(10));
            for (int i = 1; i <= 12; i++)
            {
                Game.Persistence.playerFile.GetPlayerFileGirl(Game.Data.Girls.Get(i));
            }
            //set ashley, lola, lailani to met
            Game.Persistence.playerFile.girls[0].playerMet = true;
            Game.Persistence.playerFile.girls[1].playerMet = true;
            Game.Persistence.playerFile.girls[6].playerMet = true;
            Game.Persistence.playerFile.PushDaytimeTo(ClockDaytimeType.MORNING);
            //gift us the fox plush into slot 1
            Game.Persistence.playerFile.GetPlayerFileInventorySlot(1).itemDefinition = Game.Data.Items.Get(52);
            Game.Persistence.playerFile.GetPlayerFileInventorySlot(1).daytimeStamp = Game.Persistence.playerFile.daytimeElapsed;
            //use test mode to instantly appear in hub
            AccessTools.Field(typeof(GameManager), "_testMode").SetValue(Game.Manager, true);
            Game.Persistence.SaveGame();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiCellphoneAppReconfig), "Start")]
        public static void AllowAnyDifficultyChange(UiCellphoneAppReconfig __instance, ref int ____difficultyShave)
        {
            ____difficultyShave = 0;
            __instance.Refresh();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiTitleCanvas), "LoadGame")]
        public static void SkipStoryCutscene(ref string loadSceneName)
        {
            if (loadSceneName == "StoryScene") loadSceneName = "MainScene";
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "OnArrivalComplete")]
        public static bool TutorialAndCutsceneSkips(LocationManager __instance, ref LocationDefinition ____currentLocation, ref CutsceneDefinition ____arrivalCutscene)
        {
            //if arriving to airplane, skip to hub
            if (____currentLocation.id == 25)
            {
                __instance.Depart(Game.Data.Locations.Get(21), null);
                AccessTools.Field(typeof(GameManager), "_testMode").SetValue(Game.Manager, false);
                return false;
            }
            //skip arrival cutscenes for the SIM locations which are 1 through 8
            else if (____currentLocation.id <= 8 && ____arrivalCutscene != null)
            {
                ____arrivalCutscene = null;
            }
            
            return true;
            
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationTransition), "Arrive")]
        public static void AlwaysArriveWithGirls(ref bool arriveWithGirls)
        {
            if (Game.Session.Location.currentLocation.id <= 8) arriveWithGirls = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerFile), "PopulateFinderSlots")]
        public static void DumbPermutationShit()
        {
            Datamining.TestAllPermutations();
        }

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "CreateToken")]
        public static void Testing(UiPuzzleGrid __instance, ref int col)
        {
            Datamining.Logger.LogDebug("done with createtoken");
            UiPuzzleSlot lowest = (UiPuzzleSlot)AccessTools.Method(typeof(UiPuzzleGrid), "GetLowestEmptySlot").Invoke(__instance, new object[] { col });
            if (lowest != null)
                Datamining.Logger.LogDebug("col = " + col + ", lowest empty slot = " + lowest.row);
        }*/

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "StartPuzzle")]
        public static void CheatPuzzle(UiPuzzleGrid __instance)
        {
            string[] array = "1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,4,3,3|1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,4,3,4,4|2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,4,2,3|1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4|2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3|2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3|1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4|2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3|2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3".Split(new char[]
        {
            '|'
        });
            for (int i = 0; i < array.Length; i++)
            {
                __instance.preloadedTokens.Add(new List<TokenDefinition>());
                string[] array2 = array[i].Split(new char[]
                {
                ','
                });
                for (int j = 0; j < array2.Length; j++)
                {
                    int num = StringUtils.ParseIntValue(array2[j]);
                    if (num > 0)
                    {
                        __instance.preloadedTokens[i].Add(Game.Data.Tokens.Get(num));
                    }
                    else
                    {
                        __instance.preloadedTokens[i].Add(null);
                    }
                }
            }
        }
        */
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

        */
    }
}
