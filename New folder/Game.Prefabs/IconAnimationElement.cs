using Colossal.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct IconAnimationElement : IBufferElementData
{
	public float m_Duration;

	public AnimationCurve3 m_AnimationCurve;
}
