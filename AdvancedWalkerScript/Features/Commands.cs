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
        public void HandleCommands(string argument)
        {
            string[] arguments = argument.ToLower().Split(' ');
            switch (arguments[0].Trim()) // Clean up argument, allow inputs
            {
                default:
                case "reload": // Reloads the script's blocks and configuration
                    Reload();
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
                    if ((movementOverride * Vector3.Forward).Z != 0)
                        movementOverride *= new Vector3(1, 1, 0);
                    else
                        movementOverride = arguments.Length > 1 && arguments[1].Equals("back") ? Vector3.Backward : Vector3.Forward;
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

                case "limp":
                    limp = !limp;
                    foreach (var group in legs)
                        group.Value.AllBlocks.ForEach(b => { if (b is IMyFunctionalBlock) (b as IMyFunctionalBlock).Enabled = !limp; });
                    break;

                // setup //
                case "setup":
                    setupMode = !setupMode;
                    lastSetupModeTick = GetUnixTime();
                    break;

                case "autotag":
                    TryAutoTag();
                    break;
                case "autorename":
                    if (arguments.Length < 2)
                        AutoRenameBlocks("{tag}");
                    else
                        AutoRenameBlocks(string.Join(" ", argument.Split(' ').Skip(1))); // we have to split again b/c the arguments array is all lowercased
                    break;
                case "autotype":
                    if (arguments.Length < 2)
                        break;
                    AutoRetype((int)TryParseFloat(arguments[1]));
                    break;

                case "calibrate":
                    calibrating = true;
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
                    //double stepLength = (double)TryParseFloat(arguments[1]);
                    foreach (LegGroup g in legs.Values)
                        g.Configuration.StepLength = ParseFloatArgument((float)g.Configuration.StepLength, arguments[1]);
                    break;

                case "stepheight":
                    //double stepHeight = (double)TryParseFloat(arguments[1]);
                    foreach (LegGroup g in legs.Values)
                        g.Configuration.StepHeight = ParseFloatArgument((float)g.Configuration.StepHeight, arguments[1]);
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

                case "armcontrol":
                    if (arguments.Length > 1)
                    {
                        if (arguments[1].Equals("on"))
                        {
                            armsEnabled = true;
                            break;
                        }
                        else if (arguments[1].Equals("off"))
                        {
                            armsEnabled = false;
                            break;
                        }
                    }
                    armsEnabled = !armsEnabled;
                    break;

                case "arm":
                    armPitch = 0;
                    armYaw = 0;
                    foreach (var arm in arms.Values)
                        arm.ToZero();
                    //armRoll = 0;
                    break;
            }
        }
    }
}
