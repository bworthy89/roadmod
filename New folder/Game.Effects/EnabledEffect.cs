using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Effects;

[FormerlySerializedAs("Game.Effects.EffectOwner, Game")]
[InternalBufferCapacity(2)]
public struct EnabledEffect : IBufferElementData, IEmptySerializable
{
	public int m_EffectIndex;

	public int m_EnabledIndex;
}
