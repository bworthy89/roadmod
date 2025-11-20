using Game.Citizens;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct HaveCoordinatedMeetingData : IBufferElementData
{
	public TravelPurpose m_TravelPurpose;

	public uint2 m_Delay;

	public Entity m_Notification;
}
