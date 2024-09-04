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

            public double Delta;

            /*float dvdx = 0;
            float dvdy = 0;
            Vector2 dvddir = Vector2.One;

            List<Vector3I> cubes = new List<Vector3I>();
            List<Integrity> integrities = new List<Integrity>();*/

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

            protected Integrity GetIntegrity(IEnumerable<IMyTerminalBlock> joints)
            {
                Integrity output = new Integrity();
                double integrity = 0;
                int total = 0;
                foreach (IMyTerminalBlock joint in joints)
                {
                    total += 1;
                    if (joint.IsBeingHacked)
                        output.BeingHacked = true;
                    IMySlimBlock block = joint.CubeGrid.GetCubeBlock(joint.Position);
                    if (block == null)
                        continue;
                    integrity += joint.IsWorking ? Math.Min(1 - (block.CurrentDamage / block.MaxIntegrity), block.BuildLevelRatio) : 0;
                }
                if (total == 0)
                {
                    output.Status = IntegrityStatus.Broken;
                    return output;
                }
                integrity /= total;
                output.Status =
                    integrity >= 1 ? IntegrityStatus.Nominal :
                    (integrity >= .9d ? IntegrityStatus.Damaged :
                    (integrity > 0d ? IntegrityStatus.Critical :
                                                                 IntegrityStatus.Broken));
                output.IntegrityLevel = integrity;
                return output;
            }

            static Color Gray = new Color(30, 30, 30);
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
                        return (UnixTime % 1 > .5d) ? Color.DarkRed : Color.Lerp(Color.DarkRed, Gray, .85f);
                    default:
                    case IntegrityStatus.Broken:
                        return Gray;
                }
            }

            static string Decompress(string input)
            {
                int width = int.Parse(input.Split('x')[0]);
                int height = int.Parse(input.Split(':')[0].Split('x')[1]);
                input = input.Split(':')[1];
                string data = "";
                var regex = new System.Text.RegularExpressions.Regex("(.)(\\d+)?");
                var matches = regex.Matches(input);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    char character = match.Groups[1].Value[0];
                    int amount = !string.IsNullOrWhiteSpace(match.Groups[2].Value) ? int.Parse(match.Groups[2].Value) : 1;
                    data += new string(character, amount);
                }
                Singleton.Echo($"decompressed image length: {data.Length} expecting {width*height}");
                /*for (int row = height - 1; row > 0; row--)
                {
                    Singleton.Echo($"inserting at {row}");
                    data = data.Insert(row * width, "\n");
                }*/
                return data.Replace(';', '\n');
            }

            string hand = Decompress("13x11:62;9;13;13;13;12;10;12;12;12;10");
            string arm = Decompress("20x26:32;62;10;12;213;214;132;15;16;16;16;15;16;17;17;17;17;17;17;16;15;623;42;32;32;22");
            string shoulder = Decompress("6x14:22;4;6;6;6;6;6;6;6;6;6;6;4;22");

            string cockpit = Decompress("19x26:272;292;2112;13;15;17;19;19;19;19;19;19;19;19;19;19;19;19;19;19;19;19;19;19;44;222");
            string body = Decompress("12x24:25;10;12;12;11;10;9;9;9;9;9;9;9;9;9;9;8;7;7;7;6;25;24;");

            string waist = Decompress("17x6:11;15;17;17;15;11");
            string lowerbody = Decompress("21x13:222;55;21;19;15;2132;2112;2112;2112;292;292;292;272");
            string thigh = Decompress("12x15:52;72;9;10;11;11;11;11;11;10;10;10;10;10;8");
            string calf = Decompress("14x24:;22;12;12;12;12;12;10;10;10;10;8;10;10;12;12;14;14;14;14;14;14;14;33");
            string foot = Decompress("18x6:282;16;18;18;18;282");
            /*string image = Decompress("70x70:103122210232442263222353206223205331862211244721663261121031364242113272411726316342582737220322225374622322223473422422232842526234822924224592922222355293852172322251637314425317102922822384211262724428915232254584212254311311392723323292327222310267722342321239753422321228852342325119363242362109242432367992242433232888224233232222988373242322321089333211223210782732102572789331037281011482324372882223272066223232856273434232342248626523353221124336232362522132723123422153934102331130122319262031792321232231992228323314285428322182223692183282726353832232323102264228323232377311292233236122119323324432232732232422322226322134436353231632426353424353232323923234433443231426474422213323334322233622322323242432253223233232583334222323133112452412323354852232323322253234422433232323734243322233223213253242323221420223233214202332310238231132215213223");
            */

            protected override void Render(Utensil utensil)
            {
                utensil.Surface.ContentType = ContentType.SCRIPT;
                utensil.SetScript();

                if (legs.Count == 0)
                    return;

                IMyShipController controller = cockpits.Find((pit) => pit.IsUnderControl);
                IMyShipController anyController = controller ?? (cockpits.Count > 0 ? cockpits[0] : null);

                if (anyController == null)
                    return; // no controllers, can't draw anything anyway so

                //float scale = 4f;

                //Vector3I basePos = anyController.Position;
                Location center = utensil.Center.ToLocation(AnchorPoints.Center);
                /*Vector3I size = new Vector3I(
                    anyController.CubeGrid.Max.X - anyController.CubeGrid.Min.X,
                    anyController.CubeGrid.Max.Y - anyController.CubeGrid.Min.Y,
                    anyController.CubeGrid.Max.Z - anyController.CubeGrid.Min.Z
                );

                cubes.Clear();
                integrities.Clear();
                foreach (var group in legs.Values)
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

                utensil.DrawSprite(center + new Location(0, offsetY) * scale, (new Vector2(5, 5) * scale) * (float)middleScale, "Circle");*/

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

                /*int i = 0;
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
                }*/
                /*
                utensil.DrawRectangle(center, new Vector2(20, 80), 0f); // body
                // body triangle things
                utensil.DrawSprite(center + new Location(20, 0), new Vector2(20, 35), "RightTriangle", MathHelper.ToRadians(90f));
                utensil.DrawSprite(center - new Location(20, 0), new Vector2(20, -35), "RightTriangle", MathHelper.ToRadians(90f));

                // left leg indent
                utensil.DrawRectangle(center - new Location(25, -30), new Vector2(20, 20), 0f);
                // left leg
                utensil.DrawRectangle(center - new Location(30, -50), new Vector2(20, 40), 0f);*/

                /*utensil.DrawScaledText(
                    center,
                    $"{UnixTime}", 1, Color.Black);*/
                //throw new Exception();
                /*string[] lines = image.Split(new string[1] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);

                float charHeight = utensil.Surface.MeasureStringInPixels(new StringBuilder(""), "Monospace", .1f).Y;
                for (int ln = 0; ln < lines.Length; ln++)
                {
                    string line = lines[ln];
                    utensil.DrawScaledText(new Location(15, charHeight * ln + charHeight * lines.Length / 2, AnchorPoints.TopLeft), line, .1f, Color.Red, "Monospace", TextAlignment.LEFT);
                }*/
                /*float x = Math.Min(utensil.Size.X, utensil.Size.Y);
                Vector2 size = Vector2.One * x / 3;//utensil.Size / 6;
                float speed = 300;
                dvdx += dvddir.X * (float)Delta * speed;
                dvdy += dvddir.Y * (float)Delta * speed;
                if (dvdx > utensil.Size.X - size.X)
                {
                    dvddir = new Vector2(-1, dvddir.Y);
                }
                if (dvdx < 0)
                {
                    dvddir = new Vector2(1, dvddir.Y);
                }
                if (dvdy < 0)
                {
                    dvddir = new Vector2(dvddir.X, 1);
                }
                if (dvdy > utensil.Size.Y - size.Y - 10)
                {
                    dvddir = new Vector2(dvddir.X, -1);
                }
                utensil.DrawMonoImage(new Location(dvdx + size.X / 2, dvdy + size.Y / 1.5f, AnchorPoints.Center), size, Color.White, image);*/
                //utensil.DrawScaledText(center + new Location(0, 70), $"{x} / {utensil.Size.ToString()}", 1f, Color.Blue);
                //utensil.DrawScaledText(center - new Location(0, utensil.Height), , .1f, Color.White, "Monospace");

                int leg = legs.Keys.First();
                float determiningFactor = Math.Min(utensil.Size.X, utensil.Size.Y);
                float imageScale = determiningFactor / (3072 / 35); // ~3.5 scale for 307.2 height
                //utensil.DrawScaledText(center, $"{utensil.Size.ToString()} {imageScale}", 1f, Color.White);

                center -= new Location(0, 18) * imageScale;

                Color cockpitIntegrity = GetColorForIntegrity(GetIntegrity(cockpits.Select(b => b as IMyTerminalBlock)));
                //Color bodyIntegrity = GetColorForIntegrity(GetIntegrity())
                Color bodyIntegrity = cockpitIntegrity;
                Color waistIntegrity = torsoTwistStators.Count > 0 ? GetColorForIntegrity(GetIntegrity(torsoTwistStators.Select(b => b.Stator))) : bodyIntegrity;
                Color hipIntegrity = torsoTwistStators.Count > 0 ? GetColorForIntegrity(GetIntegrity(legs[leg].LeftHipStators.Concat(legs[leg].RightHipStators).Select(b => b.Stator))) : bodyIntegrity;

                Color leftThighIntegrity = GetColorForIntegrity(GetIntegrity(legs[leg].LeftHipStators.Select(b => b.Stator)));
                Color rightThighIntegrity = GetColorForIntegrity(GetIntegrity(legs[leg].RightHipStators.Select(b => b.Stator)));
                Color leftCalfIntegrity = GetColorForIntegrity(GetIntegrity(legs[leg].LeftKneeStators.Select(b => b.Stator)));
                Color rightCalfIntegrity = GetColorForIntegrity(GetIntegrity(legs[leg].RightKneeStators.Select(b => b.Stator)));
                Color leftFootIntegrity = GetColorForIntegrity(GetIntegrity(legs[leg].LeftFootStators.Select(b => b.Stator)));
                Color rightFootIntegrity = GetColorForIntegrity(GetIntegrity(legs[leg].RightFootStators.Select(b => b.Stator)));

                utensil.DrawMonoImage(center, new Vector2(19, 26) * imageScale, cockpitIntegrity, cockpit); // cockpit

                utensil.DrawMonoImage(center - new Location(12.5f, 2/2) * imageScale, new Vector2(12, 24) * imageScale, bodyIntegrity, body); // left body
                utensil.DrawMonoImage(center - new Location(-12.5f, 2 / 2) * imageScale, new Vector2(12, 24) * imageScale, bodyIntegrity, body, true); // right body

                utensil.DrawMonoImage(center - new Location(21.5f, 4) * imageScale, new Vector2(6, 14) * imageScale, Gray, shoulder); // left shoulder
                utensil.DrawMonoImage(center - new Location(-21.5f, 4) * imageScale, new Vector2(6, 14) * imageScale, Gray, shoulder, true); // right shoulder
                utensil.DrawMonoImage(center - new Location(26.5f, -14) * imageScale, new Vector2(20, 26) * imageScale, Gray, arm); // left arm
                utensil.DrawMonoImage(center - new Location(-26.5f, -14) * imageScale, new Vector2(20, 26) * imageScale, Gray, arm, true); // right arm
                utensil.DrawMonoImage(center - new Location(25f, -27.5f) * imageScale, new Vector2(13, 11) * imageScale, Gray, hand); // left hand
                utensil.DrawMonoImage(center - new Location(-25f, -27.5f) * imageScale, new Vector2(13, 11) * imageScale, Gray, hand, true); // right hand

                utensil.DrawMonoImage(center - new Location(0f, -14) * imageScale, new Vector2(17, 6) * imageScale, waistIntegrity, waist); // waist
                utensil.DrawMonoImage(center - new Location(0f, -21.5f) * imageScale, new Vector2(21, 13) * imageScale, hipIntegrity, lowerbody); // hip/lower body

                utensil.DrawMonoImage(center - new Location(10.5f, -24.5f) * imageScale, new Vector2(12, 15) * imageScale, leftThighIntegrity, thigh); // left thigh
                utensil.DrawMonoImage(center - new Location(-10.5f, -24.5f) * imageScale, new Vector2(12, 15) * imageScale, rightThighIntegrity, thigh, true); // right thigh

                utensil.DrawMonoImage(center - new Location(11.5f, -42f) * imageScale, new Vector2(14, 24) * imageScale, leftCalfIntegrity, calf); // left calf
                utensil.DrawMonoImage(center - new Location(-11.5f, -42f) * imageScale, new Vector2(14, 24) * imageScale, rightCalfIntegrity, calf, true); // right calf

                utensil.DrawMonoImage(center - new Location(11.5f, -56f) * imageScale, new Vector2(18, 6) * imageScale, leftFootIntegrity, foot); // left foot
                utensil.DrawMonoImage(center - new Location(-11.5f, -56f) * imageScale, new Vector2(18, 6) * imageScale, rightFootIntegrity, foot, true); // right foot

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
