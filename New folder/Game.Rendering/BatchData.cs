using Unity.Entities;

namespace Game.Rendering;

public struct BatchData
{
	public Entity m_LodMesh;

	public int m_VTIndex0;

	public int m_VTIndex1;

	public float m_VTSizeFactor;

	public BatchRenderFlags m_RenderFlags;

	public byte m_ShadowCastingMode;

	public byte m_Layer;

	public byte m_SubMeshIndex;

	public byte m_MinLod;

	public byte m_ShadowLod;

	public byte m_LodIndex;

	public float m_ShadowArea;

	public float m_ShadowHeight;
}
