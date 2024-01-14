using Sandbox.Game.AI.Commands;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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

        // {{ Source Code and Wiki can be found at https://github.com/AfterAStorm/AdvancedWalkerScript }} //

        // -- Configuration -- \\

        // - Blocks

        string CockpitName = "auto"; // auto will find the main cockpit
        string ReferenceName = "auto"; // auto will find a main remote

        string IntegrityLCDName = "Mech Integrity"; // based on the Name of the block!
        string StatusLCDName = "Mech Status"; // based on the Name of the block!

        // - Joints

        /*
         * Leg Types:
         * 1 = Chicken walker
         * 2 = Humanoid
         * 3 = Spideroid
         * 4 = Digitigrade
        */

        static float AccelerationMultiplier = 1f; // how fast the mech accelerates, 1f is normal, .5f is half speed, 2f is double speed
        static float DecelerationMultiplier = 1; //  how fast the mech decelerates, same as above

        static float MaxRPM = float.MaxValue; // 60f is the max speed for rotors
        // *Configure motor limits in the blocks themselves!* //

        // - Walking

        static float WalkCycleSpeed = 3f;

        // -- Debug -- \\

        string DebugLCD = "debug";

        ///////////////// 
        // Code Script // 
        ////////////////

        // Constants //

        //const bool UseCustomDataAsConfiguration = true;
        private const UpdateFrequency RunFrequency = UpdateFrequency.Update1;

        //private MyIni configuration = new MyIni();

        // Default Joint Configuration //

        // Diagnostics //

        double[] averageRuntimes = new double[20];
        int averageRuntimeIndex = 0;

        // Variables //

        public static IMyTextPanel debug = null;

        List<IMyShipController> cockpits = new List<IMyShipController>();
        private List<LegGroup> legs = new List<LegGroup>();
        private bool crouched = false;
        private bool crouchOverride = false; // argument crouch

        Vector3 movement = Vector3.Zero;

        private static void Log(params string[] messages)
        {
            string message = string.Join(" ", messages);
            if (debug == null)
                LegGroup.Program.Echo(message);
            else
                debug.WriteText(message + "\n", true);
        }

        private LegGroup CreateLegFromType(byte type)
        {
            switch (type)
            {
                case 0:
                    return new ChickenWalkerLegGroup();
                default:
                    throw new Exception("Leg type not implemented!");
            }
        }

        /// <summary>
        /// This checks a stator and sees if it should add it to the lists of a <see cref="LegGroup"/>
        /// <br />
        /// It, Is, NIGHTMARE FUEL
        /// </summary>
        /// <param name="stator"></param>
        /// <param name="pattern"></param>
        /// <param name="lastLegs"></param>
        /// <param name="currentLegs"></param>
        /// <param name="createLegAnyway"></param>
        /// <returns></returns>
        private bool CheckStator(IMyMotorStator stator, System.Text.RegularExpressions.Regex pattern, ref List<LegGroup> lastLegs, ref List<LegGroup> currentLegs, bool createLegAnyway = false)
        {
            foreach (string segment in stator.CustomName.Split(' '))
            {
                System.Text.RegularExpressions.Match match = pattern.Match(segment.ToLower());
                if (!match.Success || match.Groups[1].Value.Length <= 0 || match.Groups[2].Value.Length <= 0)
                    continue;
                int id;
                bool parsedId = int.TryParse(match.Groups[3].Value, out id);
                LegGroup lastLeg = lastLegs.IsValidIndex(id) ? lastLegs[id] : null;
                LegConfiguration? lastConfig = lastLeg?.Configuration;
                LegGroup currentLeg = currentLegs.IsValidIndex(id) ? currentLegs[id] : null;

                if (currentLeg == null)
                {
                    if (lastConfig.HasValue && lastConfig.Value.HasChanged(stator.CustomData))
                    {
                        LegConfiguration? configuration = LegConfiguration.Parse(stator.CustomData);
                        if (configuration.HasValue)
                        {
                            currentLeg = CreateLegFromType(configuration.Value.LegType);
                            currentLeg.Configuration = configuration.Value;
                            currentLegs.Insert(id, currentLeg);
                        }
                    }
                    if (currentLeg == null && createLegAnyway)
                    {
                        LegConfiguration configuration = LegConfiguration.DEFAULT;
                        currentLeg = CreateLegFromType(configuration.LegType);
                        currentLeg.Configuration = configuration;
                        currentLegs.Insert(id, currentLeg);
                    }
                    else if (currentLeg == null)
                        return true; // check later, there is a possibility of another joint having a valid config
                }

                currentLeg.Configuration.AnimationSpeed = WalkCycleSpeed;
                stator.CustomData = currentLeg.Configuration.ToCustomDataString();

                bool isLeft = false;
                switch (match.Groups[2].Value)
                {
                    case "l":
                    case "left":
                        isLeft = true;
                        break;
                    // no need to check right since it's infered
                    // and the regex only searched for l, left, r, and right; so it can only ever be r and right anyway
                }

                Joint joint = new Joint(stator, new JointConfiguration()
                {
                    Inversed = match.Groups[4].Value.Equals("-"),
                    Offset = 0
                });

                switch (match.Groups[1].Value)
                {
                    case "h":
                    //case "hip":
                        if (isLeft)
                            currentLeg.LeftHipStators.Add(joint);
                        else
                            currentLeg.RightHipStators.Add(joint);
                        break;
                    case "k":
                    //case "knee":
                        if (isLeft)
                            currentLeg.LeftKneeStators.Add(joint);
                        else
                            currentLeg.RightKneeStators.Add(joint);
                        break;
                    case "f":
                    //case "fp":
                    //case "foot":
                    //case "feet":
                        if (isLeft)
                            currentLeg.LeftFootStators.Add(joint);
                        else
                            currentLeg.RightFootStators.Add(joint);
                        break;
                    default:
                        Log($"Unknown joint type \"{match.Groups[0].Value}\"");
                        break;
                }
                break;
            }
            return false; // do not check, it's good!
        }

        /// <summary>
        /// Gets the blocks required for operation
        /// Ran at startup and on request
        /// </summary>
        private void GetBlocks()
        {
            debug = GridTerminalSystem.GetBlockWithName(DebugLCD) as IMyTextPanel;
            debug?.WriteText(""); // clear

            // Core blocks

            string cockpitSpecifier = CockpitName;//GetConfigurationValue("Mech Core", "Cockpit").ToString();
            string referenceSpecifier = ReferenceName;//GetConfigurationValue("Mech Core", "Reference").ToString();
            Log("Looking for cockpit with specifier", cockpitSpecifier);
            Log("Looking for reference rc with specifier", referenceSpecifier);

            // Get all cockpits if they are the main cockpit (or the main remote control)
            GridTerminalSystem.GetBlocksOfType(cockpits, (controller) => (controller is IMyRemoteControl) ? (controller as IMyRemoteControl).GetProperty("MainRemoteControl").AsBool().GetValue(controller) : controller.IsMainCockpit);

            Log($"{(cockpits.Count > 0 ? "Found" : "Didn't Find")} cockpit(s)");

            List<IMyMotorStator> stators = new List<IMyMotorStator>();
            GridTerminalSystem.GetBlocksOfType(stators);

            List<LegGroup> lastLegs = legs;
            List<LegGroup> currentLegs = new List<LegGroup>();
            legs.Clear();

            // Regex: HR5+ turns into [h, r, 5, +], HL turns into [h, l, null, null], HLeft5+ turns into [h, left, 5, +]
            System.Text.RegularExpressions.Regex pattern = new System.Text.RegularExpressions.Regex(@"^([^lLrR]*)([lr]{1}|left{1}|right{1})([0-9]+)?([-+]{1})?$");

            List<IMyMotorStator> recheckLater = new List<IMyMotorStator>();
            foreach (IMyMotorStator stator in stators)
            {
                Log($"Checking stator {stator.CustomName}");
                if (CheckStator(stator, pattern, ref lastLegs, ref currentLegs))
                {
                    recheckLater.Add(stator);
                    Log($"Checking {stator.CustomName} later");
                }
                else
                    Log($"{stator.CustomName} added to leg");
            }

            foreach (IMyMotorStator stator in recheckLater)
            {
                Log($"Rechecking stator {stator.CustomName}");
                CheckStator(stator, pattern, ref lastLegs, ref currentLegs, true);
            }

            legs = currentLegs;

            /*// Legs
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

                leg.InvertHips = InvertHips ^ leg.InvertHips;
                leg.InvertKnees = InvertKnees ^ leg.InvertKnees;
                leg.InvertFeet = InvertFeet ^ leg.InvertFeet;

                leg.HipOffset = HipOffset;
                leg.KneeOffset = KneeOffset;
                leg.FootOffset = FootOffset;

                leg.Offset = offset * 4;

                legs.Add(leg);
            }*/
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

            // Initialize subclasses
            LegGroup.Program = this;

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
            // Diagnostics
            double lastRuntime = Runtime.LastRunTimeMs;

            averageRuntimes[averageRuntimeIndex] = lastRuntime;
            averageRuntimeIndex = (averageRuntimeIndex + 1) % averageRuntimes.Length;

            // Detailed Info
            Echo("Advanced Walker Script");
            Echo($"{legs.Count} leg group{(legs.Count != 1 ? "s" : "")}");
            Echo($"");
            Echo($"Last Tick: {lastRuntime}ms");
            Echo($"Average Tick: {averageRuntimes.Sum() / averageRuntimes.Length:.03}ms over {averageRuntimes.Length} samples");

            // Handle arguments
            if (argument != null)
                switch (argument.ToLower().Trim()) // Clean up argument, allow inputs
                {
                    case "reload": // Reloads the script's blocks and configuration
                        GetBlocks();
                        break;
                    case "crouch": // Toggle crouch (overrides the cockpit [c])
                        crouchOverride = !crouchOverride; // crouchOverride is for this specifically, because the normal crouched variable is set based on
                        // the MoveIndicator (then gets set to this value if true)
                        break;
                }

            // Only update during specified update times!
            if (!updateSource.HasFlag(UpdateType.Update1))
                return;

            double delta = Runtime.TimeSinceLastRun.TotalSeconds;

            IMyShipController controller = cockpits.Find((pit) => pit.IsUnderControl);

            Vector3 moveInput = controller?.MoveIndicator ?? Vector3.Zero;

            debug?.WriteText(""); // clear
            Log("MAIN LOOP");

            crouched = moveInput.Y < 0 || crouchOverride;
            // TODO: use crouched / crouching

            Vector3 movementDirection = (moveInput - movement) * .5f;

            movement.X += movementDirection.X * (movementDirection.X > 0 ? AccelerationMultiplier : DecelerationMultiplier) * (float)delta;
            movement.Z += movementDirection.Z * (movementDirection.Z > 0 ? AccelerationMultiplier : DecelerationMultiplier) * (float)delta;

            if (Math.Abs(movementDirection.X) < .01 && Math.Abs(movement.X) < .01)
                movement.X = 0;
            if (Math.Abs(movementDirection.Z) < .02 && Math.Abs(movement.Z) < .03)
                movement.Z = 0;

            Log(moveInput.ToString());
            Log(movement.ToString());

            delta *= -movement.Z; // negative because -Z is forwards!

            if (Math.Abs(movement.Z) == 0)
            {
                delta = 0;
                foreach (LegGroup leg in legs)
                    leg.AnimationStep = 0;
            }

            foreach (LegGroup leg in legs)
                leg.Update(delta);
            /*if (cockpit == null || reference == null || legs.Count <= 0)
                return; // Not ready

            double delta = Runtime.TimeSinceLastRun.TotalSeconds;
            Vector3 playerInput = cockpit.MoveIndicator;
            //playerInput = new Vector3(0, 0, 1); // always move :D

            crouched = playerInput.Y < 0 || crouchOverride; // if player is holding [c] or ran "crouch" on the pb

            if (playerInput.Z != 0d)
                movement = playerInput;
            else
                movement *= .1f * (float)delta;
            debug?.WriteText(movement.ToString()); // TODO: remove

            if (movement.Length() <= .1) // stop it at some threshold
            {
                movement = Vector3.Zero;
            }

            double forward = movement.Z;

            // Update legs
            if (Math.Abs(forward) <= .05) // .05 to zero
            {
                foreach (Leg leg in legs)
                {
                    leg.AnimationStep = 0;
                    leg.OffsetPassed = 0;
                }
                delta = 0;
            }
            foreach (Leg leg in legs)
                leg.Update(delta);*/
        }
    }
}
