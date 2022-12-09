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
        static int leftStamina, rightStamina, leftSentiment, rightSentiment, leftPassion, rightPassion, moves = 0;

        public static List<TokenDefinition> tokens = Game.Data.Tokens.GetAll();
        public static string[] tokenNames =
            { "Talent", "Flirtation", "Romance", "Sexuality", "Passion", "Broken Heart", "Joy", "Sentiment", "Stamina" };
        public static void Update()
        {
            if (!HP2SR.cheatsEnabled) return;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (Game.Session.Location.currentLocation.locationType == LocationType.DATE)
                {
                    HP2SR.ShowNotif("Affection Filled!", 2);
                    Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.AFFECTION, 999999, false);
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

            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (!Game.Manager.Ui.currentCanvas.titleCanvas)
                {
                    if (Game.Session.Location.currentLocation.locationType != LocationType.DATE || Game.Session.gameCanvas.cellphone.isOpen)
                    {
                        HP2SR.ShowNotif("Girl Finder Refreshed!", 2);
                        Game.Persistence.playerFile.PopulateFinderSlots();
                        Game.Session.gameCanvas.cellphone.LoadOpenApp();
                    }
                    else if (Game.Session.Location.currentLocation.locationType == LocationType.DATE && Game.Session.Puzzle.puzzleGrid.roundOver == false)
                    {
                        //Restart the current date
                        //Game.Session.Puzzle.puzzleStatus.NextRound();
                        //Game.Session.Puzzle.puzzleGrid.EndPuzzle();
                        //Game.Session.Puzzle.puzzleStatus.Clear();
                        
                        Game.Session.Puzzle.puzzleStatus.Reset(Game.Session.Location.currentGirlLeft, Game.Session.Location.currentGirlRight);
                        Game.Session.Puzzle.puzzleStatus.girlStatusLeft.stamina = leftStamina;
                        Game.Session.Puzzle.puzzleStatus.girlStatusRight.stamina = rightStamina;
                        Game.Session.Puzzle.puzzleStatus.girlStatusLeft.sentiment = leftSentiment;
                        Game.Session.Puzzle.puzzleStatus.girlStatusRight.sentiment = rightSentiment;
                        Game.Session.Puzzle.puzzleStatus.girlStatusLeft.passion = leftPassion;
                        Game.Session.Puzzle.puzzleStatus.girlStatusRight.passion = rightPassion;
                        Game.Session.Puzzle.puzzleStatus.movesRemaining = moves;
                        Game.Session.Puzzle.puzzleStatus.SetGirlFocusByStamina();
                        for (int i = 0; i < Game.Session.Puzzle.puzzleGrid.puzzleSlots.Count; i++)
                        {
                            UiPuzzleSlot slot = Game.Session.Puzzle.puzzleGrid.puzzleSlots[i];
                            UiPuzzleToken token = slot.token;
                            if (token.isWeighted)
                            {
                                Game.Session.Puzzle.puzzleStatus.GetTokenInfoByDefinition(token.definition).AdjustCurrentWeight(1);
                            }
                            slot.SetToken(null, 0f);
                            UnityEngine.Object.Destroy(token.gameObject);
                            //AccessTools.Method(typeof(UiPuzzleGrid), "CreateToken").Invoke(Game.Session.Puzzle.puzzleGrid,
                            //    new object[] { slot.col, true });
                            //Game.Session.Puzzle.puzzleGrid.DestroyToken(Game.Session.Puzzle.puzzleGrid.puzzleSlots[i], null, false);
                        }
                        Game.Session.Puzzle.puzzleStatus.resourceChanged = true;
                        Game.Session.Puzzle.puzzleGrid.StartPuzzle();
                        
                        //Game.Session.Location.Depart(Game.Session.Location.currentLocation, Game.Session.Location.currentGirlPair, Game.Session.Location.currentSidesFlipped);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                if (!Game.Manager.Ui.currentCanvas.titleCanvas)
                {
                    HP2SR.ShowNotif("Store Refreshed!", 2);
                    Game.Persistence.playerFile.PopulateStoreProducts();
                    Game.Session.gameCanvas.cellphone.LoadOpenApp();
                }
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.M) && (Game.Manager.Ui.currentCanvas.titleCanvas || Game.Session.Location.currentLocation.locationType != LocationType.DATE))
                {
                    if (!InputPatches.mashCheat) HP2SR.ShowThreeNotif("MASH POWER ACTIVATED");
                    else HP2SR.ShowThreeNotif("Mash power deactivated");
                    InputPatches.mashCheat = !InputPatches.mashCheat;
                }

                //Passion/Sentiment/Moves increase/decrease
                if (!Game.Manager.Ui.currentCanvas.titleCanvas && Game.Session.Location.currentLocation.locationType == LocationType.DATE)
                {
                    int mult = 1;
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) mult = -1;

                    if (Input.GetKeyDown(KeyCode.P))
                    {
                        Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.PASSION, mult, false);
                        Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.PASSION, mult, true);
                    }
                    if (Input.GetKeyDown(KeyCode.S)) Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.SENTIMENT, mult, false);
                    if (Input.GetKeyDown(KeyCode.M)) Game.Session.Puzzle.puzzleStatus.AddResourceValue(PuzzleResourceType.MOVES, mult, false);
                    Game.Session.Puzzle.puzzleStatus.CheckChanges();
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

                //percent sign
                if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    Datamining.CheckPercentage();
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
                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        if (thePair.relationshipType != GirlPairRelationshipType.UNKNOWN)
                        {
                            thePair.relationshipType--;
                            HP2SR.ShowThreeNotif("Relationship Leveled Down to " + thePair.relationshipType + "!");
                            UiCellphoneAppStatus status = (UiCellphoneAppStatus)AccessTools.Field(typeof(UiCellphone), "_currentApp").GetValue(Game.Session.gameCanvas.cellphone);
                            status.Refresh();
                        }
                    }
                    else
                    {
                        if (thePair.relationshipType != GirlPairRelationshipType.LOVERS)
                        {
                            thePair.relationshipType++;
                            HP2SR.ShowThreeNotif("Relationship Leveled Up to " + thePair.relationshipType + "!");
                            UiCellphoneAppStatus status = (UiCellphoneAppStatus)AccessTools.Field(typeof(UiCellphone), "_currentApp").GetValue(Game.Session.gameCanvas.cellphone);
                            status.Refresh();
                        }
                    }
                }

            }

            /*if (Input.GetKey(KeyCode.F9))
            {
                Game.Persistence.playerFile.alphaDateCount -= 100;

                float num = 200f;
                float num2 = 25f;
                int num3 = Mathf.Min(Game.Persistence.playerFile.relationshipUpCount, 47);
                if (Game.Persistence.playerFile.storyProgress >= 14 && !Game.Persistence.playerData.unlockedCodes.Contains(Game.Session.Puzzle.noAlphaModeCode))
                {
                    num3 += Game.Persistence.playerFile.alphaDateCount + 1;
                }
                for (int k = 0; k < num3; k++)
                {
                    num += num2;
                    num2 += 3.3525f;
                }

                Datamining.Logger.LogDebug((Mathf.RoundToInt(num / 5f) * 5));
            }

            if (Input.GetKey(KeyCode.F11))
            {
                Game.Persistence.playerFile.alphaDateCount += 1000;

                float num = 200f;
                float num2 = 25f;
                int num3 = Mathf.Min(Game.Persistence.playerFile.relationshipUpCount, 47);
                if (Game.Persistence.playerFile.storyProgress >= 14 && !Game.Persistence.playerData.unlockedCodes.Contains(Game.Session.Puzzle.noAlphaModeCode))
                {
                    num3 += Game.Persistence.playerFile.alphaDateCount + 1;
                }
                for (int k = 0; k < num3; k++)
                {
                    num += num2;
                    num2 += 3.3525f;
                }

                Datamining.Logger.LogDebug((Mathf.RoundToInt(num / 5f) * 5));
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                Game.Persistence.playerFile.alphaDateCount = 35000;
            }*/

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

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlatformManager), "UnlockAchievement")]
        private static bool NoChievos()
        {
            return false;
        }
        */

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

        public static bool weAreSkippingTutorial = false;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiCellphoneAppNew), "OnStartButtonPressed")]
        public static void SkipTutorialOnArrival()
        {
            if (!HP2SR.CheatSpeedEnabled.Value) return;
            weAreSkippingTutorial = true;
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
            if (!HP2SR.CheatSpeedEnabled.Value) return;
            if (loadSceneName == "StoryScene") loadSceneName = "MainScene";
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "OnArrivalComplete")]
        public static bool TutorialAndCutsceneSkips(LocationManager __instance, ref LocationDefinition ____currentLocation, ref CutsceneDefinition ____arrivalCutscene)
        {
            if (!HP2SR.CheatSpeedEnabled.Value) return true;
            //if arriving to airplane, skip to hub
            if (____currentLocation.id == 25 && weAreSkippingTutorial)
            {
                weAreSkippingTutorial = false;
                __instance.Depart(Game.Data.Locations.Get(21), null);
                AccessTools.Field(typeof(GameManager), "_testMode").SetValue(Game.Manager, false);
                Game.Persistence.playerFile.daytimeElapsed = 8;
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
            if (!HP2SR.CheatSpeedEnabled.Value) return;
            if (Game.Session.Location.currentLocation.id <= 8) arriveWithGirls = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerFile), "PopulateFinderSlots")]
        public static void DumbPermutationShit()
        {
            Datamining.TestAllPermutations();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiWindowActionBubbles), "OnActionBubblePressed")]
        public static void SavingStartingStamina()
        {
            leftStamina = Game.Session.Puzzle.puzzleStatus.girlStatusLeft.stamina;
            rightStamina = Game.Session.Puzzle.puzzleStatus.girlStatusRight.stamina;
            leftSentiment = Game.Session.Puzzle.puzzleStatus.girlStatusLeft.sentiment;
            rightSentiment = Game.Session.Puzzle.puzzleStatus.girlStatusRight.sentiment;
            leftPassion = Game.Session.Puzzle.puzzleStatus.girlStatusLeft.passion;
            rightPassion = Game.Session.Puzzle.puzzleStatus.girlStatusRight.passion;
            moves = Game.Session.Puzzle.puzzleStatus.movesRemaining;
        }

        public static bool skipThisSetProcess = false;
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "OnTokenDown")]
        public static bool ChangeTokenType(ref UiPuzzleSlot slot, UiPuzzleGrid __instance)
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                // make power token
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    slot.token.Upgrade();
                    return false;
                }
                
                for (int i = 0; i < tokenNames.Length; i++)
                {
                    if (tokenNames[i] == slot.token.definition.tokenName)
                    {
                        i++;
                        if (i == tokenNames.Length) i = 0;

                        skipThisSetProcess = true;
                        __instance.ChangeSlotTokens(new List<UiPuzzleSlot>() { slot },
                            new List<TokenDefinition>() { tokens[i] }, true, slot.token.upgraded);
                       
                        return false;
                    }
                }
                
            }
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "ConsumePuzzleSet")]
        public static bool PreventMatchingAfterTypeSwitch()
        {
            if (skipThisSetProcess)
            {
                skipThisSetProcess = false;
                return false;
            }
            return true;
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
            string[] array = "1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,6,9,6|1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,9,6,9|2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,4,2,3|1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4|2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3|2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3|1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4|2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3|2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4,1,2,3".Split(new char[]
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
