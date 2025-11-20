using UnityEngine;

namespace Game.Input;

public class SmoothVector2Processor : SmoothProcessor<Vector2>
{
	private const float kDelta = 1E-12f;

	protected override Vector2 Smooth(Vector2 value, ref Vector2 lastValue, float delta)
	{
		if (m_Smoothing > 0f)
		{
			float t = Mathf.Pow(m_Smoothing, delta);
			value = Vector2.Lerp(value, lastValue, t);
			if (value.sqrMagnitude < 1E-12f)
			{
				value = Vector2.zero;
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
