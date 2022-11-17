using System.IO;
using UnityEngine;

namespace LossyCompression
{
	class Utils
	{
		// Map from [-1, +1] in float to [-32000, +32000] in short. Leaving some space to prevent overflow
		private const float FLOAT_PRECISION_MULT = 32000f;

		private static readonly Quaternion[] array = new Quaternion[] 
		{ Quaternion.identity, Quaternion.Euler(0f, 90f, 0f), Quaternion.Euler(0f, 180f, 0f), Quaternion.Euler(0f, 270f, 0f)};

		// Compress Quaternion from 16 bytes to 1 or 7 bytes
		public static bool WriteCompressedRotation(BinaryWriter writer, in Vector3 pos, in Quaternion rot)
        {
			if (pos != null)
            {
				var localRot = Quaternion.Inverse(Maths.SphericalRotation(pos, 0f)) * rot;
				for (int i = 0; i < 4; i++)
                {
					// id 4~8: the local rotation and one of four direction difference < 1deg
					if (Quaternion.Dot(localRot, array[i]) > 0.99984769515)
                    {
						writer.Write((byte)(4 + i));
						return true;
					}
                }
			}

			// smallest three trick from https://gafferongames.com/post/snapshot_compression/
			int maxIndex = 0;
			float max = float.MinValue;
			for (int i = 0; i < 4; i++)
			{
				float value = rot[i] * rot[i];
				if (value > max)
				{
					maxIndex = i;
					max = value;
				}
			}
			float signed = (rot[maxIndex] + 0.00003125f) >= 0 ? 1f : -1f;

			writer.Write((byte)maxIndex);
			for (int i = 0; i < 4; i++)
            {
				if (i == maxIndex) continue;
				writer.Write((short)(rot[i] * signed * FLOAT_PRECISION_MULT));
			}
			return false;
		}

		public static Quaternion ReadCompressedRotation(BinaryReader reader, in Vector3 pos)
		{
			int id = reader.ReadByte();

			// Calcualte from local pose (N, W, E, S)
			if (id >= 4 && id <= 8)
			{
				return Maths.SphericalRotation(pos, 0f) * array[id-4];
			}

			// Read the other three fields and derive the value of the omitted field
			float a = reader.ReadInt16() / FLOAT_PRECISION_MULT;
			float b = reader.ReadInt16() / FLOAT_PRECISION_MULT;
			float c = reader.ReadInt16() / FLOAT_PRECISION_MULT;
			float d = Mathf.Sqrt(1f - (a * a + b * b + c * c));
			switch (id)
            {
				case 0:
					return new Quaternion(d, a, b, c);
				case 1:
					return new Quaternion(a, d, b, c);
				case 2:
					return new Quaternion(a, b, d, c);
				case 3:
					return new Quaternion(a, b, c, d);
				default: // Should not reach
					return Quaternion.identity;
			}
		}
	}
}
