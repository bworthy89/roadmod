using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Rendering;

[InternalBufferCapacity(0)]
public struct PlaybackLayer : IBufferElementData, IEmptySerializable
{
	public float m_RelativeClipTime;

	public float m_ClipTime;

	public float m_PlaySpeed;

	public short m_ClipIndex;

	public byte m_LayerIndex;
}
