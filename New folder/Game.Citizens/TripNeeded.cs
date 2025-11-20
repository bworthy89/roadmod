using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Citizens;

public struct TripNeeded : IBufferElementData, ISerializable
{
	public Entity m_TargetAgent;

	public Purpose m_Purpose;

	public int m_Data;

	public Resource m_Resource;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetAgent = m_TargetAgent;
		writer.Write(targetAgent);
		Purpose purpose = m_Purpose;
		writer.Write((byte)purpose);
		int data = m_Data;
		writer.Write(data);
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity targetAgent = ref m_TargetAgent;
		reader.Read(out targetAgent);
		reader.Read(out byte value);
		m_Purpose = (Purpose)value;
		ref int data = ref m_Data;
		reader.Read(out data);
		if (reader.context.version >= Version.resource32bitFix)
		{
			reader.Read(out sbyte value2);
			m_Resource = EconomyUtils.GetResource(value2);
		}
		else
		{
			reader.Read(out int value3);
			m_Resource = (Resource)value3;
		}
	}
}
