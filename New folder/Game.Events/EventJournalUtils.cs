using Unity.Entities;
using UnityEngine;

namespace Game.Events;

public static class EventJournalUtils
{
	public static bool IsValid(EventDataTrackingType type)
	{
		if (type >= EventDataTrackingType.Damages)
		{
			return type < EventDataTrackingType.Count;
		}
		return false;
	}

	public static bool IsValid(EventCityEffectTrackingType type)
	{
		if (type >= EventCityEffectTrackingType.Crime)
		{
			return type < EventCityEffectTrackingType.Count;
		}
		return false;
	}

	public static int GetValue(DynamicBuffer<EventJournalData> data, EventDataTrackingType type)
	{
		if (IsValid(type))
		{
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i].m_Type == type)
				{
					return data[i].m_Value;
				}
			}
		}
		return 0;
	}

	public static int GetValue(DynamicBuffer<EventJournalCityEffect> effects, EventCityEffectTrackingType type)
	{
		if (IsValid(type))
		{
			for (int i = 0; i < effects.Length; i++)
			{
				EventJournalCityEffect effect = effects[i];
				if (effect.m_Type == type)
				{
					return GetPercentileChange(effect);
				}
			}
		}
		return 0;
	}

	public static int GetPercentileChange(EventJournalCityEffect effect)
	{
		if (effect.m_StartValue != 0)
		{
			return Mathf.RoundToInt((float)(effect.m_Value - effect.m_StartValue) / (float)effect.m_StartValue * 100f);
		}
		return 0;
	}
}
