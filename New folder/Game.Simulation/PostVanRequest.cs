using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct PostVanRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public PostVanRequestFlags m_Flags;

	public byte m_DispatchIndex;

	public ushort m_Priority;

	public PostVanRequest(Entity target, PostVanRequestFlags flags, ushort priority)
	{
		m_Target = target;
		m_Flags = flags;
		m_Priority = priority;
		m_DispatchIndex = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity target = m_Target;
		writer.Write(target);
		PostVanRequestFlags flags = m_Flags;
		writer.Write((ushort)flags);
		ushort priority = m_Priority;
		writer.Write(priority);
		byte dispatchIndex = m_DispatchIndex;
		writer.Write(dispatchIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity target = ref m_Target;
		reader.Read(out target);
		reader.Read(out ushort value);
		ref ushort priority = ref m_Priority;
		reader.Read(out priority);
		m_Flags = (PostVanRequestFlags)value;
		if (reader.context.version >= Version.requestDispatchIndex)
		{
			ref byte dispatchIndex = ref m_DispatchIndex;
			reader.Read(out dispatchIndex);
		}
	}
}
