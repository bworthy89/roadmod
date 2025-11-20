using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
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
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ExtractorFacilityAISystem : GameSystemBase
{
	[BurstCompile]
	private struct ExtractorFacilityTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Game.Buildings.ExtractorFacility> m_ExtractorFacilityType;

		public ComponentTypeHandle<PointOfInterest> m_PointOfInterestType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Building> m_Building;

		[ReadOnly]
		public BufferLookup<Efficiency> m_BuildingEfficiencyData;

		[ReadOnly]
		public ComponentLookup<ExtractorFacilityData> m_PrefabExtractorFacilityData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.ExtractorFacility> nativeArray4 = chunk.GetNativeArray(ref m_ExtractorFacilityType);
			NativeArray<PointOfInterest> nativeArray5 = chunk.GetNativeArray(ref m_PointOfInterestType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray3[i];
				ref Game.Buildings.ExtractorFacility extractorFacility = ref nativeArray4.ElementAt(i);
				ref PointOfInterest pointOfInterest = ref nativeArray5.ElementAt(i);
				Owner owner = default(Owner);
				if (nativeArray2.Length != 0)
				{
					owner = nativeArray2[i];
				}
				Tick(unfilteredChunkIndex, entity, ref random, ref extractorFacility, ref pointOfInterest, owner, prefabRef);
			}
		}

		private void Tick(int jobIndex, Entity entity, ref Unity.Mathematics.Random random, ref Game.Buildings.ExtractorFacility extractorFacility, ref PointOfInterest pointOfInterest, Owner owner, PrefabRef prefabRef)
		{
			ExtractorFacilityData prefabExtractorFacilityData = default(ExtractorFacilityData);
			if (m_PrefabExtractorFacilityData.HasComponent(prefabRef.m_Prefab))
			{
				prefabExtractorFacilityData = m_PrefabExtractorFacilityData[prefabRef.m_Prefab];
			}
			Entity entity2 = owner.m_Owner;
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity2, out componentData))
			{
				entity2 = componentData.m_Owner;
			}
			if (m_AttachmentData.TryGetComponent(entity2, out var componentData2))
			{
				entity2 = componentData2.m_Attached;
			}
			float efficiency = BuildingUtils.GetEfficiency(entity2, ref m_BuildingEfficiencyData);
			if (m_Building.TryGetComponent(entity2, out var componentData3) && ((componentData3.m_Flags ^ extractorFacility.m_MainBuildingFlags) & Game.Buildings.BuildingFlags.LowEfficiency) != Game.Buildings.BuildingFlags.None)
			{
				m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, entity);
				extractorFacility.m_MainBuildingFlags = componentData3.m_Flags;
			}
			if (!(random.NextFloat(1f) < efficiency))
			{
				return;
			}
			if ((extractorFacility.m_Flags & ExtractorFlags.Working) != 0)
			{
				if (--extractorFacility.m_Timer == 0)
				{
					extractorFacility.m_Flags &= ~ExtractorFlags.Working;
					if (prefabExtractorFacilityData.m_RotationRange.max != prefabExtractorFacilityData.m_RotationRange.min)
					{
						StartRotating(entity, ref random, ref extractorFacility, ref pointOfInterest, prefabRef, prefabExtractorFacilityData);
					}
					else
					{
						StartWorking(entity, ref random, ref extractorFacility, ref pointOfInterest, prefabExtractorFacilityData);
					}
				}
			}
			else if ((extractorFacility.m_Flags & ExtractorFlags.Rotating) != 0)
			{
				if (--extractorFacility.m_Timer == 0)
				{
					extractorFacility.m_Flags &= ~ExtractorFlags.Rotating;
					StartWorking(entity, ref random, ref extractorFacility, ref pointOfInterest, prefabExtractorFacilityData);
				}
			}
			else if (prefabExtractorFacilityData.m_RotationRange.max != prefabExtractorFacilityData.m_RotationRange.min)
			{
				StartRotating(entity, ref random, ref extractorFacility, ref pointOfInterest, prefabRef, prefabExtractorFacilityData);
			}
			else
			{
				StartWorking(entity, ref random, ref extractorFacility, ref pointOfInterest, prefabExtractorFacilityData);
			}
		}

		private void StartRotating(Entity entity, ref Unity.Mathematics.Random random, ref Game.Buildings.ExtractorFacility extractorFacility, ref PointOfInterest pointOfInterest, PrefabRef prefabRef, ExtractorFacilityData prefabExtractorFacilityData)
		{
			Transform transform = m_TransformData[entity];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			float x = random.NextFloat(prefabExtractorFacilityData.m_RotationRange.min, prefabExtractorFacilityData.m_RotationRange.max);
			float z = objectGeometryData.m_Bounds.max.z;
			float3 v = new float3
			{
				x = math.sin(x),
				y = prefabExtractorFacilityData.m_HeightOffset.max,
				z = math.cos(x)
			};
			float2 t;
			float2 @float;
			if (pointOfInterest.m_IsValid)
			{
				float2 xz = math.rotate(math.inverse(transform.m_Rotation), pointOfInterest.m_Position - transform.m_Position).xz;
				t = default(float2);
				@float = math.normalizesafe(xz, t);
			}
			else
			{
				@float = math.forward().xz;
			}
			if (prefabExtractorFacilityData.m_RotationRange.max - prefabExtractorFacilityData.m_RotationRange.min < 6.2821856f)
			{
				float x2 = MathUtils.Center(prefabExtractorFacilityData.m_RotationRange) + MathF.PI;
				float2 float2 = new float2(math.sin(x2), math.cos(x2));
				if (MathUtils.Distance(new Line2.Segment(@float, v.xz), new Line2.Segment(float2.zero, float2), out t) < 0.001f)
				{
					v.xz = float2 * (math.dot(v.xz, float2) * 2f) - v.xz;
				}
			}
			float num = MathUtils.RotationAngle(v.xz, @float);
			v.xz *= z;
			pointOfInterest.m_Position = transform.m_Position + math.rotate(transform.m_Rotation, v);
			pointOfInterest.m_IsValid = true;
			extractorFacility.m_Flags |= ExtractorFlags.Rotating;
			extractorFacility.m_Timer = (byte)MathUtils.RoundToIntRandom(ref random, 2f + num * 2f);
		}

		private void StartWorking(Entity entity, ref Unity.Mathematics.Random random, ref Game.Buildings.ExtractorFacility extractorFacility, ref PointOfInterest pointOfInterest, ExtractorFacilityData prefabExtractorFacilityData)
		{
			extractorFacility.m_Flags |= ExtractorFlags.Working;
			extractorFacility.m_Timer = (byte)random.NextInt(10, 31);
			if (pointOfInterest.m_IsValid)
			{
				Transform transform = m_TransformData[entity];
				float y = math.rotate(math.inverse(transform.m_Rotation), pointOfInterest.m_Position - transform.m_Position).y;
				pointOfInterest.m_Position += math.rotate(transform.m_Rotation, new float3(0f, prefabExtractorFacilityData.m_HeightOffset.min - y, 0f));
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

		public ComponentTypeHandle<Game.Buildings.ExtractorFacility> __Game_Buildings_ExtractorFacility_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PointOfInterest> __Game_Common_PointOfInterest_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorFacilityData> __Game_Prefabs_ExtractorFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_ExtractorFacility_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ExtractorFacility>();
			__Game_Common_PointOfInterest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PointOfInterest>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_ExtractorFacilityData_RO_ComponentLookup = state.GetComponentLookup<ExtractorFacilityData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
		}
	}

	private EntityQuery m_BuildingQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 224;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.ExtractorFacility>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_BuildingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.Schedule(new ExtractorFacilityTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ExtractorFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ExtractorFacility_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PointOfInterestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PointOfInterest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingEfficiencyData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
			m_Building = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabExtractorFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_RandomSeed = RandomSeed.Next()
		}, m_BuildingQuery, base.Dependency);
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
	public ExtractorFacilityAISystem()
	{
	}
}
