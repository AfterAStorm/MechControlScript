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

            protected override void Render(Utensil utensil)
            {
                utensil.Surface.ContentType = ContentType.SCRIPT;
                utensil.SetScript();

                float scale = utensil.Height / 1024f;

                // Hip
                utensil.DrawRectangle(utensil.Center.ToLocation(AnchorPoints.Center) + new Location(0, 180 - 25) * scale, new Vector2(50, 150) * scale, utensil.ForegroundColor);

                // Torso
                //utensil.DrawRectangle(utensil.Center.ToLocation(AnchorPoints.Center) - new Location(0, 0) * scale, new Vector2(250, 200) * scale, utensil.ForegroundColor);
                utensil.DrawRectangle(utensil.Center.ToLocation(AnchorPoints.Center) - new Location(110, 0) * scale, new Vector2(100, 200) * scale, utensil.ForegroundColor);
                utensil.DrawRectangle(utensil.Center.ToLocation(AnchorPoints.Center) + new Location(110, 0) * scale, new Vector2(100, 200) * scale, Color.Red);
                utensil.DrawRectangle(utensil.Center.ToLocation(AnchorPoints.Center) - new Location(0, 40) * scale, new Vector2(110, 150) * scale, Color.Blue);
            }
        }
    }
}
