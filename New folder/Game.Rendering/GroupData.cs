using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Rendering;

public struct GroupData
{
	public Entity m_Mesh;

	public float3 m_SecondaryCenter;

	public float3 m_SecondarySize;

	public MeshLayer m_Layer;

	public MeshType m_MeshType;

	public ushort m_Partition;

	public BatchRenderFlags m_RenderFlags;

	public byte m_LodCount;

	private const int MAX_PROPERTY_COUNT1 = 14;

	private const int MAX_PROPERTY_COUNT2 = 16;

	public const int MAX_PROPERTY_COUNT = 16;

	private unsafe fixed sbyte m_Properties[16];

	public unsafe bool GetPropertyIndex(int property, out int index)
	{
		index = m_Properties[property];
		return index >= 0;
	}

	public unsafe void SetPropertyIndex(int property, int index)
	{
		m_Properties[property] = (sbyte)index;
	}
}
