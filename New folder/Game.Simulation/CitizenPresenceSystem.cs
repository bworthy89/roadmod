using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CitizenPresenceSystem : GameSystemBase
{
	[BurstCompile]
	private struct CitizenPresenceJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<CitizenPresence> m_CitizenPresenceType;

		[ReadOnly]
		public ComponentLookup<WorkProvider> m_WorkProviderData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyData;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CitizenPresence> nativeArray2 = chunk.GetNativeArray(ref m_CitizenPresenceType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				CitizenPresence value = nativeArray2[i];
				if (value.m_Delta == 0)
				{
					continue;
				}
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = m_PrefabRefData[entity];
				int num = GetCapacity(entity);
				if (m_Renters.HasBuffer(entity) && m_BuildingPropertyData.HasComponent(prefabRef.m_Prefab))
				{
					DynamicBuffer<Renter> dynamicBuffer = m_Renters[entity];
					BuildingPropertyData buildingPropertyData = m_BuildingPropertyData[prefabRef.m_Prefab];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						num += GetCapacity(dynamicBuffer[j].m_Renter);
					}
					int num2 = buildingPropertyData.CountProperties();
					if (num2 > dynamicBuffer.Length)
					{
						num += (num2 - dynamicBuffer.Length) * 2;
					}
				}
				if (num > 0)
				{
					int num3 = (math.abs(value.m_Delta) << 20) / num;
					num3 = random.NextInt(num3 >> 1, num3 * 3 >> 1) + 4095 >> 12;
					if (value.m_Delta > 0)
					{
						value.m_Presence = (byte)math.min(255, value.m_Presence + num3);
					}
					else
					{
						value.m_Presence = (byte)math.max(0, value.m_Presence - num3);
					}
				}
				else
				{
					value.m_Presence = 0;
				}
				value.m_Delta = 0;
				nativeArray2[i] = value;
			}
		}

		private int GetCapacity(Entity entity)
		{
			int num = 0;
			if (m_WorkProviderData.HasComponent(entity))
			{
				num += m_WorkProviderData[entity].m_MaxWorkers;
			}
			if (m_HouseholdCitizens.HasBuffer(entity))
			{
				num += m_HouseholdCitizens[entity].Length;
			}
			return num;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentTypeHandle<CitizenPresence> __Game_Buildings_CitizenPresence_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_CitizenPresence_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CitizenPresence>();
			__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
		}
	}

	private EntityQuery m_BuildingQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BuildingQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<CitizenPresence>() },
			Any = new ComponentType[0],
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		RequireForUpdate(m_BuildingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new CitizenPresenceJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CitizenPresenceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WorkProviderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingPropertyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next()
		}, m_BuildingQuery, base.Dependency);
		base.Dependency = dependency;
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
	public CitizenPresenceSystem()
	{
	}
}
