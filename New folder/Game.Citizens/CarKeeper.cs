using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct CarKeeper : IComponentData, IQueryTypeParameter, IEmptySerializable, IEnableableComponent
{
	public Entity m_Car;
}
