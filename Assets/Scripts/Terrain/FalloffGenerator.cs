using UnityEngine;
using System.Collections;

public static class FalloffGenerator {

	public static float[,] GenerateFalloffMap(int size, float radius) {
		
		float[,] map = new float[size,size];

		float R = size / 2;
		float r = R * radius;
		float falloffMax = R - r;

		for (int i = 0; i < size; i++) {
			for (int j = 0; j < size; j++) {

				int x = i - size / 2;
				int y = j - size / 2;
				float pointPosition = Mathf.Sqrt(x*x+y*y);

				if (pointPosition > r)
				{
					float E = (pointPosition - r) / falloffMax;

					if (E > 1)
						E = 1;

					map[i, j] = E;
				}
				else
					map[i, j] = 0;
			}
		}

		return map;
	}

	static float Evaluate(float value) {
		float a = 4;
		float b = 2.2f;

		return Mathf.Pow (value, a) / (Mathf.Pow (value, a) + Mathf.Pow (b - b * value, a));
	}
}
