using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct LabelExtents : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Bounds2 m_Bounds;
}
