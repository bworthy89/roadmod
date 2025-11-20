using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct ExtractorFacility : IComponentData, IQueryTypeParameter, ISerializable
{
	public ExtractorFlags m_Flags;

	public byte m_Timer;

	public BuildingFlags m_MainBuildingFlags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		ExtractorFlags flags = m_Flags;
		writer.Write((byte)flags);
		byte timer = m_Timer;
		writer.Write(timer);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		ref byte timer = ref m_Timer;
		reader.Read(out timer);
		m_Flags = (ExtractorFlags)value;
	}
}
