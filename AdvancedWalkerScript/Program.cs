using Sandbox.Game.AI.Commands;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
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
using static IngameScript.IMyMotorStatorExtensions;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region mdk preserve
        // {{ Source Code and Wiki can be found at https://github.com/AfterAStorm/AdvancedWalkerScript }} \\
        // I'd recommend looking at the wiki for quick setup and differences between other walker scripts \\

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

        static float StandingHeight = .95f; // a multiplier applied to some leg types, does what it says on the tin

        // - Joints

        /*
         * Leg Types:
         * 1 = Chicken walker
         * 2 = Humanoid
         * 3 = Spideroid
         * 4 = Digitigrade
        */
        // The default is 1, but can be changed in the CustomData of any joint of the leg group

        static float AccelerationMultiplier = 1f; // how fast the mech accelerates, 1f is normal, .5f is half speed, 2f is double speed
        static float DecelerationMultiplier = 1f; //  how fast the mech decelerates, same as above

        static float MaxRPM = float.MaxValue; // 60f is the max speed for rotors
        // *Configure motor limits in the blocks themselves!* //

        static double StandingLean = 0d; // the offset of where the foot sits when standing (idling)
        static double AccelerationLean = 0d; // the offset of where the foot sits when walking

        static float TorsoTwistSensitivity = 1f; // how sensitive the torso twist is, can also change based on the rotor's torque
        static float TorsoTwistMaxSpeed = 60f; // maximum RPM of the torso twist rotor;

        // - Walking

        static float WalkCycleSpeed = 2f;//3f;
        static bool AutoHalt = true; // if it should slow down when there is no one in the cockpit holding a direction

        // - Stablization / Steering

        static double SteeringSensitivity = 5; // x / 60th speed, specifies rotor/gyro RPM divided by 60, so 30 is half max power/rpm

        // - Controls

        /*
         * Mech Controls
         * W/S   >> Forward/Backward
         * A/D   >> Strafe Left/Right
         * Q/E   >> Turn Left/Right
         * C     >> Crouch
         * Space >> Jetpack
         * Mouse >> Torso Twist/Arm Control
         * 
         * Reversed Mech Turn Controls
         * W/S   >> (see above)
         * A/D   >> Turn Left/Right
         * Q/E   >> Strafe Left/Right
         * C     >> (see above)
         * Space >> (see above)
         * Mouse >> (see above)
         * 
         */

        bool ReverseTurnControls = false; // see above

        // -- Diagnostics -- \\

        string DebugLCD = "debug";
        const int AverageRuntimeSampleSize = 15;

        ///////////////// 
        // Script Code // 
        ////////////////

        // Constants //

        // Change these at your discretion \\
        // These are script CONSTANTs, not 
        // OPTIONs

        public static Program Singleton { get; private set; }

        public const double DefaultHipOffsets = 0d;
        public const double DefaultKneeOffsets = 0d;
        public const double DefaultFeetOffsets = 0d;
        #endregion

        // Diagnostics //

        double[] averageRuntimes = new double[AverageRuntimeSampleSize];
        int averageRuntimeIndex = 0;
        double maxRuntime = 0;
        int lastInstructions = 0;

        bool force = false;
        float forcedStep = 0;

        // Variables //

        double deltaOffset = 0;

        public static IMyTextPanel debug = null;
        public static IMyTextPanel debug2 = null;

        public static Dictionary<int, LegGroup> Legs = new Dictionary<int, LegGroup>();

        List<InvalidatableSurfaceRenderer> integrityRenderers = new List<InvalidatableSurfaceRenderer>();
        List<InvalidatableSurfaceRenderer> statusRenderers = new List<InvalidatableSurfaceRenderer>();

        List<IMyGyro> steeringGyros = new List<IMyGyro>();
        List<Gyroscope> stabilizationGyros = new List<Gyroscope>();
        List<Joint> torsoTwistStators = new List<Joint>();
        List<RotorGyroscope> azimuthStators = new List<RotorGyroscope>();
        List<RotorGyroscope> elevationStators = new List<RotorGyroscope>();
        List<RotorGyroscope> rollStators = new List<RotorGyroscope>();
        List<Gyroscope> azimuthGyros = new List<Gyroscope>();
        List<IMyShipController> cockpits = new List<IMyShipController>();
        bool crouched = false;
        bool crouchOverride = false; // argument crouch
        public static bool jumping = false;
        double jumpCooldown = 0;

        Vector3 movementOverride = Vector3.Zero;
        Vector3 movement = Vector3.Zero;
        int turnOverride = 0;
        double targetTorsoTwistAngle = -1;

        static void Log(params object[] messages)
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
        void GetBlocks()
        {
            debug = GridTerminalSystem.GetBlockWithName(DebugLCD) as IMyTextPanel;
            debug2 = GridTerminalSystem.GetBlockWithName(DebugLCD + "2") as IMyTextPanel;
            debug?.WriteText(""); // clear
            debug2?.WriteText("");

            // Get all cockpits if they are the main cockpit (or the main remote control) // MainRemoteControl
            GridTerminalSystem.GetBlocksOfType(cockpits, c =>
                c.IsSameConstructAs(Me)
                &&
                (c is IMyRemoteControl
                ?
                (RemoteControlName.Equals("auto") ? c.GetProperty("MainRemoteControl").AsBool().GetValue(c) : c.CustomName.Equals(RemoteControlName))
                :
                (CockpitName.Equals("auto") ? c.IsMainCockpit : c.CustomName.Equals(CockpitName)))
            );
            //if (GyroscopeSteering)
            //    GridTerminalSystem.GetBlocksOfType(steeringGyros, gyro => gyro.CustomName.Equals(GyroscopeNames));
            //if (GyroscopeStabilization)
            //    GridTerminalSystem.GetBlocksOfType(stabilizationGyros, gyro => gyro.CustomName.Equals(GyroscopeStabilizationNames));

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
            azimuthStators.Clear();
            elevationStators.Clear();
            rollStators.Clear();
            foreach (FetchedBlock block in BlockFinder.GetBlocksOfType<IMyMotorStator>(motor => BlockFetcher.ParseBlock(motor).HasValue).Select(motor => BlockFetcher.ParseBlock(motor)))
            {
                switch (block.Type)
                {
                    case BlockType.TorsoTwist:
                        torsoTwistStators.Add(new Joint(block));
                        break;
                    case BlockType.GyroscopeAzimuth:
                        azimuthStators.Add(new RotorGyroscope(block));
                        break;
                    case BlockType.GyroscopeElevation:
                        elevationStators.Add(new RotorGyroscope(block));
                        break;
                    case BlockType.GyroscopeRoll:
                        if (block.Side != BlockSide.Right)
                            return; // since r is keyword, we have to look for "g" then block side "r" :/
                        rollStators.Add(new RotorGyroscope(block));
                        break;
                }
            }

            stabilizationGyros.Clear();
            foreach (FetchedBlock block in BlockFinder.GetBlocksOfType<IMyGyro>(gyro => BlockFetcher.ParseBlock(gyro).HasValue).Select(gyro => BlockFetcher.ParseBlock(gyro)))
                switch (block.Type)
                {
                    case BlockType.GyroscopeAzimuth:
                    case BlockType.GyroscopeElevation:
                    case BlockType.GyroscopeRoll:
                    case BlockType.GyroscopeStabilization:
                        stabilizationGyros.Add(new Gyroscope(block));
                        break;
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
            // Initialize
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

        void Warn(string title, string info)
        {
            Echo($"[Color=#dcf71600]Warning: {title}[/Color]");
            Echo($"[Color=#c8e02d00]{info}[/Color]\n");
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
            maxRuntime = Math.Max(maxRuntime, lastRuntime);

            // Detailed Info - alpha red green blue
            Echo("[Color=#13ebca00]Advanced Walker Script[/Color]");
            Echo($"{Legs.Count} leg group{(Legs.Count != 1 ? "s" : "")}");
            Echo($"");
            Echo($"Last       Tick: {lastRuntime}ms");
            Echo($"Average Tick: {averageRuntimes.Sum() / averageRuntimes.Length:.03}ms over {averageRuntimes.Length} samples");
            Echo($"Max        Tick: {maxRuntime:.03}ms");
            Echo($"Last Instructions: {lastInstructions}");
            Echo($"Last Compexity: {lastInstructions / Runtime.MaxInstructionCount * 100:.03}%\n");

            // Some Setup Warnings
            if (cockpits.Count <= 0)
            {
                List<IMyShipController> controllers = BlockFinder.GetBlocksOfType<IMyShipController>();
                if (controllers.Count > 0) // if there is any actual controllers, add it to the warning message
                    Warn("No Cockpits Found!", "Failed to find any MAIN cockpits or remote controls\n" +
                        $"Maybe try changing {(controllers.Count > 1 ? $"one of the {controllers.Count} ship controller{(controllers.Count > 1 ? "s" : "")}" : $"{controllers[0].CustomName} to the main cockpit")}");
                else
                    Warn("No Cockpits Found!", "Failed to find any MAIN cockpits or remote controls");
            }
            if (Legs.Count <= 0) // how bruh gonna *walk* without legza?
                Warn("No Legs Found!", "Failed to find any leg groups!\nNeed help setting up? Check the documentation at github.com/AfterAStorm/AdvancedWalkerScript/wiki");

            // Handle arguments
            if (!string.IsNullOrEmpty(argument))
            {
                string[] arguments = argument.ToLower().Split(' ');
                switch (arguments[0].Trim()) // Clean up argument, allow inputs
                {
                    default:
                    case "reload": // Reloads the script's blocks and configuration
                        GetBlocks();
                        force = false;
                        break;
                    case "crouch": // Toggle crouch (overrides the cockpit [c]), argument for "on" or "true" and "off" or "false", off and false aren't checked but infered
                        if (arguments.Length > 1)
                            crouchOverride = arguments[1].Equals("on") || arguments[1].Equals("true");
                        else
                            crouchOverride = !crouchOverride; // crouchOverride is for this specifically, because the normal crouched variable is set based on
                        // the MoveIndicator (then gets set to this value if true)
                        break;
                    case "walk": // b or backwards to go backwards, forward is infered and default
                        if (arguments.Length > 1)
                            movementOverride = arguments[1].Equals("b") || arguments[1].Equals("backwards") ? Vector3.Backward : Vector3.Forward;
                        else
                            movementOverride = Vector3.Forward;
                        break;
                    case "halt": // Halt mech movement override
                        movementOverride = Vector3.Zero;
                        force = false;
                        break;
                    case "step":
                        force = false;
                        if (arguments.Length > 1)
                        {
                            force = true;
                            forcedStep = float.Parse(arguments[1]);
                        }
                        break;

                    case "turn":
                        if (arguments.Length > 1)
                            turnOverride = int.Parse(arguments[1]);
                        else
                            turnOverride = 0;
                        break;

                    // set methods //
                    case "speed":
                        WalkCycleSpeed = float.Parse(arguments[1]);
                        break;

                    case "lean":
                        float value = float.Parse(arguments[1]);
                        StandingLean = value;
                        AccelerationLean = value;
                        break;

                    case "standinglean":
                    case "standlean":
                        StandingLean = float.Parse(arguments[1]);
                        break;

                    case "accelerationlean":
                    case "accellean":
                        StandingLean = float.Parse(arguments[1]);
                        break;

                    case "steplength":
                        double stepLength = double.Parse(arguments[1]);
                        foreach (LegGroup g in Legs.Values)
                            g.Configuration.StepLength = stepLength;
                        break;

                    case "stepheight":
                        double stepHeight = double.Parse(arguments[1]);
                        foreach (LegGroup g in Legs.Values)
                            g.Configuration.StepHeight = stepHeight;
                        break;

                    case "autohalt":
                        AutoHalt = argument[1].Equals("on") || argument[1].Equals("true");
                        break;

                    case "twist":
                        targetTorsoTwistAngle = arguments.Length > 1 ? float.Parse(arguments[1]) : 0;
                        break;
                }
                if (!updateSource.HasFlag(UpdateType.Update1))
                {
                    deltaOffset += Runtime.TimeSinceLastRun.TotalMilliseconds / 1000d;
                    return;
                }
            }

            // Only update during specified update times!
            if (!updateSource.HasFlag(UpdateType.Update1))
                return;

            // Screens
            integrityRenderers.Concat(statusRenderers).ToList().ForEach(r => r.Invalidate());
            integrityRenderers.Concat(statusRenderers).ToList().ForEach(r => r.Render());

            // Get delta
            double delta = Runtime.TimeSinceLastRun.TotalMilliseconds / 1000d + deltaOffset;
            deltaOffset = 0;

            // Get controllers
            IMyShipController controller = cockpits.Find((pit) => pit.IsUnderControl);
            IMyShipController anyController = controller ?? (cockpits.Count > 0 ? cockpits[0] : null);

            Vector3 moveInput = Vector3.IsZero(movementOverride) ? Vector3.Clamp((controller?.MoveIndicator ?? Vector3.Zero), Vector3.MinusOne, Vector3.One) : movementOverride;
            Vector2 rotationInput = controller?.RotationIndicator ?? Vector2.Zero; // X is -pitch, Y is yaw // Mouse
            float rollInput = controller?.RollIndicator ?? 0f; // left is -, right is + (infered) // Q + E

            debug?.WriteText(""); // clear
            Log("MAIN LOOP");

            float turnValue = turnOverride != 0 ? turnOverride : (ReverseTurnControls ? moveInput.X : rollInput);
            HandleStabilization(turnValue);
            HandleTorsoTwist(rotationInput.Y);

            Log($"turnValue: {turnValue}");
            Log($"azimuthStators: {azimuthStators.Count}");
            Log($"elevationStators: {elevationStators.Count}");
            Log($"rollStators: {rollStators.Count}");

            bool turning = turnValue != 0;
            crouched = moveInput.Y < 0 || crouchOverride;
            if (crouched)
                jumping = false;

            if (moveInput.Y > 0)
            {
                jumping = true;
                crouched = true;
                jumpCooldown = .5d;
            }
            else if (jumpCooldown > 0)
            {
                jumpCooldown = Math.Max(0, jumpCooldown - delta);
                if (jumpCooldown <= 0)
                    jumping = false;
            }

            Log($"jumping: {jumping}");
            Log($"crouched: {crouched}");

            Vector3 movementDirection = (moveInput - movement) * .5f;

            if (!AutoHalt && controller == null)
            {
            }
            else
            {
                movement.X += movementDirection.X * (movementDirection.X > 0 ? AccelerationMultiplier : DecelerationMultiplier) * (float)delta;
                movement.Z += movementDirection.Z * (movementDirection.Z > 0 ? AccelerationMultiplier : DecelerationMultiplier) * (float)delta;
            }

            if (Math.Abs(movementDirection.X) < .01 && Math.Abs(movement.X) < .01)
                movement.X = 0;
            if (Math.Abs(movementDirection.Z) < .3 && Math.Abs(movement.Z) < .3)
                movement.Z = 0;

            Log(moveInput.ToString());
            Log(rotationInput.ToString());
            Log(movement.ToString());

            double originalDelta = delta;
            Log($"Before delta: {delta}");
            delta *= -movement.Z; // negative because -Z is forwards!
            Log($"After delta: {delta}");

            if (force)
            {
                foreach (LegGroup leg in Legs.Values)
                {
                    leg.Animation = Animation.Force;
                    leg.AnimationStep = forcedStep;
                    leg.Update(0.01, 0);
                }
                return;
            }

            if (Math.Abs(movement.Z) <= 0.00001)
                foreach (LegGroup leg in Legs.Values)
                    leg.Animation = turning ? (!crouched ? Animation.Turn : Animation.CrouchTurn) : !crouched ? Animation.Idle : Animation.Crouch;
            else
                foreach (LegGroup leg in Legs.Values)
                    leg.Animation = !crouched ? Animation.Walk : Animation.CrouchWalk;

            foreach (LegGroup leg in Legs.Values)
                leg.Update(delta, originalDelta);
            lastInstructions = Runtime.CurrentInstructionCount;
        }
    }
}
