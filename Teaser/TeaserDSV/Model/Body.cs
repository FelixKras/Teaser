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

        public bool IsLEDOn;
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

            //double[,] R_roll = Rotations.CreateRotationMatrixRoll(Rotations.ToRadians(RollAngle)); //Rx
            //double[,] R_pitch = Rotations.CreateRotationMatrixPitch(Rotations.ToRadians(PitchAngle)); //Ry
            //double[,] R_yaw = Rotations.CreateRotationMatrixYaw(Rotations.ToRadians(YawAngle)); //Rz

            //double[,] temp = BLAS.Multiply(R_roll, R_yaw);
            //double[,] R_Gen = BLAS.Multiply(R_pitch, temp);

            double[,] T_mat = Transltions.CreateTranslationMatrix(CenterOfMassCartesian);

            //R_Gen = Rotations.CreateRotationPsiThetaPhi(Rotations.ToRadians(YawAngle), Rotations.ToRadians(PitchAngle), Rotations.ToRadians(RollAngle));
            double[,] R_Gen = Rotations.CreateRotationThetaPsiPhi(Rotations.ToRadians(PitchAngle), Rotations.ToRadians(YawAngle), Rotations.ToRadians(RollAngle));

            double[,] World_mat = BLAS.Multiply(T_mat, R_Gen);

            /*
              [u,v]= Cam_Project_mat*world_matrix*[x y z]
            */

            double[,] Cam_Project_mat = Camera.CreateProjectionMatrix();
            double[,] Launch_to_Camera = Camera.CreateAxisTranformMatrix();
            double[,] FinalMat = BLAS.Multiply(Cam_Project_mat, BLAS.Multiply(Launch_to_Camera, World_mat));

            #region Debug
#warning DEBUG
            //double[] Testtransf = BLAS.Multiply(FinalMat, TestVec);
            //Testtransf[0] /= Testtransf[2];
            //Testtransf[1] /= Testtransf[2];
            //Testtransf[2] /= Testtransf[2];
            #endregion


            SetLedSize(FinalMat);

            for (int ii = 0; ii < OriginalPoints.Length; ii++)
            {
                // Apply transformation to original position
                double[] vec = new double[] { OriginalPoints[ii].X, OriginalPoints[ii].Y, OriginalPoints[ii].Z, 1 };
                double[] transf_camera = BLAS.Multiply(FinalMat, vec);


                transf_camera[0] /= transf_camera[2];
                transf_camera[1] /= transf_camera[2];
                transf_camera[2] /= transf_camera[2];

                double[] transfScaled = BLAS.Multiply(Scales.ScaleMat, transf_camera);
                if (transfScaled[0] > 0 && transfScaled[1] > 0 && transfScaled[0] < Camera.CameraSettings.SensorWidth && transfScaled[1] < Camera.CameraSettings.SensorHeight)
                {
                    ImagePoints[ii] = new ShapePoint2D(transfScaled[0], transfScaled[1], OriginalPoints[ii].isLED);
                }
                else
                {
                    ImagePoints[ii] = new ShapePoint2D(-10, -10, false);
                }


            }



        }

        private void SetLedSize(double[,] FinalMat)
        {

            //body coord
            List<double[]> ledBodyPoints = new List<double[]>
{
    //            X   Y    Z   1 body
    new double[]{-1, 2, 2, 1},
    new double[]{-1, -2, 2, 1},
    new double[]{-1, 2, -2, 1},
    new double[]{-1, -2, -2, 1}
};
            List<double[]> ledTransformed = new List<double[]>();

            for (int ii = 0; ii < ledBodyPoints.Count; ii++)
            {
                double[] ledTransformedvec = BLAS.Multiply(FinalMat, ledBodyPoints[ii]);
                ledTransformed.Add(ledTransformedvec);
            }

            float norm1 = 1;
            float norm2 = 1;
            if (Math.Abs(ledTransformed[0][2]) > 0)
            {
                norm1 = (float)ledTransformed[0][2];
            }
            if (Math.Abs(ledTransformed[3][2]) > 0)
            {
                norm2 = (float)ledTransformed[3][2];
            }

            double Width = Math.Abs((ledTransformed[0][0] / norm1 - ledTransformed[3][0] / norm2) * Scales.ScaleX) *
                           SettingsHolder.Instance.LedSizeW;
            double Height = Math.Abs((ledTransformed[0][1] / norm1 - ledTransformed[3][1] / norm2) * Scales.ScaleY) *
                            SettingsHolder.Instance.LedSizeH;

            LedSize.Height = (float)Height;
            LedSize.Width = (float)Width;


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
