using UnityEngine;


namespace ExpressEnginex
{
	public static class ImageUtils
	{
		/// <summary>
		/// Maps a world position to a pixel on image and validates if the pixel color value on specified image channel is greater than threshold. 
		/// </summary>
		public static bool ValidateSpawnOnTex(
			Texture2D tex,
			float worldScale,
			Vector3 pos,
			Vector4 channelIDx,
			float threshold = 0.01f)
		{
			if (!tex)
				return true;

			if (channelIDx.x == 1 && GetPixel(tex, worldScale, pos).r > threshold)
				return true;

			else if (channelIDx.y == 1 && GetPixel(tex, worldScale, pos).g > threshold)
				return true;

			else if (channelIDx.z == 1 && GetPixel(tex, worldScale, pos).b > threshold)
				return true;

			else if (channelIDx.w == 1 && GetPixel(tex, worldScale, pos).a > threshold)
				return true;

			return false;
		}

		/// <summary>
		/// Maps a world position to a pixel on image and returns the pixel.
		/// </summary>
		public static Color GetPixel(Texture2D tex, float worldScale, Vector3 position)
		{
			if (!tex)
				return Color.black;

			/*
			Vector3 point = spawnPoint * (tex.width / boundsScale);
			return tex.GetPixel((int)(point.x), (int)(point.z));
			*/

			return tex.GetPixel((int)((position * (tex.width / worldScale)).x), (int)((position * (tex.width / worldScale)).z));
		}

		public static string ConvertColorToHex(float r, float g, float b)
		{
			// Convert RGB values to hexadecimal
			int r_ = (int)(r * 255f);
			int g_ = (int)(g * 255f);
			int b_ = (int)(b * 255f);

			// Create hex string
			string hex = string.Format("#{0:X2}{1:X2}{2:X2}", r_, g_, b_);

			return hex;
		}

		public static Color ConvertHexToColor(string hex)
		{
			// Remove '#' if present
			if (hex.StartsWith("#"))
				hex = hex[1..];

			// Convert hexadecimal string to Color
			Color color = new Color32(
				(byte)System.Convert.ToUInt32(hex[..2], 16),            // Red component
				(byte)System.Convert.ToUInt32(hex.Substring(2, 2), 16), // Green component
				(byte)System.Convert.ToUInt32(hex.Substring(4, 2), 16), // Blue component
				255 // Alpha component (255 = fully opaque)
			);

			return color;
		}
	}
}
