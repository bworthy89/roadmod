namespace Game.Simulation;

public interface IMilestoneSystem
{
	int currentXP { get; }

	int requiredXP { get; }

	int lastRequiredXP { get; }

	int nextRequiredXP { get; }

	float progress { get; }

	int nextMilestone { get; }
}
