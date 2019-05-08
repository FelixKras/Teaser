using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeaserDSV.Model
{
    /// <summary>
    /// Represents a point of a body.
    /// Includes the 3D coordinate of the point and intensity value.
    /// </summary>
    public class BodyPoint
    {
        /// <summary>
        /// 3D vector of point position in world.
        /// </summary>
        public double[] Position { get; set; }
        public bool IsLed;

        
        /// <summary>
        /// Point intensity level.
        /// </summary>
        //public double Intensity { get; set; }
    }
}
