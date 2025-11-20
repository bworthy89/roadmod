using Colossal.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct EffectAnimation : IBufferElementData
{
	public uint m_DurationFrames;

	public AnimationCurve1 m_AnimationCurve;
}
