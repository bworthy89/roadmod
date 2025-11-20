using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct Train : IComponentData, IQueryTypeParameter, ISerializable
{
	public TrainFlags m_Flags;

	public Train(TrainFlags flags)
	{
		m_Flags = flags;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((uint)m_Flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		m_Flags = (TrainFlags)value;
		if (reader.context.version < Version.trainPrefabFlags)
		{
			m_Flags |= TrainFlags.Pantograph;
		}
	}
}
