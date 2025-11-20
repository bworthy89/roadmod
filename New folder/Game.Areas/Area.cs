using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

public struct Area : IComponentData, IQueryTypeParameter, ISerializable
{
	public AreaFlags m_Flags;

	public Area(AreaFlags flags)
	{
		m_Flags = flags;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)m_Flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		m_Flags = (AreaFlags)value;
		if (reader.context.version < Version.mapTileCompleteFix)
		{
			m_Flags |= AreaFlags.Complete;
		}
	}
}
