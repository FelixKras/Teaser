using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TeaserDSV
{
    class cCalcer
    {
        public struct CameraSettings
        {
            public const double FOVangEl = 40;
            public const double FOVangAz = 60;
            public const double Deg2Rad = Math.PI / 180;
            public const int SensorWidth = 3840;
            public const int SensorHeight = 2160;
            public const float LedSize = 0.10F; //10 cm
        }

        public static PointF ObjectToCameraProjection(SixMsg oSixMsg)
        {
            PointF pnt=new PointF(-10,-10);
            if (oSixMsg.Object_X > double.Epsilon)
            {
                var yp = CameraSettings.SensorWidth/2+ oSixMsg.Object_Y /(CameraSettings.FOVangAz  * CameraSettings.Deg2Rad * oSixMsg.Object_X)*
                         CameraSettings.SensorWidth;
                var zp = CameraSettings.SensorHeight / 2 + oSixMsg.Object_Z /(CameraSettings.FOVangEl* CameraSettings.Deg2Rad * oSixMsg.Object_X) *
                         CameraSettings.SensorHeight;
                pnt.X = (float)(yp * Screen.PrimaryScreen.Bounds.Size.Width / CameraSettings.SensorWidth * 0.653125);//0.653125??
                pnt.Y = (float)(zp * Screen.PrimaryScreen.Bounds.Size.Height / CameraSettings.SensorHeight * 1.012963);//1.012963??

                //pnt.X = (float)(yp);
                //pnt.Y = (float)(zp);

            }
            return pnt;

        }

      internal static PointF[] ObjectToCameraProjection(PointF[] pntTargetShapeOrig, SixMsg oSixMsg=default(SixMsg))
        {
            
            //PointF cm = ObjectToCameraProjection(oSixMsg); //in fpa coords

            PointF[] pntTargetShapeTransformed =new PointF[pntTargetShapeOrig.Length];

            for (int ii = 0; ii < pntTargetShapeTransformed.Length; ii++)
            {
                pntTargetShapeTransformed[ii].X = pntTargetShapeOrig[ii].X;
                pntTargetShapeTransformed[ii].Y = pntTargetShapeOrig[ii].Y;
            }

            return pntTargetShapeTransformed;
        }
    }
}
