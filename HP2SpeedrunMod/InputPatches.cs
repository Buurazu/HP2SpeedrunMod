using HarmonyLib;
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
        public static List<KeyCode> mouseKeyboardKeys = new List<KeyCode>();

        public static float horiz, vert, prevHoriz, prevVert;

        public const float DEADZONE = 0.5f;

        public static bool mashCheat = false;

        public static bool codeScreen = false;

        public static void Update()
        {
            //for (KeyCode i = KeyCode.JoystickButton0; i <= KeyCode.JoystickButton19; i++) if (Input.GetKeyDown(i)) Datamining.Logger.LogMessage(i);
            if (!HP2SR.HorizVertEnabled.Value) return;
            prevHoriz = horiz;
            prevVert = vert;
            horiz = Input.GetAxisRaw("Horizontal");
            vert = Input.GetAxisRaw("Vertical");
        }

        public static bool IsMouseKeyDown()
        {
            if (HP2SR.MouseWheelEnabled.Value && Input.GetAxis("Mouse ScrollWheel") != 0) return true;
            if (codeScreen) return false;
            if (mashCheat) return true;
            if (HP2SR.HorizVertEnabled.Value)
            {
                if (Mathf.Abs(horiz) > DEADZONE && Mathf.Abs(prevHoriz) <= DEADZONE) return true;
                if (Mathf.Abs(vert) > DEADZONE && Mathf.Abs(prevVert) <= DEADZONE) return true;
            }
            for (int i = 0; i < mouseKeyboardKeys.Count; i++)
            {
                if (Input.GetKeyDown(mouseKeyboardKeys[i])) return true;
            }
            return false;
        }

        public static bool IsMouseKeyUp()
        {
            if (HP2SR.MouseWheelEnabled.Value && Input.GetAxis("Mouse ScrollWheel") != 0) return true;
            if (codeScreen) return false;
            if (mashCheat) return true;
            if (HP2SR.HorizVertEnabled.Value)
            {
                if (Mathf.Abs(horiz) <= DEADZONE && Mathf.Abs(prevHoriz) > DEADZONE) return true;
                if (Mathf.Abs(vert) <= DEADZONE && Mathf.Abs(prevVert) > DEADZONE) return true;
            }
            for (int i = 0; i < mouseKeyboardKeys.Count; i++)
            {
                if (Input.GetKeyUp(mouseKeyboardKeys[i])) return true;
            }
            return false;
        }

        public static bool IsMouseKey()
        {
            if (HP2SR.MouseWheelEnabled.Value && Input.GetAxis("Mouse ScrollWheel") != 0) return true;
            if (codeScreen) return false;
            if (mashCheat) return true;
            if (HP2SR.HorizVertEnabled.Value)
            {
                if (Mathf.Abs(horiz) > DEADZONE) return true;
                if (Mathf.Abs(vert) > DEADZONE) return true;
            }
            for (int i = 0; i < mouseKeyboardKeys.Count; i++)
            {
                if (Input.GetKey(mouseKeyboardKeys[i])) return true;
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
