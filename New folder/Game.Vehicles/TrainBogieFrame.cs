using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

[InternalBufferCapacity(4)]
public struct TrainBogieFrame : IBufferElementData, IEmptySerializable
{
	public Entity m_FrontLane;

	public Entity m_RearLane;
}
