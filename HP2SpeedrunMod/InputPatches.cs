﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HP2SpeedrunMod
{
    public class InputPatches
    {
        public static KeyCode[] mouseKeys = new KeyCode[] {KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Q, KeyCode.E,
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.JoystickButton0, KeyCode.JoystickButton1, KeyCode.JoystickButton2, KeyCode.JoystickButton3 };

        public static float horiz, vert, prevHoriz, prevVert;

        public const float DEADZONE = 0.25f;

        public static bool mashCheat = false;

        public static bool IsMouseKeyDown()
        {
            if (mashCheat) return true;
            if (HP2SR.MouseWheelEnabled.Value && Input.GetAxis("Mouse ScrollWheel") != 0) return true;

            horiz = Input.GetAxis("Horizontal"); vert = Input.GetAxis("Vertical");
            if (Mathf.Abs(horiz) > DEADZONE && Mathf.Abs(prevHoriz) <= DEADZONE) return true;
            if (Mathf.Abs(vert) > DEADZONE && Mathf.Abs(prevVert) <= DEADZONE) return true;
            for (int i = 0; i < mouseKeys.Length; i++)
            {
                if (Input.GetKeyDown(mouseKeys[i])) return true;
            }
            return false;
        }

        public static bool IsMouseKeyUp()
        {
            if (mashCheat) return true;
            if (HP2SR.MouseWheelEnabled.Value && Input.GetAxis("Mouse ScrollWheel") != 0) return true;

            horiz = Input.GetAxis("Horizontal"); vert = Input.GetAxis("Vertical");
            if (Mathf.Abs(horiz) <= DEADZONE && Mathf.Abs(prevHoriz) > DEADZONE) return true;
            if (Mathf.Abs(vert) <= DEADZONE && Mathf.Abs(prevVert) > DEADZONE) return true;

            for (int i = 0; i < mouseKeys.Length; i++)
            {
                if (Input.GetKeyUp(mouseKeys[i])) return true;
            }
            return false;
        }

        public static bool IsMouseKey()
        {
            if (mashCheat) return true;
            if (HP2SR.MouseWheelEnabled.Value && Input.GetAxis("Mouse ScrollWheel") != 0) return true;

            horiz = Input.GetAxis("Horizontal"); vert = Input.GetAxis("Vertical");
            if (Mathf.Abs(horiz) > DEADZONE) return true;
            if (Mathf.Abs(vert) > DEADZONE) return true;
            for (int i = 0; i < mouseKeys.Length; i++)
            {
                if (Input.GetKey(mouseKeys[i])) return true;
            }
            return false;
        }


        //it's that easy
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Input), "GetMouseButtonUp")]
        public static void NoWay(Input __instance, int button, ref bool __result)
        {
            if (button == 0 && __result != true) __result = IsMouseKeyUp();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Input), "GetMouseButtonDown")]
        public static void NoWay2(Input __instance, int button, ref bool __result)
        {
            if (button == 0 && __result != true) __result = IsMouseKeyDown();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Input), "GetMouseButton")]
        public static void NoWay3(Input __instance, int button, ref bool __result)
        {
            if (button == 0 && __result != true) __result = IsMouseKey();
        }
    }
}
