using Game.Rendering;
using Unity.Entities;

namespace Game.Prefabs;

public struct ActivityPropData : IComponentData, IQueryTypeParameter
{
	public ActivityMask m_ActivityMask;

	public AnimationLayerMask m_AnimationLayerMask;

	public AnimatedPropID m_AnimatedPropID;

	public int m_BoneCount;

	public int m_ShapeCount;

	public int m_RestPoseClipIndex;
}
