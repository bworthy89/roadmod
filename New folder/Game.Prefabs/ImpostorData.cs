using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct ImpostorData : IComponentData, IQueryTypeParameter
{
	public float3 m_Offset;

	public float m_Size;
}
