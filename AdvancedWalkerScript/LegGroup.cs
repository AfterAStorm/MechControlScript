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
        public struct LegAngles
        {
            public double HipDegrees;
            public double KneeDegrees;
            public double FeetDegrees;
            public double QuadDegrees;

            public double HipRadians => HipDegrees.ToRadians();
            public double KneeRadians => KneeRadians.ToRadians();
            public double FeetRadians => FeetRadians.ToRadians();
            public double QuadRadians => QuadDegrees.ToRadians();

            public LegAngles(double hip, double knee, double feet, double quad)
            {
                HipDegrees = hip;
                KneeDegrees = knee;
                FeetDegrees = feet;
                QuadDegrees = quad;
            }

            public LegAngles(double hip, double knee, double feet)
            {
                HipDegrees = hip;
                KneeDegrees = knee;
                FeetDegrees = feet;
                QuadDegrees = 0;
            }

            public static LegAngles operator +(LegAngles left, LegAngles right) => new LegAngles(left.HipDegrees + right.HipDegrees, left.KneeDegrees + right.KneeDegrees, left.FeetDegrees + right.FeetDegrees, left.QuadDegrees + right.QuadDegrees);
            public static LegAngles operator *(LegAngles left, LegAngles right) => new LegAngles(left.HipDegrees * right.HipDegrees, left.KneeDegrees * right.KneeDegrees, left.FeetDegrees * right.FeetDegrees, left.QuadDegrees * right.QuadDegrees);
        }

        public class LegGroup
        {

            #region # - Properties

            public LegConfiguration Configuration;

            public List<LegJoint> LeftHipStators = new List<LegJoint>();
            public List<LegJoint> RightHipStators = new List<LegJoint>();

            public List<LegJoint> LeftKneeStators = new List<LegJoint>();
            public List<LegJoint> RightKneeStators = new List<LegJoint>();

            public List<LegJoint> LeftFootStators = new List<LegJoint>();
            public List<LegJoint> RightFootStators = new List<LegJoint>();

            public List<LegJoint> LeftQuadStators = new List<LegJoint>();
            public List<LegJoint> RightQuadStators = new List<LegJoint>();

            public List<FetchedBlock> LeftPistons = new List<FetchedBlock>();
            public List<FetchedBlock> RightPistons = new List<FetchedBlock>();

            public List<IMyLandingGear> LeftGears = new List<IMyLandingGear>();
            public List<IMyLandingGear> RightGears = new List<IMyLandingGear>();

            //public IMyCameraBlock[] InclineCameras; // TODO: use these, give them a purpose!

            protected double LastDelta = 1;
            public double AnimationStep = 0; // pff, who needes getters and setters?
            public double AnimationStepOffset => OffsetLegs ? (AnimationStep + 2).Modulo(4) : AnimationStep;
            public double CrouchWaitTime = 0; // extra property for stuffs that needs it
            public double IdOffset => Configuration.Id % 2 == 1 ? 0 : 2;
            public bool OffsetLegs = true;
            public Animation Animation = Animation.Idle;
            public double AnimationWaitTime = 0;

            protected double HipInversedMultiplier = 1;
            protected double KneeInversedMultiplier = 1;
            protected double FeetInversedMultiplier = 1;
            protected double QuadInversedMultiplier = 1;

            #endregion

            #region # - Constructor

            public LegGroup() {}

            #endregion

            #region # - Methods

            protected virtual void SetAnglesOf(List<LegJoint> leftStators, List<LegJoint> rightStators, double leftAngle, double rightAngle, double offset)
            {
                // We could split this into ANOTHER method, but i don't believe it's worth it
                foreach (var motor in leftStators)
                    motor.SetAngle(leftAngle * motor.Configuration.InversedMultiplier - (offset + motor.Configuration.Offset));
                    //SetJointAngle(motor, leftAngle * motor.Configuration.InversedMultiplier, offset + motor.Configuration.Offset);
                    //motor.Stator.TargetVelocityRPM = (float)MathHelper.Clamp((leftAngle * motor.Configuration.InversedMultiplier).AbsoluteDegrees(motor.Stator.BlockDefinition.SubtypeName.Contains("Hinge")) - motor.Stator.Angle.ToDegrees() - offset - motor.Configuration.Offset, -MaxRPM, MaxRPM);
                foreach (var motor in rightStators)
                    motor.SetAngle(rightAngle * motor.Configuration.InversedMultiplier - (offset + motor.Configuration.Offset));
                    //SetJointAngle(motor, -rightAngle * motor.Configuration.InversedMultiplier, offset + motor.Configuration.Offset);
                    //motor.Stator.TargetVelocityRPM = (float)MathHelper.Clamp((-rightAngle * motor.Configuration.InversedMultiplier).AbsoluteDegrees(motor.Stator.BlockDefinition.SubtypeName.Contains("Hinge")) - motor.Stator.Angle.ToDegrees() - offset - motor.Configuration.Offset, -MaxRPM, MaxRPM);
            }

            protected virtual void SetAngles(double leftHipDegrees, double leftKneeDegrees, double leftFeetDegrees, double leftQuadDegrees, double rightHipDegrees, double rightKneeDegrees, double rightFeetDegrees, double rightQuadDegrees)
            {
                // The code documents itself!
                SetAnglesOf(LeftHipStators,     RightHipStators,    (leftHipDegrees  * HipInversedMultiplier),      (rightHipDegrees * HipInversedMultiplier),     Configuration.HipOffsets);
                SetAnglesOf(LeftKneeStators,    RightKneeStators,   (leftKneeDegrees * KneeInversedMultiplier),     (rightKneeDegrees * KneeInversedMultiplier),   Configuration.KneeOffsets);
                SetAnglesOf(LeftFootStators,    RightFootStators,   (leftFeetDegrees * FeetInversedMultiplier),     (rightFeetDegrees * FeetInversedMultiplier),   Configuration.FootOffsets);
                SetAnglesOf(LeftQuadStators,    RightQuadStators,   (leftQuadDegrees * QuadInversedMultiplier),     (rightQuadDegrees * QuadInversedMultiplier),   Configuration.QuadOffsets);
            }

            protected virtual void SetAngles(LegAngles leftAngles, LegAngles rightAngles)
            {
                SetAngles(
                    leftAngles.HipDegrees,
                    leftAngles.KneeDegrees,
                    leftAngles.FeetDegrees,
                    leftAngles.QuadDegrees,
                    rightAngles.HipDegrees,
                    rightAngles.KneeDegrees,
                    rightAngles.FeetDegrees,
                    rightAngles.QuadDegrees
                );
            }

            protected double FindThighLength()
            {
                if (LeftHipStators.Count == 0 || LeftKneeStators.Count == 0)
                {
                    if (RightHipStators.Count == 0 || RightKneeStators.Count == 0)
                        return -1d;
                    return (RightHipStators.First().Stator.GetPosition() - RightKneeStators.First().Stator.GetPosition()).Length();
                }
                return (LeftHipStators.First().Stator.GetPosition() - LeftKneeStators.First().Stator.GetPosition()).Length();
            }

            protected double FindCalfLength()
            {
                if (LeftFootStators.Count == 0 || LeftKneeStators.Count == 0)
                {
                    if (RightFootStators.Count == 0 || RightKneeStators.Count == 0)
                        return -1d;
                    return (RightFootStators.First().Stator.GetPosition() - RightKneeStators.First().Stator.GetPosition()).Length();
                }
                return (LeftFootStators.First().Stator.GetPosition() - LeftKneeStators.First().Stator.GetPosition()).Length();
            }

            /// <summary>
            /// Used internally in each leg implementation, calculates each leg angle
            /// </summary>
            /// <param name="step"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            protected virtual LegAngles CalculateAngles(double step)
            {
                throw new Exception("CalculateAngles Not Implemented");
            }

            public virtual void Update(Vector3 forwardsDeltaVec, Vector3 movementVector, double delta)
            {
                double forwardsDelta = forwardsDeltaVec.Z;
                // Update multipliers, we should probably isolate this in a "Initialize" method or something
                HipInversedMultiplier = Configuration.HipsInverted ? -1 : 1;
                KneeInversedMultiplier = Configuration.KneesInverted ? -1 : 1;
                FeetInversedMultiplier = Configuration.FeetInverted ? -1 : 1;
                QuadInversedMultiplier = Configuration.QuadInverted ? -1 : 1;

                // If the legs should be offset or not, used for animation stuffs
                OffsetLegs = forwardsDelta != 0;

                if (OffsetLegs)
                    LastDelta = forwardsDelta;

                // Animate crouch
                if (!Animation.IsCrouch())
                    CrouchWaitTime = Math.Max(0, jumping ? 0 : CrouchWaitTime - delta * 2 * Configuration.CrouchSpeed);
                else
                    CrouchWaitTime = Math.Min(1, CrouchWaitTime + delta * 2 * Configuration.CrouchSpeed);

                // Update animation step
                double multiplier = forwardsDelta / Math.Abs(forwardsDelta);
                Log($"mul: {multiplier}");
                //if (!double.IsNaN(multiplier))
                    AnimationStep = animationStep;
                //else
                //    AnimationStep += (!double.IsNaN(multiplier) ? forwardsDelta : delta * (LastDelta / Math.Abs(LastDelta)) / 2) * Configuration.AnimationSpeed;//delta * (!double.IsNaN(multiplier) ? multiplier : 1) * Configuration.AnimationSpeed;
                AnimationStep %= 4; // 0 to 3
            }

            #endregion

        }
    }
}
