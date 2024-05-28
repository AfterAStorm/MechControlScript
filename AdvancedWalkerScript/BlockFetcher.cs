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
using VRageRender;
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program
    {

        // Regex: HR5+ turns into [h, r, 5, +], HL turns into [h, l, null, null], HLeft5+ turns into [h, left, 5, +], HipL turns into [hip, l, null, null]
        // It's a beauty for sure
        private static readonly System.Text.RegularExpressions.Regex NamePattern = new System.Text.RegularExpressions.Regex(@"^([^lr]*)([lr]{1}|left{1}|right{1})?([0-9]+)?([-+]{1})?$");

        public enum BlockType
        {
            // Leg
            Hip,
            Knee,
            Foot,
            Quad,

            // Arm
            Pitch,
            Yaw,
            Roll,
            Magnet, // arm landing gear

            // Misc
            LandingGear,
            TorsoTwist,
            GyroscopeAzimuth, // rotor or gyroscope, yaw
            GyroscopeElevation, // rotor or gyroscope, pitch
            GyroscopeRoll, // rotor or gyroscope, roll
            GyroscopeStabilization,

            Camera
        }

        public enum BlockSide
        {
            Left,
            Right,
        }

        public struct FetchedBlock
        {
            public IMyTerminalBlock Block;

            public BlockType Type;
            public BlockSide Side;
            public int Group;
            public bool Inverted;
            public string Name;

            //public bool AttachToLeg;

            public MyIni Ini;
        }

        public static class BlockFinder
        {
            /// <summary>
            /// Returns a list of blocks instead of changing a list in parameters
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public static List<T> GetBlocksOfType<T>(Func<T, bool> predicate = null) where T : class
            {
                List<T> blocks = new List<T>();
                Singleton.GridTerminalSystem.GetBlocksOfType(blocks, block => (block as IMyTerminalBlock).IsSameConstructAs(Singleton.Me) && (predicate == null || predicate(block)));
                return blocks;
            }
        }

        public static class BlockFetcher
        {
            static int parsedId;

            private static LegGroup CreateLegFromType(int type)
            {
                switch (type)
                {
                    case 0:
                    case 1:
                        return new ChickenWalkerLegGroup();
                    case 2:
                        return new HumanoidLegGroup();
                    case 3:
                        return new SpideroidLegGroup();
                    case 4:
                        return new CrabLegGroup();
                    case 5:
                        return new DigitigradeLegGroup();
                    case 9:
                        return new TestLegGroup();
                    default:
                        throw new Exception($"Leg type {type} not implemented!");
                }
            }

            private static ArmGroup CreateArmFromType(int type)
            {
                return new ArmGroup();
            }

            private static readonly List<BlockType> DoesntRequireSide = new List<BlockType>()
            {
                BlockType.Pitch,
                BlockType.Yaw,
                BlockType.Roll,
                BlockType.Magnet, // arm landing gear

                BlockType.TorsoTwist,

                BlockType.GyroscopeAzimuth,
                BlockType.GyroscopeElevation,
                BlockType.GyroscopeRoll,
                BlockType.GyroscopeStabilization
            };

            public static FetchedBlock? ParseBlock(IMyTerminalBlock block)
            {
                // Check each segment of the name: "Left Leg - HL" is ["Left", "Leg", "-", "HL"]
                foreach (var segment in block.CustomName.ToLower().Split(' '))
                {
                    var match = NamePattern.Match(segment);
                    if (!match.Success)
                        continue; // invalid segment

                    // Parse the type
                    BlockType? blockType = null;
                    switch (match.Groups[1].Value.Replace("+", "").Replace("-", "")) // the replace is beacuse i cannot regex for some reason D:
                    {
                        /* Leg */
                        case "h":
                            //case "hip":
                            if (!(block is IMyMotorStator) && !(block is IMyPistonBase))
                                break; // Liars!
                            blockType = BlockType.Hip;
                            break;
                        case "k":
                            //case "knee":
                            if (!(block is IMyMotorStator) && !(block is IMyPistonBase))
                                break; // Liars!
                            blockType = BlockType.Knee;
                            break;
                        case "f":
                            //case "fp":
                            //case "foot":
                            //case "feet":
                            if (!(block is IMyMotorStator) && !(block is IMyPistonBase))
                                break; // Liars!
                            blockType = BlockType.Foot;
                            break;
                        case "q":
                            if (!(block is IMyMotorStator))
                                break; // Liars!
                            blockType = BlockType.Quad;
                            break;
                        /* Arm */
                        case "ap":
                            if (!(block is IMyMotorStator))
                                break; // Liars!
                            blockType = BlockType.Pitch;
                            break;
                        case "ay":
                            if (!(block is IMyMotorStator))
                                break; // Liars!
                            blockType = BlockType.Yaw;
                            break;
                        case "ar":
                            if (!(block is IMyMotorStator))
                                break; // Liars!
                            blockType = BlockType.Roll;
                            break;
                        case "alg":
                        case "amg":
                            if (!(block is IMyLandingGear))
                                break; // Liars!
                            blockType = BlockType.Magnet;
                            break;
                        /* Other */
                        case "tt":
                            if (!(block is IMyMotorStator))
                                break; // Liars!
                            blockType = BlockType.TorsoTwist;
                            break;
                        case "gy": // y for yaw
                            //case "ga":
                            if (!(block is IMyMotorStator) && !(block is IMyGyro))
                                break; // Liars!
                            blockType = BlockType.GyroscopeAzimuth;
                            break;
                        case "gp": // p for pitch
                            //case "ge":
                            if (!(block is IMyMotorStator) && !(block is IMyGyro))
                                break; // Liars!
                            blockType = BlockType.GyroscopeElevation;
                            break;
                        case "g": // we are technically looking for "GR" but we have to check for the r (becomes BlockSide) later (in Program) because the R will get eaten by the regex
                            if (!(block is IMyMotorStator) && !(block is IMyGyro))
                                break; // Liars!
                            blockType = BlockType.GyroscopeRoll;
                            break;
                        case "gg": // g for gyro
                            if (!(block is IMyGyro))
                                break; // Liars!
                            blockType = BlockType.GyroscopeStabilization;
                            break;
                        case "mg":
                        case "lg":
                            if (!(block is IMyLandingGear))
                                break; // Liars!
                            blockType = BlockType.LandingGear;
                            break;
                        case "c":
                            blockType = BlockType.Camera;
                            break;
                    }
                    if (!blockType.HasValue)
                        continue; // invalid
                    //Log($"{block.CustomName} Got block type!", blockType.Value);

                    // Parse the side
                    BlockSide? side = null;
                    switch (match.Groups[2].Value)
                    {
                        case "l":
                        case "left":
                            side = BlockSide.Left;
                            break;
                        case "r":
                        case "right":
                            side = BlockSide.Right;
                            break;
                    }
                    if (!side.HasValue && !DoesntRequireSide.Contains(blockType.Value))
                        continue; // invalid side
                    //Log("Past side");

                    // Parse the group it's in
                    bool parsed = int.TryParse(match.Groups[3].Value, out parsedId);
                    if (!parsed) // if it fails it might output zero anyway, i'm not sure
                        parsedId = 1;

                    // Parse the ini
                    MyIni ini = new MyIni();
                    if (!ini.TryParse(block.CustomData))
                        ini = null;

                    // require a + or -, guh
                    if (!(match.Groups[4].Value.Equals("-") || match.Groups[1].Value.EndsWith("-")) && !(match.Groups[4].Value.Equals("+") || match.Groups[1].Value.EndsWith("+")))
                        continue;

                    return new FetchedBlock()
                    {
                        Block = block,
                        Type = blockType.Value,
                        Side = side ?? BlockSide.Left,
                        Group = parsedId,
                        Inverted = match.Groups[4].Value.Equals("-") || match.Groups[1].Value.EndsWith("-"),
                        Ini = ini,

                        Name = match.Groups[0].Value

                        //AttachToLeg = blockType.Value != BlockType.TorsoTwist
                    };
                }
                return null;
            }

            static bool IsForArm(FetchedBlock block)
            {
                switch (block.Type)
                {
                    case BlockType.Yaw:
                    case BlockType.Pitch:
                        return true;
                    default:
                        return false;
                }
            }

            private static void AddToLeg(FetchedBlock block, LegGroup leg) // adds a fetched block to the leg
            {
                Log($"Block {block.Block.CustomName} as {block.Type}");
                switch (block.Type)
                {
                    case BlockType.Hip:
                    case BlockType.Knee:
                    case BlockType.Foot: // if its a joint, create it and add it appropriately
                    case BlockType.Quad:
                        if (block.Block is IMyPistonBase)
                        {
                            if (block.Side == BlockSide.Left)
                                leg.LeftPistons.Add(block);
                            else
                                leg.RightPistons.Add(block);
                            break;
                        }

                        LegJoint joint = new LegJoint(block);
                        switch (block.Type)
                        {
                            case BlockType.Hip:
                                if (block.Side == BlockSide.Left)
                                    leg.LeftHipStators.Add(joint);
                                else
                                    leg.RightHipStators.Add(joint);
                                break;
                            case BlockType.Knee:
                                if (block.Side == BlockSide.Left)
                                    leg.LeftKneeStators.Add(joint);
                                else
                                    leg.RightKneeStators.Add(joint);
                                break;
                            case BlockType.Foot:
                                if (block.Side == BlockSide.Left)
                                    leg.LeftFootStators.Add(joint);
                                else
                                    leg.RightFootStators.Add(joint);
                                break;
                            case BlockType.Quad:
                                if (block.Side == BlockSide.Left)
                                    leg.LeftQuadStators.Add(joint);
                                else
                                    leg.RightQuadStators.Add(joint);
                                break;
                        }
                        break;
                    case BlockType.LandingGear: // otherwise just add it normally
                        if (block.Side == BlockSide.Left)
                            leg.LeftGears.Add(block.Block as IMyLandingGear);
                        else
                            leg.RightGears.Add(block.Block as IMyLandingGear);
                        break;
                    default:
                        return;
                }
                block.Block.CustomData = leg.Configuration.ToCustomDataString(); // set new configuration
            }

            public static void FetchLegs()
            {
                Dictionary<int, LegGroup> newLegs = new Dictionary<int, LegGroup>();
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                List<FetchedBlock> reiterate = new List<FetchedBlock>();

                Dictionary<int, LegConfiguration> lastConfigurations = new Dictionary<int, LegConfiguration>();
                foreach (var kv in legs)
                    lastConfigurations.Add(kv.Key, kv.Value.Configuration);

                blocks.AddRange(BlockFinder.GetBlocksOfType<IMyMotorStator>());
                blocks.AddRange(BlockFinder.GetBlocksOfType<IMyPistonBase>());
                blocks.AddRange(BlockFinder.GetBlocksOfType<IMyLandingGear>());
                Log($"Number of blocks found: {blocks.Count}");

                // Parse the blocks, see if any configuration changes happend
                foreach (IMyTerminalBlock block in blocks)
                {
                    FetchedBlock? triedFetch = ParseBlock(block);
                    if (!triedFetch.HasValue)
                        continue;
                    FetchedBlock fetched = triedFetch.Value;
                    if (IsForArm(fetched))
                        continue;
                    Log($"Parsed block {fetched.Block.CustomName}");
                    if (newLegs.ContainsKey(fetched.Group)) // leg already exists
                    {
                        AddToLeg(fetched, newLegs[fetched.Group]);
                        continue;
                    }

                    // doesn't have a valid ini so, it will just be defaults
                    if (fetched.Ini == null || !fetched.Ini.ContainsSection("Leg"))
                    {
                        reiterate.Add(fetched);
                        continue;
                    }

                    LegConfiguration lastLegConfiguration = lastConfigurations.GetValueOrDefault(fetched.Group); // try to find the last configuration
                    LegConfiguration jointConfiguration = LegConfiguration.Parse(fetched.Ini); // parse the possibly new configuration
                    jointConfiguration.Id = fetched.Group;
                    if (!lastLegConfiguration.Default && lastLegConfiguration.Equals(jointConfiguration)) // check if they are different
                    {
                        reiterate.Add(fetched);
                        continue; // the configuration isn't different, don't do anything quite yet
                    }

                    LegGroup leg = CreateLegFromType(jointConfiguration.LegType);
                    leg.Configuration = jointConfiguration;
                    newLegs.Add(fetched.Group, leg);
                    AddToLeg(fetched, leg);
                }

                // These are blocks that got skipped
                foreach (FetchedBlock fetched in reiterate)
                {
                    if (newLegs.ContainsKey(fetched.Group)) // leg exists
                    {
                        AddToLeg(fetched, newLegs[fetched.Group]);
                        continue;
                    }

                    // Otherwise create a new leg
                    LegConfiguration config = LegConfiguration.Parse(fetched.Ini?.ToString() ?? "");
                    config.Id = fetched.Group;
                    LegGroup leg = CreateLegFromType(config.LegType);
                    leg.Configuration = config;
                    newLegs.Add(fetched.Group, leg);
                    AddToLeg(fetched, leg);
                }

                // Set the legs
                legs = newLegs;
            }

            private static void AddToArm(FetchedBlock block, ArmGroup arm)
            {
                ArmJointConfiguration jointConfig = ArmJointConfiguration.Parse(block);
                Log($"block: {block.Block.CustomData}");
                Log($"offset: {jointConfig.Offset}");

                switch (block.Type)
                {
                    case BlockType.Pitch:
                        arm.PitchJoints.Add(new ArmJoint(block, jointConfig));
                        break;
                    case BlockType.Yaw:
                        arm.YawJoints.Add(new ArmJoint(block, jointConfig));
                        break;
                    /*case BlockType.Roll:
                        arm.RollJoints.Add(new ArmJoint(block, jointConfig));
                        break;*/
                    case BlockType.Magnet:
                        arm.Magnets.Add(block.Block as IMyLandingGear);
                        break;
                    default:
                        return;
                }
                block.Block.CustomData = arm.Configuration.ToCustomDataString() + "" + jointConfig.ToCustomDataString();
            }

            public static void FetchArms()
            {
                Dictionary<int, ArmGroup> newArms = new Dictionary<int, ArmGroup>();
                List<IMyTerminalBlock> blocks = BlockFinder.GetBlocksOfType<IMyTerminalBlock>(block => block is IMyMotorStator || block is IMyLandingGear);
                List<FetchedBlock> reiterate = new List<FetchedBlock>();

                Dictionary<int, ArmConfiguration> lastConfigurations = new Dictionary<int, ArmConfiguration>();
                foreach (var kv in arms)
                    lastConfigurations.Add(kv.Key, kv.Value.Configuration);
                
                foreach (var block in blocks)
                {
                    FetchedBlock? triedFetch = ParseBlock(block);
                    if (!triedFetch.HasValue)
                        continue;
                    FetchedBlock fetchedBlock = triedFetch.Value;
                    if (!IsForArm(fetchedBlock))
                        continue;
                    if (newArms.ContainsKey(fetchedBlock.Group))
                    {
                        AddToArm(fetchedBlock, newArms[fetchedBlock.Group]);
                        continue;
                    }

                    if (fetchedBlock.Ini == null || !fetchedBlock.Ini.ContainsSection("Arm"))
                    {
                        reiterate.Add(fetchedBlock);
                        continue;
                    }

                    ArmConfiguration lastArmConfiguration = lastConfigurations.GetValueOrDefault(fetchedBlock.Group);
                    ArmConfiguration armConfiguration = ArmConfiguration.Parse(fetchedBlock.Ini);
                    armConfiguration.Id = fetchedBlock.Group;
                    if (!lastArmConfiguration.Default && lastArmConfiguration.GetHashCode().Equals(armConfiguration.GetHashCode()))
                    {
                        reiterate.Add(fetchedBlock);
                        continue;
                    }

                    ArmGroup arm = CreateArmFromType(1);
                    arm.Configuration = armConfiguration;
                    newArms.Add(fetchedBlock.Group, arm);
                    AddToArm(fetchedBlock, arm);
                }

                foreach (var block in reiterate)
                {
                    if (newArms.ContainsKey(block.Group))
                    {
                        AddToArm(block, newArms[block.Group]);
                        continue;
                    }

                    ArmConfiguration config = ArmConfiguration.Parse(block.Ini?.ToString() ?? "");
                    config.Id = block.Group;
                    ArmGroup arm = CreateArmFromType(0);
                    arm.Configuration = config;
                    newArms.Add(block.Group, arm);
                    AddToArm(block, arm);
                }

                arms = newArms;
            }
        }
    }
}
