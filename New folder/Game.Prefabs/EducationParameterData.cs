using Unity.Entities;

namespace Game.Prefabs;

public struct EducationParameterData : IComponentData, IQueryTypeParameter
{
	public Entity m_EducationServicePrefab;

	public float m_InoperableSchoolLeaveProbability;

	public float m_EnterHighSchoolProbability;

	public float m_AdultEnterHighSchoolProbability;

	public float m_WorkerContinueEducationProbability;
}
