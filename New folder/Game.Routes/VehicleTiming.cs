using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct VehicleTiming : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_LastDepartureFrame;

	public float m_AverageTravelTime;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint lastDepartureFrame = m_LastDepartureFrame;
		writer.Write(lastDepartureFrame);
		float averageTravelTime = m_AverageTravelTime;
		writer.Write(averageTravelTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref uint lastDepartureFrame = ref m_LastDepartureFrame;
		reader.Read(out lastDepartureFrame);
		ref float averageTravelTime = ref m_AverageTravelTime;
		reader.Read(out averageTravelTime);
	}
}
