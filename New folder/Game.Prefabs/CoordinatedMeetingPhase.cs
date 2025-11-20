using System;
using Game.Citizens;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public struct CoordinatedMeetingPhase
{
	public TravelPurposeInEditor m_Purpose;

	public uint2 m_Delay;

	public PrefabBase m_Notification;
}
