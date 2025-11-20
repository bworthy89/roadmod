using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct Hearse : IComponentData, IQueryTypeParameter, ISerializable
{
	public HearseFlags m_State;

	public Entity m_TargetCorpse;

	public Entity m_TargetRequest;

	public float m_PathElementTime;

	public Hearse(Entity targetCorpse, HearseFlags state)
	{
		m_State = state;
		m_TargetCorpse = targetCorpse;
		m_TargetRequest = Entity.Null;
		m_PathElementTime = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		HearseFlags state = m_State;
		writer.Write((uint)state);
		Entity targetCorpse = m_TargetCorpse;
		writer.Write(targetCorpse);
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		float pathElementTime = m_PathElementTime;
		writer.Write(pathElementTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		ref Entity targetCorpse = ref m_TargetCorpse;
		reader.Read(out targetCorpse);
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		ref float pathElementTime = ref m_PathElementTime;
		reader.Read(out pathElementTime);
		m_State = (HearseFlags)value;
	}
}
