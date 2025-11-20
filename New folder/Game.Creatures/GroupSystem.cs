using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Citizens;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Creatures;

[CompilerGenerated]
public class GroupSystem : GameSystemBase
{
	private struct GroupData
	{
		public Entity m_Creature1;

		public Entity m_Creature2;

		public GroupData(Entity creature1, Entity creature2)
		{
			m_Creature1 = creature1;
			m_Creature2 = creature2;
		}
	}

	[BurstCompile]
	private struct ResetTripSetJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<ResetTrip> m_ResetTripType;

		public NativeParallelHashSet<Entity> m_ResetTripSet;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ResetTrip> nativeArray = chunk.GetNativeArray(ref m_ResetTripType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				m_ResetTripSet.Add(nativeArray[i].m_Creature);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FillGroupQueueJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public ComponentTypeHandle<TripSource> m_TripSourceType;

		[ReadOnly]
		public ComponentTypeHandle<Resident> m_ResidentType;

		[ReadOnly]
		public ComponentTypeHandle<Pet> m_PetType;

		[ReadOnly]
		public ComponentTypeHandle<Wildlife> m_WildlifeType;

		[ReadOnly]
		public ComponentTypeHandle<Domesticated> m_DomesticatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<ResetTrip> m_ResetTripType;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> m_GroupMemberType;

		[ReadOnly]
		public BufferTypeHandle<GroupCreature> m_GroupCreatureType;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		[ReadOnly]
		public ComponentLookup<TripSource> m_TripSourceData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMemberData;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> m_CurrentTransportData;

		[ReadOnly]
		public ComponentLookup<HouseholdPet> m_HouseholdPetData;

		[ReadOnly]
		public ComponentLookup<GroupMember> m_GroupMemberData;

		[ReadOnly]
		public ComponentLookup<Resident> m_ResidentData;

		[ReadOnly]
		public ComponentLookup<Pet> m_PetData;

		[ReadOnly]
		public ComponentLookup<Wildlife> m_WildlifeData;

		[ReadOnly]
		public ComponentLookup<Domesticated> m_DomesticatedData;

		[ReadOnly]
		public BufferLookup<GroupCreature> m_GroupCreatures;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> m_HouseholdAnimals;

		[ReadOnly]
		public BufferLookup<OwnedCreature> m_OwnedCreatures;

		[ReadOnly]
		public NativeParallelHashSet<Entity> m_ResetTripSet;

		public NativeQueue<GroupData>.ParallelWriter m_GroupQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ResetTrip> nativeArray = chunk.GetNativeArray(ref m_ResetTripType);
			if (nativeArray.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					ResetTrip resetTrip = nativeArray[i];
					if (m_DeletedData.HasComponent(resetTrip.m_Creature))
					{
						continue;
					}
					if (m_GroupMemberData.TryGetComponent(resetTrip.m_Creature, out var componentData))
					{
						CheckDeletedGroupMember(componentData);
					}
					if (m_GroupCreatures.TryGetBuffer(resetTrip.m_Creature, out var bufferData))
					{
						CheckDeletedGroupLeader(bufferData);
					}
					if (m_UpdatedData.HasComponent(resetTrip.m_Creature) && m_TripSourceData.HasComponent(resetTrip.m_Creature))
					{
						continue;
					}
					m_GroupQueue.Enqueue(new GroupData(resetTrip.m_Creature, Entity.Null));
					if (m_TripSourceData.TryGetComponent(resetTrip.m_Creature, out var componentData2) && m_TargetData.TryGetComponent(resetTrip.m_Creature, out var componentData3))
					{
						if (m_ResidentData.TryGetComponent(resetTrip.m_Creature, out var componentData4))
						{
							CheckUpdatedResident(resetTrip.m_Creature, componentData4, componentData2, componentData3);
						}
						if (m_PetData.TryGetComponent(resetTrip.m_Creature, out var componentData5))
						{
							CheckUpdatedPet(resetTrip.m_Creature, componentData5, componentData2, componentData3);
						}
						if (m_WildlifeData.HasComponent(resetTrip.m_Creature))
						{
							CheckUpdatedWildlife(resetTrip.m_Creature, componentData2);
						}
						if (m_DomesticatedData.HasComponent(resetTrip.m_Creature))
						{
							CheckUpdatedDomesticated(resetTrip.m_Creature, componentData2);
						}
					}
				}
				return;
			}
			if (chunk.Has(ref m_DeletedType))
			{
				NativeArray<GroupMember> nativeArray2 = chunk.GetNativeArray(ref m_GroupMemberType);
				BufferAccessor<GroupCreature> bufferAccessor = chunk.GetBufferAccessor(ref m_GroupCreatureType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					CheckDeletedGroupMember(nativeArray2[j]);
				}
				for (int k = 0; k < bufferAccessor.Length; k++)
				{
					CheckDeletedGroupLeader(bufferAccessor[k]);
				}
				return;
			}
			NativeArray<TripSource> nativeArray3 = chunk.GetNativeArray(ref m_TripSourceType);
			NativeArray<Target> nativeArray4 = chunk.GetNativeArray(ref m_TargetType);
			if (nativeArray3.Length != 0 && nativeArray4.Length != 0)
			{
				NativeArray<Entity> nativeArray5 = chunk.GetNativeArray(m_EntityType);
				NativeArray<Resident> nativeArray6 = chunk.GetNativeArray(ref m_ResidentType);
				NativeArray<Pet> nativeArray7 = chunk.GetNativeArray(ref m_PetType);
				NativeArray<Wildlife> nativeArray8 = chunk.GetNativeArray(ref m_WildlifeType);
				NativeArray<Domesticated> nativeArray9 = chunk.GetNativeArray(ref m_DomesticatedType);
				for (int l = 0; l < nativeArray6.Length; l++)
				{
					CheckUpdatedResident(nativeArray5[l], nativeArray6[l], nativeArray3[l], nativeArray4[l]);
				}
				for (int m = 0; m < nativeArray7.Length; m++)
				{
					CheckUpdatedPet(nativeArray5[m], nativeArray7[m], nativeArray3[m], nativeArray4[m]);
				}
				for (int n = 0; n < nativeArray8.Length; n++)
				{
					CheckUpdatedWildlife(nativeArray5[n], nativeArray3[n]);
				}
				for (int num = 0; num < nativeArray9.Length; num++)
				{
					CheckUpdatedDomesticated(nativeArray5[num], nativeArray3[num]);
				}
			}
		}

		private void CheckDeletedGroupMember(GroupMember groupMember)
		{
			if (!m_DeletedData.HasComponent(groupMember.m_Leader) && !m_ResetTripSet.Contains(groupMember.m_Leader))
			{
				m_GroupQueue.Enqueue(new GroupData(groupMember.m_Leader, Entity.Null));
			}
		}

		private void CheckDeletedGroupLeader(DynamicBuffer<GroupCreature> groupCreatures)
		{
			Entity entity = Entity.Null;
			bool flag = false;
			for (int i = 0; i < groupCreatures.Length; i++)
			{
				GroupCreature groupCreature = groupCreatures[i];
				if (!m_DeletedData.HasComponent(groupCreature.m_Creature) && !m_ResetTripSet.Contains(groupCreature.m_Creature))
				{
					if (entity != Entity.Null)
					{
						m_GroupQueue.Enqueue(new GroupData(entity, groupCreature.m_Creature));
						flag = true;
					}
					entity = groupCreature.m_Creature;
				}
			}
			if (entity != Entity.Null && !flag)
			{
				m_GroupQueue.Enqueue(new GroupData(entity, Entity.Null));
			}
		}

		private void CheckUpdatedResident(Entity creature, Resident resident, TripSource tripSource, Target target)
		{
			if (m_HouseholdMemberData.HasComponent(resident.m_Citizen))
			{
				HouseholdMember householdMember = m_HouseholdMemberData[resident.m_Citizen];
				FindHumanPartners(creature, householdMember.m_Household, tripSource.m_Source, target.m_Target);
				FindAnimalPartners(creature, householdMember.m_Household, tripSource.m_Source, target.m_Target);
			}
		}

		private void CheckUpdatedPet(Entity creature, Pet pet, TripSource tripSource, Target target)
		{
			if (m_HouseholdPetData.HasComponent(pet.m_HouseholdPet))
			{
				HouseholdPet householdPet = m_HouseholdPetData[pet.m_HouseholdPet];
				FindHumanPartners(creature, householdPet.m_Household, tripSource.m_Source, target.m_Target);
				FindAnimalPartners(creature, householdPet.m_Household, tripSource.m_Source, target.m_Target);
			}
		}

		private void CheckUpdatedWildlife(Entity creature, TripSource tripSource)
		{
			FindCreaturePartners(creature, tripSource.m_Source);
		}

		private void CheckUpdatedDomesticated(Entity creature, TripSource tripSource)
		{
			FindCreaturePartners(creature, tripSource.m_Source);
		}

		private void FindCreaturePartners(Entity creature, Entity source)
		{
			if (!m_OwnedCreatures.HasBuffer(source))
			{
				return;
			}
			DynamicBuffer<OwnedCreature> dynamicBuffer = m_OwnedCreatures[source];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				OwnedCreature ownedCreature = dynamicBuffer[i];
				if (!(ownedCreature.m_Creature == creature) && !m_DeletedData.HasComponent(ownedCreature.m_Creature) && m_TripSourceData.HasComponent(ownedCreature.m_Creature) && source == m_TripSourceData[ownedCreature.m_Creature].m_Source)
				{
					m_GroupQueue.Enqueue(new GroupData(creature, ownedCreature.m_Creature));
				}
			}
		}

		private void FindAnimalPartners(Entity creature, Entity household, Entity source, Entity target)
		{
			if (!m_HouseholdAnimals.HasBuffer(household))
			{
				return;
			}
			DynamicBuffer<HouseholdAnimal> dynamicBuffer = m_HouseholdAnimals[household];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				HouseholdAnimal householdAnimal = dynamicBuffer[i];
				if (!m_CurrentTransportData.HasComponent(householdAnimal.m_HouseholdPet))
				{
					continue;
				}
				CurrentTransport currentTransport = m_CurrentTransportData[householdAnimal.m_HouseholdPet];
				if (!(currentTransport.m_CurrentTransport == creature) && !m_DeletedData.HasComponent(currentTransport.m_CurrentTransport) && m_TripSourceData.HasComponent(currentTransport.m_CurrentTransport) && m_TargetData.HasComponent(currentTransport.m_CurrentTransport))
				{
					TripSource tripSource = m_TripSourceData[currentTransport.m_CurrentTransport];
					Target target2 = m_TargetData[currentTransport.m_CurrentTransport];
					if (source == tripSource.m_Source && target == target2.m_Target)
					{
						m_GroupQueue.Enqueue(new GroupData(creature, currentTransport.m_CurrentTransport));
					}
				}
			}
		}

		private void FindHumanPartners(Entity creature, Entity household, Entity source, Entity target)
		{
			if (!m_HouseholdCitizens.HasBuffer(household))
			{
				return;
			}
			DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				HouseholdCitizen householdCitizen = dynamicBuffer[i];
				if (!m_CurrentTransportData.HasComponent(householdCitizen.m_Citizen))
				{
					continue;
				}
				CurrentTransport currentTransport = m_CurrentTransportData[householdCitizen.m_Citizen];
				if (!(currentTransport.m_CurrentTransport == creature) && !m_DeletedData.HasComponent(currentTransport.m_CurrentTransport) && m_TripSourceData.HasComponent(currentTransport.m_CurrentTransport) && m_TargetData.HasComponent(currentTransport.m_CurrentTransport))
				{
					TripSource tripSource = m_TripSourceData[currentTransport.m_CurrentTransport];
					Target target2 = m_TargetData[currentTransport.m_CurrentTransport];
					if (source == tripSource.m_Source && target == target2.m_Target)
					{
						m_GroupQueue.Enqueue(new GroupData(creature, currentTransport.m_CurrentTransport));
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct GroupCreaturesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<Human> m_HumanData;

		[ReadOnly]
		public ComponentLookup<Resident> m_ResidentData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<CarKeeper> m_CarKeeperData;

		public ComponentLookup<GroupMember> m_GroupMemberData;

		public ComponentLookup<PathOwner> m_PathOwnerData;

		public BufferLookup<GroupCreature> m_GroupCreatures;

		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public NativeParallelHashSet<Entity> m_ResetTripSet;

		public NativeQueue<GroupData> m_GroupQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			if (m_GroupQueue.Count == 0)
			{
				return;
			}
			GroupBuilder<Entity> groupBuffer = new GroupBuilder<Entity>(Allocator.Temp);
			GroupData item;
			while (m_GroupQueue.TryDequeue(out item))
			{
				if (item.m_Creature1 != Entity.Null)
				{
					AddExistingGroupMembers(ref groupBuffer, item.m_Creature1);
					if (item.m_Creature2 != Entity.Null)
					{
						AddExistingGroupMembers(ref groupBuffer, item.m_Creature2);
						groupBuffer.AddPair(item.m_Creature1, item.m_Creature2);
					}
					else
					{
						groupBuffer.AddSingle(item.m_Creature1);
					}
				}
				else if (item.m_Creature2 != Entity.Null)
				{
					AddExistingGroupMembers(ref groupBuffer, item.m_Creature2);
					groupBuffer.AddSingle(item.m_Creature2);
				}
			}
			if (groupBuffer.TryGetFirstGroup(out var group, out var iterator))
			{
				do
				{
					ComposeGroup(group);
				}
				while (groupBuffer.TryGetNextGroup(out group, ref iterator));
			}
		}

		private void AddExistingGroupMembers(ref GroupBuilder<Entity> groupBuffer, Entity creature)
		{
			if (m_ResetTripSet.Contains(creature))
			{
				return;
			}
			if (m_GroupMemberData.HasComponent(creature))
			{
				creature = m_GroupMemberData[creature].m_Leader;
				if (m_DeletedData.HasComponent(creature) || m_ResetTripSet.Contains(creature))
				{
					return;
				}
			}
			if (!m_GroupCreatures.HasBuffer(creature))
			{
				return;
			}
			DynamicBuffer<GroupCreature> dynamicBuffer = m_GroupCreatures[creature];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity creature2 = dynamicBuffer[i].m_Creature;
				if (!m_DeletedData.HasComponent(creature2) && !m_ResetTripSet.Contains(creature2))
				{
					groupBuffer.AddPair(creature, creature2);
				}
			}
		}

		private void ComposeGroup(NativeSlice<GroupBuilder<Entity>.Result> group)
		{
			if (group.Length == 1)
			{
				Entity item = group[0].m_Item;
				RemoveGroupMember(item);
				RemoveGroupCreatures(item);
				return;
			}
			Entity entity = FindBestLeader(group);
			RemoveGroupMember(entity);
			DynamicBuffer<GroupCreature> dynamicBuffer = AddGroupCreatures(entity);
			for (int i = 0; i < group.Length; i++)
			{
				Entity item2 = group[i].m_Item;
				if (item2 != entity)
				{
					RemoveGroupCreatures(item2);
					AddGroupMember(item2, entity);
					dynamicBuffer.Add(new GroupCreature(item2));
				}
			}
		}

		private void RemoveGroupMember(Entity creature)
		{
			if (m_GroupMemberData.HasComponent(creature))
			{
				m_CommandBuffer.RemoveComponent<GroupMember>(creature);
				if (m_PathOwnerData.HasComponent(creature))
				{
					PathOwner value = m_PathOwnerData[creature];
					value.m_ElementIndex = 0;
					value.m_State |= PathFlags.Obsolete;
					m_PathOwnerData[creature] = value;
					m_PathElements[creature].Clear();
				}
			}
		}

		private void RemoveGroupCreatures(Entity creature)
		{
			if (m_GroupCreatures.HasBuffer(creature))
			{
				m_CommandBuffer.RemoveComponent<GroupCreature>(creature);
			}
		}

		private void AddGroupMember(Entity creature, Entity leader)
		{
			if (m_GroupMemberData.HasComponent(creature))
			{
				GroupMember value = m_GroupMemberData[creature];
				value.m_Leader = leader;
				m_GroupMemberData[creature] = value;
				return;
			}
			m_CommandBuffer.AddComponent(creature, new GroupMember(leader));
			m_CommandBuffer.RemoveComponent<Divert>(creature);
			if (m_PathOwnerData.HasComponent(creature))
			{
				PathOwner value2 = m_PathOwnerData[creature];
				value2.m_ElementIndex = 0;
				value2.m_State = (PathFlags)0;
				m_PathOwnerData[creature] = value2;
				m_PathElements[creature].Clear();
			}
		}

		private DynamicBuffer<GroupCreature> AddGroupCreatures(Entity creature)
		{
			if (m_GroupCreatures.HasBuffer(creature))
			{
				DynamicBuffer<GroupCreature> result = m_GroupCreatures[creature];
				result.Clear();
				return result;
			}
			return m_CommandBuffer.AddBuffer<GroupCreature>(creature);
		}

		private Entity FindBestLeader(NativeSlice<GroupBuilder<Entity>.Result> group)
		{
			Entity result = Entity.Null;
			int num = -1;
			for (int i = 0; i < group.Length; i++)
			{
				Entity item = group[i].m_Item;
				int num2 = 0;
				if (m_HumanData.HasComponent(item))
				{
					num2 += 10;
				}
				if (m_ResidentData.HasComponent(item))
				{
					Resident resident = m_ResidentData[item];
					if ((resident.m_Flags & ResidentFlags.PreferredLeader) != ResidentFlags.None)
					{
						num2 += 2;
					}
					if (m_CarKeeperData.HasEnabledComponent(resident.m_Citizen))
					{
						num2++;
					}
				}
				if (num2 > num)
				{
					result = item;
					num = num2;
				}
			}
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<ResetTrip> __Game_Creatures_ResetTrip_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TripSource> __Game_Objects_TripSource_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Resident> __Game_Creatures_Resident_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Pet> __Game_Creatures_Pet_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Wildlife> __Game_Creatures_Wildlife_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Domesticated> __Game_Creatures_Domesticated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> __Game_Creatures_GroupMember_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<GroupCreature> __Game_Creatures_GroupCreature_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TripSource> __Game_Objects_TripSource_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdPet> __Game_Citizens_HouseholdPet_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GroupMember> __Game_Creatures_GroupMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Resident> __Game_Creatures_Resident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Pet> __Game_Creatures_Pet_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Wildlife> __Game_Creatures_Wildlife_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Domesticated> __Game_Creatures_Domesticated_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<GroupCreature> __Game_Creatures_GroupCreature_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<OwnedCreature> __Game_Creatures_OwnedCreature_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Human> __Game_Creatures_Human_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;

		public ComponentLookup<GroupMember> __Game_Creatures_GroupMember_RW_ComponentLookup;

		public ComponentLookup<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentLookup;

		public BufferLookup<GroupCreature> __Game_Creatures_GroupCreature_RW_BufferLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Creatures_ResetTrip_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResetTrip>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Objects_TripSource_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TripSource>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Resident>(isReadOnly: true);
			__Game_Creatures_Pet_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Pet>(isReadOnly: true);
			__Game_Creatures_Wildlife_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Wildlife>(isReadOnly: true);
			__Game_Creatures_Domesticated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Domesticated>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroupMember>(isReadOnly: true);
			__Game_Creatures_GroupCreature_RO_BufferTypeHandle = state.GetBufferTypeHandle<GroupCreature>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Objects_TripSource_RO_ComponentLookup = state.GetComponentLookup<TripSource>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
			__Game_Citizens_HouseholdPet_RO_ComponentLookup = state.GetComponentLookup<HouseholdPet>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentLookup = state.GetComponentLookup<GroupMember>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentLookup = state.GetComponentLookup<Resident>(isReadOnly: true);
			__Game_Creatures_Pet_RO_ComponentLookup = state.GetComponentLookup<Pet>(isReadOnly: true);
			__Game_Creatures_Wildlife_RO_ComponentLookup = state.GetComponentLookup<Wildlife>(isReadOnly: true);
			__Game_Creatures_Domesticated_RO_ComponentLookup = state.GetComponentLookup<Domesticated>(isReadOnly: true);
			__Game_Creatures_GroupCreature_RO_BufferLookup = state.GetBufferLookup<GroupCreature>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_HouseholdAnimal_RO_BufferLookup = state.GetBufferLookup<HouseholdAnimal>(isReadOnly: true);
			__Game_Creatures_OwnedCreature_RO_BufferLookup = state.GetBufferLookup<OwnedCreature>(isReadOnly: true);
			__Game_Creatures_Human_RO_ComponentLookup = state.GetComponentLookup<Human>(isReadOnly: true);
			__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(isReadOnly: true);
			__Game_Creatures_GroupMember_RW_ComponentLookup = state.GetComponentLookup<GroupMember>();
			__Game_Pathfind_PathOwner_RW_ComponentLookup = state.GetComponentLookup<PathOwner>();
			__Game_Creatures_GroupCreature_RW_BufferLookup = state.GetBufferLookup<GroupCreature>();
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_CreatureQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_CreatureQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Creature>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<ResetTrip>() }
		});
		RequireForUpdate(m_CreatureQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeParallelHashSet<Entity> resetTripSet = new NativeParallelHashSet<Entity>(100, Allocator.TempJob);
		NativeQueue<GroupData> groupQueue = new NativeQueue<GroupData>(Allocator.TempJob);
		ResetTripSetJob jobData = new ResetTripSetJob
		{
			m_ResetTripType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_ResetTrip_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResetTripSet = resetTripSet
		};
		FillGroupQueueJob jobData2 = new FillGroupQueueJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TripSourceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_TripSource_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Pet_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WildlifeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Wildlife_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DomesticatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Domesticated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResetTripType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_ResetTrip_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GroupMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GroupCreatureType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Creatures_GroupCreature_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TripSourceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_TripSource_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdPetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdPet_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GroupMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Pet_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WildlifeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Wildlife_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DomesticatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Domesticated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GroupCreatures = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Creatures_GroupCreature_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdAnimals = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup, ref base.CheckedStateRef),
			m_OwnedCreatures = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Creatures_OwnedCreature_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResetTripSet = resetTripSet,
			m_GroupQueue = groupQueue.AsParallelWriter()
		};
		GroupCreaturesJob jobData3 = new GroupCreaturesJob
		{
			m_HumanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Human_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarKeeperData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GroupMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_GroupMember_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PathOwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentLookup, ref base.CheckedStateRef),
			m_GroupCreatures = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Creatures_GroupCreature_RW_BufferLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_ResetTripSet = resetTripSet,
			m_GroupQueue = groupQueue,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		};
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(dependsOn: JobChunkExtensions.Schedule(jobData, m_CreatureQuery, base.Dependency), jobData: jobData2, query: m_CreatureQuery);
		JobHandle jobHandle = IJobExtensions.Schedule(jobData3, dependsOn);
		resetTripSet.Dispose(jobHandle);
		groupQueue.Dispose(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
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
	public GroupSystem()
	{
	}
}
