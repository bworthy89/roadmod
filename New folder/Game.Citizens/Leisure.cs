using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct Leisure : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetAgent;

	public uint m_LastPossibleFrame;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetAgent = m_TargetAgent;
		writer.Write(targetAgent);
		uint lastPossibleFrame = m_LastPossibleFrame;
		writer.Write(lastPossibleFrame);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity targetAgent = ref m_TargetAgent;
		reader.Read(out targetAgent);
		ref uint lastPossibleFrame = ref m_LastPossibleFrame;
		reader.Read(out lastPossibleFrame);
	}
}
