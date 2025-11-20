using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.City;
using Game.Objects;
using Game.Prefabs;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class LevelSection : InfoSectionBase
{
	[BurstCompile]
	private struct CalculateMaxLevelJob : IJobChunk
	{
		[ReadOnly]
		public int2 m_LotSize;

		[ReadOnly]
		public Entity m_ZonePrefabEntity;

		[ReadOnly]
		public ComponentTypeHandle<BuildingData> m_BuildingDataTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingDataTypeHandle;

		public NativeArray<int> m_Result;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<BuildingData> nativeArray = chunk.GetNativeArray(ref m_BuildingDataTypeHandle);
			NativeArray<SpawnableBuildingData> nativeArray2 = chunk.GetNativeArray(ref m_SpawnableBuildingDataTypeHandle);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				SpawnableBuildingData spawnableBuildingData = nativeArray2[i];
				if (spawnableBuildingData.m_Level > m_Result[0] && spawnableBuildingData.m_ZonePrefab == m_ZonePrefabEntity && nativeArray[i].m_LotSize.Equals(m_LotSize))
				{
					m_Result[0] = spawnableBuildingData.m_Level;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(isReadOnly: true);
		}
	}

	private EntityQuery m_SpawnableBuildingQuery;

	private EntityQuery m_CityQuery;

	private NativeArray<int> m_Result;

	private TypeHandle __TypeHandle;

	protected override string group => "LevelSection";

	private int level { get; set; }

	private int maxLevel { get; set; }

	private bool isUnderConstruction { get; set; }

	private float progress { get; set; }

	private Entity zone { get; set; }

	protected override bool displayForUnderConstruction => true;

	protected override void Reset()
	{
		level = 0;
		maxLevel = 0;
		isUnderConstruction = false;
		progress = 0f;
		zone = Entity.Null;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SpawnableBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingData>(), ComponentType.ReadOnly<SpawnableBuildingData>());
		m_CityQuery = GetEntityQuery(ComponentType.ReadOnly<CityModifier>());
		m_Result = new NativeArray<int>(1, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_Result.Dispose();
	}

	private bool Visible()
	{
		if (!base.EntityManager.HasComponent<SignatureBuildingData>(selectedPrefab) && !base.EntityManager.HasComponent<Abandoned>(selectedEntity) && base.EntityManager.HasComponent<Renter>(selectedEntity) && base.EntityManager.HasComponent<BuildingData>(selectedPrefab))
		{
			return base.EntityManager.HasComponent<SpawnableBuildingData>(selectedPrefab);
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
		if (base.visible)
		{
			if (base.EntityManager.TryGetComponent<UnderConstruction>(selectedEntity, out var component) && component.m_NewPrefab == Entity.Null)
			{
				isUnderConstruction = true;
				progress = Math.Min((int)component.m_Progress, 100);
				return;
			}
			BuildingData componentData = base.EntityManager.GetComponentData<BuildingData>(selectedPrefab);
			SpawnableBuildingData componentData2 = base.EntityManager.GetComponentData<SpawnableBuildingData>(selectedPrefab);
			JobChunkExtensions.Schedule(new CalculateMaxLevelJob
			{
				m_LotSize = componentData.m_LotSize,
				m_ZonePrefabEntity = componentData2.m_ZonePrefab,
				m_BuildingDataTypeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SpawnableBuildingDataTypeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Result = m_Result
			}, m_SpawnableBuildingQuery, base.Dependency).Complete();
		}
	}

	protected override void OnProcess()
	{
		SpawnableBuildingData componentData = base.EntityManager.GetComponentData<SpawnableBuildingData>(selectedPrefab);
		zone = componentData.m_ZonePrefab;
		ZoneData componentData2 = base.EntityManager.GetComponentData<ZoneData>(zone);
		if (isUnderConstruction)
		{
			base.tooltipKeys.Add("UnderConstruction");
			m_InfoUISystem.tags.Add(SelectedInfoTags.UnderConstruction);
			return;
		}
		switch (componentData2.m_AreaType)
		{
		case AreaType.Residential:
			base.tooltipKeys.Add("Residential");
			break;
		case AreaType.Commercial:
			base.tooltipKeys.Add("Commercial");
			break;
		case AreaType.Industrial:
			base.tooltipKeys.Add(((componentData2.m_ZoneFlags & ZoneFlags.Office) != 0) ? "Office" : "Industrial");
			break;
		}
		BuildingPropertyData componentData3 = base.EntityManager.GetComponentData<BuildingPropertyData>(selectedPrefab);
		level = componentData.m_Level;
		maxLevel = math.max(m_Result[0], level);
		progress = 0f;
		if (componentData.m_Level < maxLevel)
		{
			int condition = base.EntityManager.GetComponentData<BuildingCondition>(selectedEntity).m_Condition;
			int levelingCost = BuildingUtils.GetLevelingCost(componentData2.m_AreaType, componentData3, level, base.EntityManager.GetBuffer<CityModifier>(m_CityQuery.GetSingletonEntity(), isReadOnly: true));
			progress = ((levelingCost > 0) ? ((float)condition / (float)levelingCost * 100f) : 100f);
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("zone");
		writer.Write(m_PrefabSystem.GetPrefabName(zone));
		writer.PropertyName("level");
		writer.Write(level);
		writer.PropertyName("maxLevel");
		writer.Write(maxLevel);
		writer.PropertyName("isUnderConstruction");
		writer.Write(isUnderConstruction);
		writer.PropertyName("progress");
		writer.Write(progress);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public LevelSection()
	{
	}
}
