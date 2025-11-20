using Game.Citizens;
using Unity.Entities;

namespace Game.Tutorials;

public struct HealthProblemActivationData : IComponentData, IQueryTypeParameter
{
	public HealthProblemFlags m_Require;

	public int m_RequiredCount;
}
