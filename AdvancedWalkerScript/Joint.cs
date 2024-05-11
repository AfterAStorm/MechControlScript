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
        public struct JointConfiguration
        {
            public static readonly JointConfiguration DEFAULT = new JointConfiguration()
            {
                Inversed = false,
                Offset = 0,
            };

            public bool Inversed;
            public double Offset;
            public double InversedMultiplier => Inversed ? -1 : 1;
        }

        public class Joint
        {

            public IMyMotorStator Stator;
            public JointConfiguration Configuration;

            public double Minimum => Stator.LowerLimitDeg;
            public double Maximum => Stator.UpperLimitDeg;
            public bool IsHinge => Stator.BlockDefinition.SubtypeName.Contains("Hinge");
            public bool IsRotor => !IsHinge;

            public Joint(IMyMotorStator stator, JointConfiguration? configuration = null)
            {
                Stator = stator;
                Configuration = configuration ?? JointConfiguration.DEFAULT;
            }

            public Joint(FetchedBlock block)
            {
                Stator = block.Block as IMyMotorStator;
                Configuration = new JointConfiguration()
                {
                    Inversed = block.Inverted,
                    Offset = 0
                };
            }

            public float GetRPMFor(double angle)
            {
                double current = Stator.Angle.ToDegrees();
                if (IsHinge)
                    angle = angle.ClampHinge() - current; // lock between -90 to 90; aka angle = angle - current
                else
                    angle = (angle.Modulo(360) - current + 540).Modulo(360) - 180; // find the closest direction to the target angle; thank you https://math.stackexchange.com/a/2898118 :D

                return (float)angle.Clamp(-MaxRPM, MaxRPM);
            }

            public void SetRPM(float rotationsPerMinute)
            {
                Stator.TargetVelocityRPM = rotationsPerMinute;
            }

            public void SetAngle(double angle)
            {
                Stator.TargetVelocityRPM = GetRPMFor(angle);
            }

        }

        public class RotorGyroscope : Joint
        public class Gyroscope
        {

            public IMyGyro Gyro;
            public JointConfiguration Configuration;
            public BlockType GyroType;

            public Gyroscope(IMyGyro gyro, JointConfiguration? configuration = null)
            {
                Gyro = gyro;
                Configuration = configuration ?? JointConfiguration.DEFAULT;
            }

            public Gyroscope(FetchedBlock block)
            {
                GyroType = block.Type;
                Gyro = block.Block as IMyGyro;
                Configuration = new JointConfiguration()
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
    }
}
