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
using static IngameScript.AnimationEnumExtensions;

namespace IngameScript
{
    partial class Program
    {
        public class InverseSpideroidLegGroup : LegGroup
        {
            protected LegAngles CalculateAngles(double step, Vector3 forwardsDeltaVec, Vector3 movementVec, bool left, bool invertedCrouch = false)
            {
                step = step.Modulo(4);
                double turnRotation = Math.Sign(forwardsDeltaVec.Y);
                double leftInverse = left ? -1 : 1;
                double idOffset = IdOffset == 0 ? 1 : -1;

                LegAngles angles = new LegAngles();

                double stepPercent = step / 4d;
                double sin = Math.Sin(stepPercent * 2 * Math.PI);
                double cos = Math.Cos(stepPercent * 2 * Math.PI);

                /*double crouchKneeOffset = -45 * CrouchWaitTime;
                double crouchFootOffset = -10 * CrouchWaitTime;

                /*angles.HipDegrees = sin * (10 + 5 * Configuration.StepLength) * (left ? -1 : 1);
                angles.KneeDegrees = 50 + (cos) * 15;
                angles.FeetDegrees = 80 - (cos) * 10;*/

                /*Log("Is Turn:" + Animation.IsTurn().ToString());
                angles.HipDegrees  = -sin * (10 + 5 * Configuration.StepLength) * (leftInverse) - (CrouchWaitTime * Configuration.HipOffsets * .5 * (invertedCrouch ? -1 : 1));
                angles.HipDegrees *= (1 - Math.Abs(Math.Sign(forwardsDeltaVec.X)));
                if (Animation.IsTurn())
                    angles.HipDegrees = sin * turnRotation * 15;
                angles.KneeDegrees = -45 + crouchKneeOffset + (cos) * 15 * (1 - Math.Abs(Math.Sign(forwardsDeltaVec.X))) - (cos * movementVec.X * 15);
                angles.FeetDegrees = 80 - crouchFootOffset - (cos) * 10 * (1 - Math.Abs(Math.Sign(forwardsDeltaVec.X))) - (cos * movementVec.X * -10);*/

                double thigh = LeftHipStators.Count > 0 && LeftKneeStators.Count > 0 ? (LeftHipStators[0].Stator.WorldMatrix.Translation - LeftKneeStators[0].Stator.WorldMatrix.Translation).Length() : 2.5;
                double calf = LeftKneeStators.Count > 0 && LeftFootStators.Count > 0 ? (LeftKneeStators[0].Stator.WorldMatrix.Translation - LeftFootStators[0].Stator.WorldMatrix.Translation).Length() : 2.5;
                Log($"thigh, calf: {thigh}, {calf}");

                if (calf > thigh)
                {
                    calf = calf / thigh;
                    thigh = 2.5;
                }
                else
                {
                    thigh = thigh / calf;
                    calf = 2.5;
                }

                Log($"thigh, calf calc: {thigh}, {calf}");
                Log($"turn rotation: {turnRotation}");

                double x = 3;
                double y = .5;

                if (Animation.IsTurn()) // turn
                    y += MathHelper.Clamp((cos), -1, 0);//MathHelper.Clamp(cos * , 0, 1) * 1;
                else if (Animation.IsWalk() && Math.Abs(movementVec.X) > 0) // strafe
                {
                    x += (sin) * .5 * leftInverse;
                    y += (cos) * .5;
                }
                else if (Animation.IsWalk()) // walk
                {
                    x += (cos) * leftInverse * .75;
                    y += (cos - 1) * .5;
                }

                y -= 1.5 * CrouchWaitTime;
                thigh = 2.5; // TODO: calculate
                calf = 2.75;
                //                                            2.5  , 2.75
                LegAngles ik = InverseKinematics.CalculateLeg(thigh, calf, x, y);//InverseKinematics.CalculateLeg(Configuration.ThighLength, Configuration.CalfLength, 1, 1);

                if (Animation.IsTurn())
                    angles.HipDegrees = sin * turnRotation * 15;
                else
                    angles.HipDegrees =
                        -sin
                        * leftInverse
                        * (10 + 5 * Configuration.StepLength)
                        - (CrouchWaitTime * Configuration.HipOffsets * .5 * (invertedCrouch ? -1 : 1));
                angles.HipDegrees *= (1 - Math.Abs(Math.Sign(movementVec.X)));
                angles.KneeDegrees = -(ik.HipDegrees);
                angles.FeetDegrees = -(ik.KneeDegrees - 180);
                angles.QuadDegrees = 90 - angles.KneeDegrees - angles.FeetDegrees;

                return angles;
            }
            
            public override void Update(Vector3 forwardsDeltaVec, Vector3 movementVector, double delta)
            {
                double forwardsDelta = forwardsDeltaVec.Z;
                base.Update(forwardsDeltaVec, movementVector, delta);
                Log($"Step: {AnimationStep} {Animation} {delta}");

                LegAngles leftAngles, rightAngles;
                switch (Animation)
                {
                    default:
                    case Animation.Crouch:
                    case Animation.Idle:
                        AnimationStep = 0;
                        leftAngles = CalculateAngles(AnimationStep, forwardsDeltaVec, movementVector, false);
                        rightAngles = CalculateAngles(AnimationStep, forwardsDeltaVec, movementVector, false, true);
                        break;
                    case Animation.CrouchTurn:
                    case Animation.Turn:
                        OffsetLegs = true;
                        leftAngles = CalculateAngles(AnimationStep + IdOffset, forwardsDeltaVec, movementVector, true, false);
                        rightAngles = CalculateAngles(AnimationStepOffset + IdOffset, forwardsDeltaVec, movementVector, false, false);
                        break;
                    case Animation.CrouchWalk:
                    case Animation.Walk:
                        leftAngles = CalculateAngles(AnimationStep + IdOffset, forwardsDeltaVec, movementVector, true);
                        rightAngles = CalculateAngles(AnimationStepOffset + IdOffset, forwardsDeltaVec, movementVector, false);
                        break;
                }

                Log("Inverse Spideroid (right):", rightAngles.HipDegrees, rightAngles.KneeDegrees, rightAngles.FeetDegrees, rightAngles.QuadDegrees);
                Log("Inverse Spideroid (left):", leftAngles.HipDegrees, leftAngles.KneeDegrees, leftAngles.FeetDegrees, leftAngles.QuadDegrees);
                SetAngles(leftAngles * new LegAngles(1, 1, 1, 1), rightAngles * new LegAngles(1, 1, 1, 1));
            }

            protected override void SetAnglesOf(List<Joint> leftStators, List<Joint> rightStators, double leftAngle, double rightAngle, double offset)
            {
                foreach (var motor in leftStators)
                    motor.SetAngle(leftAngle * motor.Configuration.InversedMultiplier - (offset + motor.Configuration.Offset) * -1);
                foreach (var motor in rightStators)
                    motor.SetAngle(rightAngle * motor.Configuration.InversedMultiplier - (offset + motor.Configuration.Offset));
            }
        }
    }
}
