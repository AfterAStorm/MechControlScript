using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        /// <summary>
        /// Holds configuration information
        /// Each configuration has a numerical id starting at zero (default)
        /// </summary>
        public struct LegConfiguration
        {

            /*
             * 
             * Formatted as such:
             * 
             * [Leg]
             * HipOffsets=x
             * KneeOffsets=x
             * FeetOffsets=x
             * HipsInverted=y
             * KneesInverted=y
             * FeetInverted=y
             * ThighLength=2.5
             * CalfLength=2.5
             * StepLengthMultiplier=1
             * 
             * */

            #region # - Properties

            public static readonly LegConfiguration DEFAULT = Parse("");

            public int Id;

            private static MyIni ini;

            public byte LegType;
            public bool HipsInverted, KneesInverted, FeetInverted;
            public double HipOffsets, KneeOffsets, FootOffsets;

            public double ThighLength, CalfLength;

            public double StepLengthMultiplier;

            public double AnimationSpeed => WalkCycleSpeed;

            private int defaultValue;
            public bool Default => defaultValue <= 0;

            #endregion

            #region # - Methods

            public string ToCustomDataString()
            {
                ini.Clear();
                ini.Set("Leg", "LegType", LegType);
                ini.SetComment("Leg", "LegType", "The leg type:\r\n\t1 = Chicken walker\r\n\t2 = Humanoid\r\n\t3 = Spideroid\r\n\t4 = Digitigrade");

                ini.Set("Leg", "HipOffsets", HipOffsets);
                ini.SetComment("Leg", "HipOffsets", "The joints' offsets");
                ini.Set("Leg", "KneeOffsets", KneeOffsets);
                ini.Set("Leg", "FootOffsets", FootOffsets);

                ini.Set("Leg", "HipsInverted", HipsInverted);
                ini.SetComment("Leg", "HipsInverted", "If the joints should be inverted or not");
                ini.Set("Leg", "KneesInverted", KneesInverted);
                ini.Set("Leg", "FeetInverted", FeetInverted);

                ini.Set("Leg", "ThighLength", ThighLength);
                ini.Set("Leg", "CalfLength", CalfLength);
                ini.SetComment("Leg", "ThighLength", "");

                ini.Set("Leg", "StepLengthMultiplier", StepLengthMultiplier);
                ini.SetComment("Leg", "StepLengthMultiplier", "This changes step length -- how far forwards/backwards the feet go,\n0.5 is half, 1 is default, 2 is double");

                ini.SetSectionComment("Leg", "These are all the leg group settings associated with this leg group,\nchanging these will change all the other joints in the same group");
                return ini.ToString();
            }

            public static LegConfiguration Parse(MyIni ini)
            {
                LegConfiguration config = new LegConfiguration
                {
                    LegType = ini.Get("Leg", "LegType").ToByte(),

                    HipOffsets = ini.Get("Leg", "HipOffsets").ToDouble(DefaultHipOffsets),
                    KneeOffsets = ini.Get("Leg", "KneeOffsets").ToDouble(DefaultKneeOffsets),
                    FootOffsets = ini.Get("Leg", "FootOffsets").ToDouble(DefaultFeetOffsets),

                    HipsInverted = ini.Get("Leg", "HipsInverted").ToBoolean(),
                    KneesInverted = ini.Get("Leg", "KneesInverted").ToBoolean(),
                    FeetInverted = ini.Get("Leg", "FeetInverted").ToBoolean(),

                    ThighLength = ini.Get("Leg", "ThighLength").ToDouble(2.5d),
                    CalfLength = ini.Get("Leg", "CalfLength").ToDouble(2.5d),

                    StepLengthMultiplier = ini.Get("Leg", "StepLengthMultiplier").ToDouble(1),

                    defaultValue = 1
                };
                return config;
            }

            public static LegConfiguration Parse(string iniData)
            {
                ini = ini ?? new MyIni();
                ini.Clear();
                bool parsed = ini.TryParse(iniData);
                //if (!parsed)
                //    return null;
                return Parse(ini);
            }

            public static LegConfiguration Create()
            {
                return Parse("");
            }

            #endregion

        }
    }
}
