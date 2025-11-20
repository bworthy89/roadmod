using UnityEngine;

namespace Game.Input;

public class SmoothFloatProcessor : SmoothProcessor<float>
{
	private const float kDelta = 1E-06f;

	protected override float Smooth(float value, ref float lastValue, float delta)
	{
		if (m_Smoothing > 0f)
		{
			float t = Mathf.Pow(m_Smoothing, delta);
			value = Mathf.Lerp(value, lastValue, t);
			if (Mathf.Abs(value) < 1E-06f)
			{
				value = 0f;
			}
		}
		lastValue = value;
		if (m_Time)
		{
			value *= Time.deltaTime;
		}
		return value;
	}
}
