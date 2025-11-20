using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct TransportStop : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_AccessRestriction;

	public float m_ComfortFactor;

	public float m_LoadingFactor;

	public StopFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity accessRestriction = m_AccessRestriction;
		writer.Write(accessRestriction);
		float comfortFactor = m_ComfortFactor;
		writer.Write(comfortFactor);
		float loadingFactor = m_LoadingFactor;
		writer.Write(loadingFactor);
		StopFlags flags = m_Flags;
		writer.Write((uint)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.pathfindAccessRestriction)
		{
			ref Entity accessRestriction = ref m_AccessRestriction;
			reader.Read(out accessRestriction);
		}
		ref float comfortFactor = ref m_ComfortFactor;
		reader.Read(out comfortFactor);
		if (reader.context.version >= Version.transportLoadingFactor)
		{
			ref float loadingFactor = ref m_LoadingFactor;
			reader.Read(out loadingFactor);
		}
		reader.Read(out uint value);
		m_Flags = (StopFlags)value;
	}
}
