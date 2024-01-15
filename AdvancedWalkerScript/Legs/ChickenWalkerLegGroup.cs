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
            protected override LegAngles CalculateAngles(double step)
            {
                bool crouch = Animation == Animation.Crouch || Animation == Animation.CrouchWalk;

                // shamelessly borrowed from https://opentextbooks.clemson.edu/wangrobotics/chapter/inverse-kinematics/
                // the equations anyway
                // x and y are flipped, yay!
                double targetY = Math.Sin((step / 4 * 360).ToRadians()) * 1.5f * Configuration.StepLengthMultiplier - 1; // x  //Math.Sin((AnimationStep / 4 * 360).ToRadians()) * 3;
                double targetX = MathHelper.Clamp(3 - Math.Cos((step / 4 * 360).ToRadians()) * 2f, float.MinValue, 2.5f) + 1 - (crouch ? 2 : 0); // y  //Math.Cos((AnimationStep / 4 * 360).ToRadians()) * 3;
                if (Animation == Animation.Turn)
                {
                    targetY = -1;
                    targetX = MathHelper.Clamp(Math.Sin((step / 4 * 360).ToRadians()), double.MinValue, 0) * 1.5f + 3;
                }

                Log(targetY, targetX);
                double distance = Math.Sqrt(
                        Math.Pow(targetX - 0, 2) +
                        Math.Pow(targetY - 0, 2)
                );

                double thighLength = 2.5f;
                double calfLength = 2.5f;

                double diameter = Math.Atan(targetY / targetX);
                double leftDiameter = Math.Acos(
                    (Math.Pow(thighLength, 2) + Math.Pow(distance, 2) - Math.Pow(calfLength, 2))
                    /
                    (2 * thighLength * distance)
                );
                double hipAngle = diameter - leftDiameter;

                double diameter2 = Math.Acos(
                    (Math.Pow(thighLength, 2) + Math.Pow(calfLength, 2) - Math.Pow(distance, 2))
                    /
                    (2 * thighLength * calfLength)
                );
                double kneeAngle = Math.PI - diameter2;

                double footDeg =
                    (180 - hipAngle.ToDegrees() - kneeAngle.ToDegrees());

                return new LegAngles() { 
                    HipDegrees = hipAngle.ToDegrees(),
                    KneeDegrees = kneeAngle.ToDegrees(),
                    FeetDegrees = footDeg
                };
                /*double hipDeg =
                    (!crouch ? -60 : -70) + Math.Sin(step / 4 * Math.PI * 2) * 15d * (crouch ? .5d : 1d);
                double kneeDeg =
                    hipDeg - Math.Cos(step / 4 * Math.PI * 2) * 20d * (crouch ? .5d : 1d) - (!crouch ? 30d : 40d);
                double footDeg =
                    (kneeDeg - hipDeg);
                return new double[] { hipDeg, kneeDeg, footDeg };*/
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
                        AnimationWaitTime = 0;
                        AnimationStep = 2;
                        leftAngles = CalculateAngles(2);
                        rightAngles = CalculateAngles(2);
                        foreach (IMyLandingGear lg in LeftGears.Concat(RightGears))
                        {
                            lg.Enabled = true;
                            lg.AutoLock = false;
                            lg.Unlock();
                        }
                        break;
                    case Animation.CrouchTurn:
                    case Animation.Turn:
                        leftAngles = CalculateAngles(AnimationStep);
                        OffsetLegs = true;
                        rightAngles = CalculateAngles(AnimationStepOffset);
                        break;
                    case Animation.CrouchWalk:
                    case Animation.Walk:
                        if (AnimationWaitTime == 0)
                            AnimationStep = 2.5d;
                        AnimationWaitTime += forwardsDelta * Configuration.AnimationSpeed;
                        double absWaitTime = Math.Abs(AnimationWaitTime);
                        if (absWaitTime < 1f)
                        {
                            foreach (IMyLandingGear lg in LeftGears.Concat(RightGears))
                            {
                                lg.Enabled = true;
                                lg.AutoLock = true;
                            }
                            leftAngles = CalculateAngles(AnimationWaitTime + 2f);
                            rightAngles = CalculateAngles(AnimationWaitTime + 2f);
                            break;
                        }
                        else if (absWaitTime <= 2.3f)
                        {
                            leftAngles = CalculateAngles(AnimationStep);
                            rightAngles = CalculateAngles(3f * (AnimationWaitTime / Math.Abs(AnimationWaitTime)));
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
                        {
                            if (!leftGears)
                                lg.Unlock();
                            lg.Enabled = true;
                            lg.AutoLock = true;
                        }

                        foreach (IMyLandingGear lg in RightGears)
                        {
                            if (leftGears)
                                lg.Unlock();
                            lg.Enabled = true;
                            lg.AutoLock = true;
                        }
                        break;
                }

                Log(rightAngles.HipDegrees, rightAngles.KneeDegrees, rightAngles.FeetDegrees);
                SetAngles(
                    leftAngles.HipDegrees,
                    leftAngles.KneeDegrees,
                    leftAngles.FeetDegrees,
                    rightAngles.HipDegrees,
                    rightAngles.KneeDegrees,
                    -rightAngles.FeetDegrees
                );
            }
        }
    }
}
