using UnityEngine;

namespace ExpressEngine
{
	public struct GroundInfo
    {
        public static GroundInfo Empty = new (false, Vector3.up, Vector3.zero, 0f, null);

        public bool IsOnGround   { get; set; }
        public Vector3 Normal    { get; set; }
        public Vector3 Point     { get; set; }
        public float Distance    { get; set; }
        public Collider Collider { get; set; }


        public GroundInfo(bool isOnGround, Vector3 normal, Vector3 point, float distance, Collider collider)
        {
            IsOnGround = isOnGround;
            Normal     = normal;
            Point      = point;
            Distance   = distance;
            Collider   = collider;
        }
    }
	
    public static class GroundSensor
    {
        /// <summary>
        /// Probe ground from origin downwards for a specified range.
        /// </summary>
        /// <param name="groundInfo"></param>
        /// <param name="distance"></param>
        /// <param name="thickness"></param>
        /// <param name="origin"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static bool ProbeGround(
            out GroundInfo groundInfo,
            float thresholdDistance, 
            float distance,
            float thickness,
            Vector3 origin,
            LayerMask layerMask,
            bool useRealGroundNormal = false)
        {
            groundInfo = GroundInfo.Empty;
            bool isGroundInRange = false;
            RaycastHit hitInfo;

            bool hasHit;
            if (thickness <= 0f) hasHit = Physics.Raycast(origin, Vector3.down, out hitInfo,
				maxDistance: distance, layerMask: layerMask);
            else hasHit = Physics.SphereCast(origin, thickness / 2f, Vector3.down, out hitInfo,
				maxDistance: distance, layerMask: layerMask);

            if (hasHit)
            {
                groundInfo.Distance = origin.y - hitInfo.point.y;
                if (groundInfo.Distance <= thresholdDistance)
                {
                    isGroundInRange = true;
                    groundInfo.Normal = hitInfo.normal;
                    groundInfo.Point = hitInfo.point;
                    groundInfo.Collider = hitInfo.collider;

                    if (useRealGroundNormal && thickness > 0f)
                    {
                        Vector3 tmpOrigin = hitInfo.point + new Vector3(0f, 0.01f, 0f);
                        if (hitInfo.collider.Raycast(new Ray(tmpOrigin, Vector3.down), out RaycastHit realNormalHitInfo, maxDistance: 0.1f))
                        {
                            groundInfo.Normal = realNormalHitInfo.normal;
                        }
                    }
                }
            }

            groundInfo.IsOnGround = isGroundInRange;
            return isGroundInRange;
        }
    }
}