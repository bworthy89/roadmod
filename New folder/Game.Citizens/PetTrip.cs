using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

[InternalBufferCapacity(1)]
public struct PetTrip : IBufferElementData, ISerializable
{
	public Entity m_Source;

	public Entity m_Target;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity source = m_Source;
		writer.Write(source);
		Entity target = m_Target;
		writer.Write(target);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity source = ref m_Source;
		reader.Read(out source);
		ref Entity target = ref m_Target;
		reader.Read(out target);
	}
}
