using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Effects;
using Game.Objects;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Audio;

[CompilerGenerated]
public class SFXCullingSystem : GameSystemBase
{
	[BurstCompile]
	private struct SFXCullingJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<CullingGroupData> m_CullingGroupData;

		[ReadOnly]
		public ComponentLookup<AudioSpotData> m_AudioSpotData;

		[ReadOnly]
		public ComponentLookup<AudioEffectData> m_AudioEffectDatas;

		[ReadOnly]
		public BufferLookup<AudioSourceData> m_AudioSourceDatas;

		[ReadOnly]
		public BufferLookup<Effect> m_PrefabEffects;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public float m_DeltaTime;

		[NativeDisableParallelForRestriction]
		public NativeList<EnabledEffectData> m_EnabledData;

		public NativeParallelQueue<CullingGroupItem>.Writer m_CullingGroupItems;

		public SourceUpdateData m_SourceUpdateData;

		public void Execute(int index)
		{
			ref EnabledEffectData reference = ref m_EnabledData.ElementAt(index);
			if ((reference.m_Flags & EnabledEffectFlags.IsAudio) == 0)
			{
				return;
			}
			if ((reference.m_Flags & EnabledEffectFlags.IsEnabled) == 0)
			{
				if ((reference.m_Flags & EnabledEffectFlags.WrongPrefab) != 0)
				{
					m_SourceUpdateData.WrongPrefab(new SourceInfo(reference.m_Owner, reference.m_EffectIndex));
				}
				else
				{
					m_SourceUpdateData.Remove(new SourceInfo(reference.m_Owner, reference.m_EffectIndex));
				}
				reference.m_NextTime = -1f;
				return;
			}
			if (m_CullingGroupData.TryGetComponent(reference.m_Prefab, out var componentData))
			{
				m_CullingGroupItems.Enqueue(componentData.m_GroupIndex, new CullingGroupItem
				{
					m_EnabledIndex = index,
					m_DistanceSq = math.distancesq(reference.m_Position, m_CameraPosition)
				});
				return;
			}
			float3 x = reference.m_Position;
			float num = float.MaxValue;
			if (m_AudioSourceDatas.TryGetBuffer(reference.m_Prefab, out var bufferData) && bufferData.Length > 0 && m_AudioEffectDatas.TryGetComponent(bufferData[0].m_SFXEntity, out var componentData2))
			{
				num = componentData2.m_MaxDistance;
				if (componentData2.m_SourceSize.x > 0f || componentData2.m_SourceSize.y > 0f || componentData2.m_SourceSize.z > 0f)
				{
					float3 sourceOffset = default(float3);
					if ((reference.m_Flags & EnabledEffectFlags.EditorContainer) == 0)
					{
						PrefabRef prefabRef = m_Prefabs[reference.m_Owner];
						sourceOffset = m_PrefabEffects[prefabRef.m_Prefab][reference.m_EffectIndex].m_Position;
					}
					x = AudioManager.GetClosestSourcePosition(sourceTransform: new Game.Objects.Transform(reference.m_Position, reference.m_Rotation), targetPosition: m_CameraPosition, sourceOffset: sourceOffset, sourceSize: componentData2.m_SourceSize);
				}
			}
			if (math.distancesq(x, m_CameraPosition) >= num * num)
			{
				m_SourceUpdateData.Remove(new SourceInfo(reference.m_Owner, reference.m_EffectIndex));
				reference.m_NextTime = -1f;
				return;
			}
			if (m_AudioSpotData.TryGetComponent(reference.m_Prefab, out var componentData3))
			{
				if (reference.m_NextTime <= 0f)
				{
					reference.m_NextTime = m_RandomSeed.GetRandom(index).NextFloat(componentData3.m_Interval.y);
					return;
				}
				reference.m_NextTime -= m_DeltaTime;
				if (!(reference.m_NextTime < 0f))
				{
					return;
				}
				reference.m_NextTime = m_RandomSeed.GetRandom(index).NextFloat(componentData3.m_Interval.x, componentData3.m_Interval.y);
			}
			m_SourceUpdateData.Add(new SourceInfo(reference.m_Owner, reference.m_EffectIndex));
		}
	}

	private struct CullingGroupItem : IComparable<CullingGroupItem>
	{
		public int m_EnabledIndex;

		public float m_DistanceSq;

		public int CompareTo(CullingGroupItem other)
		{
			return m_DistanceSq.CompareTo(other.m_DistanceSq);
		}
	}

	[BurstCompile]
	private struct SFXGroupCullingJob : IJobParallelFor
	{
		[ReadOnly]
		public int m_MaxAllowedAmount;

		[ReadOnly]
		public float m_MaxDistance;

		[ReadOnly]
		public NativeParallelQueue<CullingGroupItem>.Reader m_CullingGroupItems;

		[NativeDisableParallelForRestriction]
		public NativeList<EnabledEffectData> m_EnabledData;

		public SourceUpdateData m_SourceUpdateData;

		public void Execute(int groupIndex)
		{
			NativeArray<CullingGroupItem> array = m_CullingGroupItems.ToArray(groupIndex, Allocator.Temp);
			array.Sort();
			int i = 0;
			float num = m_MaxDistance * m_MaxDistance;
			for (; i < array.Length && i < m_MaxAllowedAmount; i++)
			{
				CullingGroupItem cullingGroupItem = array[i];
				if (cullingGroupItem.m_DistanceSq > num)
				{
					break;
				}
				ref EnabledEffectData reference = ref m_EnabledData.ElementAt(cullingGroupItem.m_EnabledIndex);
				if ((reference.m_Flags & (EnabledEffectFlags.EnabledUpdated | EnabledEffectFlags.AudioDisabled)) != 0)
				{
					reference.m_Flags &= ~EnabledEffectFlags.AudioDisabled;
					m_SourceUpdateData.Add(new SourceInfo(reference.m_Owner, reference.m_EffectIndex));
				}
			}
			for (; i < array.Length; i++)
			{
				CullingGroupItem cullingGroupItem2 = array[i];
				m_EnabledData.ElementAt(cullingGroupItem2.m_EnabledIndex).m_Flags |= EnabledEffectFlags.AudioDisabled;
			}
			array.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CullingGroupData> __Game_Prefabs_CullingGroupData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AudioSpotData> __Game_Prefabs_AudioSpotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AudioEffectData> __Game_Prefabs_AudioEffectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<AudioSourceData> __Game_Prefabs_AudioSourceData_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Effect> __Game_Prefabs_Effect_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CullingGroupData_RO_ComponentLookup = state.GetComponentLookup<CullingGroupData>(isReadOnly: true);
			__Game_Prefabs_AudioSpotData_RO_ComponentLookup = state.GetComponentLookup<AudioSpotData>(isReadOnly: true);
			__Game_Prefabs_AudioEffectData_RO_ComponentLookup = state.GetComponentLookup<AudioEffectData>(isReadOnly: true);
			__Game_Prefabs_AudioSourceData_RO_BufferLookup = state.GetBufferLookup<AudioSourceData>(isReadOnly: true);
			__Game_Prefabs_Effect_RO_BufferLookup = state.GetBufferLookup<Effect>(isReadOnly: true);
		}
	}

	private AudioManager m_AudioManager;

	private EffectControlSystem m_EffectControlSystem;

	private EntityQuery m_CullingAudioSettingsQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_EffectControlSystem = base.World.GetOrCreateSystemManaged<EffectControlSystem>();
		m_CullingAudioSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<CullingAudioSettingsData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		Camera main = Camera.main;
		if (!(main == null))
		{
			int num = 4;
			NativeParallelQueue<CullingGroupItem> nativeParallelQueue = new NativeParallelQueue<CullingGroupItem>(num, Allocator.TempJob);
			JobHandle dependencies;
			NativeList<EnabledEffectData> enabledData = m_EffectControlSystem.GetEnabledData(readOnly: false, out dependencies);
			JobHandle deps;
			SourceUpdateData sourceUpdateData = m_AudioManager.GetSourceUpdateData(out deps);
			SFXCullingJob jobData = new SFXCullingJob
			{
				m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CullingGroupData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CullingGroupData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AudioSpotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AudioSpotData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AudioEffectDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AudioEffectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AudioSourceDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AudioSourceData_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabEffects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_Effect_RO_BufferLookup, ref base.CheckedStateRef),
				m_CameraPosition = main.transform.position,
				m_RandomSeed = RandomSeed.Next(),
				m_DeltaTime = UnityEngine.Time.deltaTime,
				m_EnabledData = enabledData,
				m_CullingGroupItems = nativeParallelQueue.AsWriter(),
				m_SourceUpdateData = sourceUpdateData
			};
			base.Dependency = jobData.Schedule(jobData.m_EnabledData, 16, JobHandle.CombineDependencies(dependencies, deps, base.Dependency));
			JobHandle jobHandle = base.Dependency;
			if (!m_CullingAudioSettingsQuery.IsEmptyIgnoreFilter)
			{
				CullingAudioSettingsData singleton = m_CullingAudioSettingsQuery.GetSingleton<CullingAudioSettingsData>();
				jobHandle = IJobParallelForExtensions.Schedule(new SFXGroupCullingJob
				{
					m_MaxAllowedAmount = singleton.m_PublicTransCullMaxAmount,
					m_MaxDistance = singleton.m_PublicTransCullMaxDistance,
					m_CullingGroupItems = nativeParallelQueue.AsReader(),
					m_EnabledData = enabledData,
					m_SourceUpdateData = sourceUpdateData
				}, num, 1, jobHandle);
			}
			nativeParallelQueue.Dispose(jobHandle);
			m_EffectControlSystem.AddEnabledDataWriter(jobHandle);
			m_AudioManager.AddSourceUpdateWriter(jobHandle);
		}
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
	public SFXCullingSystem()
	{
	}
}
