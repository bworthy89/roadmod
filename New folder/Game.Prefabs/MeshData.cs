using Colossal.Mathematics;
using Game.Rendering;
using Unity.Entities;

namespace Game.Prefabs;

public struct MeshData : IComponentData, IQueryTypeParameter
{
	public Bounds3 m_Bounds;

	public MeshFlags m_State;

	public DecalLayers m_DecalLayer;

	public MeshLayer m_DefaultLayers;

	public MeshLayer m_AvailableLayers;

	public MeshType m_AvailableTypes;

	public byte m_MinLod;

	public byte m_ShadowLod;

	public float m_LodBias;

	public float m_ShadowBias;

	public float m_SmoothingDistance;

	public int m_SubMeshCount;

	public int m_IndexCount;

	public int m_TilingCount;
}
