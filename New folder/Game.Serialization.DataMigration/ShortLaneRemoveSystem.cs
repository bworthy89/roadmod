using Game.Common;
using Game.Net;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Serialization.DataMigration;

public class ShortLaneRemoveSystem : GameSystemBase
{
	private LoadGameSystem m_LoadGameSystem;

	private EntityQuery m_Query;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_Query = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<SubLane>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Node>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_LoadGameSystem.context.format.Has(FormatTags.ShortLaneOptimization) && !m_Query.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Updated>(m_Query);
		}
	}

	[Preserve]
	public ShortLaneRemoveSystem()
	{
	}
}
