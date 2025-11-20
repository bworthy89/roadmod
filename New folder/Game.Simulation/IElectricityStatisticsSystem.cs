namespace Game.Simulation;

public interface IElectricityStatisticsSystem
{
	int production { get; }

	int consumption { get; }

	int fulfilledConsumption { get; }

	int batteryCharge { get; }

	int batteryCapacity { get; }
}
