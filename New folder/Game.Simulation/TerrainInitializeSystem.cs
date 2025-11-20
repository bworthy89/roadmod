using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Simulation;

[FormerlySerializedAs("Colossal.Terrain.TerrainInitializeSystem, Game")]
[CompilerGenerated]
public class TerrainInitializeSystem : GameSystemBase
{
	private EntityQuery m_TerrainPropertiesQuery;

	private EntityQuery m_TerrainMaterialPropertiesQuery;

	private TerrainSystem m_TerrainSystem;

	private TerrainRenderSystem m_TerrainRenderSystem;

	private TerrainMaterialSystem m_TerrainMaterialSystem;

	private WaterSystem m_WaterSystem;

	private WaterRenderSystem m_WaterRenderSystem;

	private SnowSystem m_SnowSystem;

	private PrefabSystem m_PrefabSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_TerrainMaterialSystem = base.World.GetOrCreateSystemManaged<TerrainMaterialSystem>();
		m_TerrainRenderSystem = base.World.GetOrCreateSystemManaged<TerrainRenderSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_WaterRenderSystem = base.World.GetOrCreateSystemManaged<WaterRenderSystem>();
		m_SnowSystem = base.World.GetOrCreateSystemManaged<SnowSystem>();
		m_TerrainPropertiesQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<TerrainPropertiesData>());
		m_TerrainMaterialPropertiesQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<TerrainMaterialPropertiesData>());
		RequireForUpdate(m_TerrainPropertiesQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		Entity singletonEntity = m_TerrainPropertiesQuery.GetSingletonEntity();
		TerrainPropertiesPrefab prefab = m_PrefabSystem.GetPrefab<TerrainPropertiesPrefab>(singletonEntity);
		m_WaterSystem.MaxSpeed = prefab.m_WaterMaxSpeed;
	}

	[Preserve]
	public TerrainInitializeSystem()
	{
	}
}
