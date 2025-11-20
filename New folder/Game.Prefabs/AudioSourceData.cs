using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(1)]
public struct AudioSourceData : IBufferElementData
{
	public Entity m_SFXEntity;
}
