using System.Runtime.CompilerServices;
using Colossal.Collections;
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
public class DisasterInfoviewUISystem : InfoviewUISystemBase
{
	[BurstCompile]
	private struct UpdateDisasterResponseJob : IJobChunk
	{
		public struct Result : IAccumulable<Result>
		{
			public int m_Count;

			public int m_Capacity;

			public void Accumulate(Result other)
			{
				m_Count += other.m_Count;
				m_Capacity += other.m_Capacity;
			}
		}

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Occupant> m_OccupantType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<EmergencyShelterData> m_EmergencyShelterDatas;

		public NativeAccumulator<Result> m_Result;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<Occupant> bufferAccessor = chunk.GetBufferAccessor(ref m_OccupantType);
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<Efficiency> bufferAccessor3 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			for (int i = 0; i < chunk.Count; i++)
			{
				PrefabRef prefabRef = nativeArray[i];
				DynamicBuffer<Occupant> dynamicBuffer = bufferAccessor[i];
				if (BuildingUtils.GetEfficiency(bufferAccessor3, i) != 0f)
				{
					m_EmergencyShelterDatas.TryGetComponent(prefabRef, out var componentData);
					UpgradeUtils.CombineStats(ref componentData, bufferAccessor2, i, ref m_Prefabs, ref m_EmergencyShelterDatas);
					m_Result.Accumulate(new Result
					{
						m_Count = dynamicBuffer.Length,
						m_Capacity = componentData.m_ShelterCapacity
					});
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Occupant> __Game_Buildings_Occupant_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EmergencyShelterData> __Game_Prefabs_EmergencyShelterData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Occupant_RO_BufferTypeHandle = state.GetBufferTypeHandle<Occupant>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_EmergencyShelterData_RO_ComponentLookup = state.GetComponentLookup<EmergencyShelterData>(isReadOnly: true);
		}
	}

	private const string kGroup = "disasterInfo";

	private ValueBinding<int> m_ShelteredCount;

	private ValueBinding<int> m_ShelterCapacity;

	private ValueBinding<IndicatorValue> m_ShelterAvailability;

	private NativeAccumulator<UpdateDisasterResponseJob.Result> m_Result;

	private EntityQuery m_SheltersQuery;

	private EntityQuery m_SheltersModifiedQuery;

	private TypeHandle __TypeHandle;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_ShelterAvailability.active && !m_ShelterCapacity.active)
			{
				return m_ShelteredCount.active;
			}
			return true;
		}
	}

	protected override bool Modified => !m_SheltersModifiedQuery.IsEmptyIgnoreFilter;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SheltersQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Buildings.EmergencyShelter>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_SheltersModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Buildings.EmergencyShelter>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		AddBinding(m_ShelteredCount = new ValueBinding<int>("disasterInfo", "shelteredCount", 0));
		AddBinding(m_ShelterCapacity = new ValueBinding<int>("disasterInfo", "shelterCapacity", 0));
		AddBinding(m_ShelterAvailability = new ValueBinding<IndicatorValue>("disasterInfo", "shelterAvailability", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
		m_Result = new NativeAccumulator<UpdateDisasterResponseJob.Result>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Result.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		m_Result.Clear();
		JobChunkExtensions.Schedule(new UpdateDisasterResponseJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OccupantType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Occupant_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EmergencyShelterDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EmergencyShelterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Result = m_Result
		}, m_SheltersQuery, base.Dependency).Complete();
		UpdateDisasterResponseJob.Result result = m_Result.GetResult();
		m_ShelteredCount.Update(result.m_Count);
		m_ShelterCapacity.Update(result.m_Capacity);
		m_ShelterAvailability.Update(new IndicatorValue(0f, result.m_Capacity, result.m_Capacity - result.m_Count));
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
	public DisasterInfoviewUISystem()
	{
	}
}
