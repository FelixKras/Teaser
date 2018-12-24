using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
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
            return new double[3, 3]
            {
                {1.0, 0.0, 0.0},
                {0.0, Math.Cos(angle), -Math.Sin(angle)},
                {0.0, Math.Sin(angle), Math.Cos(angle)},

            };
        }

        //Rotation around X-axis
        public static double[,] CreateRotationMatrixRoll(double angle)
        {
            return new double[4, 4]
            {
                {1.0,0.0,0.0,0.0},
                {0.0,Math.Cos(angle), -Math.Sin(angle), 0.0},
                {0.0,Math.Sin(angle), Math.Cos(angle), 0.0},
                {0.0,0.0, 0.0, 1.0}
            };
        }

        //Rotation around Z-axis
        public static double[,] CreateRotationMatrixYaw(double angle)
        {
            return new double[4, 4]
            {
                {Math.Cos(angle), -Math.Sin(angle), 0.0, 0.0},
                {Math.Sin(angle), Math.Cos(angle), 0.0, 0.0},
                {0.0, 0.0, 1.0, 0.0},
                {0.0, 0.0, 0.0, 1.0}
            };
        }

        //Rotation around Y-axis
        public static double[,] CreateRotationMatrixPitch(double angle)
        {
            return new double[4, 4]
            {
                {Math.Cos(angle), 0.0, Math.Sin(angle),0.0},
                {0.0, 1.0, 0.0,0.0},
                {-Math.Sin(angle), 0.0, Math.Cos(angle),0.0},
                {0.0,0.0,0.0,1.0}
            };
        }

        //// <summary>
        //// Returns 3D rotation matrix around arbitrary unit vector defined by
        //// u with given angle (expressed in radians).
        //// </summary>
        //// <param name="angle">Angle for rotation in radians.</param>
        //// <returns>3D rotation matrix around general axis defined by u.</returns>
        //// <see cref="https://en.wikipedia.org/wiki/Rotation_matrix"/>
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

            double[,] part1 = new double[3, 3]
            {
                {a, 0.0, 0.0},
                {0.0, a, 0.0},
                {0.0, 0.0, a}
            };

            double[,] part2 = new double[3, 3]
            {
                {0.0, -b * u[2], b * u[1]},
                {b * u[2], 0.0, -b * u[0]},
                {-b * u[1], b * u[0], 0.0},
            };

            double[,] part3 = new double[3, 3]
            {
                {c * u[0] * u[0], c * u[0] * u[1], c * u[0] * u[2]},
                {c * u[1] * u[0], c * u[1] * u[1], c * u[1] * u[2]},
                {c * u[2] * u[0], c * u[2] * u[1], c * u[2] * u[2]}
            };

            double[,] R = BLAS.Add(BLAS.Add(part1, part2), part3);

            return R;
        }
    }

    public class Transltions
    {
        public static double[,] CreateTranslationMatrix(double[] trans_vector)
        {
            double[,] mat = new double[4, 4]
            {
                {1, 0.0, 0.0, trans_vector[0]},
                {0.0, 1, 0.0, trans_vector[1]},
                {0.0, 0.0, 1, trans_vector[2]},
                {0.0, 0.0, 0, 1.0}
            };
            return mat;
        }
    }
    public class Scales
    {
        public static double ScaleX;
        public static double ScaleY;
        public static double[,] ScaleMat;
        public static void SetScale(Size canvasSize)
        {
            ScaleX = (double)canvasSize.Width / Camera.CameraSettings.SensorWidth;
            ScaleY = (double)canvasSize.Height / Camera.CameraSettings.SensorHeight;
            ScaleMat = new double[4, 4]
            {
                {ScaleX, 0.0, 0.0, 0},
                {0.0, ScaleY, 0.0, 0},
                {0.0, 0.0, 1, 0},
                {0.0, 0.0, 0, 1.0}
            };

        }
        
    }
    public class Camera
    {
        public static double[,] CreateProjectionMatrix()
        {

            #region Explanation
            /*
            The intrinsic camera matrix is of the form:

            f_x s   x
            0   f_y y
            0   0   1
            
            Here, f_x and f_y are the focal lengths of the camera in the X and Y directions.
            s is the axis skew and is usually 0. x and y are the X and Y dimensions of the image
            produced by the camera, measured from the center of the image. 
            (So, they are half the length and width of the image.)
            
            We typically know the dimensions of the image produced by the camera.
            What is typically not provided are the focal lengths. Instead camera manufacturers provide
            the field of view (FOV) angle in the horizontal and vertical directions.

            Using the FOV angles, the focal lengths can be computed using trigonometry.
            For example, given the FOV a_x in the horizontal direction, 
            the focal length f_x can be computed using:

            f_x = x / tan(a_x / 2)

            We divide the FOV by 2 because this angle spans the entire horizontal or vertical view.

            As an example, consider the Primesense Carmine 1.09 depth camera. It produces a VGA (640×480) image. 
            Its specifications state a horizontal FOV of 57.5 degrees and vertical FOV of 45 degrees.

            Using the above information, we can compute its intrinsic camera matrix as:

            583.2829786373293       0.0                    320.0
            0.0                     579.4112549695428      240.0
            0.0                     0.0                     1.0
             
             */
            #endregion Explanation
            
            double[,] mat = new double[3, 4]
            {
                {CameraSettings.FocalX, 0.0, CameraSettings.SensorWidth/2D,0},
                {0.0, CameraSettings.FocalY, CameraSettings.SensorHeight/2D, 0},
                {0.0, 0.0, 1, 0}
                };
            return mat;
        }

        public static class CameraSettings
        {
            public const double FOVangEl = 40;
            public const double FOVangAz = 60;
            public const double Deg2Rad = Math.PI / 180;
            public const int SensorWidth = 3840;
            public const int SensorHeight = 2160;
            public const float LedSize = 0.10F; //10 cm
            public static readonly double FocalX;
            public static readonly double FocalY;
         
            static CameraSettings()
            {
                FocalX = SensorWidth/2D /  Math.Tan(Rotations.ToRadians(FOVangAz) / 2);
                FocalY = SensorHeight/2D / Math.Tan(Rotations.ToRadians(FOVangEl) / 2);
               
            }

            
        }

    }
}