namespace Game.Simulation.Flow;

public struct MaxFlowSolverState
{
	public bool m_Complete;

	public int m_NextLayerIndex;

	public int m_CurrentLabelVersion;

	public int m_CurrentActiveVersion;
}
