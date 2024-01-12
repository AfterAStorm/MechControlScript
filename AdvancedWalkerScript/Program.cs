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
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        // -- Configuration -- \\

        string CockpitName = "auto"; // auto will find the main cockpit
        string ReferenceName = "auto"; // auto will find a main remote control

        bool InvertHips = false; // invert hios?
        bool InvertKnees = false; // invert knees?
        bool InvertFeet = false; // invert feet?

        float HipOffset = 0f;
        float KneeOffset = 0f;
        float FootOffset = 0f;

        static float WalkSpeed = 2f;

        static float MaxRPM = 60f; // 60 is the max speed
        static float RotorLimits = 135f;

        // Leg Groups
        static string[] LegGroups =
        {
            "Left Leg",
            "Right Leg"
        };

        // Animation Offsets
        static float[] LegOffsets =
        {
            0f, // ex: LeftLeg offset is zero
            .75f // ex: RightLeg offset is 3/4
        };

        // -- Debug -- \\

        string DebugLCD = "debug";
        TestRenderer debugRenderer;

        ///////////////// 
        // Code Script // 
        ////////////////

        // Constants //

        //const bool UseCustomDataAsConfiguration = true;
        private const UpdateFrequency RunFrequency = UpdateFrequency.Update1;

        //private MyIni configuration = new MyIni();

        // Variables //

        IMyShipController cockpit = null;
        IMyShipController reference = null;
        static IMyTextPanel debug = null;
        private List<Leg> legs = new List<Leg>();

        Vector3 movement = Vector3.Zero;
        TimeSpan walkStart = TimeSpan.Zero;
        bool moving = false;

        private new void Echo(params string[] messages)
        {
            string message = string.Join(" ", messages);
            if (debug == null)
                base.Echo(message);
            else
                debug.WriteText(message + "\n", true);
        }

        /// <summary>
        /// Gets the blocks required for operation
        /// Ran at startup and on request
        /// </summary>
        private void GetBlocks()
        {
            debug = GridTerminalSystem.GetBlockWithName(DebugLCD) as IMyTextPanel;
            debug?.WriteText(""); // clear
            if (debug != null)
            {
                debugRenderer = new TestRenderer(debug);
            }

            // Core blocks

            string cockpitSpecifier = CockpitName;//GetConfigurationValue("Mech Core", "Cockpit").ToString();
            string referenceSpecifier = ReferenceName;//GetConfigurationValue("Mech Core", "Reference").ToString();
            Echo("Looking for cockpit with specifier", cockpitSpecifier);
            Echo("Looking for reference rc with specifier", referenceSpecifier);

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(blocks);
            foreach (IMyShipController controller in blocks) // Two for one, less effecient but it's not run that often
            {
                if (cockpit == null && ((cockpitSpecifier.Equals("auto") && controller is IMyCockpit && controller.IsMainCockpit) || controller.CustomName.Equals(cockpitSpecifier)))
                    cockpit = controller; // Main Remote Control isn't a property, so you have to get it manually AAAAAAAAAAAAAAAA
                else if (reference == null && ((referenceSpecifier.Equals("auto") && controller is IMyRemoteControl && controller.GetProperty("MainRemoteControl").AsBool().GetValue(controller)) || controller.CustomName.Equals(referenceSpecifier)))
                    reference = controller;
            }

            // Debug
            if (cockpit == null)
                Echo("No cockpit found!");
            else
                Echo("Found cockpit", cockpit.CustomName);
            if (reference == null)
                Echo("No reference RC found!");
            else
                Echo("Found reference", reference.CustomName);

            // Legs
            legs.Clear();
            List<IMyMotorStator> motors = new List<IMyMotorStator>();
            for (int i = 0; i < LegGroups.Length; i++)
            {
                string name = LegGroups[i];
                float offset = LegOffsets[i];

                IMyBlockGroup legGroup = GridTerminalSystem.GetBlockGroupWithName(name);
                if (legGroup == null)
                {
                    Echo("No group for leg", name);
                    continue;
                }
                IMyMotorStator hip = null;
                IMyMotorStator knee = null;
                IMyMotorStator foot = null;

                legGroup.GetBlocksOfType<IMyMotorStator>(motors);
                foreach (IMyMotorStator block in motors)
                {
                    string cname = block.CustomName;
                    if (cname.ToLower().Contains("hip"))
                        hip = block;
                    else if (cname.ToLower().Contains("knee"))
                        knee = block;
                    else if (cname.ToLower().Contains("foot"))
                        foot = block;
                }

                if (hip == null)
                    Echo(name, "is missing a hip rotor");
                if (knee == null)
                    Echo(name, "is missing a knee hinge");
                if (foot == null)
                    Echo(name, "is missing a foot hinge");

                if (hip == null || knee == null || foot == null)
                    continue; // don't create the leg, we are missing features!

                Leg leg = new Leg(hip, knee, foot);

                leg.InvertHips = InvertHips ? !leg.InvertHips : leg.InvertHips;
                leg.InvertKnees = InvertKnees ? !leg.InvertKnees : leg.InvertKnees;
                leg.InvertFeet = InvertFeet ? !leg.InvertFeet : leg.InvertFeet;

                leg.HipOffset = HipOffset;
                leg.KneeOffset = KneeOffset;
                leg.FootOffset = FootOffset;

                leg.Offset = offset * 4;

                legs.Add(leg);
            }
        }

        /*private MyIniValue GetConfigurationValue(string section, string key)
        {
            return configuration.Get(section, key);
        }*/

        /*/// <summary>
        /// Loads the configuration
        /// </summary>
        private void LoadConfiguration()
        {
            if (!configuration.TryParse(UseCustomDataAsConfiguration ? Me.CustomData : Storage))
            {
                if (UseCustomDataAsConfiguration && Me.CustomData.Length > 5) // arbitrary value, if its not empty don't overwrite it
                {
                    Echo("Failed to load/parse configuration from the block's CustomData, maybe a syntax error occured or there is already another type of data inside.");
                    return;
                }
                if (LegGroups.Length != LegOffsets.Length)
                {
                    Echo("There isn't enough leg offsets for leg groups or vice versa, please check the *script configuration* and not the custom data configuration");
                    return; // never set update frequency, so nothing is run again!
                }
                configuration.AddSection(       "Mech Core");
                configuration.Set(              "Mech Core", "Legs", string.Join(",", LegGroups));
                configuration.SetComment(       "Mech Core", "Legs", "The leg definitions in the configuration, labeled as [Mech Leg : (Leg Name)]");
                configuration.Set(              "Mech Core", "Cockpit", "auto");
                configuration.SetComment(       "Mech Core", "Cockpit", "If set to auto, it will attempt to find the main cockpit");
                configuration.Set(              "Mech Core", "Reference", "auto");
                configuration.SetComment(       "Mech Core", "Reference", "If set to auto, it will use the first found remote control");

                for (int i = 0; i < LegGroups.Length; i++)
                {
                    string leg = LegGroups[i];
                    float offset = LegOffsets[i];
                    string section = $"Mech Leg : {leg}";
                    configuration.AddSection(   section);
                    configuration.Set(          section, "", "");
                }
            }
        }*/

        /// <summary>
        /// Initializes the script
        /// </summary>
        public Program()
        {
            // Initialize configuration
            //LoadConfiguration();

            // Get blocks
            GetBlocks();

            // Setup loop
            Runtime.UpdateFrequency = RunFrequency;
        }

        /// <summary>
        /// Saves the current state
        /// </summary>
        public void Save()
        {

        }

        /// <summary>
        /// Main loop
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="updateSource"></param>
        public void Main(string argument, UpdateType updateSource)
        {
            debugRenderer?.Render();
            if (argument != null)
                switch (argument.ToLower().Trim()) // Clean up argument, allow inputs
                {
                    case "reload": // Reloads the script's blocks and configuration
                        GetBlocks();
                        break;
                }

            // Only update during specified update times!
            if (!updateSource.HasFlag(UpdateType.Update1))
                return;

            if (cockpit == null || reference == null || legs.Count <= 0)
                return; // Not ready

            double delta = Runtime.TimeSinceLastRun.TotalSeconds;
            Vector3 playerInput = cockpit.MoveIndicator;

            if (!Vector3.IsZero(playerInput))
            {
                movement = playerInput;
            }
            else
            {
                movement *= .1f * (float)delta;
            }
            debug?.WriteText(movement.ToString());

            if (movement.Length() <= .1) // stop it at some threshold
            {
                movement = Vector3.Zero;
            }

            double forward = movement.Z;

            // Update legs
            if (Vector3.IsZero(movement))
            {
                foreach (Leg leg in legs)
                {
                    leg.AnimationStep = 0;
                    leg.OffsetPassed = 0;
                }
                delta = 0;
            }
            foreach (Leg leg in legs)
                leg.Update(delta);

            // Update echo
            double lastRuntime = Runtime.LastRunTimeMs;
            base.Echo("Advanced Walker Script");
            base.Echo($"{legs.Count} leg{(legs.Count != 1 ? "s" : "")}");
            base.Echo($"");
            base.Echo($"Last Tick: {lastRuntime}ms");
        }
    }
}
