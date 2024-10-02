using System;
using System.Collections.Generic;
using ExpressEnginex.Fuzzy.Interfaces;

namespace ExpressEnginex.Fuzzy.MemberFunctions
{
    public class RightShoulder : IMemberFunction, IHasCentroid
    {
        private readonly double _min;
        private readonly double _center;
        private readonly double _max;

        public RightShoulder(double min, double center, double max)
        {
            _min = min;
            _center = center;
            _max = max;
        }

        public double Evaluate(double x)
        {
            if (!double.IsFinite(x)) return double.NaN;

            if (x <= _min) return 0.0;
            if (x >= _max) return 1.0;
            if (x >= _center) return 1.0;
            return (x - _min) / (_center - _min);
        }

		public Centroid GetCentroid(double y)
		{
			if (y == 0) return new Centroid(0, 0);
			if (y < 0 || y > 1) throw new ArgumentOutOfRangeException(nameof(y), "Membership degree must be between 0 and 1.");

			List<Centroid> centroids = new();

			// Left part (triangular area where membership < 1)
			double leftSlope = 1.0 / (_center - _min);
			double leftBoundary = y / leftSlope + _min;
			double leftCenter = (_min + leftBoundary) / 2.0;
			double leftWeight = (leftBoundary - _min) * y / 2.0;
			centroids.Add(new Centroid(leftCenter, leftWeight));

			// Right part (rectangular area where membership = 1)
			double rightCenter = (_center + _max) / 2.0;
			double rightWeight = (_max - _center) * y;
			centroids.Add(new Centroid(rightCenter, rightWeight));
			
			return centroids.Sum();
		}
    }
}
