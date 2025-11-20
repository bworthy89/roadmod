using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.City;

public static class CityUtils
{
	public static bool CheckOption(City city, CityOption option)
	{
		return (city.m_OptionMask & (uint)(1 << (int)option)) != 0;
	}

	public static void ApplyModifier(ref float value, DynamicBuffer<CityModifier> modifiers, CityModifierType type)
	{
		if (modifiers.Length > (int)type)
		{
			float2 delta = modifiers[(int)type].m_Delta;
			value += delta.x;
			value += value * delta.y;
		}
	}

	public static float2 GetModifier(DynamicBuffer<CityModifier> modifiers, CityModifierType type)
	{
		if (modifiers.Length > (int)type)
		{
			return modifiers[(int)type].m_Delta;
		}
		return default(float2);
	}

	public static bool HasOption(CityOptionData optionData, CityOption option)
	{
		return (optionData.m_OptionMask & (uint)(1 << (int)option)) != 0;
	}

	public static int GetCityServiceWorkplaceMaxWorkers(Entity ownerEntity, ref ComponentLookup<PrefabRef> prefabRefs, ref BufferLookup<InstalledUpgrade> installedUpgrades, ref ComponentLookup<Deleted> deleteds, ref ComponentLookup<WorkplaceData> workplaceDatas, ref ComponentLookup<SchoolData> schoolDatas, ref BufferLookup<Student> studentBufs)
	{
		int result = 0;
		if (deleteds.HasComponent(ownerEntity))
		{
			return result;
		}
		Entity entity = prefabRefs[ownerEntity];
		if (!workplaceDatas.HasComponent(entity))
		{
			return result;
		}
		result = workplaceDatas[entity].m_MaxWorkers;
		if (!installedUpgrades.HasBuffer(ownerEntity))
		{
			return result;
		}
		int num = ((workplaceDatas[entity].m_MinimumWorkersLimit == 0) ? result : workplaceDatas[entity].m_MinimumWorkersLimit);
		foreach (InstalledUpgrade item in installedUpgrades[ownerEntity])
		{
			if (prefabRefs.HasComponent(item.m_Upgrade) && !deleteds.HasComponent(item.m_Upgrade))
			{
				Entity entity2 = prefabRefs[item.m_Upgrade];
				if (workplaceDatas.HasComponent(entity2))
				{
					num += workplaceDatas[entity2].m_MinimumWorkersLimit;
					result += workplaceDatas[entity2].m_MaxWorkers;
				}
			}
		}
		if (schoolDatas.HasComponent(entity))
		{
			int studentCapacity = schoolDatas[entity].m_StudentCapacity;
			int length = studentBufs[ownerEntity].Length;
			result = math.max(num, (int)Mathf.Lerp(0f, result, 1f * (float)length / (float)studentCapacity));
		}
		return result;
	}
}
