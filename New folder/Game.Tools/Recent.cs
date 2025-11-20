using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Tools;

public struct Recent : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_ModificationFrame;

	public int m_ModificationCost;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint modificationFrame = m_ModificationFrame;
		writer.Write(modificationFrame);
		int modificationCost = m_ModificationCost;
		writer.Write(modificationCost);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref uint modificationFrame = ref m_ModificationFrame;
		reader.Read(out modificationFrame);
		ref int modificationCost = ref m_ModificationCost;
		reader.Read(out modificationCost);
	}
}
