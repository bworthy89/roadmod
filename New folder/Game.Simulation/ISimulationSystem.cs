namespace Game.Simulation;

public interface ISimulationSystem
{
	uint frameIndex { get; }

	float frameTime { get; }

	float selectedSpeed { get; set; }
}
