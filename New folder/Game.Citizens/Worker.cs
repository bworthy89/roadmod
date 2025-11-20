using Colossal.Serialization.Entities;
using Game.Companies;
using Unity.Entities;

namespace Game.Citizens;

public struct Worker : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Workplace;

	public float m_LastCommuteTime;

	public byte m_Level;

	public Workshift m_Shift;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity workplace = m_Workplace;
		writer.Write(workplace);
		float lastCommuteTime = m_LastCommuteTime;
		writer.Write(lastCommuteTime);
		byte level = m_Level;
		writer.Write(level);
		Workshift shift = m_Shift;
		writer.Write((byte)shift);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity workplace = ref m_Workplace;
		reader.Read(out workplace);
		ref float lastCommuteTime = ref m_LastCommuteTime;
		reader.Read(out lastCommuteTime);
		ref byte level = ref m_Level;
		reader.Read(out level);
		reader.Read(out byte value);
		m_Shift = (Workshift)value;
	}
}
