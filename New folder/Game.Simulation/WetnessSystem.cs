using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
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
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WetnessSystem : GameSystemBase
{
	[BurstCompile]
	private struct WetnessJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;

		public ComponentTypeHandle<Surface> m_ObjectSurfaceType;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public EntityArchetype m_SubObjectEventArchetype;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public float4 m_TargetWetness;

		[ReadOnly]
		public float4 m_WetSpeed;

		[ReadOnly]
		public float4 m_DrySpeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityTypeHandle);
			NativeArray<Surface> nativeArray2 = chunk.GetNativeArray(ref m_ObjectSurfaceType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			bool flag = chunk.Has(ref m_SubObjectType) && !chunk.Has(ref m_OwnerType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				ref Surface reference = ref nativeArray2.ElementAt(i);
				ObjectRequirementFlags objectRequirementFlags = (ObjectRequirementFlags)0;
				if (reference.m_AccumulatedSnow >= 15)
				{
					objectRequirementFlags |= ObjectRequirementFlags.Snow;
				}
				int4 @int = new int4(reference.m_Wetness, reference.m_SnowAmount, reference.m_AccumulatedWetness, reference.m_AccumulatedSnow);
				float4 @float = math.clamp(m_TargetWetness - (float4)@int * 0.003921569f, m_DrySpeed, m_WetSpeed);
				@float *= random.NextFloat4(0.8f, 1f);
				@int = math.clamp(@int + MathUtils.RoundToIntRandom(ref random, @float * 255f), 0, 255);
				reference.m_Wetness = (byte)@int.x;
				reference.m_SnowAmount = (byte)@int.y;
				reference.m_AccumulatedWetness = (byte)@int.z;
				reference.m_AccumulatedSnow = (byte)@int.w;
				ObjectRequirementFlags objectRequirementFlags2 = (ObjectRequirementFlags)0;
				if (reference.m_AccumulatedSnow >= 15)
				{
					objectRequirementFlags2 |= ObjectRequirementFlags.Snow;
				}
				ObjectRequirementFlags objectRequirementFlags3 = objectRequirementFlags2 ^ objectRequirementFlags;
				if (flag && objectRequirementFlags3 != 0)
				{
					PrefabRef prefabRef = nativeArray3[i];
					if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_SubObjectMask & objectRequirementFlags3) != 0)
					{
						Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_SubObjectEventArchetype);
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new SubObjectsUpdated(nativeArray[i]));
					}
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		public ComponentTypeHandle<Surface> __Game_Objects_Surface_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Objects_Surface_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Surface>();
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
		}
	}

	public const int SNOW_REQUIREMENT_LIMIT = 15;

	private ClimateSystem m_ClimateSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_SurfaceQuery;

	private EntityArchetype m_SubObjectEventArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SurfaceQuery = GetEntityQuery(ComponentType.ReadWrite<Surface>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Overridden>(), ComponentType.Exclude<Temp>());
		m_SubObjectEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<SubObjectsUpdated>());
		RequireForUpdate(m_SurfaceQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		float4 x = default(float4);
		float4 x2 = default(float4);
		float4 x3 = default(float4);
		float num = m_ClimateSystem.precipitation;
		float num2 = m_ClimateSystem.temperature;
		if (num2 > 0f)
		{
			x.x = math.sqrt(num);
			x.z = math.sqrt(x.x);
			x2.x = num * 0.1f;
			x2.z = num * 0.01f;
			x3.x = (1f - num) * 0.05f;
			x3.y = num2 * 0.01f;
			x3.z = (1f - num) * 0.005f;
			x3.w = num2 * 0.001f;
		}
		else
		{
			x.yw = 1f;
			x2.y = num * 0.05f;
			x2.w = num * 0.005f;
			x3.x = num2 * -0.01f;
			x3.z = num2 * -0.001f;
		}
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new WetnessJob
		{
			m_EntityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ObjectSurfaceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Surface_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjectEventArchetype = m_SubObjectEventArchetype,
			m_RandomSeed = RandomSeed.Next(),
			m_TargetWetness = math.saturate(x),
			m_WetSpeed = math.saturate(x2),
			m_DrySpeed = -math.saturate(x3),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_SurfaceQuery, base.Dependency);
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
	public WetnessSystem()
	{
	}
}
