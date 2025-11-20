using System;
using Game.Audio.Radio;
using Unity.Entities;

namespace Game.Triggers;

public struct RadioTag : IEquatable<RadioTag>
{
	public Entity m_Event;

	public Entity m_Target;

	public Radio.SegmentType m_SegmentType;

	public int m_EmergencyFrameDelay;

	public bool Equals(RadioTag other)
	{
		if (m_Event == other.m_Event && m_Target == other.m_Target)
		{
			return m_SegmentType == other.m_SegmentType;
		}
		return false;
	}
}
