using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct Duration : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_StartFrame;

	public uint m_EndFrame;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint startFrame = m_StartFrame;
		writer.Write(startFrame);
		uint endFrame = m_EndFrame;
		writer.Write(endFrame);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref uint startFrame = ref m_StartFrame;
		reader.Read(out startFrame);
		ref uint endFrame = ref m_EndFrame;
		reader.Read(out endFrame);
	}
}
