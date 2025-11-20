using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Economy;

public struct Resources : IBufferElementData, ISerializable
{
	public Resource m_Resource;

	public int m_Amount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
		int amount = m_Amount;
		writer.Write(amount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out sbyte value);
		ref int amount = ref m_Amount;
		reader.Read(out amount);
		m_Resource = EconomyUtils.GetResource(value);
		if (reader.context.version < Version.resetNegativeResource && m_Resource != Resource.Money)
		{
			if (m_Amount > 1000000)
			{
				m_Amount = 1000000;
			}
			else if (m_Amount < 0)
			{
				m_Amount = 0;
			}
		}
	}
}
