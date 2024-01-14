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
             * 
             * [Advanced]
             * WalkCycleSpeed=1
             * 
             * */

            #region # - Properties

            public static readonly LegConfiguration DEFAULT = Parse("").Value;

            private static MyIni ini;
            private string configurationString;

            public byte LegType;
            public bool HipsInverted, KneesInverted, FeetInverted;
            public double HipOffsets, KneeOffsets, FootOffsets;

            public double AnimationSpeed;

            #endregion

            #region # - Methods

            public bool HasChanged(string iniData)
            {
                return !configurationString.Equals(iniData);
            }

            public string ToCustomDataString()
            {
                ini.Set("Leg", "LegType", LegType);
                ini.SetComment("Leg", "LegType", "The leg type:\r\n\t1 = Chicken walker\r\n\t2 = Humanoid\r\n\t3 = Spideroid\r\n\t4 = Digitigrade");

                ini.Set("Leg", "HipOffsets", HipOffsets);
                ini.Set("Leg", "KneeOffsets", KneeOffsets);
                ini.Set("Leg", "FootOffsets", FootOffsets);

                ini.Set("Leg", "HipsInverted", HipsInverted);
                ini.Set("Leg", "KneesInverted", KneesInverted);
                ini.Set("Leg", "FeetInverted", FeetInverted);
                return ini.ToString();
            }

            public static LegConfiguration? Parse(string iniData)
            {
                ini = ini ?? new MyIni();
                bool parsed = ini.TryParse(iniData);
                if (!parsed)
                    return null;
                LegConfiguration config = new LegConfiguration
                {
                    LegType = ini.Get("Leg", "LegType").ToByte(),

                    HipOffsets = ini.Get("Leg", "HipOffsets").ToDouble(),
                    KneeOffsets = ini.Get("Leg", "KneeOffsets").ToDouble(),
                    FootOffsets = ini.Get("Leg", "FootOffsets").ToDouble(),

                    HipsInverted = ini.Get("Leg", "HipsInverted").ToBoolean(),
                    KneesInverted = ini.Get("Leg", "KneesInverted").ToBoolean(),
                    FeetInverted = ini.Get("Leg", "FeetInverted").ToBoolean(),
                };
                config.configurationString = config.ToCustomDataString();
                return config;
            }

            #endregion

        }
    }
}
