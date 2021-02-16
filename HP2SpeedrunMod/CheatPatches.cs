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
