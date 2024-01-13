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
        public class LegGroup
        {

            #region # - Properties

            public static Program Program; // set in the Program's constructor, a reference so we don't need to pass it in the constructor
            // ^ bad practice but this is a space engineers script, this isn't NASA!

            public LegConfiguration Configuration;

            public IMyMotorStator[] HipStators;
            public IMyMotorStator[] KneeStators;
            public IMyMotorStator[] FootStators;
            public IMyCameraBlock[] InclineCameras;

            public double AnimationStep = 0; // pff, who needes getters and setters?
            public double Offset = 0;
            public double OffsetPassed = 0;

            #endregion

            #region # - Constructor

            public LegGroup(IMyMotorStator[] hips, IMyMotorStator[] knees, IMyMotorStator[] feet)
            {
                HipStators = hips;
                KneeStators = knees;
                FootStators = feet;
            }

            public LegGroup() {}

            #endregion

            #region # - Methods

            public virtual void Update(double delta)
            {
                if (OffsetPassed < Offset)
                {
                    OffsetPassed += delta * 2;
                    if (OffsetPassed >= Offset)
                        delta = OffsetPassed - Offset; // gets the time over the offset, so if its 9.2 / 9 we get .2 points of delta back
                    else
                    {
                        delta = 0;
                        AnimationStep = 0;
                    }
                }

                AnimationStep += delta * Configuration.AnimationSpeed;
                AnimationStep %= 4; // 0 to 3
            }

            #endregion

        }
    }
}
