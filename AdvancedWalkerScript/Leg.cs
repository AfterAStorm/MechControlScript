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
        public class Leg
        {

            #region # - Properties

            public float ThighLength = 2.5f;
            public float CalfLength = 2.5f;

            public float HipOffset = 0f;
            public float KneeOffset = 0f;
            public float FootOffset = 0f;

            public bool InvertHips = true;
            public bool InvertKnees = true;
            public bool InvertFeet = true;

            public IMyMotorStator HipStator;
            public IMyMotorStator KneeStator;
            public IMyMotorStator FootStator;

            public double AnimationStep = 0;
            public double Offset = 0;
            public double OffsetPassed = 0;

            #endregion

            #region # - Constructor

            public Leg(IMyMotorStator hip, IMyMotorStator knee, IMyMotorStator foot)
            {
                HipStator = hip;
                KneeStator = knee;
                FootStator = foot;

                InvertHips = HipStator.CustomName.Contains("-");
                InvertKnees = KneeStator.CustomName.Contains("-");
                InvertFeet = FootStator.CustomName.Contains("-");

                //HipStator.UpperLimitDeg = 135;//float.MaxValue;
                //HipStator.LowerLimitDeg = -135; // float.MinValue;
                foreach (IMyMotorStator stator in new IMyMotorStator[2] { KneeStator, FootStator })
                {
                    //stator.UpperLimitDeg = 90;
                    //stator.LowerLimitDeg = -90;
                }
            }

            #endregion

            #region # - Methods

            double ToDegrees(double radians)
            {
                return (180 / Math.PI) * radians;
            }

            public void Update(double delta)
            {
                if (Offset > 0)
                {
                    if (OffsetPassed < Offset)
                        OffsetPassed += delta;
                    if (OffsetPassed >= Offset)
                    {
                        delta = Offset - OffsetPassed;
                    }
                    else
                    {
                        delta = 0;
                        AnimationStep = 0;
                    }
                }

                AnimationStep += delta;
                AnimationStep %= 4; // 0 to 3
                //int step = (int)Math.Floor(AnimationStep);
                //double standingHeight = .85f;
                //double targetY = 0;

                //double upX = 0;

                /*switch (step)
                {
                    case 0:
                        targetY = -1;
                        upX = 1;
                        break;
                    case 1:
                        targetY = 0;
                        upX = 0;
                        break;
                    case 2:
                        targetY = 1;
                        upX = 0;
                        break;
                    case 3:
                        targetY = 0;
                        upX = 1;
                        break;
                }
                double targetX = standingHeight * (ThighLength + CalfLength) - upX;*/

                double step = AnimationStep + 2 % 4;

                int hipMultiplifer = InvertHips ? -1 : 1;
                int kneesMultiplier = InvertKnees ? -1 : 1;
                int feetMultiplier = InvertFeet ? -1 : 1;

                double distances = 10;
                double measuredHeight = (distances + distances) / 2 * (float)Math.Sqrt(2) / 2;
                double pitchIncline = ToDegrees(distances / distances) - 45;

                double hipAngleRad = HipStator.Angle;
                double hipAngleDeg = ToDegrees(hipAngleRad);
                double kneeAngleRad = KneeStator.Angle;
                double kneeAngleDeg = ToDegrees(kneeAngleRad);
                double footAngleRad = FootStator.Angle;
                double footAngleDeg = ToDegrees(footAngleRad);

                /*double kneeTargetRad = Math.Acos(
                    (Math.Pow(targetX, 2) + Math.Pow(targetY, 2) - Math.Pow(CalfLength, 2) - Math.Pow(ThighLength, 2)) / (2 * ThighLength * CalfLength)
                );
                double hipTargetRad = Math.Atan(targetY / targetX) - Math.Atan(CalfLength * Math.Sin(kneeTargetRad) / (ThighLength + (CalfLength * Math.Cos(kneeTargetRad))));*/

                double sin = Math.Sin(MathHelper.ToRadians(step / 8 * 360));
                double sin2 = Math.Sin(MathHelper.ToRadians(step / 4 * 360));
                double cos = Math.Cos(MathHelper.ToRadians(step / 4 * 360));

                //double hipTargetRad = MathHelper.ToRadians(-70 + sin * 30 - cos * 15 - sin2 * 15);
                //double kneeTargetRad = MathHelper.ToRadians(-90 - sin * 60 + cos * 15);

                double hipTargetRad = MathHelper.ToRadians(
                    -60 - Math.Sin(step / 4 * Math.PI * 2) * 15
                );
                double kneeTargetRad = hipTargetRad + MathHelper.ToRadians(
                    Math.Cos(step / 4 * Math.PI * 2) * 20
                );

                double hipTargetDeg = ToDegrees(hipTargetRad) * hipMultiplifer;
                double kneeTargetDeg = ToDegrees(kneeTargetRad) * kneesMultiplier * hipMultiplifer;
                double footTargetDeg = (kneeAngleDeg - hipAngleDeg) * feetMultiplier;

                /*while (hipTargetDeg < 0)
                    hipTargetDeg = 180 - hipTargetDeg;
                while (kneeTargetDeg < 0)
                    kneeTargetDeg = 180 - kneeTargetDeg;
                while (footTargetDeg < 0)
                    footTargetDeg = 180 - footTargetDeg;*/

                if (hipAngleDeg > 180)
                    hipAngleDeg -= 360;
                if (kneeAngleDeg > 180)
                    kneeAngleDeg -= 360;
                if (footAngleDeg > 180)
                    footAngleDeg -= 360;

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
                FootStator.TargetVelocityRPM = (float)footTargetRPM;
                //HipStator.Enabled = true;
                //KneeStator.Enabled = true;

                /*

                double kneeTarget = 0;//(float)Math.Atan((0 - step) / (0 - step));
                double hipTarget = 0;//(float)Math.Atan(step * 360 * Math.PI / 180);
                double footTarget = 0;

                hipTarget *= 360;
                kneeTarget *= 360;
                footTarget *= 360;

                debug?.WriteText($"\n{step};{hipTarget};{kneeTarget};{footTarget}", true);

                float hipRPM  = (float)MathHelper.Clamp(hipTarget - ToDegrees(HipStator.Angle), -MaxRPM, MaxRPM);
                float kneeRPM = (float)MathHelper.Clamp(kneeTarget - ToDegrees(KneeStator.Angle), -MaxRPM, MaxRPM);
                float footRPM = (float)MathHelper.Clamp(footTarget - ToDegrees(FootStator.Angle), -MaxRPM, MaxRPM);
                debug?.WriteText($"\nRPM;{hipRPM};{kneeRPM};{footRPM}", true);

                HipStator.TargetVelocityRPM = hipRPM;
                KneeStator.TargetVelocityRPM = kneeRPM;
                FootStator.TargetVelocityRPM = footRPM;*/
            }

            #endregion

        }
    }
}
