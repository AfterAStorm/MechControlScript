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

            List<IMyCubeBlock> cubes = new List<IMyCubeBlock>();

            protected override void Render(Utensil utensil)
            {
                utensil.Surface.ContentType = ContentType.SCRIPT;
                utensil.SetScript();

                IMyShipController controller = cockpits.Find((pit) => pit.IsUnderControl);
                IMyShipController anyController = controller ?? (cockpits.Count > 0 ? cockpits[0] : null);

                if (anyController == null)
                    return; // no controllers, can't draw anything anyway so

                float scale = 6f;

                Vector3I basePos = anyController.Position;
                Location center = utensil.Center.ToLocation(AnchorPoints.Center);
                Vector3I size = new Vector3I(
                    anyController.CubeGrid.Max.X - anyController.CubeGrid.Min.X,
                    anyController.CubeGrid.Max.Y - anyController.CubeGrid.Min.Y,
                    anyController.CubeGrid.Max.Z - anyController.CubeGrid.Min.Z
                );

                double unixTime = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds / 1000;
                Log($"unix: {unixTime}");
                double middleScale = Math.Abs(Math.Sin(unixTime % 10 / 10 * Math.PI * 2) * 1) + 1;

                utensil.DrawRectangle(center + new Location((new Vector2(basePos.Z, basePos.X)) * scale), new Vector2(size.Z, size.X) * scale, Color.Multiply(Color.AliceBlue, .5f));

                utensil.DrawSprite(center, (new Vector2(5, 5) * scale) * (float)middleScale, "Circle");

                cubes.Clear();
                foreach (var group in Legs.Values)
                {
                    group.LeftHipStators.ForEach(x => cubes.Add(x.Stator));
                    group.RightHipStators.ForEach(x => cubes.Add(x.Stator));
                }

                int i = 0;
                foreach (IMyCubeBlock leg in cubes)
                {
                    Vector3I pos = (leg.Position);
                    Vector3I local = pos - basePos;

                    Location offset = new Location(local.Z * 1, -local.X * 1) * scale;
                    Location at = center + offset;

                    double rotation = Math.Atan2(-local.Z, local.X);

                    utensil.DrawScaledText(
                        utensil.Center.ToLocation(AnchorPoints.Center) + new Location(local.Z * 1, -local.X * 1) * scale,
                        $"{i}", 1, Color.Black);
                    utensil.DrawSprite(at, new Vector2(3, 3) * scale, "Circle", Color.SpringGreen);
                    float sin = (float)Math.Sin(rotation);
                    float cos = (float)Math.Cos(rotation);
                    utensil.DrawSprite(at + new Location(sin * -5f, cos * -5f) * scale, new Vector2(3, 3) * scale, "Circle", Color.Red);
                    utensil.DrawSprite(at + new Location(sin * -10f, cos * -10f) * scale, new Vector2(3, 3) * scale, "Circle", Color.Red);
                    i += 1;
                }

                utensil.DrawScaledText(
                    center,
                    $"{unixTime}", 1, Color.Black);


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
