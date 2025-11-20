using Unity.Entities;

namespace Game.Prefabs;

public struct HouseholdData : IComponentData, IQueryTypeParameter
{
	public int m_InitialWealthRange;

	public int m_InitialWealthOffset;

	public int m_InitialCarProbability;

	public int m_ChildCount;

	public int m_AdultCount;

	public int m_ElderCount;

	public int m_StudentCount;

	public int m_FirstPetProbability;

	public int m_NextPetProbability;

	public int m_Weight;
}
