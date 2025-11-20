using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct Feedback : IComponentData, IQueryTypeParameter
{
	public float3 m_Position;

	public Entity m_MainEntity;

	public Entity m_Prefab;

	public Entity m_MainPrefab;

	public bool m_IsDeleted;
}
