using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Net;
using Game.Objects;
using Game.Serialization;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Common;

public class NativeSystem : GameSystemBase
{
	private LoadGameSystem m_LoadGameSystem;

	private EntityQuery m_EntityQuery;

	private EntityQuery m_NativeQuery;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_EntityQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Game.Net.Node>(),
				ComponentType.ReadOnly<Object>(),
				ComponentType.ReadOnly<Area>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Native>() }
		});
		m_NativeQuery = GetEntityQuery(ComponentType.ReadOnly<Native>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		switch (m_LoadGameSystem.context.purpose)
		{
		case Purpose.NewGame:
			base.EntityManager.AddComponent<Native>(m_EntityQuery);
			break;
		case Purpose.LoadMap:
			base.EntityManager.RemoveComponent<Native>(m_NativeQuery);
			break;
		}
	}

	[Preserve]
	public NativeSystem()
	{
	}
}
