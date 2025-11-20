using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct ActivityLocationElement : IBufferElementData
{
	public Entity m_Prefab;

	public ActivityMask m_ActivityMask;

	public ActivityFlags m_ActivityFlags;

	public float3 m_Position;

	public quaternion m_Rotation;

	public AnimatedPropID m_PropID;
}
