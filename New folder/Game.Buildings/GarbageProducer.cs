using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct GarbageProducer : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_CollectionRequest;

	public int m_Garbage;

	public GarbageProducerFlags m_Flags;

	public byte m_DispatchIndex;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity collectionRequest = m_CollectionRequest;
		writer.Write(collectionRequest);
		int garbage = m_Garbage;
		writer.Write(garbage);
		GarbageProducerFlags flags = m_Flags;
		writer.Write((byte)flags);
		byte dispatchIndex = m_DispatchIndex;
		writer.Write(dispatchIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity collectionRequest = ref m_CollectionRequest;
		reader.Read(out collectionRequest);
		ref int garbage = ref m_Garbage;
		reader.Read(out garbage);
		if (reader.context.version >= Version.garbageProducerFlags)
		{
			reader.Read(out byte value);
			m_Flags = (GarbageProducerFlags)value;
		}
		if (reader.context.version >= Version.requestDispatchIndex)
		{
			ref byte dispatchIndex = ref m_DispatchIndex;
			reader.Read(out dispatchIndex);
		}
	}
}
