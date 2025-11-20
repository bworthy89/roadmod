using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Triggers;

public struct LifePathEvent : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_EventPrefab;

	public Entity m_Target;

	public uint m_Date;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity eventPrefab = m_EventPrefab;
		writer.Write(eventPrefab);
		Entity target = m_Target;
		writer.Write(target);
		uint date = m_Date;
		writer.Write(date);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity eventPrefab = ref m_EventPrefab;
		reader.Read(out eventPrefab);
		ref Entity target = ref m_Target;
		reader.Read(out target);
		ref uint date = ref m_Date;
		reader.Read(out date);
	}
}
