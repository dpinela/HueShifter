using HarmonyLib;
using UnityEngine;

namespace HueShifter;

[HarmonyPatch(typeof(CustomSceneManager), nameof(CustomSceneManager.SetLighting))]
internal static class LightingHandler
{

    private static void Prefix(ref Color ambientLightColor)
    {
        if (HueShifterPlugin.Instance.GS.ShiftLighting)
        {
            Color.RGBToHSV(ambientLightColor, out var h, out var s, out var v);
            ambientLightColor = Color.HSVToRGB( Mathf.Repeat(h + HueShifterPlugin.Instance.GetPhase(), 1.0f), s, v);
        }
    }
}