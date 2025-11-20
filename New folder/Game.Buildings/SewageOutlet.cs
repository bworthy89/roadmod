using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct SewageOutlet : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Capacity;

	public int m_LastProcessed;

	public int m_LastPurified;

	public int m_UsedPurified;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int capacity = m_Capacity;
		writer.Write(capacity);
		int lastProcessed = m_LastProcessed;
		writer.Write(lastProcessed);
		int lastPurified = m_LastPurified;
		writer.Write(lastPurified);
		int usedPurified = m_UsedPurified;
		writer.Write(usedPurified);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version < Version.waterPipeFlowSim)
		{
			reader.Read(out int _);
			if (reader.context.version >= Version.stormWater)
			{
				reader.Read(out int _);
			}
			if (reader.context.version >= Version.sewageSelectedInfoFix)
			{
				reader.Read(out int _);
			}
		}
		else
		{
			ref int capacity = ref m_Capacity;
			reader.Read(out capacity);
			ref int lastProcessed = ref m_LastProcessed;
			reader.Read(out lastProcessed);
			ref int lastPurified = ref m_LastPurified;
			reader.Read(out lastPurified);
			ref int usedPurified = ref m_UsedPurified;
			reader.Read(out usedPurified);
		}
	}
}
