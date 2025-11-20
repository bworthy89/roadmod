using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct BicycleOwner : IComponentData, IQueryTypeParameter, IEmptySerializable, IEnableableComponent
{
	public Entity m_Bicycle;
}
