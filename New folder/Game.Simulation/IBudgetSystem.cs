using Game.Economy;
using Unity.Mathematics;

namespace Game.Simulation;

public interface IBudgetSystem
{
	bool HasData { get; }

	uint LastUpdate { get; }

	int GetTrade(Resource resource);

	int GetTradeWorth(Resource resource);

	int GetHouseholdWealth();

	int GetCompanyWealth(bool service, Resource resource);

	int GetTotalTradeWorth();

	int GetHouseholdCount();

	int GetCompanyCount(bool service, Resource resource);

	int2 GetHouseholdWorkers();

	int2 GetCompanyWorkers(bool service, Resource resource);

	float2 GetCitizenWellbeing();

	int GetTouristCount();

	int2 GetLodgingData();

	int GetTouristIncome();
}
