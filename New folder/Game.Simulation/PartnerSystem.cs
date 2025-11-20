using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Citizens;
using Game.Economy;
using Game.Serialization;
using Game.Triggers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PartnerSystem : GameSystemBase, IPostDeserialize
{
	[BurstCompile]
	private struct MatchJob : IJob
	{
		public Entity m_PartnerEntity;

		public BufferLookup<LookingForPartner> m_Partners;

		public ComponentLookup<Citizen> m_Citizens;

		public ComponentLookup<Household> m_Households;

		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		public BufferLookup<HouseholdAnimal> m_HouseholdAnimals;

		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		public ComponentLookup<HouseholdPet> m_HouseholdPets;

		public BufferLookup<Resources> m_Resources;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeQueue<TriggerAction> m_TriggerBuffer;

		private void MoveTogether(Entity citizen1, Entity citizen2)
		{
			Entity household = m_HouseholdMembers[citizen1].m_Household;
			Entity household2 = m_HouseholdMembers[citizen2].m_Household;
			if (!m_Households.HasComponent(household) || !m_Households.HasComponent(household2))
			{
				return;
			}
			Household value = m_Households[household];
			Household household3 = m_Households[household2];
			value.m_Resources = (int)math.clamp((long)value.m_Resources + (long)household3.m_Resources, -2147483648L, 2147483647L);
			m_Households[household] = value;
			DynamicBuffer<Resources> resources = m_Resources[household];
			DynamicBuffer<Resources> dynamicBuffer = m_Resources[household2];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				EconomyUtils.AddResources(dynamicBuffer[i].m_Resource, dynamicBuffer[i].m_Amount, resources);
			}
			DynamicBuffer<HouseholdCitizen> dynamicBuffer2 = m_HouseholdCitizens[household];
			DynamicBuffer<HouseholdCitizen> dynamicBuffer3 = m_HouseholdCitizens[household2];
			for (int j = 0; j < dynamicBuffer3.Length; j++)
			{
				dynamicBuffer2.Add(dynamicBuffer3[j]);
				m_HouseholdMembers[dynamicBuffer3[j].m_Citizen] = new HouseholdMember
				{
					m_Household = household
				};
			}
			dynamicBuffer3.Clear();
			if (m_HouseholdAnimals.HasBuffer(household2))
			{
				DynamicBuffer<HouseholdAnimal> dynamicBuffer4 = m_HouseholdAnimals[household2];
				for (int k = 0; k < dynamicBuffer4.Length; k++)
				{
					DynamicBuffer<HouseholdAnimal> dynamicBuffer5 = default(DynamicBuffer<HouseholdAnimal>);
					(m_HouseholdAnimals.HasBuffer(household) ? m_HouseholdAnimals[household] : m_CommandBuffer.AddBuffer<HouseholdAnimal>(household)).Add(dynamicBuffer4[k]);
					m_HouseholdPets[dynamicBuffer4[k].m_HouseholdPet] = new HouseholdPet
					{
						m_Household = household
					};
				}
				dynamicBuffer4.Clear();
			}
		}

		public void Execute()
		{
			DynamicBuffer<LookingForPartner> dynamicBuffer = m_Partners[m_PartnerEntity];
			for (int num = dynamicBuffer.Length - 1; num >= 0; num--)
			{
				LookingForPartner value = dynamicBuffer[num];
				if (m_Citizens.HasComponent(value.m_Citizen))
				{
					bool flag = (m_Citizens[value.m_Citizen].m_State & CitizenFlags.Male) != 0;
					for (int num2 = num - 1; num2 >= 0; num2--)
					{
						LookingForPartner value2 = dynamicBuffer[num2];
						if (m_Citizens.HasComponent(value2.m_Citizen))
						{
							bool flag2 = (m_Citizens[value2.m_Citizen].m_State & CitizenFlags.Male) != 0;
							bool flag3 = flag == flag2;
							if ((value2.m_PartnerType == PartnerType.Any || (flag3 && value2.m_PartnerType == PartnerType.Same) || (!flag3 && value2.m_PartnerType == PartnerType.Other)) && (value.m_PartnerType == PartnerType.Any || (flag3 && value.m_PartnerType == PartnerType.Same) || (!flag3 && value.m_PartnerType == PartnerType.Other)))
							{
								MoveTogether(dynamicBuffer[num].m_Citizen, dynamicBuffer[num2].m_Citizen);
								value.m_PartnerType = PartnerType.None;
								dynamicBuffer[num] = value;
								value2.m_PartnerType = PartnerType.None;
								dynamicBuffer[num2] = value2;
								Citizen value3 = m_Citizens[value.m_Citizen];
								value3.m_State &= ~CitizenFlags.LookingForPartner;
								m_Citizens[value.m_Citizen] = value3;
								value3 = m_Citizens[value2.m_Citizen];
								value3.m_State &= ~CitizenFlags.LookingForPartner;
								m_Citizens[value2.m_Citizen] = value3;
								m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenPartneredUp, Entity.Null, value.m_Citizen, value2.m_Citizen));
								m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenPartneredUp, Entity.Null, value2.m_Citizen, value.m_Citizen));
							}
						}
					}
				}
			}
			for (int num3 = dynamicBuffer.Length - 1; num3 >= 0; num3--)
			{
				if (dynamicBuffer[num3].m_PartnerType == PartnerType.None || !m_Citizens.HasComponent(dynamicBuffer[num3].m_Citizen))
				{
					dynamicBuffer[num3] = dynamicBuffer[dynamicBuffer.Length - 1];
					dynamicBuffer.RemoveAt(dynamicBuffer.Length - 1);
				}
			}
		}
	}

	private struct TypeHandle
	{
		public BufferLookup<LookingForPartner> __Game_Citizens_LookingForPartner_RW_BufferLookup;

		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RW_ComponentLookup;

		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RW_BufferLookup;

		public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RW_BufferLookup;

		public ComponentLookup<Household> __Game_Citizens_Household_RW_ComponentLookup;

		public ComponentLookup<HouseholdPet> __Game_Citizens_HouseholdPet_RW_ComponentLookup;

		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RW_ComponentLookup;

		public BufferLookup<Resources> __Game_Economy_Resources_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Citizens_LookingForPartner_RW_BufferLookup = state.GetBufferLookup<LookingForPartner>();
			__Game_Citizens_Citizen_RW_ComponentLookup = state.GetComponentLookup<Citizen>();
			__Game_Citizens_HouseholdCitizen_RW_BufferLookup = state.GetBufferLookup<HouseholdCitizen>();
			__Game_Citizens_HouseholdAnimal_RW_BufferLookup = state.GetBufferLookup<HouseholdAnimal>();
			__Game_Citizens_Household_RW_ComponentLookup = state.GetComponentLookup<Household>();
			__Game_Citizens_HouseholdPet_RW_ComponentLookup = state.GetComponentLookup<HouseholdPet>();
			__Game_Citizens_HouseholdMember_RW_ComponentLookup = state.GetComponentLookup<HouseholdMember>();
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Resources>();
		}
	}

	public static readonly int kUpdatesPerDay = 4;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_PartnerQuery;

	private TriggerSystem m_TriggerSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PartnerQuery = GetEntityQuery(ComponentType.ReadWrite<LookingForPartner>());
	}

	public void PostDeserialize(Context context)
	{
		if (m_PartnerQuery.IsEmptyIgnoreFilter)
		{
			base.World.EntityManager.CreateEntity(ComponentType.ReadWrite<LookingForPartner>());
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		MatchJob jobData = new MatchJob
		{
			m_PartnerEntity = m_PartnerQuery.GetSingletonEntity(),
			m_Partners = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_LookingForPartner_RW_BufferLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdAnimals = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdAnimal_RW_BufferLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RW_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdPets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdPet_RW_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer()
		};
		base.Dependency = IJobExtensions.Schedule(jobData, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
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
	public PartnerSystem()
	{
	}
}
