using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Elevation : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Elevation;

	public ElevationFlags m_Flags;

	public Elevation(float elevation, ElevationFlags flags)
	{
		m_Elevation = elevation;
		m_Flags = flags;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float elevation = m_Elevation;
		writer.Write(elevation);
		ElevationFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float elevation = ref m_Elevation;
		reader.Read(out elevation);
		if (reader.context.version >= Version.stackedObjects)
		{
			reader.Read(out byte value);
			m_Flags = (ElevationFlags)value;
		}
	}
}
