using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct WorkVehicle : IComponentData, IQueryTypeParameter, ISerializable
{
	public WorkVehicleFlags m_State;

	public float m_WorkAmount;

	public float m_DoneAmount;

	public WorkVehicle(WorkVehicleFlags flags, float workAmount)
	{
		m_State = flags;
		m_WorkAmount = workAmount;
		m_DoneAmount = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		WorkVehicleFlags state = m_State;
		writer.Write((uint)state);
		float workAmount = m_WorkAmount;
		writer.Write(workAmount);
		float doneAmount = m_DoneAmount;
		writer.Write(doneAmount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		ref float workAmount = ref m_WorkAmount;
		reader.Read(out workAmount);
		ref float doneAmount = ref m_DoneAmount;
		reader.Read(out doneAmount);
		m_State = (WorkVehicleFlags)value;
	}
}
