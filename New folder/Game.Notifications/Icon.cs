using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Notifications;

public struct Icon : IComponentData, IQueryTypeParameter, IEquatable<Icon>, ISerializable
{
	public float3 m_Location;

	public IconPriority m_Priority;

	public IconClusterLayer m_ClusterLayer;

	public IconFlags m_Flags;

	public int m_ClusterIndex;

	public bool Equals(Icon other)
	{
		return m_Location.Equals(other.m_Location) & (m_Priority == other.m_Priority) & (m_ClusterLayer == other.m_ClusterLayer) & (m_Flags == other.m_Flags);
	}

	public override int GetHashCode()
	{
		return m_Location.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 location = m_Location;
		writer.Write(location);
		IconPriority priority = m_Priority;
		writer.Write((byte)priority);
		IconClusterLayer clusterLayer = m_ClusterLayer;
		writer.Write((byte)clusterLayer);
		IconFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 location = ref m_Location;
		reader.Read(out location);
		reader.Read(out byte value);
		m_Priority = (IconPriority)value;
		if (reader.context.version >= Version.iconClusteringData)
		{
			reader.Read(out byte value2);
			reader.Read(out byte value3);
			m_ClusterLayer = (IconClusterLayer)value2;
			m_Flags = (IconFlags)value3;
		}
	}
}
