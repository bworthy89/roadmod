using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(256)]
public struct FeedbackLocalEffectFactor : IBufferElementData
{
	public float m_Factor;
}
