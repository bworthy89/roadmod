using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct BuildingNotifications : IComponentData, IQueryTypeParameter, ISerializable
{
	public BuildingNotification m_Notifications;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)m_Notifications);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		m_Notifications = (BuildingNotification)value;
	}

	public bool HasNotification(BuildingNotification notification)
	{
		return (m_Notifications & notification) != 0;
	}
}
