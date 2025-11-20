using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

public struct Storage : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Amount;

	public float m_WorkAmount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int amount = m_Amount;
		writer.Write(amount);
		float workAmount = m_WorkAmount;
		writer.Write(workAmount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int amount = ref m_Amount;
		reader.Read(out amount);
		if (reader.context.version >= Version.garbageFacilityRefactor)
		{
			ref float workAmount = ref m_WorkAmount;
			reader.Read(out workAmount);
		}
	}
}
