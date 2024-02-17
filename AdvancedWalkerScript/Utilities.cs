using System;
using VRageMath;

namespace IngameScript
{
    public partial class Program
    {
        public static class InverseKinematics2
        {
            public static LegAngles CalculateLeg(double thighLength, double calfLength, double x, double y)
            {
                LegAngles angles = new LegAngles();

                /*
                // We assume the origin is 0, 0
                double distance = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
                // ^ gets the hypotenuse

                // shamelessly borrowed from https://opentextbooks.clemson.edu/wangrobotics/chapter/inverse-kinematics/
                // the equations anyway
                // y and x are inversed, so i inversed them in each place they are used
                double diameter = Math.Atan(x / y);
                double topDiameter = Math.Acos(
                    (Math.Pow(thighLength, 2) + Math.Pow(distance, 2) - Math.Pow(calfLength, 2))
                    /
                    (2 * thighLength * distance)
                );
                angles.HipDegrees = (diameter - topDiameter).ToDegrees();

                double diameter2 = Math.Acos(
                    (Math.Pow(thighLength, 2) + Math.Pow(calfLength, 2) - Math.Pow(distance, 2))
                    /
                    (2 * thighLength * calfLength)
                );
                angles.KneeDegrees = (Math.PI - diameter2).ToDegrees();
                angles.FeetDegrees = (180d - angles.HipDegrees.Absolute360() - angles.KneeDegrees.Absolute360());
                Log(angles.FeetDegrees);
                */
                return angles;
            }
        }
    }

    public static class AngleConversions
    {
        public static double Modulo(double x, double divisor)
        {
            double r = x % divisor;
            return (r < 0 ? r + divisor : r);//(x % divisor + divisor) % divisor;
        }

        public static double Mod(this double x, double divisor)
        {
            return Modulo(x, divisor);
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

        public static double Absolute(this double x) => Math.Abs(x);

        public static double ModuloHinge(this double x)
        {
            return MathHelper.Clamp(x, -90, 90);
            /*
            while (x <= -90)
                x += 90;
            while (x >= 90)
                x -= 90;
            return x;*/
        }

        /// <summary>
        /// Turns a -180 to 180 (since rotors operate between 0 and 360)
        /// </summary>
        /// <param name="degrees"></param>
        /// <param name="nineties">If it should be from -90 to 90 (hinges)</param>
        /// <returns></returns>
        public static double AbsoluteDegrees(this double degrees, bool nineties = false)
        {
            // Some angle black magic to spice up your day!
            if (nineties)
            {
                while (degrees < -90)
                    degrees += 180;
                while (degrees > 90)
                    degrees -= 180;
                return degrees;
            }
            /*while (degrees < 0)
                degrees += 360;
            while (degrees > 360)
                degrees -= 360;*/
            return degrees % 360;
        }

        /// <summary>
        /// Turns -180/180 to 0/360
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static double To360(this double degrees)
        {
            return degrees + 180;
        }

        /// <summary>
        /// Turns 0/360 to -180/180
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static double To180(this double degrees)
        {
            return degrees - 180;
        }

        /// <summary>
        /// Keeps angles in range of 180s
        /// </summary>
        /// <param name="degres"></param>
        /// <returns></returns>
        public static double Absolute360(this double degrees)
        {
            return Modulo(degrees, 360);
        }

        /// <summary>
        /// Keeps angles in range of 180s
        /// </summary>
        /// <param name="degres"></param>
        /// <returns></returns>
        public static double Absolute180(this double degrees)
        {
            return Modulo(degrees, 180);
        }

        /// <summary>
        /// Keeps angles in range of 90s
        /// </summary>
        /// <param name="degres"></param>
        /// <returns></returns>
        public static double Absolute90(this double degrees)
        {
            return Modulo(degrees, 90);
        }
    }
}
