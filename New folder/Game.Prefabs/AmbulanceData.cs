using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct AmbulanceData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_PatientCapacity;

	public AmbulanceData(int patientCapacity)
	{
		m_PatientCapacity = patientCapacity;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_PatientCapacity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_PatientCapacity);
	}
}
