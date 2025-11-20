using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class LevelInfoviewUISystem : InfoviewUISystemBase
{
	private enum Result
	{
		Residential,
		Commercial,
		Industrial,
		Office,
		ResultCount
	}

	[BurstCompile]
	private struct UpdateLevelsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResidentialProperty> m_ResidentialPropertyHandle;

		[ReadOnly]
		public ComponentTypeHandle<CommercialProperty> m_CommercialPropertyHandle;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialProperty> m_IndustrialPropertyHandle;

		[ReadOnly]
		public ComponentLookup<OfficeBuilding> m_OfficeBuildingFromEntity;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingFromEntity;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> m_SignatureBuildingFromEntity;

		public NativeArray<Levels> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefHandle);
			Levels levels = default(Levels);
			Levels levels2 = default(Levels);
			Levels levels3 = default(Levels);
			Levels levels4 = default(Levels);
			for (int i = 0; i < chunk.Count; i++)
			{
				PrefabRef prefabRef = nativeArray[i];
				if (!m_SpawnableBuildingFromEntity.TryGetComponent(prefabRef.m_Prefab, out var componentData) || m_SignatureBuildingFromEntity.HasComponent(prefabRef.m_Prefab))
				{
					continue;
				}
				if (chunk.Has(ref m_ResidentialPropertyHandle))
				{
					AddLevel(componentData, ref levels);
				}
				if (chunk.Has(ref m_CommercialPropertyHandle))
				{
					AddLevel(componentData, ref levels2);
				}
				if (chunk.Has(ref m_IndustrialPropertyHandle))
				{
					if (m_OfficeBuildingFromEntity.HasComponent(prefabRef.m_Prefab))
					{
						AddLevel(componentData, ref levels4);
					}
					else
					{
						AddLevel(componentData, ref levels3);
					}
				}
			}
			m_Results[0] += levels;
			m_Results[1] += levels2;
			m_Results[2] += levels3;
			m_Results[3] += levels4;
		}

		private void AddLevel(SpawnableBuildingData spawnableBuildingData, ref Levels levels)
		{
			switch (spawnableBuildingData.m_Level)
			{
			case 1:
				levels.Level1++;
				break;
			case 2:
				levels.Level2++;
				break;
			case 3:
				levels.Level3++;
				break;
			case 4:
				levels.Level4++;
				break;
			case 5:
				levels.Level5++;
				break;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct Levels
	{
		public int Level1;

		public int Level2;

		public int Level3;

		public int Level4;

		public int Level5;

		public Levels(int level1, int level2, int level3, int level4, int level5)
		{
			Level1 = level1;
			Level2 = level2;
			Level3 = level3;
			Level4 = level4;
			Level5 = level5;
		}

		public static Levels operator +(Levels left, Levels right)
		{
			return new Levels(left.Level1 + right.Level1, left.Level2 + right.Level2, left.Level3 + right.Level3, left.Level4 + right.Level4, left.Level5 + right.Level5);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResidentialProperty> __Game_Buildings_ResidentialProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CommercialProperty> __Game_Buildings_CommercialProperty_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialProperty> __Game_Buildings_IndustrialProperty_RO_ComponentTypeHandle;

		public ComponentLookup<OfficeBuilding> __Game_Prefabs_OfficeBuilding_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_ResidentialProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentialProperty>(isReadOnly: true);
			__Game_Buildings_CommercialProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommercialProperty>(isReadOnly: true);
			__Game_Buildings_IndustrialProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialProperty>(isReadOnly: true);
			__Game_Prefabs_OfficeBuilding_RW_ComponentLookup = state.GetComponentLookup<OfficeBuilding>();
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup = state.GetComponentLookup<SignatureBuildingData>(isReadOnly: true);
		}
	}

	private const string kGroup = "levelInfo";

	private RawValueBinding m_ResidentialLevels;

	private RawValueBinding m_CommercialLevels;

	private RawValueBinding m_IndustrialLevels;

	private RawValueBinding m_OfficeLevels;

	private EntityQuery m_SpawnableQuery;

	private NativeArray<Levels> m_Results;

	private TypeHandle __TypeHandle;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_ResidentialLevels.active && !m_CommercialLevels.active && !m_IndustrialLevels.active)
			{
				return m_OfficeLevels.active;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SpawnableQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<ResidentialProperty>(),
				ComponentType.ReadOnly<CommercialProperty>(),
				ComponentType.ReadOnly<IndustrialProperty>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		AddBinding(m_ResidentialLevels = new RawValueBinding("levelInfo", "residential", UpdateResidentialLevels));
		AddBinding(m_CommercialLevels = new RawValueBinding("levelInfo", "commercial", UpdateCommercialLevels));
		AddBinding(m_IndustrialLevels = new RawValueBinding("levelInfo", "industrial", UpdateIndustrialLevels));
		AddBinding(m_OfficeLevels = new RawValueBinding("levelInfo", "office", UpdateOfficeLevels));
		m_Results = new NativeArray<Levels>(4, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		UpdateBindings();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		UpdateBindings();
	}

	private void UpdateBindings()
	{
		ResetResults(m_Results);
		JobChunkExtensions.Schedule(new UpdateLevelsJob
		{
			m_PrefabRefHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResidentialPropertyHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ResidentialProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CommercialPropertyHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CommercialProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IndustrialPropertyHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_IndustrialProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OfficeBuildingFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OfficeBuilding_RW_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SignatureBuildingFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_SpawnableQuery, base.Dependency).Complete();
		m_ResidentialLevels.Update();
		m_CommercialLevels.Update();
		m_IndustrialLevels.Update();
		m_OfficeLevels.Update();
	}

	private void UpdateResidentialLevels(IJsonWriter writer)
	{
		Levels levels = m_Results[0];
		WriteLevels(writer, levels);
	}

	private void UpdateCommercialLevels(IJsonWriter writer)
	{
		Levels levels = m_Results[1];
		WriteLevels(writer, levels);
	}

	private void UpdateIndustrialLevels(IJsonWriter writer)
	{
		Levels levels = m_Results[2];
		WriteLevels(writer, levels);
	}

	private void UpdateOfficeLevels(IJsonWriter writer)
	{
		Levels levels = m_Results[3];
		WriteLevels(writer, levels);
	}

	private void WriteLevels(IJsonWriter writer, Levels levels)
	{
		InfoviewsUIUtils.UpdateFiveSlicePieChartData(writer, levels.Level1, levels.Level2, levels.Level3, levels.Level4, levels.Level5);
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
	public LevelInfoviewUISystem()
	{
	}
}
