using System.Collections;
using UnityEngine;
using ExpressEnginex.Fuzzy.MemberFunctions;

namespace ExpressEnginex.Fuzzy.Utils
{
	public enum ShapeType {
		Triangle,
		Trapezoid,
		RightShoulder,
		LeftShoulder,
	}
	
	public class MembershipFunctionInfo
	{
		public ShapeType shapeType;
		public float leftOverlap;
		public float rightOverlap;
		public int FLV;
		
		
		public MembershipFunctionInfo(ShapeType shapeType, float leftOverlap, float rightOverlap, int FLV)
		{
			this.shapeType    = shapeType;
			this.leftOverlap  = leftOverlap;
			this.rightOverlap = rightOverlap;
			this.FLV       	  = FLV;
		}
	}
	
	public class TrapezoidFunctionInfo : MembershipFunctionInfo
	{
		public float centerScale;
		
		
		public TrapezoidFunctionInfo(ShapeType shapeType, float leftOverlap, float centerScale, float rightOverlap, int FLV) :
								base(shapeType, leftOverlap, rightOverlap, FLV)
		{
			this.centerScale = centerScale;
		}
	}
	
	public class FuzzyUtils
	{				
		public static void FitMembershipFunctions(double min, double max, MembershipFunctionInfo[] functionInfos, FuzzyInput inputSet)
		{
			if(min < 0)
			{ Debug.LogError("MembershipFunction 'min value' must be greater than or equal to 0"); return; } 
			
			double totalRange = max - min;
			double range = totalRange / functionInfos.Length;  // coverage area (horizontal) for one shape

			for (int i = 0; i < functionInfos.Length; i++)
			{						
				double start = min + i * range;
				double end   = start + range;
				
				// calculate overlaps
				start = i > 0 ? start - (range * functionInfos[i].leftOverlap) : start;                      // left overlap
				end   = i < functionInfos.Length - 1 ? end + (range * functionInfos[i].rightOverlap) : end;  // right overlap 

				// create membership sets and set them on input or output fuzzy membership functions
				switch(functionInfos[i].shapeType)
				{
					case ShapeType.Triangle:
						double peak  = (start + end) / 2;
						inputSet.Set(functionInfos[i].FLV, new Triangle(start, peak, end));
						//Debug.LogFormat("Triangle ShapeCreate {0}-{1}-{2}", start, peak, end);
						break;
						
					case ShapeType.Trapezoid: 
						// cast to trapezoid info
						float centerScale = ((TrapezoidFunctionInfo)functionInfos[i]).centerScale;
						
						// calculate in between points a and b
						double a = start + (1.0/3.0) * (end-start);
						a -= start * centerScale;
						a = System.Math.Clamp(a, start, (start + end) /2f);
						
						double b = start + (2.0/3.0) * (end-start);
						b += start * centerScale;
						b = System.Math.Clamp(b, (start + end) /2f, end);
						
						inputSet.Set(functionInfos[i].FLV, new Trapezoid(start, a, b, end));
						//Debug.LogFormat("Trapezoid ShapeCreate {0}-{1}-{2}-{3}", start, a, b, end);
						break;
						
					case ShapeType.LeftShoulder:
						peak  = (start + end) / 2;
						inputSet.Set(functionInfos[i].FLV, new LeftShoulder(start, peak, end));
						//Debug.LogFormat("LeftShoulder ShapeCreate {0}-{1}", start, end);
						break;
						
					case ShapeType.RightShoulder:
						peak  = (start + end) / 2;
						inputSet.Set(functionInfos[i].FLV, new RightShoulder(start, peak, end));
						//Debug.LogFormat("RightShoulder ShapeCreate {0}-{1}", start, end);
						break;
				}
			}
		}

		public static void FitMembershipFunctions(double min, double max, MembershipFunctionInfo[] functionInfos, FuzzyOutput outputSet)
		{
			if(min < 0)
			{ Debug.LogError("MembershipFunction 'min value' must be greater than or equal to 0"); return; } 
			
			double totalRange = max - min;
			double range = totalRange / functionInfos.Length;  // coverage area (horizontal) for one shape

			for (int i = 0; i < functionInfos.Length; i++)
			{				
				double start = min + i * range;
				double end   = start + range;
				
				// calculate overlaps
				start = i > 0 ? start - (range * functionInfos[i].leftOverlap) : start;                      // left overlap
				end   = i < functionInfos.Length - 1 ? end + (range * functionInfos[i].rightOverlap) : end;  // right overlap 

				// create membership sets and set them on input or output fuzzy membership functions
				switch(functionInfos[i].shapeType)
				{
					case ShapeType.Triangle:
						double peak = (start + end) / 2.0;
						outputSet.Set(functionInfos[i].FLV, new Triangle(start, peak, end));
						//Debug.LogFormat("Triangle ShapeCreate {0}-{1}-{2}", start, peak, end);
						break;
						
					case ShapeType.Trapezoid: 
						// cast to trapezoid info
						float centerScale = ((TrapezoidFunctionInfo)functionInfos[i]).centerScale;
						
						// calculate in between points a and b
						double a = start + (1.0/3.0) * (end-start);
						a -= start * centerScale;
						a = System.Math.Clamp(a, start, (start + end) /2f);
						
						double b = start + (2.0/3.0) * (end-start);
						b += start * centerScale;
						b = System.Math.Clamp(b, (start + end) /2f, end);
						
						outputSet.Set(functionInfos[i].FLV, new Trapezoid(start, a, b, end));
						//Debug.LogFormat("Trapezoid ShapeCreate {0}-{1}-{2}-{3}", start, a, b, end);
						break;
						
					case ShapeType.LeftShoulder:
						peak = (start + end) / 2.0;
						outputSet.Set(functionInfos[i].FLV, new LeftShoulder(start, peak, end));
						Debug.LogFormat("LeftShoulder ShapeCreate {0}-{1}-{2}", start, peak, end);
						break;
						
					case ShapeType.RightShoulder:
						peak = (start + end) / 2.0;
						outputSet.Set(functionInfos[i].FLV, new RightShoulder(start, peak, end));
						//Debug.LogFormat("RightShoulder ShapeCreate {0}-{1}-{2}", start, peak, end);
						break;
				}
			}
		}
	}
}
