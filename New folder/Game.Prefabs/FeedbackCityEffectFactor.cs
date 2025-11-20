using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(256)]
public struct FeedbackCityEffectFactor : IBufferElementData
{
	public float m_Factor;
}
