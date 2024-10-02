using UnityEngine;


namespace ExpressEnginex {

    public static class Collision_Utils
    {
		public static bool LineLine(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out Vector3 intersectionPoint)
		{
			intersectionPoint = Vector3.zero;

			// Calculate the distance to intersection point
			float uA = ((p4.x - p3.x) * (p1.z - p3.z) - (p4.z - p3.z) * (p1.x - p3.x)) / ((p4.z - p3.z) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.z - p1.z));
			float uB = ((p2.x - p1.x) * (p1.z - p3.z) - (p2.z - p1.z) * (p1.x - p3.x)) / ((p4.z - p3.z) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.z - p1.z));

			// If uA and uB are between 0-1, lines are colliding
			if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
			{
				// Calculate the intersection point
				intersectionPoint = new Vector3(p1.x + (uA * (p2.x - p1.x)), p1.y, p1.z + (uA * (p2.z - p1.z)));
				return true;
			}

			return false;
		}

		public static bool SphereCollision(float r1, Vector3 p1, float r2, Vector3 p2)
		{
			if (Vector3.Dot(p2 - p1, p2 - p1) < (r1 + r2) * (r1 + r2))
				return true;

			return false;
		}

		public static bool IsPosWithinBounds2d(Vector3 btmLeftPoint, Vector3 topRightPoint, Vector3 pos)
		{
			return (pos.x > btmLeftPoint.x && pos.x < topRightPoint.x) && (pos.z > btmLeftPoint.z && pos.z < topRightPoint.z);
		}
	}
}
