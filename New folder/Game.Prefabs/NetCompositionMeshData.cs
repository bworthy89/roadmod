using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct NetCompositionMeshData : IComponentData, IQueryTypeParameter
{
	public MeshLayer m_DefaultLayers;

	public MeshLayer m_AvailableLayers;

	public MeshFlags m_State;

	public CompositionFlags m_Flags;

	public Bounds1 m_HeightRange;

	public float m_Width;

	public float m_MiddleOffset;

	public float m_IndexFactor;

	public float m_LodBias;

	public float m_ShadowBias;

	public int m_Hash;
}
