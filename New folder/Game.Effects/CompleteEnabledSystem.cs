using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Effects;

[CompilerGenerated]
public class CompleteEnabledSystem : GameSystemBase
{
	[BurstCompile]
	private struct EffectCleanupJob : IJob
	{
		public BufferLookup<EnabledEffect> m_EffectOwners;

		public NativeList<EnabledEffectData> m_EnabledData;

		public NativeQueue<VFXUpdateInfo> m_VFXUpdateQueue;

		public void Execute()
		{
			for (int i = 0; i < m_EnabledData.Length; i++)
			{
				ref EnabledEffectData reference = ref m_EnabledData.ElementAt(i);
				if ((reference.m_Flags & (EnabledEffectFlags.EnabledUpdated | EnabledEffectFlags.OwnerUpdated)) == 0)
				{
					continue;
				}
				if ((reference.m_Flags & EnabledEffectFlags.IsEnabled) == 0)
				{
					if ((reference.m_Flags & EnabledEffectFlags.Deleted) == 0)
					{
						DynamicBuffer<EnabledEffect> dynamicBuffer = m_EffectOwners[reference.m_Owner];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							if (dynamicBuffer[j].m_EffectIndex == reference.m_EffectIndex)
							{
								dynamicBuffer.RemoveAt(j);
								break;
							}
						}
					}
					m_EnabledData.RemoveAtSwapBack(i);
					if (i < m_EnabledData.Length)
					{
						ref EnabledEffectData reference2 = ref m_EnabledData.ElementAt(i);
						if ((reference2.m_Flags & EnabledEffectFlags.Deleted) == 0)
						{
							DynamicBuffer<EnabledEffect> dynamicBuffer2 = m_EffectOwners[reference2.m_Owner];
							for (int k = 0; k < dynamicBuffer2.Length; k++)
							{
								ref EnabledEffect reference3 = ref dynamicBuffer2.ElementAt(k);
								if (reference3.m_EffectIndex == reference2.m_EffectIndex)
								{
									reference3.m_EnabledIndex = i;
									break;
								}
							}
						}
						if ((reference2.m_Flags & (EnabledEffectFlags.IsEnabled | EnabledEffectFlags.IsVFX)) == (EnabledEffectFlags.IsEnabled | EnabledEffectFlags.IsVFX))
						{
							m_VFXUpdateQueue.Enqueue(new VFXUpdateInfo
							{
								m_Type = VFXUpdateType.MoveIndex,
								m_EnabledIndex = new int2(i, m_EnabledData.Length)
							});
						}
					}
					i--;
				}
				else
				{
					reference.m_Flags &= ~(EnabledEffectFlags.EnabledUpdated | EnabledEffectFlags.OwnerUpdated);
				}
			}
		}
	}

	private struct TypeHandle
	{
		public BufferLookup<EnabledEffect> __Game_Effects_EnabledEffect_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Effects_EnabledEffect_RW_BufferLookup = state.GetBufferLookup<EnabledEffect>();
		}
	}

	private EffectControlSystem m_EffectControlSystem;

	private VFXSystem m_VFXSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EffectControlSystem = base.World.GetOrCreateSystemManaged<EffectControlSystem>();
		m_VFXSystem = base.World.GetOrCreateSystemManaged<VFXSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle jobHandle = IJobExtensions.Schedule(new EffectCleanupJob
		{
			m_EffectOwners = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Effects_EnabledEffect_RW_BufferLookup, ref base.CheckedStateRef),
			m_EnabledData = m_EffectControlSystem.GetEnabledData(readOnly: false, out dependencies),
			m_VFXUpdateQueue = m_VFXSystem.GetSourceUpdateData()
		}, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_EffectControlSystem.AddEnabledDataWriter(jobHandle);
		m_VFXSystem.AddSourceUpdateWriter(jobHandle);
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
	public CompleteEnabledSystem()
	{
	}
}
