using UnityEngine;
using HarmonyLib;

namespace HueShifter;

[HarmonyPatch(typeof(SceneColorManager), nameof(SceneColorManager.UpdateScriptParameters))]
internal static class HeroLightHandler
{
    private static void Postfix()
    {
        if (HueShifterPlugin.Instance.GS.ModEnabled)
        {
            Color.RGBToHSV(HeroController.instance.heroLight.BaseColor, out var h, out var s, out var v);
            HeroController.instance.heroLight.BaseColor = Color.HSVToRGB( Mathf.Repeat(h + HueShifterPlugin.Instance.GetPhase(), 1.0f), s, v);
        }
    }
}