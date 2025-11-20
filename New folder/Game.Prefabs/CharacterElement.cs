using Game.Rendering;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct CharacterElement : IBufferElementData
{
	public Entity m_Style;

	public BlendWeights m_ShapeWeights;

	public BlendWeights m_TextureWeights;

	public BlendWeights m_OverlayWeights;

	public BlendWeights m_MaskWeights;

	public int m_RestPoseClipIndex;

	public int m_CorrectiveClipIndex;
}
