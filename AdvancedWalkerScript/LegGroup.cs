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

            public List<Joint> LeftHipStators = new List<Joint>();
            public List<Joint> RightHipStators = new List<Joint>();

            public List<Joint> LeftKneeStators = new List<Joint>();
            public List<Joint> RightKneeStators = new List<Joint>();

            public List<Joint> LeftFootStators = new List<Joint>();
            public List<Joint> RightFootStators = new List<Joint>();

            public List<IMyLandingGear> LeftGears = new List<IMyLandingGear>();
            public List<IMyLandingGear> RightGears = new List<IMyLandingGear>();

            public IMyCameraBlock[] InclineCameras;

            public double AnimationStep = 0; // pff, who needes getters and setters?
            public double AnimationStepOffset => OffsetLegs ? AnimationStep + 2 % 4 : AnimationStep;
            public bool OffsetLegs = true;
            public Animation Animation = Animation.Idle;
            public double AnimationWaitTime = 0;

            protected double HipInversedMultiplier = 1;
            protected double KneeInversedMultiplier = 1;
            protected double FeetInversedMultiplier = 1;

            #endregion

            #region # - Constructor

            public LegGroup() {}

            #endregion

            #region # - Methods

            private void SetAnglesOf(List<Joint> leftStators, List<Joint> rightStators, double leftAngle, double rightAngle, double offset)
            {
                // We could split this into ANOTHER method, but i don't believe it's worth it
                foreach (var motor in leftStators)
                    motor.Stator.TargetVelocityRPM = (float)MathHelper.Clamp((leftAngle * motor.Configuration.InversedMultiplier).AbsoluteDegrees(motor.Stator.BlockDefinition.SubtypeName.Contains("Hinge")) - motor.Stator.Angle.ToDegrees() - offset - motor.Configuration.Offset, -MaxRPM, MaxRPM);
                foreach (var motor in rightStators)
                    motor.Stator.TargetVelocityRPM = (float)MathHelper.Clamp((-rightAngle * motor.Configuration.InversedMultiplier).AbsoluteDegrees(motor.Stator.BlockDefinition.SubtypeName.Contains("Hinge")) - motor.Stator.Angle.ToDegrees() - offset - motor.Configuration.Offset, -MaxRPM, MaxRPM);
            }

            protected virtual void SetAngles(double leftHipDegrees, double leftKneeDegrees, double leftFeetDegrees, double rightHipDegrees, double rightKneeDegrees, double rightFeetDegrees)
            {
                // The code documents itself!
                SetAnglesOf(LeftHipStators,     RightHipStators,    (leftHipDegrees  * HipInversedMultiplier).AbsoluteDegrees(),      (rightHipDegrees * HipInversedMultiplier).AbsoluteDegrees(),    Configuration.HipOffsets);
                SetAnglesOf(LeftKneeStators,    RightKneeStators,   (leftKneeDegrees * KneeInversedMultiplier).AbsoluteDegrees(),     (rightKneeDegrees * KneeInversedMultiplier).AbsoluteDegrees(),   Configuration.KneeOffsets);
                SetAnglesOf(LeftFootStators,    RightFootStators,   (leftFeetDegrees * FeetInversedMultiplier),     (rightFeetDegrees * FeetInversedMultiplier),   Configuration.FootOffsets);
            }

            protected virtual double[] CalculateAngles(double step)
            {
                throw new Exception("Not Implemented");
            }

            public virtual void Update(double delta)
            {
                // Update multipliers, we should probably isolate this in a "Initialize" method or something
                HipInversedMultiplier = Configuration.HipsInverted ? -1 : 1;
                KneeInversedMultiplier = Configuration.KneesInverted ? -1 : 1;
                FeetInversedMultiplier = Configuration.FeetInverted ? -1 : 1;

                OffsetLegs = delta != 0;

                // Update animation step
                AnimationStep += delta * Configuration.AnimationSpeed;
                AnimationStep %= 4; // 0 to 3
            }

            #endregion

        }
    }
}
