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
            protected LegAngles CalculateAngles(double step, bool left)
            {
                step = step.Modulo(4);

                LegAngles angles = new LegAngles();

                double stepPercent = step / 4d;
                double sin = Math.Sin(stepPercent * 2 * Math.PI);
                double cos = Math.Cos(stepPercent * 2 * Math.PI);

                angles.HipDegrees = sin * (10 + 5 * Configuration.StepLength) * (left ? -1 : 1);
                angles.KneeDegrees = 50 + (cos) * 15;
                angles.FeetDegrees = 80 - (cos) * 10;

                return angles;
            }

            public override void Update(double forwardsDelta, double delta)
            {
                base.Update(forwardsDelta, delta);
                Log($"Step: {AnimationStep} {Animation} {delta}");

                LegAngles leftAngles, rightAngles;
                switch (Animation)
                {
                    default:
                    case Animation.Crouch:
                    case Animation.Idle:
                        AnimationStep = 0;
                        leftAngles = CalculateAngles(AnimationStep, false);
                        rightAngles = CalculateAngles(AnimationStepOffset, false);
                        break;
                    case Animation.CrouchTurn:
                    case Animation.Turn:
                        leftAngles = CalculateAngles(AnimationStep, true);
                        rightAngles = CalculateAngles(AnimationStepOffset, false);
                        break;
                    case Animation.CrouchWalk:
                    case Animation.Walk:
                        leftAngles = CalculateAngles(AnimationStep + IdOffset, true);
                        rightAngles = CalculateAngles(AnimationStepOffset + IdOffset, false);
                        break;
                }

                Log("Spideroid (right):", rightAngles.HipDegrees, rightAngles.KneeDegrees, rightAngles.FeetDegrees);
                Log("Spideroid (left):", leftAngles.HipDegrees, leftAngles.KneeDegrees, leftAngles.FeetDegrees);
                SetAngles(leftAngles * new LegAngles(1, 1, 1), rightAngles * new LegAngles(1, 1, 1));
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
