using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class ReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateBuildingReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<SpawnLocation> m_SpawnLocationType;

		[ReadOnly]
		public ComponentTypeHandle<HangaroundLocation> m_HangaroundLocationType;

		[ReadOnly]
		public ComponentTypeHandle<ParkingLane> m_ParkingLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		public BufferLookup<SpawnLocationElement> m_SpawnLocations;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			SpawnLocationType type = SpawnLocationType.None;
			if (chunk.Has(ref m_SpawnLocationType))
			{
				type = SpawnLocationType.SpawnLocation;
			}
			else if (chunk.Has(ref m_HangaroundLocationType))
			{
				type = SpawnLocationType.HangaroundLocation;
			}
			else if (chunk.Has(ref m_ParkingLaneType))
			{
				type = SpawnLocationType.ParkingLane;
			}
			if (chunk.Has(ref m_DeletedType))
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity spawnLocation = nativeArray[i];
					Owner owner = nativeArray2[i];
					if (m_SpawnLocations.TryGetBuffer(owner.m_Owner, out var bufferData))
					{
						CollectionUtils.RemoveValue(bufferData, new SpawnLocationElement(spawnLocation, type));
					}
					Owner componentData;
					while (m_OwnerData.TryGetComponent(owner.m_Owner, out componentData))
					{
						owner = componentData;
						if (m_SpawnLocations.TryGetBuffer(owner.m_Owner, out bufferData))
						{
							CollectionUtils.RemoveValue(bufferData, new SpawnLocationElement(spawnLocation, type));
						}
					}
				}
				return;
			}
			bool flag = chunk.Has(ref m_TempType);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				Entity spawnLocation2 = nativeArray[j];
				Owner owner2 = nativeArray2[j];
				if (flag && !m_TempData.HasComponent(owner2.m_Owner))
				{
					continue;
				}
				if (m_SpawnLocations.TryGetBuffer(owner2.m_Owner, out var bufferData2))
				{
					CollectionUtils.TryAddUniqueValue(bufferData2, new SpawnLocationElement(spawnLocation2, type));
				}
				Owner componentData2;
				while (m_OwnerData.TryGetComponent(owner2.m_Owner, out componentData2))
				{
					owner2 = componentData2;
					if (flag && !m_TempData.HasComponent(owner2.m_Owner))
					{
						break;
					}
					if (m_SpawnLocations.TryGetBuffer(owner2.m_Owner, out bufferData2))
					{
						CollectionUtils.TryAddUniqueValue(bufferData2, new SpawnLocationElement(spawnLocation2, type));
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
		public ComponentTypeHandle<SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HangaroundLocation> __Game_Areas_HangaroundLocation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkingLane> __Game_Net_ParkingLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		public BufferLookup<SpawnLocationElement> __Game_Buildings_SpawnLocationElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnLocation>(isReadOnly: true);
			__Game_Areas_HangaroundLocation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HangaroundLocation>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkingLane>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Buildings_SpawnLocationElement_RW_BufferLookup = state.GetBufferLookup<SpawnLocationElement>();
		}
	}

	private EntityQuery m_SpawnLocationQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SpawnLocationQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Owner>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<SpawnLocation>(),
				ComponentType.ReadOnly<HangaroundLocation>(),
				ComponentType.ReadOnly<ParkingLane>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Secondary>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Owner>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<SpawnLocation>(),
				ComponentType.ReadOnly<HangaroundLocation>(),
				ComponentType.ReadOnly<ParkingLane>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Secondary>() }
		});
		RequireForUpdate(m_SpawnLocationQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateBuildingReferencesJob jobData = new UpdateBuildingReferencesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HangaroundLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_HangaroundLocation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkingLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_SpawnLocationElement_RW_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_SpawnLocationQuery, base.Dependency);
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
	public ReferencesSystem()
	{
	}
}
