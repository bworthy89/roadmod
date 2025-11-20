using Unity.Entities;

namespace Game.Prefabs;

public struct CharacterStyleData : IComponentData, IQueryTypeParameter
{
	public ActivityMask m_ActivityMask;

	public AnimationLayerMask m_AnimationLayerMask;

	public int m_BoneCount;

	public int m_ShapeCount;

	public int m_RestPoseClipIndex;
}
