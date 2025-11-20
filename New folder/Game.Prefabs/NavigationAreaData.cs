using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct NavigationAreaData : IComponentData, IQueryTypeParameter, ISerializable
{
	public RouteConnectionType m_ConnectionType;

	public RouteConnectionType m_SecondaryType;

	public TrackTypes m_TrackTypes;

	public RoadTypes m_RoadTypes;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte value = (byte)m_ConnectionType;
		writer.Write(value);
		byte value2 = (byte)m_SecondaryType;
		writer.Write(value2);
		TrackTypes trackTypes = m_TrackTypes;
		writer.Write((byte)trackTypes);
		RoadTypes roadTypes = m_RoadTypes;
		writer.Write((byte)roadTypes);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		reader.Read(out byte value2);
		reader.Read(out byte value3);
		reader.Read(out byte value4);
		m_ConnectionType = (RouteConnectionType)value;
		m_SecondaryType = (RouteConnectionType)value2;
		m_TrackTypes = (TrackTypes)value3;
		m_RoadTypes = (RoadTypes)value4;
	}
}
