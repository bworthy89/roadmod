namespace Game.Simulation;

public interface ITimeSystem
{
	int daysPerYear { get; }

	float normalizedTime { get; }

	float normalizedDate { get; }

	int year { get; }
}
