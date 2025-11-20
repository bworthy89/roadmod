using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Policies;

[CompilerGenerated]
public class DistrictModifierInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeDistrictModifiersJob : IJobChunk
	{
		[ReadOnly]
		public DistrictModifierRefreshData m_DistrictModifierRefreshData;

		[ReadOnly]
		public BufferTypeHandle<Policy> m_PolicyType;

		public ComponentTypeHandle<District> m_DistrictType;

		public BufferTypeHandle<DistrictModifier> m_DistrictModifierType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<District> nativeArray = chunk.GetNativeArray(ref m_DistrictType);
			BufferAccessor<DistrictModifier> bufferAccessor = chunk.GetBufferAccessor(ref m_DistrictModifierType);
			BufferAccessor<Policy> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PolicyType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				DynamicBuffer<Policy> policies = bufferAccessor2[i];
				if (policies.Length != 0)
				{
					District district = nativeArray[i];
					m_DistrictModifierRefreshData.RefreshDistrictOptions(ref district, policies);
					nativeArray[i] = district;
					if (bufferAccessor.Length != 0)
					{
						DynamicBuffer<DistrictModifier> modifiers = bufferAccessor[i];
						m_DistrictModifierRefreshData.RefreshDistrictModifiers(modifiers, policies);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public struct DistrictModifierRefreshData
	{
		public ComponentLookup<PolicySliderData> m_PolicySliderData;

		public ComponentLookup<DistrictOptionData> m_DistrictOptionData;

		public BufferLookup<DistrictModifierData> m_DistrictModifierData;

		public DistrictModifierRefreshData(SystemBase system)
		{
			m_PolicySliderData = system.GetComponentLookup<PolicySliderData>(isReadOnly: true);
			m_DistrictOptionData = system.GetComponentLookup<DistrictOptionData>(isReadOnly: true);
			m_DistrictModifierData = system.GetBufferLookup<DistrictModifierData>(isReadOnly: true);
		}

		public void Update(SystemBase system)
		{
			m_PolicySliderData.Update(system);
			m_DistrictOptionData.Update(system);
			m_DistrictModifierData.Update(system);
		}

		public void RefreshDistrictOptions(ref District district, DynamicBuffer<Policy> policies)
		{
			district.m_OptionMask = 0u;
			for (int i = 0; i < policies.Length; i++)
			{
				Policy policy = policies[i];
				if ((policy.m_Flags & PolicyFlags.Active) != 0 && m_DistrictOptionData.HasComponent(policy.m_Policy))
				{
					DistrictOptionData districtOptionData = m_DistrictOptionData[policy.m_Policy];
					district.m_OptionMask |= districtOptionData.m_OptionMask;
				}
			}
		}

		public void RefreshDistrictModifiers(DynamicBuffer<DistrictModifier> modifiers, DynamicBuffer<Policy> policies)
		{
			modifiers.Clear();
			for (int i = 0; i < policies.Length; i++)
			{
				Policy policy = policies[i];
				if ((policy.m_Flags & PolicyFlags.Active) == 0 || !m_DistrictModifierData.HasBuffer(policy.m_Policy))
				{
					continue;
				}
				DynamicBuffer<DistrictModifierData> dynamicBuffer = m_DistrictModifierData[policy.m_Policy];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					DistrictModifierData modifierData = dynamicBuffer[j];
					float delta;
					if (m_PolicySliderData.HasComponent(policy.m_Policy))
					{
						PolicySliderData policySliderData = m_PolicySliderData[policy.m_Policy];
						float falseValue = (policy.m_Adjustment - policySliderData.m_Range.min) / (policySliderData.m_Range.max - policySliderData.m_Range.min);
						falseValue = math.select(falseValue, 0f, policySliderData.m_Range.min == policySliderData.m_Range.max);
						falseValue = math.saturate(falseValue);
						delta = math.lerp(modifierData.m_Range.min, modifierData.m_Range.max, falseValue);
					}
					else
					{
						delta = modifierData.m_Range.min;
					}
					AddModifier(modifiers, modifierData, delta);
				}
			}
		}

		private static void AddModifier(DynamicBuffer<DistrictModifier> modifiers, DistrictModifierData modifierData, float delta)
		{
			while (modifiers.Length <= (int)modifierData.m_Type)
			{
				modifiers.Add(default(DistrictModifier));
			}
			DistrictModifier value = modifiers[(int)modifierData.m_Type];
			switch (modifierData.m_Mode)
			{
			case ModifierValueMode.Relative:
				value.m_Delta.y = value.m_Delta.y * (1f + delta) + delta;
				break;
			case ModifierValueMode.Absolute:
				value.m_Delta.x += delta;
				break;
			case ModifierValueMode.InverseRelative:
				delta = 1f / math.max(0.001f, 1f + delta) - 1f;
				value.m_Delta.y = value.m_Delta.y * (1f + delta) + delta;
				break;
			}
			modifiers[(int)modifierData.m_Type] = value;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferTypeHandle<Policy> __Game_Policies_Policy_RO_BufferTypeHandle;

		public ComponentTypeHandle<District> __Game_Areas_District_RW_ComponentTypeHandle;

		public BufferTypeHandle<DistrictModifier> __Game_Areas_DistrictModifier_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Policies_Policy_RO_BufferTypeHandle = state.GetBufferTypeHandle<Policy>(isReadOnly: true);
			__Game_Areas_District_RW_ComponentTypeHandle = state.GetComponentTypeHandle<District>();
			__Game_Areas_DistrictModifier_RW_BufferTypeHandle = state.GetBufferTypeHandle<DistrictModifier>();
		}
	}

	private EntityQuery m_CreatedQuery;

	private DistrictModifierRefreshData m_DistrictModifierRefreshData;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_DistrictModifierRefreshData = new DistrictModifierRefreshData(this);
		m_CreatedQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadWrite<District>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CreatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_DistrictModifierRefreshData.Update(this);
		InitializeDistrictModifiersJob jobData = new InitializeDistrictModifiersJob
		{
			m_DistrictModifierRefreshData = m_DistrictModifierRefreshData,
			m_PolicyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Policies_Policy_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_DistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_District_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DistrictModifierType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_DistrictModifier_RW_BufferTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CreatedQuery, base.Dependency);
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
	public DistrictModifierInitializeSystem()
	{
	}
}
