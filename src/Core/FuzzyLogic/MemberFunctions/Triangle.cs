using System;
using System.Collections.Generic;
using ExpressEnginex.Fuzzy.Interfaces;

namespace ExpressEnginex.Fuzzy.MemberFunctions
{
    public class Triangle : IMemberFunction, IHasCentroid
    {
        private readonly double _min;
        private readonly double _center;
        private readonly double _max;

        public Triangle(double min, double center, double max)
        {
            _min = min;
            _center = center;
            _max = max;
        }

        public double Evaluate(double x)
        {
			// The Evaluate method computes the membership value of an input x based 
			// on the triangular shape.
			
			// If x is outside the triangle range (_min to _max), it returns 0.
			// Otherwise, it calculates the membership value based on which side
			// of the triangle x falls on.
			
            if (!double.IsFinite(x)) return double.NaN;

            if (x < _min || x > _max) return 0.0;
            if (x < _center) return (x - _min) / (_center - _min);
            if (x >= _center) return (_max - x) / (_max - _center);

            throw new InvalidOperationException();
        }

        Centroid IHasCentroid.GetCentroid(double y)
        {
            if (y == 0) return new Centroid(0, 0);

            List<Centroid> centroids = new();

			// left part
            double leftSlope = 1 / (_center - _min);
            double leftMax = y / leftSlope;
            double leftCenter = (leftMax + _min) / 2;
            double leftWeight = (leftMax - _min) * y / 2;
            centroids.Add(new Centroid(leftCenter, leftWeight));
			
			// right part
            double rightSlope = 1 / (_max - _center);
            double rightMin = y / rightSlope;
            double rightCenter = (_max + rightMin) / 2;
            double rightWeight = (_max - rightMin) * y / 2;
            centroids.Add(new Centroid(rightCenter, rightWeight));
			
			// center
            double centerCenter = (rightMin + leftMax) / 2;
            double centerWeight = (rightMin - leftMax) * y;
            centroids.Add(new Centroid(centerCenter, centerWeight));

			// The centroids.Sum() call uses the Sum extension method to combine
			// the individual centroids into a single centroid representing the
			// entire area under the curve.

            return centroids.Sum();
        }
    }
}
