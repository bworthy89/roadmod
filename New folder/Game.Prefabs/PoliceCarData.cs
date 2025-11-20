using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PoliceCarData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_CriminalCapacity;

	public float m_CrimeReductionRate;

	public uint m_ShiftDuration;

	public PolicePurpose m_PurposeMask;

	public PoliceCarData(int criminalCapacity, float crimeReductionRate, uint shiftDuration, PolicePurpose purposeMask)
	{
		m_CriminalCapacity = criminalCapacity;
		m_CrimeReductionRate = crimeReductionRate;
		m_ShiftDuration = shiftDuration;
		m_PurposeMask = purposeMask;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		PolicePurpose purposeMask = m_PurposeMask;
		writer.Write((uint)purposeMask);
		int criminalCapacity = m_CriminalCapacity;
		writer.Write(criminalCapacity);
		float crimeReductionRate = m_CrimeReductionRate;
		writer.Write(crimeReductionRate);
		uint shiftDuration = m_ShiftDuration;
		writer.Write(shiftDuration);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		ref int criminalCapacity = ref m_CriminalCapacity;
		reader.Read(out criminalCapacity);
		ref float crimeReductionRate = ref m_CrimeReductionRate;
		reader.Read(out crimeReductionRate);
		ref uint shiftDuration = ref m_ShiftDuration;
		reader.Read(out shiftDuration);
		m_PurposeMask = (PolicePurpose)value;
	}
}
