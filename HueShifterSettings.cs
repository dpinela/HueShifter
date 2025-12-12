using System;

namespace HueShifter
{
    public enum RandomPhaseSetting
    {
        Fixed,
        RandomPerMapArea,
        RandomPerRoom,
    }
    
    public class HueShifterSettings
    {
        public bool ModEnabled = true;
        public float Phase = 0;
        public RandomPhaseSetting RandomPhase = RandomPhaseSetting.RandomPerMapArea;
        public bool ShiftLighting = true;
        public bool RespectLighting = true;
        public float XFrequency = 0;
        public float YFrequency = 0;
        public float ZFrequency = 0;
        public float TimeFrequency = 0;
        public bool AllowVanillaPhase = false;
    }
}