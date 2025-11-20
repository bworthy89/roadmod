using Colossal.Serialization.Entities;
using Game.Citizens;
using Game.Economy;
using Unity.Entities;

namespace Game.Creatures;

public struct Divert : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public Resource m_Resource;

	public int m_Data;

	public Game.Citizens.Purpose m_Purpose;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity target = m_Target;
		writer.Write(target);
		Game.Citizens.Purpose purpose = m_Purpose;
		writer.Write((byte)purpose);
		int data = m_Data;
		writer.Write(data);
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity target = ref m_Target;
		reader.Read(out target);
		reader.Read(out byte value);
		m_Purpose = (Game.Citizens.Purpose)value;
		if (reader.context.version >= Version.divertResources)
		{
			ref int data = ref m_Data;
			reader.Read(out data);
			reader.Read(out sbyte value2);
			m_Resource = EconomyUtils.GetResource(value2);
		}
	}
}
