namespace Game.Simulation;

public interface IAvailabilityInfoCell
{
	void AddAttractiveness(float amount);

	void AddConsumers(float amount);

	void AddServices(float amount);

	void AddWorkplaces(float amount);
}
