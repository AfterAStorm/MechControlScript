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
        public class ChickenWalkerLegGroup : LegGroup
        {
            protected override double[] CalculateAngles(double step)
            {
                bool crouch = Animation == Animation.Crouch || Animation == Animation.CrouchWalk;
                double hipDeg =
                    (!crouch ? -60 : -70) + Math.Sin(step / 4 * Math.PI * 2) * 15d * (crouch ? .5d : 1d);
                double kneeDeg =
                    hipDeg - Math.Cos(step / 4 * Math.PI * 2) * 20d * (crouch ? .5d : 1d) - (!crouch ? 30d : 40d);
                double footDeg =
                    (kneeDeg - hipDeg);
                return new double[] { hipDeg, kneeDeg, footDeg };
            }

            public override void Update(double delta)
            {
                base.Update(delta);
                Log($"Step: {AnimationStep}");

                double[] leftAngles, rightAngles;
                switch (Animation)
                {
                    default:
                    case Animation.Crouch:
                    case Animation.Idle:
                        AnimationWaitTime = 0;
                        AnimationStep = 0;
                        leftAngles = CalculateAngles(0);
                        rightAngles = CalculateAngles(0);
                        foreach (IMyLandingGear lg in LeftGears.Concat(RightGears))
                        {
                            lg.Enabled = true;
                            lg.AutoLock = true;
                        }
                        break;
                    case Animation.CrouchWalk:
                    case Animation.Walk:
                        if (AnimationWaitTime == 0)
                            AnimationStep = 0d;
                        AnimationWaitTime += delta * Configuration.AnimationSpeed;
                        if (Math.Abs(AnimationWaitTime) <= 3f)
                        {
                            foreach (IMyLandingGear lg in LeftGears.Concat(RightGears))
                            {
                                lg.Enabled = false;
                                lg.AutoLock = true;
                            }
                            leftAngles = CalculateAngles(-AnimationWaitTime);
                            rightAngles = CalculateAngles(-AnimationWaitTime);
                            break;
                        }
                        // else
                        leftAngles = CalculateAngles(AnimationStep);
                        rightAngles = CalculateAngles(AnimationStepOffset);

                        /*
                         *   3
                         * 2   4
                         *   1
                         * */

                        bool leftGears = AnimationStep < 3f && AnimationStep > 1f;
                        foreach (IMyLandingGear lg in LeftGears)
                            lg.Enabled = leftGears;

                        foreach (IMyLandingGear lg in RightGears)
                            lg.Enabled = !leftGears;
                        break;
                }

                Log($"{leftAngles[0]};{leftAngles[1]};{leftAngles[2]}");
                Log($"{rightAngles[0]};{rightAngles[1]};{rightAngles[2]}");
                SetAngles(leftAngles[0], leftAngles[1], leftAngles[2], rightAngles[0], rightAngles[1], -rightAngles[2]);
                /*double step = AnimationStep; //+ 2 % 4;

                double hipMultiplifer = InvertHips ? -1d : 1d;
                double kneesMultiplier = InvertKnees ? -1d : 1d;
                double feetMultiplier = InvertFeet ? -1d : 1d;

                double hipAngleRad = HipStator.Angle;
                double hipAngleDeg = hipAngleRad.ToDegrees();
                double kneeAngleRad = KneeStator.Angle;
                double kneeAngleDeg = kneeAngleRad.ToDegrees();
                double footAngleRad = FootStator.Angle;
                double footAngleDeg = footAngleRad.ToDegrees();

                double sin = Math.Sin(MathHelper.ToRadians(step / 8 * 360));
                double sin2 = Math.Sin(MathHelper.ToRadians(step / 4 * 360));
                double cos = Math.Cos(MathHelper.ToRadians(step / 4 * 360));

                double hipTargetRad = (
                    -60 + Math.Sin(step / 4 * Math.PI * 2) * 15
                ).ToRadians();
                double kneeTargetRad = hipTargetRad - (
                    Math.Cos(step / 4 * Math.PI * 2) * 20
                ).ToRadians() - (30d).ToRadians();

                double hipTargetDeg = (hipTargetRad.ToDegrees() * hipMultiplifer);
                double kneeTargetDeg = (kneeTargetRad.ToDegrees() * kneesMultiplier * hipMultiplifer);
                double footTargetDeg = ((kneeAngleDeg - hipAngleDeg) * (double)feetMultiplier);


                hipTargetDeg = hipTargetDeg.AbsoluteDegrees(); // for rotors
                kneeTargetDeg = kneeTargetDeg.AbsoluteDegrees(); // for rotors

                double hipTargetRPM = MathHelper.Clamp(hipTargetDeg - hipAngleDeg - HipOffset - 0, -MaxRPM, MaxRPM);
                double kneeTargetRPM = MathHelper.Clamp(kneeTargetDeg - kneeAngleDeg - KneeOffset, -MaxRPM, MaxRPM);
                double footTargetRPM = MathHelper.Clamp(footTargetDeg - footAngleDeg - FootOffset, -MaxRPM, MaxRPM);

                debug?.WriteText($"\n{Math.Sin(MathHelper.ToRadians(AnimationStep / 8 * 360))}", true);
                debug?.WriteText($"\n{AnimationStep};{step}", true);
                debug?.WriteText($"\n{step};{hipTargetRPM};{kneeTargetRPM}", true);

                debug?.WriteText($"\n{HipStator.CustomName}: {hipTargetDeg}/{hipAngleDeg}; rotating {hipTargetRPM}", true);
                debug?.WriteText($"\n{KneeStator.CustomName}: {kneeTargetDeg}/{kneeAngleDeg}; rotating {kneeTargetRPM}", true);

                HipStator.TargetVelocityRPM = (float)hipTargetRPM;
                KneeStator.TargetVelocityRPM = (float)kneeTargetRPM;
                FootStator.TargetVelocityRPM = (float)footTargetRPM;*/
            }
        }
    }
}
