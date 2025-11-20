using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public static class FlowUtils
{
	public static int ConsumeFromTotal(int demand, ref int totalSupply, ref int totalDemand)
	{
		int num = 0;
		if (demand > 0)
		{
			int lowerBound = totalSupply - (totalDemand - demand);
			int upperBound = totalSupply;
			int num2 = totalDemand * 100 / demand;
			num = math.clamp(totalSupply * 100 / num2, lowerBound, upperBound);
			totalSupply -= num;
			totalDemand -= demand;
		}
		return num;
	}

	public static float GetRenterConsumptionMultiplier(Entity prefab, DynamicBuffer<Renter> renterBuffer, ref BufferLookup<HouseholdCitizen> householdCitizens, ref BufferLookup<Employee> employees, ref ComponentLookup<Citizen> citizens, ref ComponentLookup<SpawnableBuildingData> spawnableDatas)
	{
		int num = 0;
		float num2 = 0f;
		foreach (Renter item in renterBuffer)
		{
			if (householdCitizens.TryGetBuffer(item, out var bufferData))
			{
				foreach (HouseholdCitizen item2 in bufferData)
				{
					if (citizens.TryGetComponent(item2.m_Citizen, out var componentData))
					{
						num2 += (float)componentData.GetEducationLevel();
						num++;
					}
				}
			}
			else
			{
				if (!employees.TryGetBuffer(item, out var bufferData2))
				{
					continue;
				}
				foreach (Employee item3 in bufferData2)
				{
					if (citizens.TryGetComponent(item3.m_Worker, out var componentData2))
					{
						num2 += (float)componentData2.GetEducationLevel();
						num++;
					}
				}
			}
		}
		if (num != 0)
		{
			SpawnableBuildingData componentData3;
			float num3 = (spawnableDatas.TryGetComponent(prefab, out componentData3) ? ((float)(int)componentData3.m_Level) : 5f);
			return 5f * (float)num / (num3 + 0.5f * (num2 / (float)num));
		}
		return 0f;
	}
}
