using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct GarbageCollectionRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public int m_Priority;

	public GarbageCollectionRequestFlags m_Flags;

	public byte m_DispatchIndex;

	public GarbageCollectionRequest(Entity target, int priority, GarbageCollectionRequestFlags flags)
	{
		m_Target = target;
		m_Priority = priority;
		m_Flags = flags;
		m_DispatchIndex = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity target = m_Target;
		writer.Write(target);
		int priority = m_Priority;
		writer.Write(priority);
		GarbageCollectionRequestFlags flags = m_Flags;
		writer.Write((byte)flags);
		byte dispatchIndex = m_DispatchIndex;
		writer.Write(dispatchIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity target = ref m_Target;
		reader.Read(out target);
		ref int priority = ref m_Priority;
		reader.Read(out priority);
		if (reader.context.version >= Version.industrialWaste)
		{
			reader.Read(out byte value);
			m_Flags = (GarbageCollectionRequestFlags)value;
		}
		if (reader.context.version >= Version.requestDispatchIndex)
		{
			ref byte dispatchIndex = ref m_DispatchIndex;
			reader.Read(out dispatchIndex);
		}
	}
}
