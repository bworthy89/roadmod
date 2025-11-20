using Colossal.Serialization.Entities;
using Game.Common;
using Game.Serialization;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Zones;

public class LoadSystem : GameSystemBase
{
	private LoadGameSystem m_LoadGameSystem;

	private EntityQuery m_EntityQuery;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_EntityQuery = GetEntityQuery(ComponentType.ReadOnly<Block>());
		RequireForUpdate(m_EntityQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_LoadGameSystem.context.purpose == Purpose.NewGame)
		{
			base.EntityManager.AddComponent<Updated>(m_EntityQuery);
		}
	}

	[Preserve]
	public LoadSystem()
	{
	}
}
