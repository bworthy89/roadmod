using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct AuxiliaryNet : IBufferElementData
{
	public Entity m_Prefab;

	public float3 m_Position;

	public NetInvertMode m_InvertMode;
}
