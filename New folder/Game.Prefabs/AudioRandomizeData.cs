using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(2)]
public struct AudioRandomizeData : IBufferElementData
{
	public Entity m_SFXEntity;
}
