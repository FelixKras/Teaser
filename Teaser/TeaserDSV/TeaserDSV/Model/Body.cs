using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeaserDSV.Utilities;

namespace TeaserDSV.Model
{
    /// <summary>
    /// Represents a complete body with additional functionality.
    /// 
    /// A body is composed of points (as a point cloud for example).
    /// </summary>
    public class Body
    {
        /// <summary>
        /// Array of original points that describe face edges of the object.
        /// Also can be thought of as a wireframe representation.
        /// </summary>
        public ShapePoint3D[] OriginalPoints { get; set; }
        public ShapePoint2D[] ImagePoints { get; set; }
        public double YawAngle { get; set; }

        public double PitchAngle { get; set; }

        public double RollAngle { get; set; }

        public SizeF LedSize; //LedSize in pixels

        public void ComputeProjection(double[] CenterOfMassCartesian)
        {   /*
            rotation_matrix = yaw_rotation_matrix * pitch_rotation_matrix * roll_rotation_matrix;
            world_matrix = translation_matrix * rotation_matrix;
            */

            ImagePoints = new ShapePoint2D[OriginalPoints.Length];

            #region Debug
#warning DEBUG
            //var TestVec = new double[] {-0.5, 0.5, 0.7071, 1};
            //RollAngle = 0;
            //PitchAngle = 45;
            //YawAngle = 45;
            //CenterOfMassCartesian = new double[] { 0, 50, 100 };
            
            #endregion

            double[,] R_roll = Rotations.CreateRotationMatrixRoll(Rotations.ToRadians(RollAngle)); //Rx
            double[,] R_pitch = Rotations.CreateRotationMatrixPitch(Rotations.ToRadians(PitchAngle)); //Ry
            double[,] R_yaw = Rotations.CreateRotationMatrixYaw(Rotations.ToRadians(YawAngle)); //Rz

            double[,] temp = BLAS.Multiply(R_pitch, R_roll);
            double[,] R_Gen = BLAS.Multiply(R_yaw, temp);

            double[,] T_mat = Transltions.CreateTranslationMatrix(CenterOfMassCartesian);

            double[,] World_mat = BLAS.Multiply(T_mat, R_Gen);

            /*
              [u,v]= Cam_Project_mat*world_matrix*[x y z]
            */

            double[,] Cam_Project_mat = Camera.CreateProjectionMatrix();
   
            double[,] FinalMat = BLAS.Multiply(Cam_Project_mat, World_mat);

            #region Debug
#warning DEBUG
            //double[] Testtransf = BLAS.Multiply(FinalMat, TestVec);
            //Testtransf[0] /= Testtransf[2];
            //Testtransf[1] /= Testtransf[2];
            //Testtransf[2] /= Testtransf[2];
            #endregion


            var computedLedWidth = (float) (Camera.CameraSettings.LedSize * Camera.CameraSettings.SensorHeight /
                                            (Math.Sin(Camera.CameraSettings.FOVangAz / 2 * Camera.CameraSettings.Deg2Rad) *
                                             CenterOfMassCartesian[2]) );
            var computedLedHeight = (float)(Camera.CameraSettings.LedSize * Camera.CameraSettings.SensorWidth /
                                            (Math.Sin(Camera.CameraSettings.FOVangEl / 2 * Camera.CameraSettings.Deg2Rad) *
                                             CenterOfMassCartesian[2]) );
            LedSize.Width = computedLedWidth <2? 2:computedLedWidth;
            LedSize.Height= computedLedHeight < 2 ? 2 : computedLedHeight;


            for (int ii = 0; ii < OriginalPoints.Length; ii++)
            {
                // Apply transformation to original position
                double[] vec = new double[] { OriginalPoints[ii].X, OriginalPoints[ii].Y, OriginalPoints[ii].Z, 1 };
                double[] transf = BLAS.Multiply(FinalMat, vec);
                transf[0] /= transf[2];
                transf[1] /= transf[2];
                transf[2] /= transf[2];
                double[] transfScaled = BLAS.Multiply(Scales.ScaleMat, transf);
                ImagePoints[ii] = new ShapePoint2D(transfScaled[0], transfScaled[1], OriginalPoints[ii].isLED);
            }
        }
    }


    public struct ShapePoint3D
    {
        public double X;
        public double Y;
        public double Z;
        public bool isLED;

        public double[] Position
        {
            get
            {
                return new double[] { X, Y, Z };
            }
            set
            {
                if (value.Length == 3)
                {
                    X = value[0];
                    Y = value[1];
                    Z = value[2];
                }
            }
        }


        public ShapePoint3D(double[] arr, bool isLEDPoint = false)
        {
            if (arr.Length == 3)
            {
                X = arr[0];
                Y = arr[1];
                Z = arr[2];
            }
            else
            {
                X = 0;
                Y = 0;
                Z = 0;
            }
            isLED = isLEDPoint;
        }

        public ShapePoint3D(double x, double y, double z, bool isLEDPoint = false)
        {
            X = x;
            Y = y;
            Z = z;
            isLED = isLEDPoint;
        }


        public double Norm()
        {
            double sumOfSquares = X * X + Y * Y + Z * Z;
            return Math.Sqrt(sumOfSquares);
        }


    }
    public struct ShapePoint2D
    {
        public PointF point;
        public bool isLED;

        public double[] Position
        {
            get
            {
                return new double[] { point.X, point.Y };
            }
            set
            {
                if (value.Length == 2)
                {
                    point.X = (float)value[0];
                    point.Y = (float)value[1];
                }
            }
        }


        public ShapePoint2D(double[] arr, bool isLEDPoint = false)
        {
            if (arr.Length == 2)
            {
                point = new PointF((float)arr[0], (float)arr[1]);
            }
            else
            {
                point = new PointF();
            }
            isLED = isLEDPoint;
        }

        public ShapePoint2D(double x, double y, bool isLEDPoint = false)
        {
            point = new PointF((float)x, (float)y);
            isLED = isLEDPoint;
        }


        public double Norm()
        {
            double sumOfSquares = point.X * point.X + point.Y * point.Y;
            return Math.Sqrt(sumOfSquares);
        }


    }

}
