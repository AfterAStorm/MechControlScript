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

        public class Joint
        {
            public IMyMotorStator Stator;

            public double Minimum => Stator.LowerLimitDeg;
            public double Maximum => Stator.UpperLimitDeg;
            public bool IsHinge => Stator.BlockDefinition.SubtypeName.Contains("Hinge");
            public bool IsRotor => !IsHinge;

            public Joint(FetchedBlock block)
            {
                Stator = block.Block as IMyMotorStator;
            }

            public double ClampDegrees(double angle)
            {
                double current = Stator.Angle.ToDegrees();
                if (IsHinge)
                    return angle.ClampHinge() - current; // lock between -90 to 90; aka angle = angle - current
                else
                    return (angle.Modulo(360) - current + 540).Modulo(360) - 180; // find the closest direction to the target angle; thank you https://math.stackexchange.com/a/2898118 :D*/
            }

            public float GetRPMFor(double angle)
            {
                angle = ClampDegrees(angle);

                return (float)angle.Clamp(-MaxRPM, MaxRPM);
            }

            public void SetRPM(float rotationsPerMinute)
            {
                Stator.TargetVelocityRPM = rotationsPerMinute * .9f;
            }

            public void SetAngle(double angle)
            {
                SetRPM(GetRPMFor(angle));
                //Stator.RotorLock = (Stator.Angle - ClampDegrees(angle)).Absolute() < 2d;
            }
        }

        #region # Legs

        public struct LegJointConfiguration
        {
            public static readonly LegJointConfiguration DEFAULT = new LegJointConfiguration()
            {
                Inversed = false,
                Offset = 0,
            };

            public bool Inversed;
            public double Offset;
            public double InversedMultiplier => Inversed ? -1 : 1;
        }

        public class LegJoint : Joint
        {
            public LegJointConfiguration Configuration;

            public LegJoint(FetchedBlock block) : base(block)
            {
                Configuration = new LegJointConfiguration()
                {
                    Inversed = block.Inverted,
                    Offset = 0
                };
            }
        }

        public class RotorGyroscope : LegJoint
        {
            public RotorGyroscope(FetchedBlock block) : base(block)
            {
                foreach (IMyGyro gyro in BlockFinder.GetBlocksOfType<IMyGyro>((gyro) => gyro.CubeGrid == Stator.TopGrid))
                {
                    SubGyros.Add(gyro);
                }
            }

            public List<IMyGyro> SubGyros = new List<IMyGyro>();
        }

        public class Gyroscope
        {

            public IMyGyro Gyro;
            public LegJointConfiguration Configuration;
            public BlockType GyroType;

            public Gyroscope(IMyGyro gyro, LegJointConfiguration? configuration = null)
            {
                Gyro = gyro;
                Configuration = configuration ?? LegJointConfiguration.DEFAULT;
            }

            public Gyroscope(FetchedBlock block)
            {
                GyroType = block.Type;
                Gyro = block.Block as IMyGyro;
                Configuration = new LegJointConfiguration()
                {
                    Inversed = block.Inverted,
                    Offset = 0
                };
            }

            public void SetOverrides(float pitch, float yaw, float roll) // PYR
            {
                if (!Gyro.GyroOverride)
                    Gyro.GyroOverride = true;
                Gyro.Pitch = pitch;
                Gyro.Yaw = yaw;
                Gyro.Roll = roll;
            }
        }

        #endregion

        #region # Arm

        public struct ArmJointConfiguration
        {
            public static readonly ArmJointConfiguration DEFAULT = new ArmJointConfiguration()
            {
                Inversed = false,
                Offset = 0,
                Multiplier = 1
            };

            public bool Inversed;
            public double Offset;
            public double Multiplier;
            public double InversedMultiplier => Inversed ? -1 : 1;

            public static ArmJointConfiguration Parse(FetchedBlock block)
            {
                MyIni ini = new MyIni();
                ini.TryParse(block.Block.CustomData, "Joint");
                return new ArmJointConfiguration()
                {
                    Inversed = block.Inverted,
                    Offset = ini.Get("Joint", "Offset").ToDouble(0),
                    Multiplier = ini.Get("Joint", "Multiplier").ToDouble(1)
                };
            }

            public string ToCustomDataString()
            {
                MyIni ini = new MyIni();
                ini.Set("Joint", "Offset", Offset);
                ini.SetComment("Joint", "Offset", "The starting offset");
                ini.Set("Joint", "Multiplier", Multiplier);
                ini.SetComment("Joint", "Multiplier", "A multiplier on how much movement affects this stator");

                ini.SetSectionComment("Joint", "This specific joint's settings, ONLY THIS BLOCK will be affected");
                return ini.ToString();
            }
        }

        public class ArmJoint : Joint
        {
            public ArmJointConfiguration Configuration;

            public ArmJoint(FetchedBlock block, ArmJointConfiguration config) : base(block)
            {
                Configuration = config;
            }
        }

        #endregion
    }
}
