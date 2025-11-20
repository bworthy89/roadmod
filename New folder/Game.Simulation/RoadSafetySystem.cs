using System.Runtime.CompilerServices;
using Game.Areas;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
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
public class RoadSafetySystem : GameSystemBase
{
	[BurstCompile]
	private struct RoadSafetyJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_FirePrefabChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<NetCondition> m_NetConditionType;

		[ReadOnly]
		public ComponentTypeHandle<Road> m_RoadType;

		[ReadOnly]
		public ComponentTypeHandle<Composition> m_CompositionType;

		[ReadOnly]
		public ComponentTypeHandle<BorderDistrict> m_BorderDistrictType;

		[ReadOnly]
		public ComponentTypeHandle<EventData> m_PrefabEventType;

		[ReadOnly]
		public ComponentTypeHandle<TrafficAccidentData> m_PrefabTrafficAccidentType;

		[ReadOnly]
		public ComponentTypeHandle<Locked> m_LockedType;

		[ReadOnly]
		public ComponentLookup<RoadComposition> m_RoadCompositionData;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public BufferLookup<DistrictModifier> m_DistrictModifiers;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public float4 m_TimeFactors;

		[ReadOnly]
		public float m_Brightness;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			if (random.NextInt(64) != 0)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<NetCondition> nativeArray2 = chunk.GetNativeArray(ref m_NetConditionType);
			NativeArray<Road> nativeArray3 = chunk.GetNativeArray(ref m_RoadType);
			NativeArray<Composition> nativeArray4 = chunk.GetNativeArray(ref m_CompositionType);
			NativeArray<BorderDistrict> nativeArray5 = chunk.GetNativeArray(ref m_BorderDistrictType);
			DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				NetCondition netCondition = nativeArray2[i];
				Road road = nativeArray3[i];
				Composition composition = nativeArray4[i];
				float duration = math.dot(road.m_TrafficFlowDuration0 + road.m_TrafficFlowDuration1, m_TimeFactors) * 2.6666667f;
				float num = math.dot(road.m_TrafficFlowDistance0 + road.m_TrafficFlowDistance1, m_TimeFactors) * 2.6666667f;
				if (num < 0.01f || !m_RoadCompositionData.TryGetComponent(composition.m_Edge, out var componentData))
				{
					continue;
				}
				float trafficFlowSpeed = NetUtils.GetTrafficFlowSpeed(duration, num);
				float start = math.select(0.7f, 0.9f, (componentData.m_Flags & Game.Prefabs.RoadFlags.HasStreetLights) != 0 && (road.m_Flags & Game.Net.RoadFlags.LightsOff) == 0);
				float num2 = 500f / math.sqrt(num);
				num2 *= math.lerp(0.5f, 1f, trafficFlowSpeed);
				num2 *= math.lerp(1f, 0.75f, math.csum(netCondition.m_Wear) * 0.05f);
				num2 *= math.lerp(start, 1f, math.min(1f, m_Brightness * 2f));
				if ((componentData.m_Flags & Game.Prefabs.RoadFlags.SeparatedCarriageways) != 0)
				{
					num2 *= 1.1f;
				}
				if ((componentData.m_Flags & Game.Prefabs.RoadFlags.UseHighwayRules) == 0 && nativeArray5.Length != 0)
				{
					float2 x = num2;
					BorderDistrict borderDistrict = nativeArray5[i];
					if (m_DistrictModifiers.TryGetBuffer(borderDistrict.m_Left, out var bufferData))
					{
						AreaUtils.ApplyModifier(ref x.x, bufferData, DistrictModifierType.StreetTrafficSafety);
					}
					if (m_DistrictModifiers.TryGetBuffer(borderDistrict.m_Right, out bufferData))
					{
						AreaUtils.ApplyModifier(ref x.y, bufferData, DistrictModifierType.StreetTrafficSafety);
					}
					num2 = ((!(math.cmax(x) >= num2)) ? math.min(num2, math.cmax(x)) : math.max(num2, math.cmin(x)));
				}
				if ((componentData.m_Flags & Game.Prefabs.RoadFlags.UseHighwayRules) != 0)
				{
					CityUtils.ApplyModifier(ref num2, modifiers, CityModifierType.HighwayTrafficSafety);
				}
				TryStartAccident(unfilteredChunkIndex, ref random, entity, num2, EventTargetType.Road);
			}
		}

		private void TryStartAccident(int jobIndex, ref Random random, Entity entity, float roadSafety, EventTargetType targetType)
		{
			for (int i = 0; i < m_FirePrefabChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_FirePrefabChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<EventData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabEventType);
				NativeArray<TrafficAccidentData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_PrefabTrafficAccidentType);
				EnabledMask enabledMask = archetypeChunk.GetEnabledMask(ref m_LockedType);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					TrafficAccidentData trafficAccidentData = nativeArray3[j];
					if (trafficAccidentData.m_RandomSiteType == targetType && (!enabledMask.EnableBit.IsValid || !enabledMask[j]))
					{
						float num = trafficAccidentData.m_OccurenceProbability / math.max(1f, roadSafety);
						if (random.NextFloat(1f) < num)
						{
							CreateAccidentEvent(jobIndex, entity, nativeArray[j], nativeArray2[j], trafficAccidentData);
							return;
						}
					}
				}
			}
		}

		private void CreateAccidentEvent(int jobIndex, Entity targetEntity, Entity eventPrefab, EventData eventData, TrafficAccidentData trafficAccidentData)
		{
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, eventData.m_Archetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(eventPrefab));
			m_CommandBuffer.SetBuffer<TargetElement>(jobIndex, e).Add(new TargetElement(targetEntity));
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

		[ReadOnly]
		public ComponentTypeHandle<NetCondition> __Game_Net_NetCondition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Road> __Game_Net_Road_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EventData> __Game_Prefabs_EventData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrafficAccidentData> __Game_Prefabs_TrafficAccidentData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Locked> __Game_Prefabs_Locked_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<RoadComposition> __Game_Prefabs_RoadComposition_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_NetCondition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCondition>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Road>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BorderDistrict>(isReadOnly: true);
			__Game_Prefabs_EventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EventData>(isReadOnly: true);
			__Game_Prefabs_TrafficAccidentData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrafficAccidentData>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Locked>(isReadOnly: true);
			__Game_Prefabs_RoadComposition_RO_ComponentLookup = state.GetComponentLookup<RoadComposition>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(isReadOnly: true);
		}
	}

	private const int UPDATES_PER_DAY = 64;

	private TimeSystem m_TimeSystem;

	private CitySystem m_CitySystem;

	private LightingSystem m_LightingSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_RoadQuery;

	private EntityQuery m_AccidentPrefabQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 4096;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_LightingSystem = base.World.GetOrCreateSystemManaged<LightingSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_RoadQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Composition>()
			},
			Any = new ComponentType[1] { ComponentType.ReadOnly<Road>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_AccidentPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<EventData>(), ComponentType.ReadOnly<TrafficAccidentData>(), ComponentType.Exclude<Locked>());
		RequireForUpdate(m_RoadQuery);
		RequireForUpdate(m_AccidentPrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		float num = m_TimeSystem.normalizedTime * 4f;
		float4 x = new float4(math.max(num - 3f, 1f - num), 1f - math.abs(num - new float3(1f, 2f, 3f)));
		x = math.saturate(x);
		JobHandle outJobHandle;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new RoadSafetyJob
		{
			m_FirePrefabChunks = m_AccidentPrefabQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_NetConditionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NetCondition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoadType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Road_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BorderDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabEventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_EventData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabTrafficAccidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TrafficAccidentData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LockedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoadCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadComposition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_DistrictModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_RandomSeed = RandomSeed.Next(),
			m_TimeFactors = x,
			m_Brightness = m_LightingSystem.dayLightBrightness,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_RoadQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public RoadSafetySystem()
	{
	}
}
