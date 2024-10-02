using UnityEngine;


namespace ExpressEnginex.Utils
{	
	public static class Other
	{
		/// <summary>
		/// Returns the position of a tile in a 2d array, given a world position, tile size and tile radius.
		/// </summary>
		public static Vector3 GetTilePos(Vector3 pos, float tileSize, float tileRadius)
		{
			return Vector3.right   * ((pos.x / tileSize) * tileSize + tileRadius) +
				   Vector3.forward * ((pos.z / tileSize) * tileSize + tileRadius);
		}

		/// <summary>
		/// Returns 1D index of an element in a 2D array, given world position on tile, tile size and total tiles count. 
		/// </summary>
		public static int GetTileIdxFromPos(Vector3 pos, int tileSize, int tilesCount_1D)
		{
			return (int)(pos.x / tileSize) * tilesCount_1D + (int)(pos.z / tileSize);
		}
	}
}
