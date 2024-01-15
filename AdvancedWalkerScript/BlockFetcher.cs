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
    partial class Program
    {

        // Regex: HR5+ turns into [h, r, 5, +], HL turns into [h, l, null, null], HLeft5+ turns into [h, left, 5, +], HipL turns into [hip, l, null, null]
        // It's a beauty for sure
        private static readonly System.Text.RegularExpressions.Regex NamePattern = new System.Text.RegularExpressions.Regex(@"^([^lLrR]*)([lr]{1}|left{1}|right{1})?([0-9]+)?([-+]{1})?$");

        public enum BlockType
        {
            // Specific
            Hip,
            Knee,
            Foot,

            // Misc
            LandingGear,
            TorsoTwist,
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

            public bool AttachToLeg;

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
                Singleton.GridTerminalSystem.GetBlocksOfType(blocks, predicate);
                return blocks;
            }
        }

        public static class BlockFetcher
        {
            static int parsedId;

            private static LegGroup CreateLegFromType(byte type)
            {
                switch (type)
                {
                    case 0:
                        return new ChickenWalkerLegGroup();
                    default:
                        throw new Exception("Leg type not implemented!");
                }
            }

            private static List<BlockType> DoesntRequireSide = new List<BlockType>()
            {
                BlockType.TorsoTwist
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
                    switch (match.Groups[1].Value)
                    {
                        case "h":
                            //case "hip":
                            if (!(block is IMyMotorStator))
                                break; // Liars!
                            blockType = BlockType.Hip;
                            break;
                        case "k":
                            //case "knee":
                            if (!(block is IMyMotorStator))
                                break; // Liars!
                            blockType = BlockType.Knee;
                            break;
                        case "f":
                            //case "fp":
                            //case "foot":
                            //case "feet":
                            if (!(block is IMyMotorStator))
                                break; // Liars!
                            blockType = BlockType.Foot;
                            break;
                        case "tt":
                            if (!(block is IMyMotorStator))
                                break; // Liars!
                            blockType = BlockType.TorsoTwist;
                            break;
                        case "mg":
                        case "lg":
                            if (!(block is IMyLandingGear))
                                break; // Liars!
                            blockType = BlockType.LandingGear;
                            break;
                    }
                    if (!blockType.HasValue)
                        continue; // invalid
                    Log($"{block.CustomName} Got block type!", blockType.Value);

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
                    Log("Past side");

                    // Parse the group it's in
                    bool parsed = int.TryParse(match.Groups[3].Value, out parsedId);
                    if (!parsed) // if it fails it might output zero anyway, i'm not sure
                        parsedId = 0;

                    // Parse the ini
                    MyIni ini = new MyIni();
                    if (!ini.TryParse(block.CustomData))
                        ini = null;

                    return new FetchedBlock()
                    {
                        Block = block,
                        Type = blockType.Value,
                        Side = side ?? BlockSide.Left,
                        Group = parsedId,
                        Inverted = match.Groups[4].Value.Equals("-"),
                        Ini = ini,

                        AttachToLeg = blockType.Value != BlockType.TorsoTwist
                    };
                }
                return null;
            }

            private static void AddToLeg(FetchedBlock block, LegGroup leg) // adds a fetched block to the leg
            {
                block.Block.CustomData = leg.Configuration.ToCustomDataString(); // set new configuration
                Log($"Block {block.Block.CustomName} as {block.Type}");
                switch (block.Type)
                {
                    case BlockType.Hip:
                    case BlockType.Knee:
                    case BlockType.Foot: // if its a joint, create it and add it appropriately
                        Joint joint = new Joint(block.Block as IMyMotorStator, new JointConfiguration()
                        {
                            Inversed = false,
                            Offset = 0
                        });
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
                        }
                        break;
                    case BlockType.LandingGear: // otherwise just add it normally
                        if (block.Side == BlockSide.Left)
                            leg.LeftGears.Add(block.Block as IMyLandingGear);
                        else
                            leg.RightGears.Add(block.Block as IMyLandingGear);
                        break;
                }
            }

            public static void GetBlocks()
            {
                List<LegGroup> newLegs = new List<LegGroup>();
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                List<FetchedBlock> reiterate = new List<FetchedBlock>();

                List<LegConfiguration> lastConfigurations = Legs.Select(leg => leg.Configuration).ToList();

                blocks.AddRange(BlockFinder.GetBlocksOfType<IMyMotorStator>());
                blocks.AddRange(BlockFinder.GetBlocksOfType<IMyLandingGear>());
                Log($"Number of blocks found: {blocks.Count}");

                // Parse the blocks, see if any configuration changes happend
                foreach (IMyTerminalBlock block in blocks)
                {
                    FetchedBlock? triedFetch = ParseBlock(block);
                    if (!triedFetch.HasValue)
                        continue;
                    FetchedBlock fetched = triedFetch.Value;
                    Log($"Parsed block {fetched.Block.CustomName}");
                    if (newLegs.IsValidIndex(fetched.Group)) // leg already exists
                    {
                        AddToLeg(fetched, newLegs[fetched.Group]);
                        continue;
                    }

                    // doesn't have a valid ini so, it will just be defaults
                    if (fetched.Ini == null)
                    {
                        reiterate.Add(fetched);
                        continue;
                    }

                    LegConfiguration lastLegConfiguration = lastConfigurations.Find(c => c.Id == fetched.Group); // try to find the last configuration
                    LegConfiguration jointConfiguration = LegConfiguration.Parse(fetched.Ini); // parse the possibly new configuration
                    jointConfiguration.Id = fetched.Group;
                    if (!lastLegConfiguration.Default && lastLegConfiguration.Equals(jointConfiguration)) // check if they are different
                    {
                        reiterate.Add(fetched);
                        continue; // the configuration isn't different, don't do anything quite yet
                    }

                    LegGroup leg = CreateLegFromType(jointConfiguration.LegType);
                    leg.Configuration = jointConfiguration;
                    newLegs.Add(leg);
                    AddToLeg(fetched, leg);
                }

                // These are blocks that got skipped
                foreach (FetchedBlock fetched in reiterate)
                {
                    if (newLegs.IsValidIndex(fetched.Group)) // leg exists
                    {
                        AddToLeg(fetched, newLegs[fetched.Group]);
                        continue;
                    }

                    // Otherwise create a new leg
                    LegConfiguration config = LegConfiguration.Parse(fetched.Ini?.ToString() ?? "");
                    config.Id = fetched.Group;
                    LegGroup leg = CreateLegFromType(config.LegType);
                    leg.Configuration = config;
                    newLegs.Add(leg);
                    AddToLeg(fetched, leg);
                }

                // Set the legs
                Legs = newLegs;
            }
        }
    }
}
