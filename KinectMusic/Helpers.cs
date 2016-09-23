using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace KinectMusic
{
    public class Helpers
    {
        /// <summary>
        /// Helper function for number scale conversion
        /// </summary>
        /// <param name="minInput"> The input scale's minimum value. </param>
        /// <param name="maxInput"> The inupt scale's maximum value. </param>
        /// <param name="value"> The value to be converted. </param>
        /// <param name="minOutput"> The desired scale's minimum value. </param>
        /// <param name="maxOutput"> The desired scale's maximum value. </param>
        /// <returns> The value in the desired scale. </returns>
        public static int GetValueInRange(int minInput, int maxInput, double value, int minOutput, int maxOutput)
        { 
            int inputRange = maxInput - minInput;
            int outputRange = maxOutput - minOutput;

            int trueInputValue = (int)value - minInput;
            int returnValue = ((trueInputValue * outputRange) / inputRange) + minOutput;
            while (returnValue > maxOutput)
            {
                returnValue -= 1;
            }
            while (returnValue < minOutput)
            {
                returnValue += 1;
            }
            return returnValue;
        }

        public static double DistanceBetweenPoints(Point p1, Point p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        public static Point GetPointClosestToY(PointCollection pointCollection, double pointY)
        {

            Point closestPoint = new Point();
            foreach (Point p in pointCollection)
            {
                if (Math.Abs(p.Y - pointY) < Math.Abs(closestPoint.Y - pointY))
                    closestPoint = p;
            }

            return closestPoint;
        }
    }
}
