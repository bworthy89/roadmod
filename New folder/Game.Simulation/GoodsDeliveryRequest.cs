using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Simulation;

public struct GoodsDeliveryRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_ResourceNeeder;

	public GoodsDeliveryFlags m_Flags;

	public Resource m_Resource;

	public int m_Amount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity resourceNeeder = m_ResourceNeeder;
		writer.Write(resourceNeeder);
		GoodsDeliveryFlags flags = m_Flags;
		writer.Write((ushort)flags);
		int resourceIndex = EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(resourceIndex);
		int amount = m_Amount;
		writer.Write(amount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity resourceNeeder = ref m_ResourceNeeder;
		reader.Read(out resourceNeeder);
		reader.Read(out ushort value);
		m_Flags = (GoodsDeliveryFlags)value;
		reader.Read(out int value2);
		m_Resource = EconomyUtils.GetResource(value2);
		ref int amount = ref m_Amount;
		reader.Read(out amount);
	}
}
