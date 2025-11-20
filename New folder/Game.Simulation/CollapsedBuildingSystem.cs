#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CollapsedBuildingSystem : GameSystemBase
{
	[BurstCompile]
	private struct CollapsedBuildingJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<RescueTarget> m_RescueTargetType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<Extension> m_ExtensionType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Attached> m_AttachedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

		[ReadOnly]
		public ComponentLookup<Area> m_AreaData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public EntityArchetype m_RescueRequestArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Destroyed> nativeArray2 = chunk.GetNativeArray(ref m_DestroyedType);
			NativeArray<RescueTarget> nativeArray3 = chunk.GetNativeArray(ref m_RescueTargetType);
			NativeArray<Owner> nativeArray4 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			bool flag = chunk.Has(ref m_AttachedType);
			bool flag2 = chunk.Has(ref m_ServiceUpgradeType);
			bool flag3 = chunk.Has(ref m_ExtensionType);
			if (nativeArray3.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Destroyed destroyed = nativeArray2[i];
					Entity entity = nativeArray[i];
					if (destroyed.m_Cleared < 1f)
					{
						RescueTarget rescueTarget = nativeArray3[i];
						RequestRescueIfNeeded(unfilteredChunkIndex, entity, rescueTarget);
					}
					else
					{
						m_CommandBuffer.RemoveComponent<RescueTarget>(unfilteredChunkIndex, entity);
					}
				}
			}
			else
			{
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					ref Destroyed reference = ref nativeArray2.ElementAt(j);
					PrefabRef prefabRef = nativeArray5[j];
					bool flag4 = false;
					if (m_PrefabBuildingData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						flag4 = (componentData.m_Flags & Game.Prefabs.BuildingFlags.RequireRoad) != 0;
						if (CollectionUtils.TryGet(nativeArray4, j, out var value) && m_PrefabRefData.TryGetComponent(value.m_Owner, out var componentData2) && m_PrefabBuildingData.TryGetComponent(componentData2.m_Prefab, out componentData))
						{
							flag4 |= (componentData.m_Flags & Game.Prefabs.BuildingFlags.RequireRoad) != 0;
						}
					}
					if (reference.m_Cleared < 0f)
					{
						Entity e = nativeArray[j];
						reference.m_Cleared += 1.0666667f;
						if (reference.m_Cleared >= 0f)
						{
							reference.m_Cleared = math.select(1f, 0f, flag4);
							m_CommandBuffer.RemoveComponent<InterpolatedTransform>(unfilteredChunkIndex, e);
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(Updated));
						}
					}
					else if (reference.m_Cleared < 1f && !flag3)
					{
						if (flag4)
						{
							Entity entity2 = nativeArray[j];
							RescueTarget rescueTarget2 = default(RescueTarget);
							RequestRescueIfNeeded(unfilteredChunkIndex, entity2, rescueTarget2);
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, rescueTarget2);
						}
						else
						{
							reference.m_Cleared = 1f;
						}
					}
				}
			}
			for (int k = 0; k < nativeArray2.Length; k++)
			{
				if (!(nativeArray2[k].m_Cleared >= 1f) || flag2 || flag3)
				{
					continue;
				}
				Entity entity3 = nativeArray[k];
				PrefabRef prefabRef2 = nativeArray5[k];
				if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData3))
				{
					if ((componentData3.m_Flags & Game.Objects.GeometryFlags.Overridable) != Game.Objects.GeometryFlags.None && !flag)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity3, default(Deleted));
					}
					else
					{
						if (!CollectionUtils.TryGet(nativeArray4, k, out var value2) || !m_AreaData.HasComponent(value2.m_Owner))
						{
							continue;
						}
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity3, default(Deleted));
						if (m_SubAreas.TryGetBuffer(entity3, out var bufferData))
						{
							for (int l = 0; l < bufferData.Length; l++)
							{
								m_CommandBuffer.AddComponent(unfilteredChunkIndex, bufferData[l].m_Area, default(Updated));
							}
						}
					}
				}
				else
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity3, default(Deleted));
				}
			}
		}

		private void RequestRescueIfNeeded(int jobIndex, Entity entity, RescueTarget rescueTarget)
		{
			if (!m_FireRescueRequestData.HasComponent(rescueTarget.m_Request))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_RescueRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new FireRescueRequest(entity, 10f, FireRescueRequestType.Disaster));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
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
		public ComponentTypeHandle<RescueTarget> __Game_Buildings_RescueTarget_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Extension> __Game_Buildings_Extension_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Attached> __Game_Objects_Attached_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> __Game_Simulation_FireRescueRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Area> __Game_Areas_Area_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_RescueTarget_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RescueTarget>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Buildings_Extension_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Extension>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Attached>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Destroyed_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>();
			__Game_Simulation_FireRescueRequest_RO_ComponentLookup = state.GetComponentLookup<FireRescueRequest>(isReadOnly: true);
			__Game_Areas_Area_RO_ComponentLookup = state.GetComponentLookup<Area>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_CollapsedQuery;

	private EntityArchetype m_RescueRequestArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CollapsedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Destroyed>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Extension>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_RescueRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<FireRescueRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_CollapsedQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CollapsedBuildingJob jobData = new CollapsedBuildingJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_RescueTargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_RescueTarget_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceUpgradeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ExtensionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Extension_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AttachedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FireRescueRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_FireRescueRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Area_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_RescueRequestArchetype = m_RescueRequestArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CollapsedQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public CollapsedBuildingSystem()
	{
	}
}
