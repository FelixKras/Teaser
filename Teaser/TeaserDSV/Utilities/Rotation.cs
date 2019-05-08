using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeaserDSV.Utilities
{
    public class Rotations
    {
        /// <summary>
        /// Converts angle from degrees to radians and returns the result.
        /// </summary>
        /// <param name="angle">Angle in degrees to convert to radians.</param>
        /// <returns>Angle in radians.</returns>
        public static double ToRadians(double angle)
        {
            return angle * Math.PI / 180.0;
        }

        /// <summary>
        /// Returns 3D rotation matrix around Z axis with given angle 
        /// (expressed in radians).
        /// </summary>
        /// <param name="angle">Angle for rotation in radians.</param>
        /// <returns>3D rotation matrix around Z axis.</returns>
        public static double[,] CreateRotationMatrixZ(double angle)
        {
            return new double[3, 3] {
                { Math.Cos(angle), -Math.Sin(angle), 0.0 },
                { Math.Sin(angle),  Math.Cos(angle), 0.0 },
                { 0.0, 0.0, 1.0 }
            };
        }

        /// <summary>
        /// Returns 3D rotation matrix around arbitrary unit vector defined by
        /// u with given angle (expressed in radians).
        /// </summary>
        /// <param name="angle">Angle for rotation in radians.</param>
        /// <returns>3D rotation matrix around general axis defined by u.</returns>
        /// <see cref="https://en.wikipedia.org/wiki/Rotation_matrix"/>
        public static double[,] CreateRotationMatrixGeneric(double[] u, double angle)
        {
            // Construct rotation matrix using formulas.
            // In simplified notation:
            // R = cos(angle) * I + sin(angle) * [u]_x + (1 - cos(angle)) * [u (x) u].
            // 
            // I is a 3x3 identity matrix.
            // 
            // [u]_x is a cross product matrix formed from u elements.
            // |0.0  -u_3  u_2|
            // |u_3  0.0  -u_1|
            // |-u_2 u_1   0.0|
            // 
            // u (x) u is a tensor product and equals uu^T.
            double a = Math.Cos(angle);
            double b = Math.Sin(angle);
            double c = 1 - a;

            double[,] part1 = new double[3, 3] {
                { a, 0.0, 0.0 },
                { 0.0, a, 0.0 },
                { 0.0, 0.0, a }
            };

            double[,] part2 = new double[3, 3] {
                { 0.0, -b * u[2], b * u[1] },
                { b * u[2], 0.0, -b * u[0] },
                { -b * u[1], b * u[0], 0.0 },
            };

            double[,] part3 = new double[3, 3] {
                { c * u[0] * u[0], c * u[0] * u[1], c * u[0] * u[2] },
                { c * u[1] * u[0], c * u[1] * u[1], c * u[1] * u[2] },
                { c * u[2] * u[0], c * u[2] * u[1], c * u[2] * u[2] }
            };

            double[,] R = BLAS.Add(BLAS.Add(part1, part2), part3);

            return R;
        }
    }
}
