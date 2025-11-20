using Colossal.Serialization.Entities;
using Unity.Entities;
using UnityEngine;

namespace Game.Common;

public struct TimeData : IComponentData, IQueryTypeParameter, IDefaultSerializable, ISerializable
{
	public uint m_FirstFrame;

	public int m_StartingYear;

	public byte m_StartingMonth;

	public byte m_StartingHour;

	public byte m_StartingMinutes;

	public float TimeOffset
	{
		get
		{
			return (float)(int)m_StartingHour / 24f + (float)(int)m_StartingMinutes / 1440f + 1E-05f;
		}
		set
		{
			m_StartingHour = (byte)Mathf.FloorToInt((value * 24f + 1E-05f) % 24f);
			m_StartingMinutes = (byte)(Mathf.RoundToInt(value * 1440f) % 60);
		}
	}

	public float GetDateOffset(int daysPerYear)
	{
		return (float)(int)m_StartingMonth / (float)daysPerYear;
	}

	public void SetDefaults(Context context)
	{
		m_FirstFrame = 0u;
		m_StartingYear = 2021;
		m_StartingMonth = 5;
		m_StartingHour = 7;
		m_StartingMinutes = 0;
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref uint firstFrame = ref m_FirstFrame;
		reader.Read(out firstFrame);
		ref int startingYear = ref m_StartingYear;
		reader.Read(out startingYear);
		ref byte startingMonth = ref m_StartingMonth;
		reader.Read(out startingMonth);
		ref byte startingHour = ref m_StartingHour;
		reader.Read(out startingHour);
		ref byte startingMinutes = ref m_StartingMinutes;
		reader.Read(out startingMinutes);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint firstFrame = m_FirstFrame;
		writer.Write(firstFrame);
		int startingYear = m_StartingYear;
		writer.Write(startingYear);
		byte startingMonth = m_StartingMonth;
		writer.Write(startingMonth);
		byte startingHour = m_StartingHour;
		writer.Write(startingHour);
		byte startingMinutes = m_StartingMinutes;
		writer.Write(startingMinutes);
	}

	public static TimeData GetSingleton(EntityQuery query)
	{
		if (!query.IsEmptyIgnoreFilter)
		{
			return query.GetSingleton<TimeData>();
		}
		return default(TimeData);
	}
}
