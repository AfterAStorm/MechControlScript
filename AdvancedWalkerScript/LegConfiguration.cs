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
        /// Each configuration has a numerical id starting at one (default)
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

            public static readonly LegConfiguration DEFAULT = Create();

            public int Id;

            private static MyIni ini;

            public int LegType;
            public bool HipsInverted, KneesInverted, FeetInverted, QuadInverted;
            public double HipOffsets, KneeOffsets, FootOffsets, QuadOffsets;

            public double ThighLength, CalfLength;

            public double StepLength;
            public double StepHeight;

            public double AnimationSpeed;// => WalkCycleSpeed;
            public double CrouchSpeed;

            private int defaultValue;
            public bool Default => defaultValue <= 0;

            #endregion

            #region # - Methods

            public override bool Equals(object obj)
            {
                LegConfiguration a = (LegConfiguration)obj;
                return LegType == a.LegType && HipOffsets == a.HipOffsets && KneeOffsets == a.KneeOffsets && FootOffsets == a.FootOffsets && ThighLength == a.ThighLength && CalfLength == a.CalfLength && StepLength == a.StepHeight && StepHeight == a.StepHeight && AnimationSpeed == a.AnimationSpeed && CrouchSpeed == a.CrouchSpeed;
            }

            public string ToCustomDataString()
            {
                /**
                 * 
; Change joint offsets in degrees
HipOffsets=0
KneeOffsets=0
FootOffsets=0
; How far forwards/backwards and up/down legs step
; 0.5 is half, 1 is default, 2 is double
StepLength=1
StepHeight=1
; How fast legs crouch
CrouchSpeed=1
; Change theoretical apendage lengths
ThighLength=2.5
CalfLength=2.5

                 */

                ini.Clear();
                ini.Set("Leg", "LegType", LegType);
                ini.SetComment("Leg", "LegType", "1 = Chicken walker\n2 = Humanoid\n3 = Spideroid\n4 = Crab\n5 = Digitigrade");

                ini.Set("Leg", "HipOffsets", HipOffsets);
                ini.SetComment("Leg", "HipOffsets", "The joints' offsets (in degrees)");
                ini.Set("Leg", "KneeOffsets", KneeOffsets);
                ini.Set("Leg", "FootOffsets", FootOffsets);
                ini.Set("Leg", "QuadOffsets", QuadOffsets);

                ini.Set("Leg", "StepLength", StepLength);
                ini.SetComment("Leg", "StepLength", "How far forwards/backwards and up/down legs step\n0.5 is half, 1 is default, 2 is double");
                ini.Set("Leg", "StepHeight", StepHeight);

                ini.Set("Leg", "WalkSpeed", AnimationSpeed);
                ini.Set("Leg", "CrouchSpeed", CrouchSpeed);
                ini.SetComment("Leg", "CrouchSpeed", "How fast legs walk and crouch");

                ini.Set("Leg", "ThighLength", ThighLength);
                ini.Set("Leg", "CalfLength", CalfLength);
                ini.SetComment("Leg", "ThighLength", "Change theoretical apendage lengths");

                ini.SetSectionComment("Leg", $"Leg (group {Id}) settings. These change all of the joints in the same group.");
                return ini.ToString();
            }

            public static LegConfiguration Parse(MyIni ini)
            {
                LegConfiguration config = new LegConfiguration
                {
                    LegType = ini.Get("Leg", "LegType").ToInt32(1),

                    HipOffsets = ini.Get("Leg", "HipOffsets").ToDouble(DefaultHipOffsets),
                    KneeOffsets = ini.Get("Leg", "KneeOffsets").ToDouble(DefaultKneeOffsets),
                    FootOffsets = ini.Get("Leg", "FootOffsets").ToDouble(DefaultFeetOffsets),
                    QuadOffsets = ini.Get("Leg", "QuadOffsets").ToDouble(DefaultQuadOffsets),

                    /*HipsInverted = ini.Get("Leg", "HipsInverted").ToBoolean(),
                    KneesInverted = ini.Get("Leg", "KneesInverted").ToBoolean(),
                    FeetInverted = ini.Get("Leg", "FeetInverted").ToBoolean(),
                    QuadInverted = ini.Get("Leg", "QuadInverted").ToBoolean(),*/

                    ThighLength = ini.Get("Leg", "ThighLength").ToDouble(2.5d),
                    CalfLength = ini.Get("Leg", "CalfLength").ToDouble(2.5d),

                    StepLength = ini.Get("Leg", "StepLength").ToDouble(1),
                    StepHeight = ini.Get("Leg", "StepHeight").ToDouble(1),

                    AnimationSpeed = ini.Get("Leg", "WalkSpeed").ToDouble(2),
                    CrouchSpeed = ini.Get("Leg", "CrouchSpeed").ToDouble(1),

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
