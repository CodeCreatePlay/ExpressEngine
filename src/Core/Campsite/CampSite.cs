using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using ExpressEnginex.CampSite_; 
#endif

namespace ExpressEnginex.CampSite_
{
    /// <summary>
    /// 
    /// </summary>
	[System.Serializable]
    public class CampSite : MonoBehaviour, ISerializationCallbackReceiver
    {
		[System.Serializable]
        public class CampSitePos
        { 
            public Vector3 pos;
            public Vector3 posLocal;
            public bool    occupied;
            public int     idx;

            public CampSitePos(Vector3 pos, Vector3 localPos, bool occupied, int idx)
            {
                this.pos	  = pos;
                this.occupied = occupied;
                this.posLocal = localPos;
                this.idx	  = idx;
            }
        }

		[Header("Radius Settings")]
        public float innerRadius   = 5f;  // Inner radius of the circle.
        public float outerRadius   = 10f; // Outer radius of the circle.
		
		[Header("Position Distribution Settings")]
        public float minDistance   = 2f;       // Minimum distance between positions.
		public float offsetFromGround = 0.0f;  
		public int   iterations    = 100; 
        public int   maxAttempts   = 10;
		public bool  automaticDistribution = true;
		public bool  editMode              = false;
		public bool  remapAutomatiacally   = false;

        [HideInInspector] public Dictionary<int, List<CampSitePos>> possMap;
		private List<GameObject> allCharacters;
        private CampSitePos[] allDestinations;
        [SerializeField, HideInInspector] private Vector3 lastPos;
		public Vector3 LastPos { get { return lastPos; } }
        private Quaternion lastRotation;
		public Quaternion LastRotation { get { return lastRotation; } }

		[Header("Miscallenous")]
		// public bool dynamic = false;
		public bool firewoodAmount;
		public bool gasoline;
		 
		[Header("Debugging")]
        public bool debug = false;


        private void Awake()
        {			
			allCharacters = new();
			
			_Awake();
			
			if (automaticDistribution)
				GeneratePositions();
			else
			{
				// fix any position-quadrant mismatch that may have occur during edit mode editing
				Remap();
			}
        }
		
		private void Update()
		{
			_Update();
		}
		 
		public void _Awake()
		{
			lastPos      = transform.position;
            lastRotation = transform.rotation;
		}

        public void _Update()
        {
            if (lastPos != transform.position || lastRotation != transform.rotation)
            {
                foreach (var key in possMap.Keys)
                {
                    for (int i = 0; i < possMap[key].Count; i++)
                    {
                        possMap[key][i].pos = transform.TransformPoint(possMap[key][i].posLocal);
                    }
                }

				if(remapAutomatiacally && lastRotation != transform.rotation)
					Remap();

                lastPos = transform.position;
				lastRotation = transform.rotation;
            }
        }
		
		public void Remap()
		{
			int quadrant = -1;
			Dictionary<int, List<CampSitePos>> tempMap = new();

			for(int i = 1; i < 5; i++)
			{
				if(!possMap.ContainsKey(i))
					continue;

				for(int j = 0; j < possMap[i].Count; j++)
				{
					quadrant = FindQuadrant(possMap[i][j].pos, transform.position);
					
					if(!tempMap.ContainsKey(quadrant))
						tempMap[quadrant] = new();
					
					possMap[i][j].posLocal = transform.InverseTransformPoint(possMap[i][j].pos);		
					tempMap[quadrant].Add(possMap[i][j]);
				}
			}
			
			possMap = tempMap;
			_Sort();
		}
		
		/// <summary>
        /// 
        /// </summary>
        public List<CampSitePos> GeneratePositions()
        {
            // clear existing positions
            List<CampSitePos> positions = new List<CampSitePos>();
            possMap = new();
			
            int currentAttemptsCount = 0;
			
			// cache
			Vector3 tempPos;
			
			// 
            for (int i = 0; i < iterations; i++)
            {
                if (currentAttemptsCount > maxAttempts)
                    break;

                float theta = 2 * Mathf.PI * Random.Range(0f, 1f);
                float r = outerRadius * Mathf.Pow(Random.Range(0f, 1f), 1 / 2f);
                Vector3 position = new(r * Mathf.Cos(theta), 0f, r * Mathf.Sin(theta));
                position += transform.position;

				// calculate y-position
				position.y += offsetFromGround;

                // Push the position outside a distance r from the origin
				// replace this distance check with sphere-sphere collision check
                if (Vector3.Distance(position, transform.position) < innerRadius)
                {		 			
                    // Scale up the position vector to ensure its magnitude is greater than r
					tempPos = position.normalized * innerRadius * 2f;
					position.x += tempPos.x;
					position.z += tempPos.z;
					
                    // position += position.normalized * innerRadius * 2f;
                }

                // Ensure the position satisfies the minimum distance constraint
                bool validPosition = true;
                foreach (CampSitePos existingPosition in positions)
                {
                    if (Vector3.Distance(position, existingPosition.pos) <= minDistance)
                    {
                        validPosition = false;
                        break;
                    }
                }

                // If the position is valid, add it to the list
                if (validPosition)
                {
                    positions.Add(new CampSitePos(pos:position, localPos:transform.InverseTransformPoint(position), occupied:false, idx: i));

                    int quadrant = CampSite.FindQuadrant(positions[^1].pos, transform.position);
					
                    if (!possMap.ContainsKey(quadrant))
                        possMap[quadrant] = new List<CampSitePos>();
					
                    possMap[quadrant].Add(positions[^1]);
                }
                else
                {
                    // If the position is not valid, decrement the iteration count to try again
                    i--;
                    currentAttemptsCount++;
                }
            }
			
			// sort according to distances from center
			_Sort();
			
            return positions;
        }
		
		private void _Sort()
		{
			// sort according to distances from center
			for(int i = 0; i < 5; i++)
			{
				if(!possMap.ContainsKey(i))
					continue;
				
				possMap[i] = possMap[i].OrderBy(campSiteDest => Vector3.Distance(transform.position, campSiteDest.pos)).ToList();
			}
		}
		
		public CampSitePos GetNearestPosition(Vector3 fromPos)
        {
			// find nearest quadrant
			int     nearestQuadrantIdx      = -1;
			float   nearestQuadrantDistance = float.MaxValue;
			Vector3 nearestQuadrantPos      = Vector3.zero * -1f;
			
			float nearestDistance = -1;
			for(int i = 1; i < 5; i++)
			{
			    Vector3 posInQuad = possMap[i][possMap[i].Count-1].pos;
				nearestDistance    = Vector3.Distance(fromPos, posInQuad);
				
				if (nearestDistance < nearestQuadrantDistance)
				{
					nearestQuadrantIdx      = i;
					nearestQuadrantPos      = posInQuad;
					nearestQuadrantDistance = nearestDistance;
				}
			}
			
			// get the closest position to center of campsite
            CampSitePos nearest = default;

			for (int i = possMap[nearestQuadrantIdx].Count-1; i > -1; i--)
			{
				if (possMap[nearestQuadrantIdx][i].occupied)
					continue;

				nearest = possMap[nearestQuadrantIdx][i];
			}

            return nearest;
        }

        public CampSitePos GetNextDestination()
        {
            int key = -1;  

            for (int i = 0; i < 100; i++)
            {
				key = Random.Range(1, 5);  // replace this with weighted probability

				// brute force now, improve it later
				for (int j = 0; j < possMap[key].Count; j++)
				{
					if (!possMap[key][j].occupied)
						return possMap[key][j];
				}
            }
			
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3[] CalculatePath(Vector3 circleCenter, float circleRadius, Vector3 startPosition, Vector3 endPosition)
        {
            // Calculate the angle between the starting and ending positions
            float startAngle = Mathf.Atan2(startPosition.z - circleCenter.z, startPosition.x - circleCenter.x);
            float endAngle   = Mathf.Atan2(endPosition.z - circleCenter.z, endPosition.x - circleCenter.x);
            float angleRange = endAngle - startAngle;
            angleRange = Mathf.Atan2(Mathf.Sin(angleRange), Mathf.Cos(angleRange));  // Ensure angle range is within [-?, ?]

            // Determine the direction to travel (clockwise or counterclockwise)
            // float shortestAngle = Mathf.Abs(angleRange) < Mathf.PI ? angleRange : angleRange - Mathf.Sign(angleRange) * Mathf.PI * 2;
            // extremePosDirCalculated = shortestAngle > 0 ? Vector3.right : Vector3.left;

            // Generate positions evenly spaced between the starting and ending angles
			float arcLength = Mathf.Abs(angleRange * circleRadius);  // arc length for the given angle range
			int numberOfPositions = Mathf.FloorToInt(arcLength / 1f);
			float angleIncrement = angleRange / (numberOfPositions - 1);  // Calculate the angle increment
			
            Vector3[] positions = new Vector3[numberOfPositions];
			float radiusStep = (circleRadius - outerRadius) / (numberOfPositions - 1);

            for (int i = 0; i < numberOfPositions; i++)
            {
                // Calculate the angle for the current position
                float angle = startAngle + i * angleIncrement;

                // Wrap the angle within [-?, ?] range
                angle = Mathf.Atan2(Mathf.Sin(angle), Mathf.Cos(angle));

                // Calculate the position in polar coordinates
                float x = circleCenter.x + (circleRadius- (i * radiusStep)) * Mathf.Cos(angle);
                float z = circleCenter.z + (circleRadius- (i * radiusStep)) * Mathf.Sin(angle);

                // Create a Vector3 position
                Vector3 position = new(x, circleCenter.y, z);
				position.y += offsetFromGround;
                positions[i] = position;
            }
			
            return positions;
        }
		
		public void AddCharacter(GameObject character)
		{
			if(!allCharacters.Contains(character))
				allCharacters.Add(character);
			
			character.layer = LayerMask.NameToLayer("CampsiteAICharacter");
		}
		
		public void RemoveCharacter(GameObject character, int layerMask = -1)
		{
			if(allCharacters.Contains(character))
				allCharacters.Remove(character);
			
			if(layerMask != -1)
				character.layer = layerMask;
		}
		
		public List<GameObject> GetCharacters() => allCharacters;
		
		public void FreeAllDestPoss()
		{
			foreach(List<CampSitePos> item in possMap.Values)
				for(int i = 0; i < item.Count; i++)
					item[i].occupied = false;
				
			allCharacters.Clear();
		}
		
		/// <summary>
        /// 
        /// </summary>
        public static int FindQuadrant(Vector3 targetPos, Vector3 refPos)
        {
            // Calculate the vector from the center to the test point
            Vector3 vectorToTestPoint = targetPos - refPos;

            // Check the signs of x and y components to determine the quadrant
            if (vectorToTestPoint.x > 0 && vectorToTestPoint.z > 0)
            {
				return 1;
                // Debug.Log("First Quadrant");
            }
            else if (vectorToTestPoint.x < 0 && vectorToTestPoint.z > 0)
            {
				return 2;
                // Debug.Log("Second Quadrant");
            }
            else if (vectorToTestPoint.x < 0 && vectorToTestPoint.z < 0)
            {
				return 3;
                // Debug.Log("Third Quadrant");
            }
            else if (vectorToTestPoint.x > 0 && vectorToTestPoint.z < 0)
            {
				return 4;
                // Debug.Log("Fourth Quadrant");
            }
            else
            {
				return 0;
                // Debug.Log("On axis or at center");
            }

            return -1;
        }
		
		#region Serialization	
		[SerializeField, HideInInspector]
		private List<CampSitePos> savedPoss;
	
		public void OnBeforeSerialize()
		{
			_Serialize();
		}
		 
		public void OnAfterDeserialize()
		{
			_Deserialize();
		}
		
		public void _Serialize()
		{
			if(possMap == null)
				return;
			
			savedPoss = new();
			
			foreach(List<CampSitePos> item in possMap.Values)
				for(int i = 0; i < item.Count; i++)
					savedPoss.Add(item[i]);	
		}
		
		public void _Deserialize()
		{
			if(savedPoss == null)
				return;
			
			possMap = new();
			 
			int quadrant = -1;
			for(int i = 0; i < savedPoss.Count; i++)
			{
				quadrant = FindQuadrant(savedPoss[i].pos, lastPos);
				if (!possMap.ContainsKey(quadrant))
                    possMap[quadrant] = new List<CampSitePos>();
				possMap[quadrant].Add(savedPoss[i]);
			}
		}
		#endregion
    }
}

# if UNITY_EDITOR
[CustomEditor(typeof(CampSite))]
public class CampSiteEditor : Editor
{
	private CampSite campsite;
	private Vector2 lastMousePos = Vector3.zero;
	
	public void OnEnable()
	{
		campsite = target as CampSite;
		campsite._Awake();
	}
	
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		
		/*
		if(campsite.automaticDistribution)
			campsite.editMode = false;
		*/
		
		GUILayout.Space(5);
		
		if(!campsite.automaticDistribution)
		{
			if (GUILayout.Button("GeneratePositions", GUILayout.Height(50)))
			{
				campsite.GeneratePositions();
				SceneView.RepaintAll();
				EditorUtility.SetDirty(campsite);
			}
			
            using (new GUILayout.HorizontalScope())
            {
				if (GUILayout.Button("RemapPoss", GUILayout.Height(24)))
				{
					campsite.Remap();
					EditorUtility.SetDirty(campsite);
				}
			
				if (GUILayout.Button("ClearAll", GUILayout.Height(24)))
				{
					campsite.possMap.Clear();
					SceneView.RepaintAll();
					EditorUtility.SetDirty(campsite);
				}
            }
		}
	}
	
	public void OnSceneGUI()
	{		
		if(campsite == null)
			return;

		if(campsite.possMap == null)
			campsite.possMap = new();

		if(campsite.debug)
		{
			Handles.color = Color.white;
			Handles.DrawWireDisc(campsite.transform.position, Vector3.up, campsite.innerRadius);
            Handles.DrawWireDisc(campsite.transform.position, Vector3.up, campsite.outerRadius);
			
			if (campsite.possMap != null && campsite.possMap.Keys.Count > 0)
			{
				foreach (var key in campsite.possMap.Keys)
				{
					if (key == 1)
						Handles.color = Color.red;
					else if (key == 2)
						Handles.color = Color.green;
					else if (key == 3)
						Handles.color = Color.blue;
					else if (key == 4)
						Handles.color = Color.black;
					
					for (int i = 0; i < campsite.possMap[key].Count; i++)
						Handles.SphereHandleCap(0, campsite.possMap[key][i].pos, Quaternion.identity, 1f, EventType.Repaint);
				}
			}
		}

		if(campsite.editMode)
		{
			float y = campsite.transform.position.y + campsite.offsetFromGround;
			Vector3 mwp = GetMouseWorldPosition(SceneView.currentDrawingSceneView, new Vector3(0, y, 0));
			
			foreach(var item in campsite.possMap.Values)
			{
				for(int i = 0; i < item.Count; i++)
				{
					Handles.color = Color.red;
					item[i].pos = Handles.FreeMoveHandle(item[i].pos, Quaternion.identity, 0.15f, Vector2.zero, Handles.SphereHandleCap);
					
					Handles.color = Color.gray;
					DrawDottedCircle(item[i].pos, 0.5f, 10, 0.1f);
					
					// I am drawing a gizmo here for visual feedback when mouse is hovering over FreeMoveHandle, because for some reason 
					// its not giving any feedback as it should, at least in Unity version 2021.2.7f1 personal, comment out this code
					// to check with other versions.
					Handles.color = Color.yellow;
					if(Vector3.Distance(mwp, item[i].pos) < 0.15f*0.5f)
						Handles.SphereHandleCap(0, item[i].pos, Quaternion.identity, 0.15f, EventType.Repaint);
				}
			}
			
			if(lastMousePos != Event.current.mousePosition)
			{
				SceneView.RepaintAll();
				lastMousePos = Event.current.mousePosition;
			}
		}
		
		if(campsite.transform.position != campsite.LastPos || campsite.transform.rotation != campsite.LastRotation)
			campsite._Update();

		if (campsite.editMode && Selection.activeGameObject != campsite.transform.gameObject)
			Selection.activeGameObject = campsite.transform.gameObject;
	}
	
	private void DrawDottedCircle(Vector3 center, float radius, int segments, float dottedSpacing)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * Mathf.PI * 2.0f / segments;
            float angle2 = (i + 1) * Mathf.PI * 2.0f / segments;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;

            Handles.DrawDottedLine(point1, point2, dottedSpacing);
        }
    }
	
	public static Vector3 GetMouseWorldPosition(SceneView sceneView, Vector3 yPos=default)
    {
        // Get the current mouse position in screen coordinates
        Vector2 mousePos = Event.current.mousePosition;
		
        // Convert GUI coordinates to screen coordinates
        mousePos.y = sceneView.camera.pixelHeight - mousePos.y;

        // Create a ray from the screen coordinates
        Ray ray = sceneView.camera.ScreenPointToRay(mousePos);

        // Define a plane (e.g., horizontal plane at y = 0)
        Plane plane = new Plane(Vector3.up, yPos);

        // Find the intersection of the ray with the plane
        if (plane.Raycast(ray, out float distance))
        {
            // Calculate the intersection point
            Vector3 worldPosition = ray.GetPoint(distance);
            return worldPosition;
        }

        // Return zero vector if no intersection is found (shouldn't happen with a well-defined plane)
        return Vector3.zero;
    }

	public static Ray GetRay(SceneView sceneView)
	{
		Vector3 mousePos = Event.current.mousePosition;
		mousePos.y = sceneView.camera.pixelHeight - mousePos.y;
		Ray ray = sceneView.camera.ScreenPointToRay(mousePos);
		return ray;
	}
}
# endif 
