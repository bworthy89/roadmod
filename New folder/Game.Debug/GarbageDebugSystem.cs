using System.Runtime.CompilerServices;
using Colossal;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class GarbageDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct GarbageGizmoJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<GarbageProducer> m_GarbageProducerType;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public GarbageParameterData m_GarbageParameterData;

		public bool m_AccumulatedOption;

		public bool m_ProduceOption;

		public GizmoBatcher m_GizmoBatcher;

		private void DrawGarbage(Game.Objects.Transform t, int value)
		{
			float3 position = t.m_Position;
			float num = (float)value / 2f;
			position.y += num / 2f;
			int num2 = m_GarbageParameterData.m_HappinessEffectBaseline + m_GarbageParameterData.m_HappinessEffectStep;
			UnityEngine.Color color = UnityEngine.Color.green;
			if (value > num2)
			{
				color = UnityEngine.Color.Lerp(UnityEngine.Color.green, UnityEngine.Color.red, math.saturate((float)(value - num2) * 1f / (float)m_GarbageParameterData.m_HappinessEffectStep * 9f));
			}
			m_GizmoBatcher.DrawWireCube(position, new float3(5f, num, 5f), color);
		}

		private void DrawConsume(Game.Objects.Transform t, float value)
		{
			float3 position = t.m_Position;
			float num = value / 3f;
			position.y += num / 2f;
			UnityEngine.Color color = UnityEngine.Color.Lerp(UnityEngine.Color.green, UnityEngine.Color.red, math.saturate(value / 20000f));
			m_GizmoBatcher.DrawWireCube(position, new float3(5f, num, 5f), color);
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<GarbageProducer> nativeArray2 = chunk.GetNativeArray(ref m_GarbageProducerType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			if (m_AccumulatedOption)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					DrawGarbage(nativeArray3[i], nativeArray2[i].m_Garbage);
				}
			}
			if (m_ProduceOption)
			{
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity prefab = nativeArray[j].m_Prefab;
					DrawConsume(nativeArray3[j], m_ConsumptionDatas[prefab].m_GarbageAccumulation);
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
		public ComponentTypeHandle<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_GarbageProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GarbageProducer>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(isReadOnly: true);
		}
	}

	private EntityQuery m_BuildingGroup;

	private GizmosSystem m_GizmosSystem;

	private Option m_AccumulatedOption;

	private Option m_ProduceOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_BuildingGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Objects.Transform>(),
				ComponentType.ReadOnly<GarbageProducer>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Hidden>()
			}
		});
		base.Enabled = false;
		m_AccumulatedOption = AddOption("Accumulated Garbage", defaultEnabled: true);
		m_ProduceOption = AddOption("Produce Garbage", defaultEnabled: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_BuildingGroup.IsEmptyIgnoreFilter)
		{
			JobHandle dependencies;
			GarbageGizmoJob jobData = new GarbageGizmoJob
			{
				m_GarbageProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ConsumptionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AccumulatedOption = m_AccumulatedOption.enabled,
				m_ProduceOption = m_ProduceOption.enabled,
				m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies),
				m_GarbageParameterData = GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>()).GetSingleton<GarbageParameterData>()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_BuildingGroup, JobHandle.CombineDependencies(base.Dependency, dependencies));
			m_GizmosSystem.AddGizmosBatcherWriter(base.Dependency);
		}
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
	public GarbageDebugSystem()
	{
	}
}
