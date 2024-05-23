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
            protected LegAngles CalculateAngles(double step, bool left, bool invertedCrouch = false)
            {
                step = step.Modulo(4);

                LegAngles angles = new LegAngles();

                double stepPercent = step / 4d;
                double sin = Math.Sin(stepPercent * 2 * Math.PI);
                double cos = Math.Cos(stepPercent * 2 * Math.PI);

                double crouchKneeOffset = -45 * CrouchWaitTime;
                double crouchFootOffset = -35 * CrouchWaitTime;

                /*angles.HipDegrees = sin * (10 + 5 * Configuration.StepLength) * (left ? -1 : 1);
                angles.KneeDegrees = 50 + (cos) * 15;
                angles.FeetDegrees = 80 - (cos) * 10;*/

                Log("Is Turn:" + Animation.IsTurn().ToString());
                angles.HipDegrees  = sin * (10 + 5 * Configuration.StepLength) * (left ? -1 : 1) - (CrouchWaitTime * Configuration.HipOffsets * .5 * (invertedCrouch ? -1 : 1));
                if (Animation.IsTurn())
                    angles.HipDegrees = 0;
                angles.KneeDegrees = 40 + crouchKneeOffset + (cos) * 15;
                angles.FeetDegrees = 65 - crouchFootOffset - (cos) * 10;
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
                        leftAngles = CalculateAngles(AnimationStep, false);
                        rightAngles = CalculateAngles(AnimationStep, false, true);
                        break;
                    case Animation.CrouchTurn:
                    case Animation.Turn:
                        AnimationStep += delta;
                        OffsetLegs = true;
                        leftAngles = CalculateAngles(AnimationStep + IdOffset, true);
                        rightAngles = CalculateAngles(AnimationStepOffset + IdOffset, false);
                        break;
                    case Animation.CrouchWalk:
                    case Animation.Walk:
                        leftAngles = CalculateAngles(AnimationStep + IdOffset, true);
                        rightAngles = CalculateAngles(AnimationStepOffset + IdOffset, false);
                        break;
                }

                Log("Spideroid (right):", rightAngles.HipDegrees, rightAngles.KneeDegrees, rightAngles.FeetDegrees);
                Log("Spideroid (left):", leftAngles.HipDegrees, leftAngles.KneeDegrees, leftAngles.FeetDegrees);
                SetAngles(leftAngles * new LegAngles(1, 1, 1, 1), rightAngles * new LegAngles(1, 1, 1, 1));
            }

            protected override void SetAnglesOf(List<LegJoint> leftStators, List<LegJoint> rightStators, double leftAngle, double rightAngle, double offset)
            {
                foreach (var motor in leftStators)
                    motor.SetAngle(leftAngle * motor.Configuration.InversedMultiplier - (offset + motor.Configuration.Offset) * -1);
                foreach (var motor in rightStators)
                    motor.SetAngle(rightAngle * motor.Configuration.InversedMultiplier - (offset + motor.Configuration.Offset));
            }
        }
    }
}
