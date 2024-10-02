using ExpressEnginex.Fuzzy.Interfaces;

namespace ExpressEnginex.Fuzzy.MemberFunctions
{
    public class Line : IMemberFunction
    {
        private readonly double _min;
        private readonly double _max;
        
        private readonly bool _reversed;

        public Line(double min, double max)
        {
			// The Line membership function class defines a linear
			// fuzzy set that can either increase or decrease linearly
			// between specified minimum and maximum value
			
            _reversed = max < min;

            if (_reversed)
            {
                _min = max;
                _max = min;
            }
            else
            {
                _min = min;
                _max = max;
            }

        }
        
        public double Evaluate(double x)
        {
			// The Evaluate method calculates the membership value of a given input x.
			// If x is less than _min, it returns 1 if the line is reversed, otherwise it returns 0.
			// If x is greater than _max, it returns 0 if the line is reversed, otherwise it returns 1.
			// For values between _min and _max, it calculates the slope of the line:
			// * slope = 1 / (_max - _min): This gives the rate of change of the membership value.
			// * If the line is reversed (_reversed is true), it calculates the membership value as 1 - (x - _min) * slope.
			// * If the line is not reversed, it calculates the membership value as (x - _min) * slope.
			
            if (x < _min) return _reversed ? 1 : 0;
            if (x > _max) return _reversed ? 0 : 1;

            double slope = 1 / (_max - _min);
            
            if (_reversed) return 1 - (x - _min) * slope;
            return (x - _min) * slope;
        }
    }
}
