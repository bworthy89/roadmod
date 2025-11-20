using Game.City;
using Unity.Mathematics;

namespace Game.Simulation;

public interface IServiceFeeSystem
{
	int3 GetServiceFees(PlayerResource resource);

	int GetServiceFeeIncomeEstimate(PlayerResource resource, float fee);
}
