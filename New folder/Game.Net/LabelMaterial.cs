using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct LabelMaterial : ISharedComponentData, IQueryTypeParameter, IEmptySerializable
{
	public int m_Index;
}
