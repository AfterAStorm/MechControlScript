using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using VRageMath;
using static IngameScript.Program;

namespace IngameScript
{
    public static class IMyMotorStatorExtensions
    {
        public static bool IsSharingInertiaTensor(this IMyMotorStator stator)
        {
            return stator.GetProperty("ShareInertiaTensor").AsBool().GetValue(stator);
        }
    }

    public static class AnimationEnumExtensions
    {
        internal static bool IsIdle(this Program.Animation animation) => animation == Program.Animation.Idle || animation == Program.Animation.Crouch;
        internal static bool IsWalk(this Program.Animation animation) => animation == Program.Animation.Walk || animation == Program.Animation.CrouchWalk;
        internal static bool IsCrouch(this Program.Animation animation) => animation == Program.Animation.Crouch || animation == Program.Animation.CrouchWalk || animation == Program.Animation.CrouchTurn;
        internal static bool IsTurn(this Program.Animation animation) => animation == Program.Animation.Turn || animation == Program.Animation.CrouchTurn;
    }

    partial class Program
    {
        internal static string ToInitial(BlockType type)
        {
            switch (type)
            {
                case BlockType.Hip:
                    return "H";
                case BlockType.Knee:
                    return "K";
                case BlockType.Foot:
                    return "F";
                case BlockType.Quad:
                    return "Q";
            }
            throw new Exception("Invalid block type");
        }

        internal static string ToName(BlockType type)
        {
            switch (type)
            {
                case BlockType.Hip:
                    return "Hip";
                case BlockType.Knee:
                    return "Knee";
                case BlockType.Foot:
                    return "Foot";
                case BlockType.Quad:
                    return "Quad";
            }
            throw new Exception("Invalid block type");
        }

        internal static string ToInitial(BlockSide side)
        {
            switch (side)
            {
                case BlockSide.Left:
                    return "L";
                case BlockSide.Right:
                    return "R";
            }
            throw new Exception("Invalid block side");
        }

        internal static string ToName(BlockSide side)
        {
            switch (side)
            {
                case BlockSide.Left:
                    return "Left";
                case BlockSide.Right:
                    return "Right";
            }
            throw new Exception("Invalid block side");
        }
    }

    public static class AngleConversions
    {
        /// <summary>
        /// Clamps a value, shorthand for AngleConversions.Modulo(x, divisor)
        /// C#'s % operator is remainder, so this supports negative numbers
        /// </summary>
        /// <param name="x"></param>
        /// <param name="divisor"></param>
        /// <returns></returns>
        public static double Modulo(this double x, double divisor)
        {
            double r = x % divisor;
            return (r < 0 ? r + divisor : r);//(x % divisor + divisor) % divisor;
        }

        /// <summary>
        /// Clamps a value, shorthand for MathHelper.Clamp(x, min, max)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Clamp(this double x, double min, double max)
        {
            return MathHelper.Clamp(x, min, max);
        }

        /// <summary>
        /// Converts radians to degrees
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static double ToDegrees(this double radians)
        {
            return MathHelper.ToDegrees(radians);
        }

        /// <summary>
        /// Converts radians to degrees
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static float ToDegrees(this float radians)
        {
            return MathHelper.ToDegrees(radians);
        }

        /// <summary>
        /// Converts degrees to radians
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static double ToRadians(this double degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        /// <summary>
        /// Converts degrees to radians
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static float ToRadians(this float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        /// <summary>
        /// Absolutes the value, shorthand for Math.Abs(x)
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double Absolute(this double x) => Math.Abs(x);

        /// <summary>
        /// Absolutes the value, shorthand for Math.Abs(x)
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static float Absolute(this float x) => Math.Abs(x);

        public static double ClampHinge(this double x)
        {
            return MathHelper.Clamp(x, -90, 90);
        }
    }
}
