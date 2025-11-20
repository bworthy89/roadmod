using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetCompositionLane : IBufferElementData
{
	public Entity m_Lane;

	public float3 m_Position;

	public LaneFlags m_Flags;

	public byte m_Carriageway;

	public byte m_Group;

	public byte m_Index;

	public NetCompositionLane(DefaultNetLane source)
	{
		m_Lane = source.m_Lane;
		m_Position = source.m_Position;
		m_Flags = source.m_Flags;
		m_Carriageway = source.m_Carriageway;
		m_Group = source.m_Group;
		m_Index = source.m_Index;
	}
}
