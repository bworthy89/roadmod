using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Battery : IComponentData, IQueryTypeParameter, ISerializable
{
	public long m_StoredEnergy;

	public int m_Capacity;

	public int m_LastFlow;

	public int storedEnergyHours => (int)(m_StoredEnergy / 85);

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		long storedEnergy = m_StoredEnergy;
		writer.Write(storedEnergy);
		int capacity = m_Capacity;
		writer.Write(capacity);
		int lastFlow = m_LastFlow;
		writer.Write(lastFlow);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref long storedEnergy = ref m_StoredEnergy;
		reader.Read(out storedEnergy);
		if (reader.context.version >= Version.batteryStats)
		{
			ref int capacity = ref m_Capacity;
			reader.Read(out capacity);
		}
		if (reader.context.version >= Version.batteryLastFlow)
		{
			ref int lastFlow = ref m_LastFlow;
			reader.Read(out lastFlow);
		}
	}
}
