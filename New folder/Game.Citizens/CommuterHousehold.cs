using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct CommuterHousehold : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_OriginalFrom;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_OriginalFrom);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.commuterOriginalFrom)
		{
			ref Entity originalFrom = ref m_OriginalFrom;
			reader.Read(out originalFrom);
		}
	}
}
