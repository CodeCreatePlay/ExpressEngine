using UnityEngine;


namespace ExpressEnginex
{
    public class MathUtils
    {
		public static Quaternion RandQuat(Vector3 originalRotation)
		{
			return Quaternion.Euler(originalRotation.x, Random.Range(originalRotation.y, 360f), originalRotation.z);
		}


		public static float GetNormalizedValue(float value, float min_val, float max_val)
		{
			return (value - min_val) / (max_val - min_val);
		}


		/// <summary>
		/// Converts a value to 0-1 range.
		/// </summary>
		public static float ConvertToRange(float oldMin, float oldMax, float newMin, float newMax, float value)
		{
			if ((oldMax - oldMin) == 0)
				return newMin;
			else
				return (((value - oldMin) * (newMax - newMin)) / (oldMax - oldMin)) + newMin;
		}


		/// <summary>
		/// Weighted choice with a list of floats as input.
		/// </summary>
		private static System.Collections.Generic.List<float> totals = new();
		private static float runningTotal = 0;
		private static float randVal;
		public static int WeightedChoice(System.Collections.Generic.List<float> weights)
		{
			totals = new System.Collections.Generic.List<float>();
			runningTotal = 0;

			for (int i = 0; i < weights.Count; i++)
			{
				runningTotal += weights[i];
				totals.Add(runningTotal);
			}

			randVal = UnityEngine.Random.Range(0f, 1f) * runningTotal;

			for (int i = 0; i < totals.Count; i++)
			{
				if (randVal < totals[i])
					return i;
			}

			return -1;
		}


		/// <summary>
		/// Weighted choice with an array of floats as input.
		/// </summary>
		public static int WeightedChoice(float[] weights)
		{
			totals = new System.Collections.Generic.List<float>();
			runningTotal = 0;

			for (int i = 0; i < weights.Length; i++)
			{
				runningTotal += weights[i];
				totals.Add(runningTotal);
			}

			randVal = UnityEngine.Random.Range(0f, 1f) * runningTotal;

			for (int i = 0; i < totals.Count; i++)
			{
				if (randVal < totals[i])
					return i;
			}

			return -1;
		}
	}
}