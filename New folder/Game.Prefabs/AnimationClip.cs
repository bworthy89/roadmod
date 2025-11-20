using Colossal.Mathematics;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct AnimationClip : IBufferElementData
{
	public float m_AnimationLength;

	public float m_MovementSpeed;

	public float m_TargetValue;

	public float m_FrameRate;

	public float m_Acceleration;

	public int m_RootMotionBone;

	public int m_InfoIndex;

	public int2 m_MotionRange;

	public Bounds1 m_SpeedRange;

	public AnimatedPropID m_PropID;

	public AnimationType m_Type;

	public AnimationLayer m_Layer;

	public ClipState m_ClipState;

	public ActivityType m_Activity;

	public GenderMask m_Gender;

	public ActivityCondition m_Conditions;

	public byte m_VariationIndex;

	public AnimationPlayback m_Playback;

	public byte m_VariationCount;
}
