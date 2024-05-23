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
        public class TestLegGroup : LegGroup
        {
            List<IMyCameraBlock> leftfeetcam = new List<IMyCameraBlock>();
            List<IMyCameraBlock> rightfeetcam = new List<IMyCameraBlock>();
            MyDetectedEntityInfo? leftcam = null;
            MyDetectedEntityInfo? rightcam = null;

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

                IMyCameraBlock camera = left ? leftfeetcam.First() : rightfeetcam.First();
                camera.EnableRaycast = true;

                MyDetectedEntityInfo? raycast = null;
                var lastCast = left ? leftcam : rightcam;
                Log($"distance: {camera.AvailableScanRange}");
                if (camera.AvailableScanRange < 100)
                {
                    raycast = lastCast;
                }
                else
                {
                    raycast = camera.Raycast(camera.AvailableScanRange);
                    if (raycast.Value.IsEmpty() || raycast.Value.EntityId == Program.Singleton.Me.CubeGrid.EntityId)
                    {
                        raycast = lastCast;
                    }
                    else if (!lastCast.HasValue || (lastCast.HasValue && (raycast.Value.HitPosition.Value - lastCast.Value.HitPosition.Value).Length() > 0.02))
                    {
                        if (left)
                            leftcam = raycast;
                        else
                            rightcam = raycast;
                    }
                    else
                        raycast = lastCast;
                }
                if (raycast == null || raycast.Value.IsEmpty())
                    return default(LegAngles);
                Log($"raycast: {raycast?.HitPosition.ToString()} {raycast?.Name} {raycast?.Type} {raycast?.EntityId}");


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

                double thigh = 1.99978506970641;//LeftKneeStators.Count > 0 && LeftFootStators.Count > 0 ? (LeftKneeStators[0].Stator.WorldMatrix.Translation - LeftFootStators[0].Stator.WorldMatrix.Translation).Length() : 2.5;
                double calf = 2.50019512698404;//LeftFootStators.Count > 0 && LeftQuadStators.Count > 0 ? (LeftFootStators[0].Stator.WorldMatrix.Translation - LeftQuadStators[0].Stator.WorldMatrix.Translation).Length() : 2.5;
                double quad = 2.26048425270379;//(LeftQuadStators[0].Stator.WorldMatrix.Translation - leftfeetcam.First().WorldMatrix.Translation).Length();
                //double quad = LeftKneeStators.Count > 0 && LeftFootStators.Count > 0 ? (LeftKneeStators[0].Stator.WorldMatrix.Translation - LeftFootStators[0].Stator.WorldMatrix.Translation).Length() : 2.5;
                Log($"thigh, calf, quad: {thigh}, {calf}, {quad}");

                //Log($"thigh, calf calc: {thigh}, {calf}");
                //Log($"turn rotation: {turnRotation}");

                double offset = -3;

                var hip = (left ? LeftHipStators[0] : RightHipStators[0]);
                Vector3 point = raycast.Value.HitPosition.Value;

                var up = cockpits.First().WorldMatrix.Up;
                var down = -up;

                var target = up * (camera.WorldMatrix.Translation - point) - (up * (offset - quad));
                    //point * (Vector3)(camera.WorldMatrix.Backward) - camera.WorldMatrix.Translation * (Vector3)(camera.WorldMatrix.Backward) - camera.WorldMatrix.Backward * offset - camera.WorldMatrix.Backward * -quad;

                //Vector3 local = raycast.Value.HitPosition.Value - camera.WorldMatrix.Translation;
                //local = raycast.Value.HitPosition.Value - (left ? LeftHipStators[0] : RightHipStators[0]).Stator.WorldMatrix.Translation + local;
                Log($"local: {target}");
                Log($"back cam: {camera.WorldMatrix.Backward}");
                Log($"back hip: {hip.Stator.WorldMatrix.Backward}");

                //local -= new Vector3(0, offset, 0);

                double x = 4;//local.X;
                double y = (up * target).Length() * 1.5;

                //y = Math.Abs(point.Y - camera.WorldMatrix.Translation.Y + offset) + Math.Abs((camera.WorldMatrix.Translation - hip.Stator.WorldMatrix.Translation).Y);
                Log($"y: {y}");
                //y *= -1;

                /*if (Animation.IsTurn()) // turn
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
                }*/

                y -= 1.5 * CrouchWaitTime;
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
                angles.QuadDegrees = 90 - MathHelper.Clamp(angles.KneeDegrees, -90, 90) - MathHelper.Clamp(angles.FeetDegrees, -90, 90);

                return angles;
            }

            public override void Update(Vector3 forwardsDeltaVec, Vector3 movementVector, double delta)
            {
                double forwardsDelta = forwardsDeltaVec.Z;
                base.Update(forwardsDeltaVec, movementVector, delta);
                Log($"Step: {AnimationStep} {Animation} {delta}");

                leftfeetcam = BlockFinder.GetBlocksOfType<IMyCameraBlock>().Select(x => BlockFetcher.ParseBlock(x)).Where(x => x.HasValue && x.Value.Side == BlockSide.Left && x.Value.Group == Configuration.Id).Select(x => x.Value.Block as IMyCameraBlock).ToList();
                rightfeetcam = BlockFinder.GetBlocksOfType<IMyCameraBlock>().Select(x => BlockFetcher.ParseBlock(x)).Where(x => x.HasValue && x.Value.Side == BlockSide.Right && x.Value.Group == Configuration.Id).Select(x => x.Value.Block as IMyCameraBlock).ToList();

                LegAngles leftAngles, rightAngles;
                switch (Animation)
                {
                    default:
                    case Animation.Crouch:
                    case Animation.Idle:
                        AnimationStep = 0;
                        leftAngles = CalculateAngles(AnimationStep, forwardsDeltaVec, movementVector, true);
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

                Log("Test Spideroid (right):", rightAngles.HipDegrees, rightAngles.KneeDegrees, rightAngles.FeetDegrees, rightAngles.QuadDegrees);
                Log("Test Spideroid (left):", leftAngles.HipDegrees, leftAngles.KneeDegrees, leftAngles.FeetDegrees, leftAngles.QuadDegrees);
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
