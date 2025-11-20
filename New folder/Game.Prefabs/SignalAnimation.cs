using Unity.Collections;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct SignalAnimation
{
	private FixedList128Bytes<SignalGroupMask> m_Buffer;

	private float m_LengthFactor;

	public SignalAnimation(SignalGroupMask[] masks)
	{
		m_Buffer = default(FixedList128Bytes<SignalGroupMask>);
		m_Buffer.Length = masks.Length;
		for (int i = 0; i < masks.Length; i++)
		{
			m_Buffer[i] = masks[i];
		}
		m_LengthFactor = m_Buffer.Length;
	}

	public float Evaluate(SignalGroupMask signalGroupMask, float time)
	{
		int index = math.clamp((int)math.floor(time * m_LengthFactor), 0, m_Buffer.Length - 1);
		return math.select(0f, 1f, (m_Buffer[index] & signalGroupMask) != 0);
	}
}
