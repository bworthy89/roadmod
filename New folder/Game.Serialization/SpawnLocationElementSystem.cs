using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class SpawnLocationElementSystem : GameSystemBase
{
	[BurstCompile]
	private struct SpawnLocationElementJob : IJobChunk
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
		public ComponentLookup<Owner> m_OwnerData;

		public BufferLookup<SpawnLocationElement> m_SpawnLocationElements;

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
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity spawnLocation = nativeArray[i];
				Owner owner = nativeArray2[i];
				if (m_SpawnLocationElements.TryGetBuffer(owner.m_Owner, out var bufferData))
				{
					bufferData.Add(new SpawnLocationElement(spawnLocation, type));
				}
				Owner componentData;
				while (m_OwnerData.TryGetComponent(owner.m_Owner, out componentData))
				{
					owner = componentData;
					if (m_SpawnLocationElements.TryGetBuffer(owner.m_Owner, out bufferData))
					{
						bufferData.Add(new SpawnLocationElement(spawnLocation, type));
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
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Buildings_SpawnLocationElement_RW_BufferLookup = state.GetBufferLookup<SpawnLocationElement>();
		}
	}

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Query = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Owner>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<SpawnLocation>(),
				ComponentType.ReadOnly<HangaroundLocation>(),
				ComponentType.ReadOnly<ParkingLane>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Secondary>() }
		});
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		SpawnLocationElementJob jobData = new SpawnLocationElementJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HangaroundLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_HangaroundLocation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkingLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_SpawnLocationElement_RW_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_Query, base.Dependency);
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
	public SpawnLocationElementSystem()
	{
	}
}
