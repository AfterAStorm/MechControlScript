using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region mdk preserve
        // {{ Source Code and Wiki can be found at https://github.com/AfterAStorm/AdvancedWalkerScript }} \\
        // I'd recommend looking at the wiki for quick setup and differences between other walker scripts \\

        // -- Configuration -- \\

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

        // - Mech

        static float StandingHeight = .95f; // a multiplier applied to some leg types
        ThrusterMode ThrusterBehavior = ThrusterMode.Override; // valid: Override, Hover

        // - Walking

        static float WalkCycleSpeed = 2f; // a global speed multiplier
        static bool AutoHalt = true; // if it should stop walking when there is no one in the cockpit holding a direction

        // - Joints

        static float AccelerationMultiplier = 1f; // how fast the mech accelerates, 1f is normal, .5f is half speed, 2f is double speed
        static float DecelerationMultiplier = 1f; //  how fast the mech decelerates, same as above

        static float MaxRPM = float.MaxValue; // 60f is the max speed for rotors
                                              // *Configure motor limits in the blocks themselves!* //

        static double StandingLean = 0d; // the offset of where the foot sits when standing (idling)
        static double AccelerationLean = 0d; // the offset of where the foot sits when walking

        static float TorsoTwistSensitivity = 1f; // how sensitive the torso twist is, can also change based on the rotor's torque
        static float TorsoTwistMaxSpeed = 60f; // maximum RPM of the torso twist rotor;

        // - Stablization / Steering

        static double SteeringSensitivity = 5; // x / 60th speed, specifies rotor/gyro RPM divided by 60, so 30 is half max power/rpm

        // - Blocks

        string CockpitName = "auto"; // auto will find the main cockpit; optional (for manually controlling)
        string RemoteControlName = "auto"; // auto will find a main remote control; optional (for remote controlling)

        string IntegrityLCDName = "Mech Integrity"; // based on the Name of the block
        string StatusLCDName = "Mech Status"; // based on the Name of the block

        bool UseCockpitLCDs = true; // should cockpits show the leds instead?
        int IntegrityLEDNumber = 1; // starting at one, if the cockpit has more than one screen you can change it here
        int StatusLEDNumber = 3; // set to zero to disable

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
        public const double DefaultQuadOffsets = 0d;
        #endregion

        // Diagnostics //

        static bool debugMode = true;//false;

        double[] averageRuntimes = new double[AverageRuntimeSampleSize];
        int averageRuntimeIndex = 0;
        double maxRuntime = 0;
        int lastInstructions = 0;
        int maxInstructions = 0;

        bool force = false;
        float forcedStep = 0;

        // Variables //

        enum ThrusterMode
        {
            Override = 0,
            Hover = 1,
        }

        ScriptState state;

        double deltaOffset = 0;
        bool setupMode = false;

        public static IMyTextPanel debug = null;
        public static IMyTextPanel debug2 = null;

        public static Dictionary<int, LegGroup> legs = new Dictionary<int, LegGroup>();
        public static Dictionary<int, ArmGroup> arms = new Dictionary<int, ArmGroup>();

        List<InvalidatableSurfaceRenderer> integrityRenderers = new List<InvalidatableSurfaceRenderer>();
        List<InvalidatableSurfaceRenderer> statusRenderers = new List<InvalidatableSurfaceRenderer>();

        List<IMyGyro> steeringGyros = new List<IMyGyro>();
        List<Gyroscope> stabilizationGyros = new List<Gyroscope>();
        List<LegJoint> torsoTwistStators = new List<LegJoint>();
        List<RotorGyroscope> azimuthStators = new List<RotorGyroscope>();
        List<RotorGyroscope> elevationStators = new List<RotorGyroscope>();
        List<RotorGyroscope> rollStators = new List<RotorGyroscope>();
        List<Gyroscope> azimuthGyros = new List<Gyroscope>();
        public static List<IMyShipController> cockpits = new List<IMyShipController>();
        static bool crouched = false;
        static bool crouchOverride = false; // argument crouch
        public static bool jumping = false;
        double jumpCooldown = 0;

        public static double targetArmPitch = 0;
        public static double targetArmYaw = 0;
        public static double armPitch = 0;
        public static double armYaw = 0;

        public static double animationStepCounter = 0;

        bool thrustersEnabled = true;
        List<IMyThrust> thrusters = new List<IMyThrust>();

        Vector3 movementOverride = Vector3.Zero;
        Vector3 movement = Vector3.Zero;
        float turnOverride = 0;
        double targetTorsoTwistAngle = -1;

        double lastSetupModeTick = 0;

        static double GetUnixTime()
        {
            return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        //IMyFlightMovementBlock moveBlock;

        static void Log(params object[] messages)
        {
            if (!debugMode)
                return;
            string message = string.Join(" ", messages);
            if (debug == null)
                Singleton.Echo(message);
            else
                debug.WriteText(message + "\n", true);
            //Singleton.Echo(message);
        }
        

        /// <summary>
        /// Gets the blocks required for operation
        /// Ran at startup and on request
        /// </summary>
        void GetBlocks()
        {
            setupWarnings.Clear();
            //moveBlock = BlockFinder.GetBlocksOfType<IMyFlightMovementBlock>()[0];
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
                        torsoTwistStators.Add(new LegJoint(block));
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

            // Get stabilization gyros
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

            // Get thrusters
            thrusters.Clear(); // before you cry, BlockFinder.GetBlocksOfType checks IsSameConstructAs
            thrusters.AddRange(BlockFinder.GetBlocksOfType<IMyThrust>()
                .Select(BlockFetcher.ParseBlock)
                .Where(f => f.HasValue)
                .Select(f => f.Value)
                .Where(f => f.Type == BlockType.Thruster)
                .Select(f => f.Block as IMyThrust));

            // Get the leg groups and the blocks associated with them
            // Get the arm groups and the blocks associated with them
            var configs = legs.Select((kv) => new KeyValuePair<int, JointConfiguration>(kv.Key, kv.Value.Configuration)).ToDictionary(pair => pair.Key, pair => pair.Value);
            BlockFetcher.FetchGroups(ref legs, configs, BlockFetcher.IsForLeg, BlockFetcher.CreateLegFromType, LegConfiguration.Parse, BlockFetcher.AddToLeg);
            configs = arms.Select((kv) => new KeyValuePair<int, JointConfiguration>(kv.Key, kv.Value.Configuration)).ToDictionary(pair => pair.Key, pair => pair.Value);
            BlockFetcher.FetchGroups(ref arms, configs, BlockFetcher.IsForArm, BlockFetcher.CreateArmFromType, ArmConfiguration.Parse, BlockFetcher.AddToArm);
            //BlockFetcher.FetchLegs();
            //BlockFetcher.FetchArms();

            // Fix jump after reload
            if (crouchOverride || crouched)
                foreach (LegGroup leg in legs.Values)
                    leg.CrouchWaitTime = 1;
        }

        /// <summary>
        /// Initializes the script
        /// </summary>
        public Program()
        {
            // Initialize
            Singleton = this;
            state = new ScriptState();
            Load();

            // Get blocks
            GetBlocks();

            // Setup loop
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Load()
        {
            state.Parse(Storage ?? "");
        }

        /// <summary>
        /// Saves the current state
        /// </summary>
        public void Save()
        {
            Storage = state.Serialize();
        }

        struct Warning
        {
            public string Title;
            public string Info;
        }

        static List<Warning> setupWarnings = new List<Warning>();

        static void StaticWarn(string title, string info)
        {
            if (!setupWarnings.Any(w => w.Title == title))
                setupWarnings.Add(new Warning()
                {
                    Title = title,
                    Info = info
                });
        }

        void Warn(string title, string info)
        {
            Echo($"[Color=#dcf71600]Warning: {title}[/Color]");
            Echo($"[Color=#c8e02d00]{info}[/Color]\n");
        }

        float TryParseFloat(string str)
        {
            float result;
            bool parsed = float.TryParse(str, out result);
            return result;
        }

        float ParseFloatArgument(float current, string str)
        {
            if (str.StartsWith("+"))
                return TryParseFloat(str.Substring(1));
            if (str.StartsWith("-"))
                return -TryParseFloat(str.Substring(1));
            float value = TryParseFloat(str);
            return value - current;
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
            maxInstructions = Math.Max(maxInstructions, lastInstructions);

            // Detailed Info - alpha red green blue
            Echo("[Color=#13ebca00]Advanced Walker Script[/Color]");
            Echo($"{legs.Count} leg group{(legs.Count != 1 ? "s" : "")}");
            Echo($"");
            Echo($"Last       Tick: {lastRuntime:f3}ms");
            Echo($"Average Tick: {averageRuntimes.Sum() / averageRuntimes.Length:f3}ms over {averageRuntimes.Length} samples");
            Echo($"Max        Tick: {maxRuntime:f3}ms");
            Echo($"Last Instructions: {lastInstructions}");
            Echo($"Last Compexity: {lastInstructions / Runtime.MaxInstructionCount * 100:f1}%");
            Echo($"Max Instructions: {maxInstructions}");
            Echo($"Max Compexity: {maxInstructions / Runtime.MaxInstructionCount * 100:f1}%\n");

            if (setupMode)
                Warn("Setup Mode Active", "Any changes will be detected, beware that the script uses a lot more resources");

            setupWarnings.ForEach(warning => Warn(warning.Title, warning.Info));

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
            if (legs.Count <= 0) // how bruh gonna *walk* without legza?
                Warn("No Legs Found!", "Failed to find any leg groups!\nNeed help setting up? Check the documentation at github.com/AfterAStorm/AdvancedWalkerScript/wiki");

            // Handle arguments
            if (!string.IsNullOrEmpty(argument))
            {
                string[] arguments = argument.ToLower().Split(' ');
                switch (arguments[0].Trim()) // Clean up argument, allow inputs
                {
                    default:
                    case "reload": // Reloads the script's blocks and configuration
                        Save();
                        Load();
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
                            movementOverride = arguments[1].Equals("toggle") ? (movementOverride == Vector3.Forward ? Vector3.Zero : Vector3.Forward) : arguments[1].Equals("back") ? Vector3.Backward : Vector3.Forward;
                        else
                            movementOverride = Vector3.Forward;
                        break;
                    case "march":
                        int direction = arguments.Length > 1 ? 1 : -1;
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
                            forcedStep += float.Parse(arguments[1]);//ParseFloatArgument(forcedStep, arguments[1]);
                            forcedStep %= 4;
                        }
                        break;

                    case "turn":
                        if (arguments.Length > 1)
                            turnOverride = MathHelper.Clamp(turnOverride + ParseFloatArgument(turnOverride, arguments[1]), -1, 1);
                        else
                            turnOverride = 0;
                        break;

                    case "setup":
                        setupMode = !setupMode;
                        lastSetupModeTick = GetUnixTime();
                        break;

                    case "debug":
                        debugMode = !debugMode;
                        break;

                    // thrusters //
                    case "thrusters":
                        if (arguments.Length > 1)
                            thrustersEnabled = arguments[1].Equals("on");
                        else
                            thrustersEnabled = !thrustersEnabled;
                        break;
                    case "hover":
                        if (arguments.Length > 1)
                            ThrusterBehavior = !arguments[1].Equals("toggle")
                                ? (arguments[1].Equals("on") ? ThrusterMode.Hover : ThrusterMode.Override)
                                : ((ThrusterMode)(((int)ThrusterBehavior + 1) % 2));
                        else
                            ThrusterBehavior = (ThrusterMode)(((int)ThrusterBehavior + 1) % 2);
                        break;
                    // set methods //
                    case "speed":
                        WalkCycleSpeed += ParseFloatArgument(WalkCycleSpeed, arguments[1]);
                        break;

                    case "lean":
                        StandingLean += ParseFloatArgument((float)StandingLean, arguments[1]);
                        AccelerationLean = StandingLean;
                        break;

                    case "standinglean":
                    case "standlean":
                        StandingLean += ParseFloatArgument((float)StandingLean, arguments[1]);
                        break;

                    case "accelerationlean":
                    case "accellean":
                        AccelerationLean += ParseFloatArgument((float)AccelerationLean, arguments[1]);
                        break;

                    case "steplength":
                        double stepLength = (double)TryParseFloat(arguments[1]);
                        foreach (LegGroup g in legs.Values)
                            g.Configuration.StepLength = stepLength;
                        break;

                    case "stepheight":
                        double stepHeight = (double)TryParseFloat(arguments[1]);
                        foreach (LegGroup g in legs.Values)
                            g.Configuration.StepHeight = stepHeight;
                        break;

                    case "autohalt":
                        if (arguments.Length > 1)
                            AutoHalt = argument[1].Equals("on") || argument[1].Equals("true");
                        else
                            AutoHalt = !AutoHalt;
                        break;

                    case "twist":
                        targetTorsoTwistAngle = arguments.Length > 1 ? TryParseFloat(arguments[1]) : 0;
                        targetTorsoTwistAngle = targetTorsoTwistAngle.Modulo(360);
                        break;

                    case "arm":
                        armPitch = 0;
                        armYaw = 0;
                        foreach (var arm in arms.Values)
                            arm.ToZero();
                        //armRoll = 0;
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

            if (setupMode)
            {
                if (GetUnixTime() - lastSetupModeTick > .1d)
                {
                    lastSetupModeTick = GetUnixTime();
                    Save();
                    Load();
                    GetBlocks();
                }
            }

            debug?.WriteText(""); // clear
            Log("MAIN LOOP");
            /*Log($"waypoint: {(moveBlock.CurrentWaypoint != null ? moveBlock.CurrentWaypoint.RelativeMatrix.Translation.ToString() : "no waypoint")}");
            if (moveBlock.CurrentWaypoint != null)
            {
                double distance = (moveBlock.WorldMatrix.Translation - moveBlock.CurrentWaypoint.Matrix.Translation).Length();
                Log($"distance: {distance}(m?)");
                if (distance < 6)
                {
                    movementOverride = Vector3.Zero;
                }
                else
                    movementOverride = Vector3.Forward;
            }
            else
                movementOverride = Vector3.Zero;*/
            // Screens
            if (integrityRenderers.Count > 0)
            {
                /*integrityRenderers.Concat(statusRenderers).ToList().ForEach(r => r.Invalidate());
                integrityRenderers.Concat(statusRenderers).ToList().ForEach(r => r.Render());*/
            }

            // Get delta
            double delta = Runtime.TimeSinceLastRun.TotalMilliseconds / 1000d + deltaOffset;
            deltaOffset = 0;

            // Get controllers
            IMyShipController controller = cockpits.Find((pit) => pit.IsUnderControl);
            IMyShipController anyController = controller ?? (cockpits.Count > 0 ? cockpits[0] : null);

            Vector3 moveInput = Vector3.IsZero(movementOverride) ? Vector3.Clamp((controller?.MoveIndicator ?? Vector3.Zero), Vector3.MinusOne, Vector3.One) : movementOverride;
            Vector2 rotationInput = controller?.RotationIndicator ?? Vector2.Zero; // X is -pitch, Y is yaw // Mouse
            float rollInput = controller?.RollIndicator ?? 0f; // left is -, right is + (infered) // Q + E

            float turnValue = turnOverride != 0 ? turnOverride : (ReverseTurnControls ? moveInput.X : rollInput);
            HandleStabilization(turnValue);

            armPitch = -rotationInput.X;
            armYaw = rotationInput.Y;
            HandleTorsoTwist(rotationInput.Y);

            Log($"turnValue: {turnValue}");
            Log($"azimuthStators: {azimuthStators.Count}");
            Log($"elevationStators: {elevationStators.Count}");
            Log($"rollStators: {rollStators.Count}");

            Log($"thrusters: {thrusters.Count} : {ThrusterBehavior}");

            foreach (IMyThrust thruster in thrusters)
            {
                thruster.ThrustOverridePercentage = (moveInput.Y > 0 && ThrusterBehavior == ThrusterMode.Override) ? 1 : 0;
                // users decide //thruster.Enabled = thrustersEnabled && (moveInput.Y > 0 || ThrusterBehavior == ThrusterMode.Hover);
            }

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

            if (Math.Abs(movementDirection.X) < .3 && Math.Abs(movement.X) < .3)
                movement.X = 0;
            if (Math.Abs(movementDirection.Z) < .3 && Math.Abs(movement.Z) < .3)
                movement.Z = 0;

            Log(moveInput.ToString());
            Log(rotationInput.ToString());
            Log(movement.ToString());

            double originalDelta = delta;
            Log($"Delta: {delta}");
            //delta *= -movement.Z; // negative because -Z is forwards!
            //Log($"After delta: {delta}");

            Vector3 movementVec = new Vector3(movement.X, turnValue, -movement.Z);
            Vector3 movementDelta = movementVec * new Vector3((float)delta);
            Log($"Movement Delta: {movementDelta} {movementDelta.Length()}");

            if (force)
            {
                foreach (LegGroup leg in legs.Values)
                {
                    leg.Animation = Animation.Force;
                    leg.AnimationStep = forcedStep;
                    leg.Update(new Vector3(0, 0, .01), movementVec, 0);
                }
                return;
            }

            if (Math.Abs((movementDelta * new Vector3(1, 0, 1)).Length()) <= 0.00001)
                foreach (LegGroup leg in legs.Values)
                    leg.Animation = turning ? (!crouched ? Animation.Turn : Animation.CrouchTurn) : !crouched ? Animation.Idle : Animation.Crouch;
            else
            {
                foreach (LegGroup leg in legs.Values)
                {
                    bool wasIdle = leg.Animation.IsIdle();
                    leg.Animation = !crouched ? Animation.Walk : Animation.CrouchWalk;
                    /*if (wasIdle && !stepEndedOn0)
                    {
                        Log("AAAAAAAAAAAAAAAAAAAAA\nAAAAAAAAA\nAAAAAAAAAAA");
                        leg.Update(2, 2);
                        did = true;
                    }*/
                }
            }

            double animationStepMovement = (Math.Abs(movementDelta.Z) <= .001 ? movementDelta.Length() : movementDelta.Z) * WalkCycleSpeed;

            animationStepCounter += animationStepMovement;
            //animationStep %= 4;
            if (animationStepMovement == 0)
            {
                // 0
                // 1
                // 2
                // 3
                // 3.99
                if (Math.Abs(2 - animationStepCounter % 4) <= 1)
                    animationStepCounter = 2;
                else
                    animationStepCounter = 0;
            }

            //animationStep = forcedStep;
            Log("step:" + animationStepCounter);
            foreach (LegGroup leg in legs.Values)
                leg.Update(movementDelta, movementVec, originalDelta);

            Log($"arms: {arms.Count}");
            foreach (ArmGroup arm in arms.Values)
                arm.Update();
            lastInstructions = Runtime.CurrentInstructionCount;
        }
    }
}
