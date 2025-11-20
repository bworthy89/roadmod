using Colossal.Serialization.Entities;
using Game.Rendering;
using Unity.Entities;

namespace Game.Prefabs;

public struct BrandData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public ColorSet m_ColorSet;
}
