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
        public struct JointConfiguration
        {
            public static readonly JointConfiguration DEFAULT = new JointConfiguration()
            {
                Inversed = false,
                Offset = 0,
            };

            public bool Inversed;
            public double Offset;
            public double InversedMultiplier => Inversed ? -1 : 1;
        }

        public class Joint
        {

            public IMyMotorStator Stator;
            public JointConfiguration Configuration;

            public double Minimum => Stator.LowerLimitDeg;
            public double Maximum => Stator.UpperLimitDeg;

            public Joint(IMyMotorStator stator, JointConfiguration? configuration = null)
            {
                Stator = stator;
                Configuration = configuration ?? JointConfiguration.DEFAULT;
            }

        }
    }
}
