using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetCompositionObject : IBufferElementData
{
	public Entity m_Prefab;

	public float2 m_Position;

	public float3 m_Offset;

	public quaternion m_Rotation;

	public SubObjectFlags m_Flags;

	public CompositionFlags.General m_SpacingIgnore;

	public float2 m_UseCurveRotation;

	public float2 m_CurveOffsetRange;

	public int m_Probability;

	public float m_Spacing;

	public float m_AvoidSpacing;

	public float m_MinLength;
}
