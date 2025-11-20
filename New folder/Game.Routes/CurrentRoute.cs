using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct CurrentRoute : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Route;

	public CurrentRoute(Entity route)
	{
		m_Route = route;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Route);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Route);
	}
}
