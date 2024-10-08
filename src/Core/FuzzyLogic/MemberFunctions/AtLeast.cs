﻿using ExpressEnginex.Fuzzy.Interfaces;

namespace ExpressEnginex.Fuzzy.MemberFunctions
{
    public class AtLeast : IMemberFunction
    {
        public AtLeast(double value)
        {
            Value = value;
        }

        private double Value { get; }

        public double Evaluate(double x) => x >= Value ? 1 : 0;
    }
}