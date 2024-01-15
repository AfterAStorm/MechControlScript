using Sandbox.Game.AI.Commands;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

        string CockpitName = "auto"; // auto will find the main cockpit; optional (for manually controlling)
        string RemoteControlName = "auto"; // auto will find a main remote control; optional (for remote controlling)

        string IntegrityLCDName = "Mech Integrity"; // based on the Name of the block!
        string StatusLCDName = "Mech Status"; // based on the Name of the block!

        bool UseCockpitLCDs     = true; // should cockpits show the leds instead?
        int IntegrityLEDNumber  = 2; // starting at one, if the cockpit has more than one screen you can change it here
        int StatusLEDNumber     = 3; // set to zero to disable

        // - Mech

        public static float StandingHeight = .95f; // a multiplier applied to some leg types, does what it says on the tin

        // - Joints

        /*
         * Leg Types:
         * 1 = Chicken walker
         * 2 = Humanoid
         * 3 = Spideroid
         * 4 = Digitigrade
        */

        static float AccelerationMultiplier = 1f; // how fast the mech accelerates, 1f is normal, .5f is half speed, 2f is double speed
        static float DecelerationMultiplier = 1f; //  how fast the mech decelerates, same as above

        static float MaxRPM = float.MaxValue; // 60f is the max speed for rotors
        // *Configure motor limits in the blocks themselves!* //

        // - Walking

        static float WalkCycleSpeed = 3f;

        // - Gyroscopes

        static bool GyroscopeSteering = true; // if we should change the override of gyroscopes to turn (yaw)
        static string GyroscopeNames = "Mech Steering"; // the name of the gyroscopes
        static bool GyroscopesInverted = false; // should invert the yaw
        static bool GyroscopesDisableOverride = false; // when not turning, should we turn off override?

        static bool GyroscopeStabilization = true; // if we should use gyroscopes to limit roll/pitch
        static string GyroscopeStablizationNames = "Mech Stablization";

        // -- Debug -- \\

        string DebugLCD = "debug";

        ///////////////// 
        // Code Script // 
        ////////////////

        // Constants //

        public static Program Singleton;

        public const double DefaultHipOffsets = 0d;
        public const double DefaultKneeOffsets = 0d;
        public const double DefaultFeetOffsets = 0d;

        // Default Joint Configuration //

        // Diagnostics //

        double[] averageRuntimes = new double[15];
        int averageRuntimeIndex = 0;

        // Variables //

        public static IMyTextPanel debug = null;
        public static IMyTextPanel debug2 = null;

        public static List<LegGroup> Legs = new List<LegGroup>();

        List<InvalidatableSurfaceRenderer> integrityRenderers = new List<InvalidatableSurfaceRenderer>();
        List<InvalidatableSurfaceRenderer> statusRenderers = new List<InvalidatableSurfaceRenderer>();

        List<IMyGyro> steeringGyros = new List<IMyGyro>();
        List<Joint> torsoTwistStators = new List<Joint>();
        List<IMyShipController> cockpits = new List<IMyShipController>();
        private bool crouched = false;
        private bool crouchOverride = false; // argument crouch

        private Vector3 movementOverride = Vector3.Zero;
        Vector3 movement = Vector3.Zero;

        private static void Log(params object[] messages)
        {
            string message = string.Join(" ", messages);
            if (debug == null)
                Singleton.Echo(message);
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
            debug2 = GridTerminalSystem.GetBlockWithName(DebugLCD + "2") as IMyTextPanel;
            debug?.WriteText(""); // clear
            debug2?.WriteText("");

            // Get all cockpits if they are the main cockpit (or the main remote control) // MainRemoteControl
            GridTerminalSystem.GetBlocksOfType(cockpits, c =>
                c is IMyRemoteControl
                ?
                (RemoteControlName.Equals("auto") ? c.GetProperty("MainRemoteControl").AsBool().GetValue(c) : c.CustomName.Equals(RemoteControlName))
                :
                (CockpitName.Equals("auto") ? c.IsMainCockpit : c.CustomName.Equals(CockpitName))
            );
            if (GyroscopeSteering)
                GridTerminalSystem.GetBlocksOfType(steeringGyros, gyro => gyro.CustomName.Equals(GyroscopeNames));

            Log($"{(cockpits.Count > 0 ? "Found" : "Didn't Find")} cockpit(s)");

            // Get LCDs
            integrityRenderers.Clear();
            statusRenderers.Clear();
            if (UseCockpitLCDs)
                foreach (IMyShipController controller in cockpits)
                {
                    if (controller is IMyTextSurfaceProvider)
                    {
                        IMyTextSurface integrity = (controller as IMyTextSurfaceProvider).GetSurface(IntegrityLEDNumber - 1);
                        IMyTextSurface status = (controller as IMyTextSurfaceProvider).GetSurface(StatusLEDNumber - 1);
                        if (integrity != null)
                            integrityRenderers.Add(new IntegrityRenderer(integrity));
                        if (status != null)
                            statusRenderers.Add(new StatusRenderer(status));
                    }
                }

            // Get torso twist stators and other blocks
            torsoTwistStators.Clear();
            foreach (FetchedBlock block in BlockFinder.GetBlocksOfType<IMyMotorStator>(motor => BlockFetcher.ParseBlock(motor).HasValue).Select(motor => BlockFetcher.ParseBlock(motor)))
            {
                Log(block.Type);
                switch (block.Type)
                {
                    case BlockType.TorsoTwist:
                        torsoTwistStators.Add(new Joint(block.Block as IMyMotorStator, new JointConfiguration()
                        {
                            Inversed = block.Inverted,
                            Offset = 0
                        }));
                        break;
                }
            }

            // Get the leg groups and the blocks associated with them
            BlockFetcher.GetBlocks();
        }

        /*/// <summary> // I'm keeping this for sentimental value, it will get removed in the workshop publish from minification anyway
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
            // Initialize subclasses
            Singleton = this;

            // Get blocks
            GetBlocks();

            // Setup loop
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
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
            Echo($"{Legs.Count} leg group{(Legs.Count != 1 ? "s" : "")}");
            Echo($"");
            Echo($"Last       Tick: {lastRuntime}ms");
            Echo($"Average Tick: {averageRuntimes.Sum() / averageRuntimes.Length:.03}ms over {averageRuntimes.Length} samples");
            //Echo($"{string.Join(", ", averageRuntimes)}");

            // Handle arguments
            if (argument != null)
            {
                string[] arguments = argument.ToLower().Split(' ');
                switch (arguments[0].Trim()) // Clean up argument, allow inputs
                {
                    case "reload": // Reloads the script's blocks and configuration
                        GetBlocks();
                        break;
                    case "crouch": // Toggle crouch (overrides the cockpit [c]), argument for "on" or "true" and "off" or "false", off and false aren't checked but infered
                        if (argument.Length > 1)
                            crouchOverride = arguments[1].Equals("on") || argument[1].Equals("true");
                        else
                            crouchOverride = !crouchOverride; // crouchOverride is for this specifically, because the normal crouched variable is set based on
                        // the MoveIndicator (then gets set to this value if true)
                        break;
                    case "walk": // b or backwards to go backwards, forward is infered and default
                        if (argument.Length > 1)
                            movementOverride = argument[1].Equals("b") || argument[1].Equals("backwards") ? Vector3.Backward : Vector3.Forward;
                        else
                            movementOverride = Vector3.Forward;
                        break;
                    case "halt": // Halt mech movement override
                        movementOverride = Vector3.Zero;
                        break;
                }
            }

            // Only update during specified update times!
            if (!updateSource.HasFlag(UpdateType.Update1))
                return;

            // Screens
            integrityRenderers.Concat(statusRenderers).ToList().ForEach(r => r.Invalidate());
            integrityRenderers.Concat(statusRenderers).ToList().ForEach(r => r.Render());

            double delta = Runtime.TimeSinceLastRun.TotalSeconds;

            IMyShipController controller = cockpits.Find((pit) => pit.IsUnderControl);

            Vector3 moveInput = Vector3.Clamp((controller?.MoveIndicator ?? Vector3.Zero) + movementOverride, Vector3.MinusOne, Vector3.One);
            Vector2 rotationInput = controller?.RotationIndicator ?? Vector2.Zero; // X is pitch, Y is yaw
            float rollInput = controller?.RollIndicator ?? 0f; // X is pitch, Y is yaw

            if (GyroscopeSteering)
            {
                bool overrideEnabled = !GyroscopesDisableOverride || moveInput.X != 0;
                foreach (var gyro in steeringGyros)
                {
                    if (!overrideEnabled && gyro.GyroOverride)
                    {
                        gyro.Yaw = 0;
                        gyro.GyroOverride = false;
                    }
                    else if (overrideEnabled)
                    {
                        gyro.GyroOverride = true;
                        gyro.Yaw = moveInput.X * float.MaxValue * (GyroscopesInverted ? -1 : 1);
                    }
                }
            }

            foreach (var joint in torsoTwistStators)
            {
                joint.Stator.TargetVelocityRPM = rotationInput.Y * (float)joint.Configuration.InversedMultiplier;
            }

            debug?.WriteText(""); // clear
            Log("MAIN LOOP");

            bool turning = moveInput.X != 0;
            crouched = moveInput.Y < 0 || crouchOverride;

            Vector3 movementDirection = (moveInput - movement) * .5f;

            movement.X += movementDirection.X * (movementDirection.X > 0 ? AccelerationMultiplier : DecelerationMultiplier) * (float)delta;
            movement.Z += movementDirection.Z * (movementDirection.Z > 0 ? AccelerationMultiplier : DecelerationMultiplier) * (float)delta;

            if (Math.Abs(movementDirection.X) < .01 && Math.Abs(movement.X) < .01)
                movement.X = 0;
            if (Math.Abs(movementDirection.Z) < .3 && Math.Abs(movement.Z) < .3)
                movement.Z = 0;

            Log(moveInput.ToString());
            Log(rotationInput.ToString());
            Log(movement.ToString());

            delta *= -movement.Z; // negative because -Z is forwards!

            if (Math.Abs(movement.Z) <= 0.035)
                foreach (LegGroup leg in Legs)
                    leg.Animation = turning ? (!crouched ? Animation.Turn : Animation.CrouchTurn) : !crouched ? Animation.Idle : Animation.Crouch;
            else
                foreach (LegGroup leg in Legs)
                    leg.Animation = !crouched ? Animation.Walk : Animation.CrouchWalk;

            foreach (LegGroup leg in Legs)
                leg.Update(delta, Runtime.TimeSinceLastRun.TotalSeconds);
        }
    }
}
