using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input;

public abstract class SmoothProcessor<TValue> : InputProcessor<TValue>, IDisableableProcessor where TValue : struct
{
	public float m_Smoothing = 1E-06f;

	public bool m_CanBeDisabled = true;

	public bool m_Time;

	private TValue m_LastValue;

	private int m_LastFrame;

	private float m_LastTime;

	private float m_LastDelta;

	public bool canBeDisabled => m_CanBeDisabled;

	public bool disabled { get; set; }

	public override TValue Process(TValue value, InputControl control)
	{
		if (canBeDisabled && disabled)
		{
			return value;
		}
		if (m_LastFrame == 0)
		{
			m_LastValue = default(TValue);
			m_LastTime = Time.time;
			m_LastFrame = Time.frameCount;
			value = Smooth(value, ref m_LastValue, Time.deltaTime);
		}
		else if (m_LastFrame == Time.frameCount)
		{
			TValue lastValue = m_LastValue;
			value = Smooth(value, ref lastValue, m_LastDelta);
		}
		else
		{
			m_LastDelta = Time.time - m_LastTime;
			value = Smooth(value, ref m_LastValue, m_LastDelta);
			m_LastTime = Time.time;
			m_LastFrame = Time.frameCount;
		}
		return value;
	}

	protected abstract TValue Smooth(TValue value, ref TValue lastValue, float delta);
}
