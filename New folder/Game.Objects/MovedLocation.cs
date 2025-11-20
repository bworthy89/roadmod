using Unity.Entities;
using Unity.Mathematics;

namespace Game.Objects;

public struct MovedLocation : IComponentData, IQueryTypeParameter
{
	public float3 m_OldPosition;
}
