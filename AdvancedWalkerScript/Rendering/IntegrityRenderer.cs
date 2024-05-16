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
        public class IntegrityRenderer : InvalidatableSurfaceRenderer
        {
            public IntegrityRenderer(IMyTextSurface surface) : base(surface)
            {
            }

            List<Vector3I> cubes = new List<Vector3I>();
            List<Integrity> integrities = new List<Integrity>();

            public struct Integrity
            {
                public IntegrityStatus Status;
                public double IntegrityLevel;
                public bool BeingHacked;
            }

            public enum IntegrityStatus
            {
                Nominal     = 0,
                Damaged     = 1,
                Critical    = 2,
                Broken      = 3
            }

            protected Integrity GetIntegrity(List<IMyMotorStator> joints)
            {
                Integrity output = new Integrity();
                double integrity = 0;
                foreach (IMyMotorStator joint in joints)
                {
                    if (joint.IsBeingHacked)
                        output.BeingHacked = true;
                    IMySlimBlock block = joint.CubeGrid.GetCubeBlock(joint.Position);
                    integrity += joint.IsWorking ? (block?.BuildLevelRatio ?? 0) : 0;
                }
                integrity /= joints.Count;
                output.Status =
                    integrity >= 1 ? IntegrityStatus.Nominal :
                    (integrity >= .85d ? IntegrityStatus.Damaged :
                    (integrity > 0d ? IntegrityStatus.Critical :
                                                                 IntegrityStatus.Broken));
                output.IntegrityLevel = integrity;
                return output;
            }

            double UnixTime => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds / 1000;

            protected Color GetColorForIntegrity(Integrity integrity)
            {
                if (integrity.BeingHacked && UnixTime % 1 > .5d)
                    return Color.Blue;
                switch (integrity.Status)
                {
                    case IntegrityStatus.Nominal:
                        return Color.SpringGreen;
                    case IntegrityStatus.Damaged:
                        return Color.Lerp(Color.SpringGreen, Color.DarkRed, (float)(((integrity.IntegrityLevel - 0) * (1 - .85)) / (1 - 0)) + .85f);
                    case IntegrityStatus.Critical:
                        return (UnixTime % 1 > .5d) ? Color.DarkRed : Color.Lerp(Color.DarkRed, Color.Gray, .85f);
                    default:
                    case IntegrityStatus.Broken:
                        return Color.Gray;
                }
            }

            protected override void Render(Utensil utensil)
            {
                utensil.Surface.ContentType = ContentType.SCRIPT;
                utensil.SetScript();

                IMyShipController controller = cockpits.Find((pit) => pit.IsUnderControl);
                IMyShipController anyController = controller ?? (cockpits.Count > 0 ? cockpits[0] : null);

                if (anyController == null)
                    return; // no controllers, can't draw anything anyway so

                float scale = 4f;

                Vector3I basePos = anyController.Position;
                Location center = utensil.Center.ToLocation(AnchorPoints.Center);
                Vector3I size = new Vector3I(
                    anyController.CubeGrid.Max.X - anyController.CubeGrid.Min.X,
                    anyController.CubeGrid.Max.Y - anyController.CubeGrid.Min.Y,
                    anyController.CubeGrid.Max.Z - anyController.CubeGrid.Min.Z
                );

                cubes.Clear();
                integrities.Clear();
                foreach (var group in Legs.Values)
                {
                    cubes.Add(new Vector3I((int)group.LeftHipStators.Average(x => x.Stator.Position.X), (int)group.LeftHipStators.Average(x => x.Stator.Position.Y), (int)group.LeftHipStators.Average(x => x.Stator.Position.Z)) - basePos);
                    integrities.Add(GetIntegrity(group.LeftHipStators.Select(x => x.Stator).ToList()));
                    integrities.Add(GetIntegrity(group.LeftKneeStators.Select(x => x.Stator).ToList()));
                    integrities.Add(GetIntegrity(group.LeftFootStators.Select(x => x.Stator).ToList()));
                    integrities.Add(GetIntegrity(group.LeftQuadStators.Select(x => x.Stator).ToList()));
                    cubes.Add(new Vector3I((int)group.RightHipStators.Average(x => x.Stator.Position.X), (int)group.RightHipStators.Average(x => x.Stator.Position.Y), (int)group.RightHipStators.Average(x => x.Stator.Position.Z)) - basePos);
                    integrities.Add(GetIntegrity(group.RightHipStators.Select(x => x.Stator).ToList()));
                    integrities.Add(GetIntegrity(group.RightKneeStators.Select(x => x.Stator).ToList()));
                    integrities.Add(GetIntegrity(group.RightFootStators.Select(x => x.Stator).ToList()));
                    integrities.Add(GetIntegrity(group.RightQuadStators.Select(x => x.Stator).ToList()));
                    //group.RightHipStators.ForEach(x => cubes.Add(x.Stator));
                }

                Vector3I min = cubes.Min();
                Vector3I max = cubes.Max();
                double minX = -min.X;
                double maxX = -max.X;

                double difference = maxX - minX;
                float offsetY = (float)difference / 2;

                double middleScale = Math.Abs(Math.Sin(UnixTime % 10 / 10 * Math.PI * 2) * 1) + 1;

                utensil.DrawRectangle(center + new Location(basePos.Z, basePos.X + offsetY) * scale, new Vector2(size.Z, size.X) * scale, Color.Multiply(Color.AliceBlue, .5f));

                utensil.DrawSprite(center + new Location(0, offsetY) * scale, (new Vector2(5, 5) * scale) * (float)middleScale, "Circle");

                /*
                utensil.DrawSprite(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "Uno",
                    Color = Color.Red,
                    Position = new Vector2(0, utensil.Surface.MeasureStringInPixels(new StringBuilder("Uno"), "White", 3f).Y),
                    FontId = "White",
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 3f
                });
                utensil.DrawSprite(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "Dos",
                    Color = Color.Red,
                    Position = new Vector2(utensil.Width / 2, utensil.Surface.MeasureStringInPixels(new StringBuilder("Dos"), "White", 3f).Y),
                    FontId = "White",
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 3f
                });
                utensil.DrawSprite(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "Tres",
                    Color = Color.Red,
                    Position = new Vector2(utensil.Width / 2, utensil.Surface.MeasureStringInPixels(new StringBuilder("Tres"), "White", 3f).Y / 2 + utensil.Height / 2),
                    FontId = "White",
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 3f
                });
                utensil.DrawSprite(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "Cuartos",
                    Color = Color.Red,
                    Position = new Vector2(utensil.Width / 2, utensil.Height + utensil.Surface.MeasureStringInPixels(new StringBuilder("Cuartos"), "White", 3f).Y / 3),
                    FontId = "White",
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 3f
                });

                utensil.DrawScaledText(
                    new Location(0, 0, AnchorPoints.TopLeft),
                    $"top left", .5f * (float)middleScale, Color.Black, "White", TextAlignment.LEFT);
                utensil.DrawScaledText(
                    new Location(utensil.Width / 2, 0, AnchorPoints.TopMiddle),
                    $"top middle", .5f * (float)middleScale, Color.Black, "White");
                utensil.DrawScaledText(
                    new Location(utensil.Width, 0, AnchorPoints.TopRight),
                    $"top right", .5f * (float)middleScale, Color.Black, "White", TextAlignment.RIGHT);

                utensil.DrawScaledText(
                    new Location(0, utensil.Height / 2, AnchorPoints.MiddleLeft),
                    $"middle left", .5f * (float)middleScale, Color.Black, "White", TextAlignment.LEFT);
                utensil.DrawScaledText(
                    new Location(utensil.Width / 2, utensil.Height / 2, AnchorPoints.Center),
                    $"middle middle", .5f * (float)middleScale, Color.Black, "White");
                utensil.DrawScaledText(
                    new Location(utensil.Width, utensil.Height / 2, AnchorPoints.MiddleRight),
                    $"middle right", .5f * (float)middleScale, Color.Black, "White", TextAlignment.RIGHT);

                utensil.DrawScaledText(
                    new Location(0, utensil.Height, AnchorPoints.BottomLeft),
                    $"bottom left", .5f * (float)middleScale, Color.Black, "White", TextAlignment.LEFT);
                utensil.DrawScaledText(
                    new Location(utensil.Width / 2, utensil.Height, AnchorPoints.BottomMiddle),
                    $"bottom middle", .5f * (float)middleScale, Color.Black, "White");
                utensil.DrawScaledText(
                    new Location(utensil.Width, utensil.Height, AnchorPoints.BottomRight),
                    $"bottom right", .5f * (float)middleScale, Color.Black, "White", TextAlignment.RIGHT);
                */
                int i = 0;
                foreach (Vector3I local in cubes)
                {
                    //Vector3I local = pos - basePos;

                    Location offset = new Location(local.Z * 1, -local.X * 1 + offsetY) * scale;
                    Location at = center + offset;

                    double rotation = Math.Atan2(-local.Z, local.X);

                    Integrity hipInteg = integrities[0];
                    Integrity kneeInteg = integrities[1];
                    Integrity footInteg = integrities[2];
                    Integrity quadInteg = integrities[3];
                    integrities.RemoveRange(0, 4);

                    utensil.DrawScaledText(
                        at + new Location(0, utensil.Height / 2),
                        $"{i}", .5f, Color.Black);
                    utensil.DrawSprite(at, new Vector2(3, 3) * scale, "Circle", GetColorForIntegrity(hipInteg));//Color.SpringGreen);
                    float sin = (float)Math.Sin(rotation);
                    float cos = (float)Math.Cos(rotation);
                    utensil.DrawSprite(at + new Location(sin * -5f, cos * -5f) * scale, new Vector2(3, 3) * scale, "Circle", GetColorForIntegrity(kneeInteg));//Color.Red);
                    utensil.DrawSprite(at + new Location(sin * -10f, cos * -10f) * scale, new Vector2(3, 3) * scale, "Circle", GetColorForIntegrity(footInteg));//Color.Red);
                    i += 1;
                }

                utensil.DrawScaledText(
                    center,
                    $"{UnixTime}", 1, Color.Black);


                /*float scale = utensil.Height / 1024f;

                // Hip
                utensil.DrawRectangle(utensil.Center.ToLocation(AnchorPoints.Center) + new Location(0, 180 - 25) * scale, new Vector2(50, 150) * scale, utensil.ForegroundColor);

                // Torso
                //utensil.DrawRectangle(utensil.Center.ToLocation(AnchorPoints.Center) - new Location(0, 0) * scale, new Vector2(250, 200) * scale, utensil.ForegroundColor);
                utensil.DrawRectangle(utensil.Center.ToLocation(AnchorPoints.Center) - new Location(110, 0) * scale, new Vector2(100, 200) * scale, utensil.ForegroundColor);
                utensil.DrawRectangle(utensil.Center.ToLocation(AnchorPoints.Center) + new Location(110, 0) * scale, new Vector2(100, 200) * scale, Color.Red);
                utensil.DrawRectangle(utensil.Center.ToLocation(AnchorPoints.Center) - new Location(0, 40) * scale, new Vector2(110, 150) * scale, Color.Blue);*/
            }
        }
    }
}
