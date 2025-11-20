namespace Game.Simulation;

public interface IWaterStatisticsSystem
{
	int freshCapacity { get; }

	int freshConsumption { get; }

	int fulfilledFreshConsumption { get; }

	int sewageCapacity { get; }

	int sewageConsumption { get; }

	int fulfilledSewageConsumption { get; }
}
