using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Economy;

public struct ResourceInfo : IComponentData, IQueryTypeParameter, ISerializable
{
	public Resource m_Resource;

	public float m_Price;

	public float m_TradeDistance;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
		float price = m_Price;
		writer.Write(price);
		float tradeDistance = m_TradeDistance;
		writer.Write(tradeDistance);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out sbyte value);
		ref float price = ref m_Price;
		reader.Read(out price);
		ref float tradeDistance = ref m_TradeDistance;
		reader.Read(out tradeDistance);
		m_Resource = EconomyUtils.GetResource(value);
	}
}
