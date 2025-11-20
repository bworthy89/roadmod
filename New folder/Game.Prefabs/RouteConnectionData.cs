using Colossal.Serialization.Entities;
using Game.Net;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

public struct RouteConnectionData : IComponentData, IQueryTypeParameter, ISerializable
{
	public RouteConnectionType m_AccessConnectionType;

	public RouteConnectionType m_RouteConnectionType;

	public TrackTypes m_AccessTrackType;

	public TrackTypes m_RouteTrackType;

	public RoadTypes m_AccessRoadType;

	public RoadTypes m_RouteRoadType;

	public SizeClass m_RouteSizeClass;

	public float m_StartLaneOffset;

	public float m_EndMargin;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte value = (byte)m_AccessConnectionType;
		writer.Write(value);
		byte value2 = (byte)m_RouteConnectionType;
		writer.Write(value2);
		TrackTypes accessTrackType = m_AccessTrackType;
		writer.Write((byte)accessTrackType);
		TrackTypes routeTrackType = m_RouteTrackType;
		writer.Write((byte)routeTrackType);
		RoadTypes accessRoadType = m_AccessRoadType;
		writer.Write((byte)accessRoadType);
		RoadTypes routeRoadType = m_RouteRoadType;
		writer.Write((byte)routeRoadType);
		SizeClass routeSizeClass = m_RouteSizeClass;
		writer.Write((byte)routeSizeClass);
		float startLaneOffset = m_StartLaneOffset;
		writer.Write(startLaneOffset);
		float endMargin = m_EndMargin;
		writer.Write(endMargin);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		reader.Read(out byte value2);
		reader.Read(out byte value3);
		reader.Read(out byte value4);
		reader.Read(out byte value5);
		reader.Read(out byte value6);
		reader.Read(out byte value7);
		ref float startLaneOffset = ref m_StartLaneOffset;
		reader.Read(out startLaneOffset);
		ref float endMargin = ref m_EndMargin;
		reader.Read(out endMargin);
		m_AccessConnectionType = (RouteConnectionType)value;
		m_RouteConnectionType = (RouteConnectionType)value2;
		m_AccessTrackType = (TrackTypes)value3;
		m_RouteTrackType = (TrackTypes)value4;
		m_AccessRoadType = (RoadTypes)value5;
		m_RouteRoadType = (RoadTypes)value6;
		m_RouteSizeClass = (SizeClass)value7;
	}
}
