using Colossal.Entities;
using Game.Buildings;
using Game.Companies;
using Game.Prefabs;
using Game.Zones;
using Unity.Entities;

namespace Game.UI.InGame;

public static class CompanyUIUtils
{
	public static bool HasCompany(EntityManager entityManager, Entity entity, Entity prefab, out Entity company)
	{
		company = Entity.Null;
		if (entityManager.HasComponent<Renter>(entity) && entityManager.TryGetComponent<BuildingPropertyData>(prefab, out var component) && component.CountProperties(AreaType.Commercial) + component.CountProperties(AreaType.Industrial) > 0)
		{
			if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Renter> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					if (entityManager.HasComponent<CompanyData>(buffer[i].m_Renter))
					{
						company = buffer[i].m_Renter;
						break;
					}
				}
			}
			return true;
		}
		return false;
	}

	public static bool HasCompany(Entity entity, Entity prefab, ref BufferLookup<Renter> renterFromEntity, ref ComponentLookup<BuildingPropertyData> buildingPropertyDataFromEntity, ref ComponentLookup<CompanyData> companyDataFromEntity, out Entity company)
	{
		company = Entity.Null;
		if (renterFromEntity.HasBuffer(entity) && buildingPropertyDataFromEntity.TryGetComponent(prefab, out var componentData) && componentData.CountProperties(AreaType.Commercial) + componentData.CountProperties(AreaType.Industrial) > 0)
		{
			if (renterFromEntity.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (companyDataFromEntity.HasComponent(bufferData[i].m_Renter))
					{
						company = bufferData[i].m_Renter;
						break;
					}
				}
			}
			return true;
		}
		return false;
	}

	public static CompanyProfitabilityKey GetProfitabilityKey(int profit)
	{
		if (profit > 128)
		{
			return CompanyProfitabilityKey.Profitable;
		}
		if (profit > 32)
		{
			return CompanyProfitabilityKey.GettingBy;
		}
		if (profit > -64)
		{
			return CompanyProfitabilityKey.BreakingEven;
		}
		if (profit > -182)
		{
			return CompanyProfitabilityKey.LosingMoney;
		}
		return CompanyProfitabilityKey.Bankrupt;
	}
}
