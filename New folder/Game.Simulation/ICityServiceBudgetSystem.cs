using Game.City;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public interface ICityServiceBudgetSystem
{
	int GetIncome(IncomeSource source);

	int GetExpense(ExpenseSource source);

	int GetBalance();

	int GetTotalIncome();

	int GetTotalExpenses();

	int GetTotalTaxIncome();

	int GetServiceBudget(Entity servicePrefab);

	int GetMoneyDelta();

	void SetServiceBudget(Entity servicePrefab, int percentage);

	int GetServiceEfficiency(Entity servicePrefab, int budget);

	void GetEstimatedServiceBudget(Entity servicePrefab, out int upkeep);

	int GetNumberOfServiceBuildings(Entity serviceBuildingPrefab);

	int2 GetWorkersAndWorkplaces(Entity serviceBuildingPrefab);

	Entity[] GetServiceBuildings(Entity servicePrefab);
}
