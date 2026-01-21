using BepInEx;
using HarmonyLib;
using Silksong.DataManager;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UObject = UnityEngine.Object;

namespace HueShifter
{
    [BepInAutoPlugin(id: "io.github.dpinela.hueshifter", "HueShifter")]
    [BepInDependency("org.silksong-modding.datamanager")]
    [BepInDependency("org.silksong-modding.modmenu")]
    public partial class HueShifterPlugin : BaseUnityPlugin, IModMenuCustomMenu, IProfileDataMod<HueShifterSettings>
    {
        public static HueShifterPlugin Instance;
        public HueShifterSettings? ProfileData
        {
            get;
            set
            {
                field = value != null ? value : new();
            }
        } = new();
        internal HueShifterSettings GS => ProfileData!;

        //private Menu menuRef;

        public Shader RainbowDefault;
        public Shader RainbowScreenBlend;
        public Shader RainbowLit;
        public Shader RainbowParticleAdd;
        public Shader RainbowParticleAddSoft;
        public Shader RainbowGrassDefault;
        public Shader RainbowGrassLit;

        public readonly Dictionary<string, float> Palette = new();
        internal readonly MaterialPropertyBlock materialPropertyBlock = new();
        private readonly Dictionary<Material, Material> materialSwaps = new();

        private static readonly int PhaseProperty = Shader.PropertyToID("_Phase");
        private static readonly int FrequencyProperty = Shader.PropertyToID("_Frequency");

        public void LoadAssets()
        {
            var platform = Application.platform switch
            {
                RuntimePlatform.LinuxPlayer => "linux",
                RuntimePlatform.WindowsPlayer => "windows",
                RuntimePlatform.OSXPlayer => "osx",
                _ => throw new PlatformNotSupportedException("What platform are you even on??")
            };

            var assetBundle = AssetBundle.LoadFromStream(
                typeof(HueShifterPlugin).Assembly.GetManifestResourceStream(
                    $"HueShifter.Resources.AssetBundles.hueshiftshaders-{platform}"));
            // foreach (var name in assetBundle.GetAllAssetNames()) Log($"assetBundle contains {name}");

            RainbowDefault = assetBundle.LoadAsset<Shader>("assets/shader/rainbowdefault.shader");
            RainbowScreenBlend = assetBundle.LoadAsset<Shader>("assets/shader/rainbowscreenblend.shader");
            RainbowLit = assetBundle.LoadAsset<Shader>("assets/shader/rainbowlit.shader");
            RainbowParticleAdd = assetBundle.LoadAsset<Shader>("assets/shader/rainbowparticleadd.shader");
            RainbowParticleAddSoft = assetBundle.LoadAsset<Shader>("assets/shader/rainbowparticleaddsoft.shader");
            RainbowGrassDefault = assetBundle.LoadAsset<Shader>("assets/shader/rainbowgrassdefault.shader");
            RainbowGrassLit = assetBundle.LoadAsset<Shader>("assets/shader/rainbowgrasslit.shader");
        }

        public void Start()
        {
            Instance = this;
            if (RainbowDefault is null) LoadAssets();
            new Harmony(Id).PatchAll();
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.OnNextLevelReady))]
        private static class NextLevelPatch
        {
            private static void Postfix(GameManager __instance)
            {
                if (!Instance.GS.ModEnabled)
                {
                    return;
                }

                IEnumerator DelayedShaderSet()
                {
                    yield return null; // wait a frame
                    HueShifterPlugin.Instance.SetAllTheShaders();
                }
                __instance.StartCoroutine(DelayedShaderSet());
            }
        }

        public float GetPhase()
        {
            string location;
            switch (GS.RandomPhase)
            {
                case RandomPhaseSetting.RandomPerMapArea:
                    location = GameManager.instance.sm.mapZone.ToString();
                    break;
                case RandomPhaseSetting.RandomPerRoom:
                    location = GameManager.instance.sceneName;
                    break;
                case RandomPhaseSetting.Fixed:
                default:
                    return GS.Phase / 360;
            }

            if (!Palette.ContainsKey(location))
                Palette[location] = GS.AllowVanillaPhase ? Random.Range(0f, 1f) : Random.Range(0.05f, 0.95f);
            return Palette[location];
        }

        private void SetAllTheShaders()
        {
            var frequencyVector = new Vector4(GS.XFrequency / 40, GS.YFrequency / 40, GS.ZFrequency / 200,
                GS.TimeFrequency / 10);
            var phase = GetPhase();

            for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                // GetAllScenes is deprecated???
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (GameManager.GetBaseSceneName(scene.name) != GameManager.instance.sceneName) continue;
                
                foreach (var rootObj in scene.GetRootGameObjects())
                    foreach (var renderer in rootObj.GetComponentsInChildren<Renderer>(true))
                        if (ShouldShift(renderer))
                            SetShader(renderer, phase, frequencyVector);
            }

            foreach (var renderer in GameCameras.instance.sceneParticles.GetComponentsInChildren<Renderer>(false))
                if (renderer.enabled)
                    SetShader(renderer, phase, frequencyVector);
            
            materialSwaps.Clear();
        }

        public static bool ShouldShift(Renderer renderer)
        {
            var go = renderer.gameObject;
            return
                renderer is not SpriteRenderer {color.maxColorComponent: 0} &&
            go.name != "Item Sprite";
        }
        
        public void SetShader(Renderer renderer, float phase, Vector4 frequencyVector)
        {
            var hasShifted = false;

            var oldMaterials = renderer.sharedMaterials; // Unity returns copies of these arrays
            for (var i = 0; i < oldMaterials.Length; i++)
            {
                var oldMaterial = oldMaterials[i];
                if (oldMaterial is null) continue;
                if (oldMaterial.shader.name is 
                    "Custom/RainbowLit" or
                    "Custom/RainbowDefault" or
                    "Custom/RainbowScreenBlend" or
                    "Custom/RainbowParticleAdd" or
                    "Custom/RainbowParticleAddSoft" or
                    "Custom/RainbowGrassDefault" or
                    "Custom/RainbowGrassLit")
                {
                    hasShifted = true;
                    continue;
                }
                
                if (!materialSwaps.ContainsKey(oldMaterial))
                {
                    var newShader = oldMaterial.shader.name switch
                    {
                        "Sprites/Lit" => GS.RespectLighting ? RainbowLit : RainbowDefault,
                        "Sprites/Default" => RainbowDefault,
                        "Sprites/Cherry-Default" => RainbowDefault,
                        "UI/BlendModes/Screen" => RainbowScreenBlend,
                        "Sprites/Screen" => RainbowScreenBlend,
                        "Legacy Shaders/Particles/Additive" => RainbowParticleAdd,
                        "Legacy Shaders/Particles/Additive (Soft)" => RainbowParticleAddSoft,
                        "Hollow Knight/Grass-Default" => RainbowGrassDefault,
                        "Hollow Knight/Grass-Diffuse" => GS.RespectLighting ? RainbowGrassLit : RainbowGrassDefault,
                        _ => null,
                    };
                    if (newShader is null) continue;
                    var newMaterial = UObject.Instantiate(oldMaterial);
                    newMaterial.shader = newShader;
                    materialSwaps[oldMaterial] = newMaterial;
                }
                hasShifted = true;
                oldMaterials[i] = materialSwaps[oldMaterial];
            }
            
            if (!hasShifted) return;
            renderer.sharedMaterials = oldMaterials;
            
            renderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetFloat(PhaseProperty, phase);
            materialPropertyBlock.SetVector(FrequencyProperty, frequencyVector);
            renderer.SetPropertyBlock(materialPropertyBlock);
        }

        private const string MenuName = "HueShifter";

        public string ModMenuName() => MenuName;

        public AbstractMenuScreen BuildCustomMenu()
        {
            var pagedMenu = new PaginatedMenuScreen(MenuName);
            var mainPage = new VerticalGroup();

            Action refreshMenu = () => {};

            mainPage.Add(Toggle("Mod Enabled", () => GS.ModEnabled, (v) => GS.ModEnabled = v, ref refreshMenu));
            {
                var model = ChoiceModels.ForEnum<RandomPhaseSetting>();
                model.OnValueChanged += (ps) =>
                {
                    GS.RandomPhase = ps;
                };
                refreshMenu += () => model.SetValue(GS.RandomPhase);
                mainPage.Add(new ChoiceElement<RandomPhaseSetting>("Randomize Hues", model));
            }
            mainPage.Add(Slider("Hue Shift Angle", 0, 360, 37, () => GS.Phase, (v) => GS.Phase = v, ref refreshMenu));
            mainPage.Add(Toggle("Allow Vanilla Colours", () => GS.AllowVanillaPhase, (v) => GS.AllowVanillaPhase = v, ref refreshMenu));
            mainPage.Add(Toggle("Shift Scene Lighting", () => GS.ShiftLighting, (v) => GS.ShiftLighting = v, ref refreshMenu));
            mainPage.Add(Toggle("Respect Lighting", () => GS.RespectLighting, (v) => GS.RespectLighting = v, ref refreshMenu));
            mainPage.Add(Button("Re-roll Palette", () =>
            {
                Palette.Clear();
                SetAllTheShaders();
            }));
            pagedMenu.AddPage(mainPage);

            var rainbowPage = new VerticalGroup();
            rainbowPage.Add(Slider("Rainbow Y", -100, 100, 21, () => GS.YFrequency, (v) => GS.YFrequency = v, ref refreshMenu));
            rainbowPage.Add(Slider("Rainbow Z", -100, 100, 21, () => GS.ZFrequency, (v) => GS.ZFrequency = v, ref refreshMenu));
            rainbowPage.Add(Slider("Rainbow X", -100, 100, 21, () => GS.XFrequency, (v) => GS.XFrequency = v, ref refreshMenu));
            rainbowPage.Add(Slider("Animation Speed", -100, 100, 21, () => GS.TimeFrequency, (v) => GS.TimeFrequency = v, ref refreshMenu));
            rainbowPage.Add(Button("Apply to Current Room", SetAllTheShaders));
            rainbowPage.Add(Button("Reset to Defaults", () =>
            {
                ProfileData = new();
                refreshMenu();
                SetAllTheShaders();
            }));
            pagedMenu.AddPage(rainbowPage);

            refreshMenu();

            return pagedMenu;
        }

        private static ChoiceElement<bool> Toggle(string label, Func<bool> getter, Action<bool> setter, ref Action refresh)
        {
            var model = ChoiceModels.ForBool("Off", "On");
            model.OnValueChanged += setter;
            refresh += () => model.SetValue(getter());
            return new(label, model);
        }

        private static SliderElement<float> Slider(string label, float min, float max, int numSteps, Func<float> getter, Action<float> setter, ref Action refresh)
        {
            var model = SliderModels.ForFloats(min, max, numSteps);
            model.OnValueChanged += setter;
            refresh += () => model.SetValue(getter());
            return new(label, model);
        }

        private static TextButton Button(string label, Action effect)
        {
            var button = new TextButton(label);
            button.OnSubmit += effect;
            return button;
        }

        public bool ToggleButtonInsideMenu => true;
    }
}
