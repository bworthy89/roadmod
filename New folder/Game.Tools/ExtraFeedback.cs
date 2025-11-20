using Unity.Entities;

namespace Game.Tools;

[InternalBufferCapacity(32)]
public struct ExtraFeedback : IBufferElementData
{
	public Entity m_Prefab;
}
