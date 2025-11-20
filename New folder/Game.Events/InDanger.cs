using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct InDanger : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public Entity m_EvacuationRequest;

	public DangerFlags m_Flags;

	public uint m_EndFrame;

	public InDanger(Entity _event, Entity evacuationRequest, DangerFlags flags, uint endFrame)
	{
		m_Event = _event;
		m_EvacuationRequest = evacuationRequest;
		m_Flags = flags;
		m_EndFrame = endFrame;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_Event;
		writer.Write(value);
		Entity evacuationRequest = m_EvacuationRequest;
		writer.Write(evacuationRequest);
		DangerFlags flags = m_Flags;
		writer.Write((uint)flags);
		uint endFrame = m_EndFrame;
		writer.Write(endFrame);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_Event;
		reader.Read(out value);
		ref Entity evacuationRequest = ref m_EvacuationRequest;
		reader.Read(out evacuationRequest);
		reader.Read(out uint value2);
		m_Flags = (DangerFlags)value2;
		if (reader.context.version >= Version.dangerTimeout)
		{
			ref uint endFrame = ref m_EndFrame;
			reader.Read(out endFrame);
		}
	}
}
