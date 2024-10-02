using System.Collections.Generic;

namespace ExpressEnginex.Fuzzy
{
    internal static class CentroidExtensions
    {
        internal static Centroid Sum(this IEnumerable<Centroid> centroids)
        {
            double weight = 0d;
            double value = 0d;
            
            foreach (var centroid in centroids)
            {
                weight += centroid.Weight;
                value += centroid.Value * centroid.Weight;
            }

            if (weight > 0) value /= weight;
            
            return new Centroid(value, weight);
        }
    }
}
