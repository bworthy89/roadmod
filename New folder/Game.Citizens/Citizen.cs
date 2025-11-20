using Colossal.Serialization.Entities;
using Game.Common;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Citizens;

public struct Citizen : IComponentData, IQueryTypeParameter, ISerializable
{
	public ushort m_PseudoRandom;

	public CitizenFlags m_State;

	public byte m_WellBeing;

	public byte m_Health;

	public byte m_LeisureCounter;

	public byte m_PenaltyCounter;

	public int m_UnemploymentCounter;

	public short m_BirthDay;

	public float m_UnemploymentTimeCounter;

	public int m_SicknessPenalty;

	public int Happiness => (m_WellBeing + m_Health) / 2;

	public float GetAgeInDays(uint simulationFrame, TimeData timeData)
	{
		return TimeSystem.GetDay(simulationFrame, timeData) - m_BirthDay;
	}

	public Random GetPseudoRandom(CitizenPseudoRandom reason)
	{
		Random random = new Random((uint)((ulong)reason ^ (ulong)((m_PseudoRandom << 16) | m_PseudoRandom)));
		random.NextUInt();
		uint num = random.NextUInt();
		num = math.select(num, uint.MaxValue, num == 0);
		return new Random(num);
	}

	public int GetEducationLevel()
	{
		if ((m_State & CitizenFlags.EducationBit3) != CitizenFlags.None)
		{
			return 4;
		}
		return (((m_State & CitizenFlags.EducationBit1) != CitizenFlags.None) ? 2 : 0) + (((m_State & CitizenFlags.EducationBit2) != CitizenFlags.None) ? 1 : 0);
	}

	public void SetEducationLevel(int level)
	{
		if (level == 4)
		{
			m_State |= CitizenFlags.EducationBit3;
		}
		else
		{
			m_State &= ~CitizenFlags.EducationBit3;
		}
		if (level >= 2)
		{
			m_State |= CitizenFlags.EducationBit1;
		}
		else
		{
			m_State &= ~CitizenFlags.EducationBit1;
		}
		if (level % 2 != 0)
		{
			m_State |= CitizenFlags.EducationBit2;
		}
		else
		{
			m_State &= ~CitizenFlags.EducationBit2;
		}
	}

	public int GetFailedEducationCount()
	{
		return (((m_State & CitizenFlags.FailedEducationBit1) != CitizenFlags.None) ? 2 : 0) + (((m_State & CitizenFlags.FailedEducationBit2) != CitizenFlags.None) ? 1 : 0);
	}

	public void SetFailedEducationCount(int fails)
	{
		if (fails >= 2)
		{
			m_State |= CitizenFlags.FailedEducationBit1;
		}
		else
		{
			m_State &= ~CitizenFlags.FailedEducationBit1;
		}
		if (fails % 2 != 0)
		{
			m_State |= CitizenFlags.FailedEducationBit2;
		}
		else
		{
			m_State &= ~CitizenFlags.FailedEducationBit2;
		}
	}

	public void SetAge(CitizenAge newAge)
	{
		m_State = (CitizenFlags)((int)((uint)(m_State & ~(CitizenFlags.AgeBit1 | CitizenFlags.AgeBit2)) | (uint)(((newAge & CitizenAge.Adult) != CitizenAge.Child) ? 1 : 0)) | (((int)newAge % 2 != 0) ? 2 : 0));
	}

	public CitizenAge GetAge()
	{
		return (CitizenAge)(2 * (((m_State & CitizenFlags.AgeBit1) != CitizenFlags.None) ? 1 : 0) + (((m_State & CitizenFlags.AgeBit2) != CitizenFlags.None) ? 1 : 0));
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		CitizenFlags state = m_State;
		writer.Write((short)state);
		byte wellBeing = m_WellBeing;
		writer.Write(wellBeing);
		byte health = m_Health;
		writer.Write(health);
		byte leisureCounter = m_LeisureCounter;
		writer.Write(leisureCounter);
		byte penaltyCounter = m_PenaltyCounter;
		writer.Write(penaltyCounter);
		short birthDay = m_BirthDay;
		writer.Write(birthDay);
		ushort pseudoRandom = m_PseudoRandom;
		writer.Write(pseudoRandom);
		int unemploymentCounter = m_UnemploymentCounter;
		writer.Write(unemploymentCounter);
		float unemploymentTimeCounter = m_UnemploymentTimeCounter;
		writer.Write(unemploymentTimeCounter);
		int sicknessPenalty = m_SicknessPenalty;
		writer.Write(sicknessPenalty);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version < Version.saveOptimizations)
		{
			reader.Read(out uint _);
		}
		reader.Read(out short value2);
		ref byte wellBeing = ref m_WellBeing;
		reader.Read(out wellBeing);
		ref byte health = ref m_Health;
		reader.Read(out health);
		ref byte leisureCounter = ref m_LeisureCounter;
		reader.Read(out leisureCounter);
		if (reader.context.version >= Version.penaltyCounter)
		{
			ref byte penaltyCounter = ref m_PenaltyCounter;
			reader.Read(out penaltyCounter);
		}
		ref short birthDay = ref m_BirthDay;
		reader.Read(out birthDay);
		m_State = (CitizenFlags)value2;
		if (reader.context.version >= Version.snow)
		{
			ref ushort pseudoRandom = ref m_PseudoRandom;
			reader.Read(out pseudoRandom);
		}
		if (reader.context.version >= Version.economyFix)
		{
			ref int unemploymentCounter = ref m_UnemploymentCounter;
			reader.Read(out unemploymentCounter);
		}
		if (reader.context.format.Has(FormatTags.UnemploymentAffectHappiness))
		{
			ref float unemploymentTimeCounter = ref m_UnemploymentTimeCounter;
			reader.Read(out unemploymentTimeCounter);
		}
		if (reader.context.format.Has(FormatTags.SicknessHealthPenalty))
		{
			ref int sicknessPenalty = ref m_SicknessPenalty;
			reader.Read(out sicknessPenalty);
		}
	}
}
