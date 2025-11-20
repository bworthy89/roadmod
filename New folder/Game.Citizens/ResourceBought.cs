using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Citizens;

public struct ResourceBought : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Seller;

	public Entity m_Payer;

	public Resource m_Resource;

	public int m_Amount;

	public float m_Distance;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity seller = m_Seller;
		writer.Write(seller);
		Entity payer = m_Payer;
		writer.Write(payer);
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
		int amount = m_Amount;
		writer.Write(amount);
		float distance = m_Distance;
		writer.Write(distance);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity seller = ref m_Seller;
		reader.Read(out seller);
		ref Entity payer = ref m_Payer;
		reader.Read(out payer);
		reader.Read(out sbyte value);
		ref int amount = ref m_Amount;
		reader.Read(out amount);
		ref float distance = ref m_Distance;
		reader.Read(out distance);
		m_Resource = EconomyUtils.GetResource(value);
	}
}
