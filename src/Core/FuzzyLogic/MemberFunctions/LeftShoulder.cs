using System;
using System.Collections.Generic;
using ExpressEnginex.Fuzzy.Interfaces;

namespace ExpressEnginex.Fuzzy.MemberFunctions
{
    public class LeftShoulder : IMemberFunction, IHasCentroid
    {
        private readonly double _min;
        private readonly double _center;
        private readonly double _max;

        public LeftShoulder(double min, double center, double max)
        {
            _min = min;
            _center = center;
            _max = max;
        }

        public double Evaluate(double x)
        {
            if (!double.IsFinite(x)) return double.NaN;

            if (x <= _min) return 1.0;
            if (x >= _max) return 0.0;
            if (x <= _center) return 1.0;
            return (_max - x) / (_max - _center);
        }

        public Centroid GetCentroid(double y)
        {
			if (y == 0) return new Centroid(0, 0);
			if (y < 0 || y > 1) throw new ArgumentOutOfRangeException(nameof(y), "Membership degree must be between 0 and 1.");

            List<Centroid> centroids = new();

            // Left part (constant at y)
            double leftCenter = (_min + _center) / 2.0;
            double leftWeight = (_center - _min) * y / 2.0;
            centroids.Add(new Centroid(leftCenter, leftWeight));

            // Right part (triangular)
            double rightSlope = 1.0 / (_max - _center);
            double rightMin = _max - y / rightSlope;
            double rightCenter = (rightMin + _max) / 2.0;
            double rightWeight = (_max - rightMin) * y;
            centroids.Add(new Centroid(rightCenter, rightWeight));

            return centroids.Sum();
        }
    }
}
