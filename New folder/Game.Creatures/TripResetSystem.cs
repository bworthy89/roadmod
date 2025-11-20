using System.Runtime.CompilerServices;
using Game.Citizens;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Creatures;

[CompilerGenerated]
public class TripResetSystem : GameSystemBase
{
	[BurstCompile]
	private struct CreatureTripResetJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ResetTrip> m_ResetTripType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleted;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurpose;

		[ReadOnly]
		public BufferLookup<TripNeeded> m_TripNeeded;

		[ReadOnly]
		public BufferLookup<GroupCreature> m_GroupCreatures;

		public ComponentLookup<HumanCurrentLane> m_HumanCurrentLane;

		public ComponentLookup<AnimalCurrentLane> m_AnimalCurrentLane;

		public ComponentLookup<Resident> m_Resident;

		public ComponentLookup<Pet> m_Pet;

		public ComponentLookup<Target> m_Target;

		public ComponentLookup<Divert> m_Divert;

		public ComponentLookup<PathOwner> m_PathOwner;

		public BufferLookup<PathElement> m_PathElements;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ResetTrip> nativeArray2 = chunk.GetNativeArray(ref m_ResetTripType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				ResetTrip resetTrip = nativeArray2[i];
				if (m_Deleted.HasComponent(resetTrip.m_Creature))
				{
					continue;
				}
				if (m_HumanCurrentLane.TryGetComponent(resetTrip.m_Creature, out var componentData))
				{
					componentData.m_Flags &= ~CreatureLaneFlags.EndOfPath;
					m_HumanCurrentLane[resetTrip.m_Creature] = componentData;
				}
				if (m_AnimalCurrentLane.TryGetComponent(resetTrip.m_Creature, out var componentData2))
				{
					componentData2.m_Flags &= ~CreatureLaneFlags.EndOfPath;
					m_AnimalCurrentLane[resetTrip.m_Creature] = componentData2;
				}
				if (m_Target.TryGetComponent(resetTrip.m_Creature, out var componentData3))
				{
					bool flag = false;
					bool flag2 = false;
					if (resetTrip.m_DivertPurpose != Purpose.None)
					{
						if (m_Divert.TryGetComponent(resetTrip.m_Creature, out var componentData4))
						{
							if (componentData4.m_Purpose != resetTrip.m_DivertPurpose || componentData4.m_Target != resetTrip.m_DivertTarget)
							{
								componentData4.m_Purpose = resetTrip.m_DivertPurpose;
								componentData4.m_Target = resetTrip.m_DivertTarget;
								componentData4.m_Data = resetTrip.m_DivertData;
								componentData4.m_Resource = resetTrip.m_DivertResource;
								m_Divert[resetTrip.m_Creature] = componentData4;
								flag = true;
							}
							else
							{
								flag2 = true;
							}
						}
						else
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, resetTrip.m_Creature, new Divert
							{
								m_Target = resetTrip.m_DivertTarget,
								m_Purpose = resetTrip.m_DivertPurpose,
								m_Data = resetTrip.m_DivertData,
								m_Resource = resetTrip.m_DivertResource
							});
							flag = true;
						}
						if (flag && m_PathOwner.TryGetComponent(resetTrip.m_Creature, out var componentData5))
						{
							componentData5.m_State &= ~PathFlags.Failed;
							if (resetTrip.m_HasDivertPath && (componentData5.m_State & PathFlags.Pending) == 0 && m_PathElements.TryGetBuffer(entity, out var bufferData) && m_PathElements.TryGetBuffer(resetTrip.m_Creature, out var bufferData2))
							{
								PathUtils.CopyPath(bufferData, default(PathOwner), 0, bufferData2);
								componentData5.m_ElementIndex = 0;
								componentData5.m_State &= ~PathFlags.DivertObsolete;
								componentData5.m_State |= PathFlags.Updated | PathFlags.CachedObsolete;
							}
							else
							{
								componentData5.m_State |= PathFlags.DivertObsolete;
							}
							m_PathOwner[resetTrip.m_Creature] = componentData5;
						}
					}
					else if (m_Divert.HasComponent(resetTrip.m_Creature))
					{
						m_CommandBuffer.RemoveComponent<Divert>(unfilteredChunkIndex, resetTrip.m_Creature);
					}
					if (resetTrip.m_Target != componentData3.m_Target)
					{
						if (m_Resident.TryGetComponent(resetTrip.m_Creature, out var componentData6))
						{
							componentData6.m_Flags &= ~(ResidentFlags.Arrived | ResidentFlags.Hangaround | ResidentFlags.PreferredLeader | ResidentFlags.IgnoreBenches | ResidentFlags.IgnoreAreas | ResidentFlags.CannotIgnore);
							componentData6.m_Flags |= resetTrip.m_ResidentFlags;
							componentData6.m_Timer = 0;
							m_Resident[resetTrip.m_Creature] = componentData6;
						}
						if (m_Pet.TryGetComponent(resetTrip.m_Creature, out var componentData7))
						{
							componentData7.m_Flags &= ~(PetFlags.Hangaround | PetFlags.Arrived | PetFlags.LeaderArrived);
							m_Pet[resetTrip.m_Creature] = componentData7;
						}
						if (m_PathOwner.TryGetComponent(resetTrip.m_Creature, out var componentData8))
						{
							componentData8.m_State &= ~PathFlags.Failed;
							if (!resetTrip.m_HasDivertPath && (componentData8.m_State & PathFlags.Pending) == 0 && m_PathElements.TryGetBuffer(entity, out var bufferData3) && m_PathElements.TryGetBuffer(resetTrip.m_Creature, out var bufferData4))
							{
								PathUtils.CopyPath(bufferData3, default(PathOwner), 0, bufferData4);
								componentData8.m_ElementIndex = 0;
								if (flag || flag2)
								{
									componentData8.m_State &= ~PathFlags.CachedObsolete;
								}
								else
								{
									componentData8.m_State &= ~PathFlags.Obsolete;
								}
								componentData8.m_State |= PathFlags.Updated;
							}
							else if (flag || flag2)
							{
								componentData8.m_State |= PathFlags.CachedObsolete;
							}
							else
							{
								componentData8.m_State |= PathFlags.Obsolete;
							}
							m_PathOwner[resetTrip.m_Creature] = componentData8;
						}
						m_Target[resetTrip.m_Creature] = new Target(resetTrip.m_Target);
					}
				}
				if (m_Resident.TryGetComponent(resetTrip.m_Creature, out var componentData9))
				{
					if (resetTrip.m_Arrived != Entity.Null && componentData9.m_Citizen != Entity.Null)
					{
						if (m_TripNeeded.HasBuffer(componentData9.m_Citizen))
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, componentData9.m_Citizen, new CurrentBuilding(resetTrip.m_Arrived));
							m_CommandBuffer.SetComponentEnabled<Arrived>(unfilteredChunkIndex, componentData9.m_Citizen, value: true);
						}
						if (m_GroupCreatures.TryGetBuffer(resetTrip.m_Creature, out var bufferData5))
						{
							for (int j = 0; j < bufferData5.Length; j++)
							{
								Entity creature = bufferData5[j].m_Creature;
								if (m_Pet.TryGetComponent(creature, out var componentData10))
								{
									componentData10.m_Flags |= PetFlags.LeaderArrived;
									m_Pet[creature] = componentData10;
								}
							}
						}
					}
					if (resetTrip.m_TravelPurpose != Purpose.None && componentData9.m_Citizen != Entity.Null)
					{
						if (m_TravelPurpose.HasComponent(componentData9.m_Citizen))
						{
							if (m_TravelPurpose[componentData9.m_Citizen].m_Purpose != resetTrip.m_TravelPurpose)
							{
								TravelPurpose component = new TravelPurpose
								{
									m_Purpose = resetTrip.m_TravelPurpose,
									m_Data = resetTrip.m_TravelData,
									m_Resource = resetTrip.m_TravelResource
								};
								m_CommandBuffer.SetComponent(unfilteredChunkIndex, componentData9.m_Citizen, component);
							}
						}
						else
						{
							TravelPurpose component2 = new TravelPurpose
							{
								m_Purpose = resetTrip.m_TravelPurpose
							};
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, componentData9.m_Citizen, component2);
						}
					}
					if (resetTrip.m_NextPurpose != Purpose.None && componentData9.m_Citizen != Entity.Null && m_TripNeeded.HasBuffer(componentData9.m_Citizen))
					{
						DynamicBuffer<TripNeeded> dynamicBuffer = m_TripNeeded[componentData9.m_Citizen];
						DynamicBuffer<TripNeeded> dynamicBuffer2 = m_CommandBuffer.SetBuffer<TripNeeded>(unfilteredChunkIndex, componentData9.m_Citizen);
						dynamicBuffer2.ResizeUninitialized(1 + dynamicBuffer.Length);
						dynamicBuffer2[0] = new TripNeeded
						{
							m_Purpose = resetTrip.m_NextPurpose,
							m_TargetAgent = resetTrip.m_NextTarget,
							m_Data = resetTrip.m_NextData,
							m_Resource = resetTrip.m_NextResource
						};
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							dynamicBuffer2[k + 1] = dynamicBuffer[k];
						}
					}
				}
				if (resetTrip.m_Source != Entity.Null)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, resetTrip.m_Creature, new TripSource(resetTrip.m_Source, resetTrip.m_Delay));
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
		public ComponentTypeHandle<ResetTrip> __Game_Creatures_ResetTrip_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TripNeeded> __Game_Citizens_TripNeeded_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<GroupCreature> __Game_Creatures_GroupCreature_RO_BufferLookup;

		public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RW_ComponentLookup;

		public ComponentLookup<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RW_ComponentLookup;

		public ComponentLookup<Resident> __Game_Creatures_Resident_RW_ComponentLookup;

		public ComponentLookup<Pet> __Game_Creatures_Pet_RW_ComponentLookup;

		public ComponentLookup<Target> __Game_Common_Target_RW_ComponentLookup;

		public ComponentLookup<Divert> __Game_Creatures_Divert_RW_ComponentLookup;

		public ComponentLookup<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Creatures_ResetTrip_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResetTrip>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Citizens_TripNeeded_RO_BufferLookup = state.GetBufferLookup<TripNeeded>(isReadOnly: true);
			__Game_Creatures_GroupCreature_RO_BufferLookup = state.GetBufferLookup<GroupCreature>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RW_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>();
			__Game_Creatures_AnimalCurrentLane_RW_ComponentLookup = state.GetComponentLookup<AnimalCurrentLane>();
			__Game_Creatures_Resident_RW_ComponentLookup = state.GetComponentLookup<Resident>();
			__Game_Creatures_Pet_RW_ComponentLookup = state.GetComponentLookup<Pet>();
			__Game_Common_Target_RW_ComponentLookup = state.GetComponentLookup<Target>();
			__Game_Creatures_Divert_RW_ComponentLookup = state.GetComponentLookup<Divert>();
			__Game_Pathfind_PathOwner_RW_ComponentLookup = state.GetComponentLookup<PathOwner>();
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_ResetQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_ResetQuery = GetEntityQuery(ComponentType.ReadOnly<ResetTrip>());
		RequireForUpdate(m_ResetQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CreatureTripResetJob jobData = new CreatureTripResetJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ResetTripType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_ResetTrip_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Deleted = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TravelPurpose = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TripNeeded = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_TripNeeded_RO_BufferLookup, ref base.CheckedStateRef),
			m_GroupCreatures = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Creatures_GroupCreature_RO_BufferLookup, ref base.CheckedStateRef),
			m_HumanCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_AnimalCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Resident = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Resident_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Pet = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Pet_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Target = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Divert = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Divert_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PathOwner = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_ResetQuery, base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
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
	public TripResetSystem()
	{
	}
}
