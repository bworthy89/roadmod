using System.Runtime.InteropServices;
using Colossal.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[StructLayout(LayoutKind.Explicit)]
[InternalBufferCapacity(0)]
public struct LightAnimation : IBufferElementData
{
	[FieldOffset(0)]
	public uint m_DurationFrames;

	[FieldOffset(4)]
	public AnimationCurve1 m_AnimationCurve;

	[FieldOffset(4)]
	public SignalAnimation m_SignalAnimation;
}
