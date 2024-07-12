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
        public class SpideroidLegGroup : LegGroup
        {
            protected LegAngles CalculateAngles(double step, Vector3 forwardsDeltaVec, Vector3 movementVec, bool left, bool invertedCrouch = false)
            {
                step = step.Modulo(4);
                double turnRotation = Math.Sign(forwardsDeltaVec.Y);
                double leftInverse = left ? -1d : 1d;
                double idOffset = IdOffset == 0 ? 1 : -1;

                LegAngles angles = new LegAngles();

                double stepPercent = step / 4d;
                double sin = Math.Sin(stepPercent * 2 * Math.PI);
                double cos = Math.Cos(stepPercent * 2 * Math.PI);

                double crouchKneeOffset = -45 * CrouchWaitTime;
                double crouchFootOffset = -35 * CrouchWaitTime;

                /*angles.HipDegrees = sin * (10 + 5 * Configuration.StepLength) * (left ? -1 : 1);
                angles.KneeDegrees = 50 + (cos) * 15;
                angles.FeetDegrees = 80 - (cos) * 10;*/

                /* OLD WORKING CODE :DDD
                Log("Is Turn:" + Animation.IsTurn().ToString());
                angles.HipDegrees  = sin * (10 + 5 * Configuration.StepLength) * (left ? -1 : 1) - (CrouchWaitTime * Configuration.HipOffsets * .5 * (invertedCrouch ? -1 : 1));
                if (Animation.IsTurn())
                    angles.HipDegrees = 0;
                angles.KneeDegrees = 40 + crouchKneeOffset + (cos) * 15;
                angles.FeetDegrees = 65 - crouchFootOffset - (cos) * 10;
                angles.QuadDegrees = 180 - angles.KneeDegrees - angles.FeetDegrees;
                */

                double x = 3;
                double y = .65d;

                // y+ is down
                // x+ is ????

                if (Animation.IsTurn()) // turn
                    y += MathHelper.Clamp((cos), -1, 0);//MathHelper.Clamp(cos * , 0, 1) * 1;
                else if (Animation.IsWalk() && Math.Abs(movementVec.X) > 0) // strafe
                {
                    double offset = idOffset == -1 ? 1 : 0;
                    //x += (sin) * 1 * leftInverse;
                    //y += (cos) * .5;
                    // x += MathHelper.Clamp(sin, -1, 1) * 1;
                    // y += -MathHelper.Clamp(cos, 0, 1);
                    //  x -= .5d - (sin * .5d);
                    x -= Math.Sign(movementVec.X) * leftInverse * sin * .85 + .85;//.5 + sin * leftInverse * .5;//-= .5d - ((cos * leftInverse) * .5d);
                    y += -MathHelper.Clamp(cos + .5, 0, 1) * .5d;
                }
                else if (Animation.IsWalk()) // walk
                {
                    Log($"walk");
                    x += ((cos) * .75) + .1;
                    y += -.75 + (cos - 1) * .1;
                    //if (left)
                    //    y *= -1;
                }

                /* DANCE
                 * x += -Math.Abs(sin * leftInverse);
                   y += cos * .5;
                 */

                y -= 1.5 * CrouchWaitTime;
                Log($"x:{x}; y:{y}");
                y *= -1;

                double thigh = Configuration.ThighLength;
                double calf = Configuration.CalfLength;

                LegAngles ik = InverseKinematics.CalculateLeg(thigh, calf, x, y);//InverseKinematics.CalculateLeg(Configuration.ThighLength, Configuration.CalfLength, 1, 1);

                if (Animation.IsTurn())
                    angles.HipDegrees = -sin * turnRotation * 15;
                else
                    angles.HipDegrees =
                        sin
                        * leftInverse
                        * (10 + 5 * Configuration.StepLength)
                        - (CrouchWaitTime * Configuration.HipOffsets * .5 * (invertedCrouch ? -1 : 1));
                angles.HipDegrees *= (1 - Math.Abs(Math.Sign(movementVec.X)));
                angles.KneeDegrees = (ik.HipDegrees);
                angles.FeetDegrees = -(ik.KneeDegrees - 180);
                angles.QuadDegrees = 180 - angles.KneeDegrees - angles.FeetDegrees;

                return angles;
            }

            public override void Update(Vector3 forwardsDeltaVec, Vector3 movementVector, double delta)
            {
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
                        //AnimationStep += delta;
                        OffsetLegs = true;
                        leftAngles = CalculateAngles(AnimationStep + IdOffset, forwardsDeltaVec, movementVector, true);
                        rightAngles = CalculateAngles(AnimationStepOffset + IdOffset, forwardsDeltaVec, movementVector, false);
                        break;
                    case Animation.CrouchWalk:
                    case Animation.Walk:
                        leftAngles = CalculateAngles(AnimationStep + IdOffset, forwardsDeltaVec, movementVector, true);
                        rightAngles = CalculateAngles(AnimationStepOffset + IdOffset, forwardsDeltaVec, movementVector,false);
                        break;
                }

                Log("Spideroid (right):", rightAngles.HipDegrees, rightAngles.KneeDegrees, rightAngles.FeetDegrees);
                Log("Spideroid (left):", leftAngles.HipDegrees, leftAngles.KneeDegrees, leftAngles.FeetDegrees);
                SetAngles(leftAngles * new LegAngles(1, 1, 1, 1), rightAngles * new LegAngles(1, 1, 1, 1));
                HandlePistons();
            }

            /*protected override void SetAnglesOf(List<LegJoint> leftStators, List<LegJoint> rightStators, double leftAngle, double rightAngle, double offset)
            {
                foreach (var motor in leftStators)
                    motor.SetAngle(leftAngle * motor.Configuration.InversedMultiplier - (offset + motor.Configuration.Offset) * -1);
                foreach (var motor in rightStators)
                    motor.SetAngle(rightAngle * motor.Configuration.InversedMultiplier - (offset + motor.Configuration.Offset));
            }*/
        }
    }
}
