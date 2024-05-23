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
        public class ArmGroup
        {

            #region # - Properties

            public ArmConfiguration Configuration;

            public List<ArmJoint> PitchJoints = new List<ArmJoint>();
            public List<ArmJoint> YawJoints = new List<ArmJoint>();
            public List<ArmJoint> RollJoints = new List<ArmJoint>();

            public List<IMyLandingGear> Magnets = new List<IMyLandingGear>();

            public double Pitch => armPitch;
            public double Yaw => armYaw;
            public double Roll => armRoll;

            #endregion

            #region # - Methods

            public void Update()
            {
                foreach (var joint in PitchJoints)
                {
                    joint.SetAngle((Pitch + joint.Configuration.Offset) * joint.Configuration.InversedMultiplier * joint.Configuration.Multiplier);
                }
                foreach (var joint in YawJoints)
                {
                    joint.SetAngle((Yaw + joint.Configuration.Offset) * joint.Configuration.InversedMultiplier * joint.Configuration.Multiplier);
                }
                foreach (var joint in RollJoints)
                {
                    joint.SetAngle((Roll + joint.Configuration.Offset) * joint.Configuration.InversedMultiplier * joint.Configuration.Multiplier);
                }
            }

            #endregion

        }
    }
}
