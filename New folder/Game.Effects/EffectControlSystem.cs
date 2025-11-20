using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Effects;

[CompilerGenerated]
public class EffectControlSystem : GameSystemBase, IPostDeserialize
{
	[BurstCompile]
	private struct EffectControlJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<CullingInfo> m_CullingInfoType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Tools.EditorContainer> m_EditorContainerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Object> m_ObjectType;

		[ReadOnly]
		public ComponentTypeHandle<Static> m_StaticType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Events.Event> m_EventType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<EnabledEffect> m_EffectOwnerType;

		[ReadOnly]
		public ComponentLookup<EffectData> m_PrefabEffectData;

		[ReadOnly]
		public ComponentLookup<TrafficLights> m_TrafficLightsData;

		[ReadOnly]
		public ComponentLookup<LightEffectData> m_PrefabLightEffectData;

		[ReadOnly]
		public ComponentLookup<AudioEffectData> m_PrefabAudioEffectData;

		[ReadOnly]
		public BufferLookup<Effect> m_PrefabEffects;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		[ReadOnly]
		public NativeList<EnabledEffectData> m_EnabledEffectData;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelQueue<EnabledAction>.Writer m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<EnabledEffect> bufferAccessor = chunk.GetBufferAccessor(ref m_EffectOwnerType);
			if (chunk.Has(ref m_DeletedType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity owner = nativeArray[i];
					DynamicBuffer<EnabledEffect> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						EnabledEffect enabledEffect = dynamicBuffer[j];
						m_ActionQueue.Enqueue(new EnabledAction
						{
							m_Owner = owner,
							m_EffectIndex = enabledEffect.m_EffectIndex,
							m_Flags = ActionFlags.Deleted
						});
					}
				}
				return;
			}
			bool flag = !chunk.Has(ref m_TempType) && (chunk.Has(ref m_StaticType) || (!chunk.Has(ref m_ObjectType) && !chunk.Has(ref m_EventType)));
			NativeArray<CullingInfo> nativeArray2 = chunk.GetNativeArray(ref m_CullingInfoType);
			NativeArray<Game.Tools.EditorContainer> nativeArray3 = chunk.GetNativeArray(ref m_EditorContainerType);
			NativeArray<Transform> transforms = default(NativeArray<Transform>);
			NativeArray<Curve> curves = default(NativeArray<Curve>);
			NativeArray<PrefabRef> nativeArray4 = default(NativeArray<PrefabRef>);
			if (flag)
			{
				transforms = chunk.GetNativeArray(ref m_TransformType);
				curves = chunk.GetNativeArray(ref m_CurveType);
			}
			if (nativeArray3.Length == 0)
			{
				nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			}
			for (int k = 0; k < nativeArray.Length; k++)
			{
				Entity entity = nativeArray[k];
				DynamicBuffer<EnabledEffect> dynamicBuffer2 = bufferAccessor[k];
				if (flag && m_TrafficLightsData.TryGetComponent(entity, out var componentData) && (componentData.m_Flags & TrafficLightFlags.MoveableBridge) != 0)
				{
					flag = false;
				}
				if (CollectionUtils.TryGet(nativeArray3, k, out var value))
				{
					for (int l = 0; l < dynamicBuffer2.Length; l++)
					{
						EnabledEffect enabledEffect2 = dynamicBuffer2[l];
						EnabledEffectData enabledEffectData = m_EnabledEffectData[enabledEffect2.m_EnabledIndex];
						if (value.m_Prefab != enabledEffectData.m_Prefab)
						{
							m_ActionQueue.Enqueue(new EnabledAction
							{
								m_Owner = entity,
								m_EffectIndex = enabledEffect2.m_EffectIndex,
								m_Flags = ActionFlags.WrongPrefab
							});
						}
					}
					if (!m_PrefabEffectData.TryGetComponent(value.m_Prefab, out var componentData2))
					{
						continue;
					}
					ActionFlags flags = (flag ? (ActionFlags.CheckEnabled | ActionFlags.IsStatic | ActionFlags.OwnerUpdated) : (ActionFlags.CheckEnabled | ActionFlags.OwnerUpdated));
					if (componentData2.m_OwnerCulling)
					{
						if (!IsNearCamera(nativeArray2, k))
						{
							continue;
						}
					}
					else if (flag)
					{
						Effect effect = new Effect
						{
							m_Effect = value.m_Prefab
						};
						if (!IsNearCamera(transforms, curves, k, effect))
						{
							flags = (ActionFlags)0;
						}
					}
					m_ActionQueue.Enqueue(new EnabledAction
					{
						m_Owner = entity,
						m_EffectIndex = 0,
						m_Flags = flags
					});
					continue;
				}
				PrefabRef prefabRef = nativeArray4[k];
				m_PrefabEffects.TryGetBuffer(prefabRef.m_Prefab, out var bufferData);
				for (int m = 0; m < dynamicBuffer2.Length; m++)
				{
					EnabledEffect enabledEffect3 = dynamicBuffer2[m];
					EnabledEffectData enabledEffectData2 = m_EnabledEffectData[enabledEffect3.m_EnabledIndex];
					if (!bufferData.IsCreated || bufferData.Length <= enabledEffect3.m_EffectIndex || bufferData[enabledEffect3.m_EffectIndex].m_Effect != enabledEffectData2.m_Prefab)
					{
						m_ActionQueue.Enqueue(new EnabledAction
						{
							m_Owner = entity,
							m_EffectIndex = enabledEffect3.m_EffectIndex,
							m_Flags = ActionFlags.WrongPrefab
						});
					}
				}
				if (!bufferData.IsCreated)
				{
					continue;
				}
				bool flag2 = IsNearCamera(nativeArray2, k);
				for (int n = 0; n < bufferData.Length; n++)
				{
					Effect effect2 = bufferData[n];
					if (!m_PrefabEffectData.TryGetComponent(effect2.m_Effect, out var componentData3))
					{
						continue;
					}
					ActionFlags flags2 = (flag ? (ActionFlags.CheckEnabled | ActionFlags.IsStatic | ActionFlags.OwnerUpdated) : (ActionFlags.CheckEnabled | ActionFlags.OwnerUpdated));
					if (componentData3.m_OwnerCulling)
					{
						bool flag3 = m_PrefabAudioEffectData.HasComponent(effect2.m_Effect);
						if (!flag2 && !flag3)
						{
							continue;
						}
					}
					if (flag && !IsNearCamera(transforms, curves, k, effect2))
					{
						flags2 = (ActionFlags)0;
					}
					m_ActionQueue.Enqueue(new EnabledAction
					{
						m_Owner = entity,
						m_EffectIndex = n,
						m_Flags = flags2
					});
				}
			}
		}

		private bool IsNearCamera(NativeArray<Transform> transforms, NativeArray<Curve> curves, int index, Effect effect)
		{
			QuadTreeBoundsXZ bounds = SearchSystem.GetBounds(transforms, curves, index, effect, ref m_PrefabLightEffectData, ref m_PrefabAudioEffectData);
			float num = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
			return RenderingUtils.CalculateLod(num * num, m_LodParameters) >= bounds.m_MinLod;
		}

		private bool IsNearCamera(NativeArray<CullingInfo> cullingInfos, int index)
		{
			if (CollectionUtils.TryGet(cullingInfos, index, out var value) && value.m_CullingIndex != 0)
			{
				return (m_CullingData[value.m_CullingIndex].m_Flags & PreCullingFlags.NearCamera) != 0;
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct EffectCullingJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<Static> m_StaticData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<EffectData> m_PrefabEffectData;

		[ReadOnly]
		public BufferLookup<EnabledEffect> m_EffectOwners;

		[ReadOnly]
		public BufferLookup<Effect> m_PrefabEffects;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		[ReadOnly]
		public NativeList<EnabledEffectData> m_EnabledEffectData;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelQueue<EnabledAction>.Writer m_ActionQueue;

		public void Execute(int index)
		{
			PreCullingData preCullingData = m_CullingData[index];
			if ((preCullingData.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Deleted | PreCullingFlags.Created | PreCullingFlags.EffectInstances)) != (PreCullingFlags.NearCameraUpdated | PreCullingFlags.EffectInstances))
			{
				return;
			}
			if ((preCullingData.m_Flags & PreCullingFlags.NearCamera) != 0)
			{
				PrefabRef prefabRef = m_PrefabRefData[preCullingData.m_Entity];
				bool flag = ((preCullingData.m_Flags & PreCullingFlags.Temp) == 0 && (preCullingData.m_Flags & PreCullingFlags.Object) == 0) || m_StaticData.HasComponent(preCullingData.m_Entity);
				Game.Tools.EditorContainer componentData2;
				EffectData componentData3;
				if (m_PrefabEffects.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						Effect effect = bufferData[i];
						if (m_PrefabEffectData.TryGetComponent(effect.m_Effect, out var componentData) && componentData.m_OwnerCulling)
						{
							m_ActionQueue.Enqueue(new EnabledAction
							{
								m_Owner = preCullingData.m_Entity,
								m_EffectIndex = i,
								m_Flags = ((!flag) ? ActionFlags.CheckEnabled : (ActionFlags.CheckEnabled | ActionFlags.IsStatic))
							});
						}
					}
				}
				else if (m_EditorContainerData.TryGetComponent(preCullingData.m_Entity, out componentData2) && m_PrefabEffectData.TryGetComponent(componentData2.m_Prefab, out componentData3) && componentData3.m_OwnerCulling)
				{
					m_ActionQueue.Enqueue(new EnabledAction
					{
						m_Owner = preCullingData.m_Entity,
						m_EffectIndex = 0,
						m_Flags = ((!flag) ? ActionFlags.CheckEnabled : (ActionFlags.CheckEnabled | ActionFlags.IsStatic))
					});
				}
				return;
			}
			DynamicBuffer<EnabledEffect> dynamicBuffer = m_EffectOwners[preCullingData.m_Entity];
			for (int j = 0; j < dynamicBuffer.Length; j++)
			{
				EnabledEffect enabledEffect = dynamicBuffer[j];
				EnabledEffectData enabledEffectData = m_EnabledEffectData[enabledEffect.m_EnabledIndex];
				if (m_PrefabEffectData[enabledEffectData.m_Prefab].m_OwnerCulling)
				{
					m_ActionQueue.Enqueue(new EnabledAction
					{
						m_Owner = preCullingData.m_Entity,
						m_EffectIndex = enabledEffect.m_EffectIndex,
						m_Flags = (ActionFlags)0
					});
				}
			}
		}
	}

	[BurstCompile]
	private struct TreeCullingJob1 : IJob
	{
		[ReadOnly]
		public NativeQuadTree<SourceInfo, QuadTreeBoundsXZ> m_EffectSearchTree;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float4 m_PrevLodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_PrevCameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public float3 m_PrevCameraDirection;

		[NativeDisableParallelForRestriction]
		public NativeArray<int> m_NodeBuffer;

		[NativeDisableParallelForRestriction]
		public NativeArray<int> m_SubDataBuffer;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelQueue<EnabledAction>.Writer m_ActionQueue;

		public void Execute()
		{
			TreeCullingIterator iterator = new TreeCullingIterator
			{
				m_LodParameters = m_LodParameters,
				m_PrevLodParameters = m_PrevLodParameters,
				m_CameraPosition = m_CameraPosition,
				m_PrevCameraPosition = m_PrevCameraPosition,
				m_CameraDirection = m_CameraDirection,
				m_PrevCameraDirection = m_PrevCameraDirection,
				m_ActionQueue = m_ActionQueue
			};
			m_EffectSearchTree.Iterate(ref iterator, 3, m_NodeBuffer, m_SubDataBuffer);
		}
	}

	[BurstCompile]
	private struct TreeCullingJob2 : IJobParallelFor
	{
		[ReadOnly]
		public NativeQuadTree<SourceInfo, QuadTreeBoundsXZ> m_EffectSearchTree;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float4 m_PrevLodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_PrevCameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public float3 m_PrevCameraDirection;

		[ReadOnly]
		public NativeArray<int> m_NodeBuffer;

		[ReadOnly]
		public NativeArray<int> m_SubDataBuffer;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelQueue<EnabledAction>.Writer m_ActionQueue;

		public void Execute(int index)
		{
			TreeCullingIterator iterator = new TreeCullingIterator
			{
				m_LodParameters = m_LodParameters,
				m_PrevLodParameters = m_PrevLodParameters,
				m_CameraPosition = m_CameraPosition,
				m_PrevCameraPosition = m_PrevCameraPosition,
				m_CameraDirection = m_CameraDirection,
				m_PrevCameraDirection = m_PrevCameraDirection,
				m_ActionQueue = m_ActionQueue
			};
			m_EffectSearchTree.Iterate(ref iterator, m_SubDataBuffer[index], m_NodeBuffer[index]);
		}
	}

	private struct TreeCullingIterator : INativeQuadTreeIteratorWithSubData<SourceInfo, QuadTreeBoundsXZ, int>, IUnsafeQuadTreeIteratorWithSubData<SourceInfo, QuadTreeBoundsXZ, int>
	{
		public float4 m_LodParameters;

		public float3 m_CameraPosition;

		public float3 m_CameraDirection;

		public float3 m_PrevCameraPosition;

		public float4 m_PrevLodParameters;

		public float3 m_PrevCameraDirection;

		public NativeParallelQueue<EnabledAction>.Writer m_ActionQueue;

		public bool Intersect(QuadTreeBoundsXZ bounds, ref int subData)
		{
			switch (subData)
			{
			case 1:
			{
				float num13 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				int num14 = RenderingUtils.CalculateLod(num13 * num13, m_LodParameters);
				if (num14 < bounds.m_MinLod)
				{
					return false;
				}
				float num15 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
				int num16 = RenderingUtils.CalculateLod(num15 * num15, m_PrevLodParameters);
				if (num16 < bounds.m_MaxLod)
				{
					return num14 > num16;
				}
				return false;
			}
			case 2:
			{
				float num9 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
				int num10 = RenderingUtils.CalculateLod(num9 * num9, m_PrevLodParameters);
				if (num10 < bounds.m_MinLod)
				{
					return false;
				}
				float num11 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				int num12 = RenderingUtils.CalculateLod(num11 * num11, m_LodParameters);
				if (num12 < bounds.m_MaxLod)
				{
					return num10 > num12;
				}
				return false;
			}
			default:
			{
				float num = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				float num2 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
				int num3 = RenderingUtils.CalculateLod(num * num, m_LodParameters);
				int num4 = RenderingUtils.CalculateLod(num2 * num2, m_PrevLodParameters);
				subData = 0;
				if (num3 >= bounds.m_MinLod)
				{
					float num5 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
					int num6 = RenderingUtils.CalculateLod(num5 * num5, m_PrevLodParameters);
					subData |= math.select(0, 1, num6 < bounds.m_MaxLod && num3 > num6);
				}
				if (num4 >= bounds.m_MinLod)
				{
					float num7 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
					int num8 = RenderingUtils.CalculateLod(num7 * num7, m_LodParameters);
					subData |= math.select(0, 2, num8 < bounds.m_MaxLod && num4 > num8);
				}
				return subData != 0;
			}
			}
		}

		public void Iterate(QuadTreeBoundsXZ bounds, int subData, SourceInfo sourceInfo)
		{
			switch (subData)
			{
			case 1:
			{
				float num = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				if (RenderingUtils.CalculateLod(num * num, m_LodParameters) >= bounds.m_MinLod)
				{
					float num2 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
					if (RenderingUtils.CalculateLod(num2 * num2, m_PrevLodParameters) < bounds.m_MaxLod)
					{
						m_ActionQueue.Enqueue(new EnabledAction
						{
							m_Owner = sourceInfo.m_Entity,
							m_EffectIndex = sourceInfo.m_EffectIndex,
							m_Flags = (ActionFlags.SkipEnabled | ActionFlags.IsStatic)
						});
					}
				}
				return;
			}
			case 2:
			{
				float num3 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
				if (RenderingUtils.CalculateLod(num3 * num3, m_PrevLodParameters) >= bounds.m_MinLod)
				{
					float num4 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
					if (RenderingUtils.CalculateLod(num4 * num4, m_LodParameters) < bounds.m_MaxLod)
					{
						m_ActionQueue.Enqueue(new EnabledAction
						{
							m_Owner = sourceInfo.m_Entity,
							m_EffectIndex = sourceInfo.m_EffectIndex,
							m_Flags = (ActionFlags)0
						});
					}
				}
				return;
			}
			}
			float num5 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
			float num6 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
			int num7 = RenderingUtils.CalculateLod(num5 * num5, m_LodParameters);
			int num8 = RenderingUtils.CalculateLod(num6 * num6, m_PrevLodParameters);
			bool flag = num7 >= bounds.m_MinLod;
			bool flag2 = num8 >= bounds.m_MaxLod;
			if (flag != flag2)
			{
				m_ActionQueue.Enqueue(new EnabledAction
				{
					m_Owner = sourceInfo.m_Entity,
					m_EffectIndex = sourceInfo.m_EffectIndex,
					m_Flags = (flag ? (ActionFlags.SkipEnabled | ActionFlags.IsStatic) : ((ActionFlags)0))
				});
			}
		}
	}

	[Flags]
	public enum ActionFlags : byte
	{
		CheckEnabled = 1,
		Deleted = 2,
		SkipEnabled = 4,
		IsStatic = 8,
		OwnerUpdated = 0x10,
		WrongPrefab = 0x20
	}

	private struct EnabledAction
	{
		public Entity m_Owner;

		public int m_EffectIndex;

		public ActionFlags m_Flags;

		public override int GetHashCode()
		{
			return m_Owner.GetHashCode();
		}
	}

	private struct OverflowAction
	{
		public Entity m_Owner;

		public Entity m_Prefab;

		public int m_DataIndex;

		public int m_EffectIndex;

		public EnabledEffectFlags m_Flags;
	}

	[BurstCompile]
	private struct EnabledActionJob : IJobParallelFor
	{
		[ReadOnly]
		public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<VFXData> m_VFXDatas;

		[ReadOnly]
		public ComponentLookup<RandomTransformData> m_RandomTransformDatas;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryDatas;

		[ReadOnly]
		public BufferLookup<AudioSourceData> m_AudioSourceDatas;

		[ReadOnly]
		public BufferLookup<Effect> m_PrefabEffects;

		[NativeDisableParallelForRestriction]
		public BufferLookup<EnabledEffect> m_EffectOwners;

		[ReadOnly]
		public NativeParallelQueue<EnabledAction>.Reader m_CullingActions;

		public NativeQueue<OverflowAction>.ParallelWriter m_OverflowActions;

		public NativeQueue<VFXUpdateInfo>.ParallelWriter m_VFXUpdateQueue;

		[NativeDisableParallelForRestriction]
		public NativeList<EnabledEffectData> m_EnabledData;

		[NativeDisableParallelForRestriction]
		public NativeReference<int> m_EnabledDataIndex;

		public EffectControlData m_EffectControlData;

		public void Execute(int index)
		{
			NativeParallelQueue<EnabledAction>.Enumerator enumerator = m_CullingActions.GetEnumerator(index);
			while (enumerator.MoveNext())
			{
				EnabledAction current = enumerator.Current;
				if ((current.m_Flags & (ActionFlags.CheckEnabled | ActionFlags.SkipEnabled)) != 0)
				{
					PrefabRef prefabRef = m_EffectControlData.m_Prefabs[current.m_Owner];
					Entity entity = Entity.Null;
					bool isAnimated = false;
					bool isEditorContainer = false;
					Game.Tools.EditorContainer componentData;
					if (m_PrefabEffects.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
					{
						Effect effect = bufferData[current.m_EffectIndex];
						entity = effect.m_Effect;
						isAnimated = effect.m_BoneIndex.x >= 0 || effect.m_AnimationIndex >= 0;
					}
					else if (m_EditorContainerData.TryGetComponent(current.m_Owner, out componentData))
					{
						entity = componentData.m_Prefab;
						isAnimated = componentData.m_GroupIndex >= 0;
						isEditorContainer = true;
					}
					bool checkEnabled = (current.m_Flags & ActionFlags.CheckEnabled) != 0;
					if (m_EffectControlData.ShouldBeEnabled(current.m_Owner, entity, checkEnabled, isEditorContainer))
					{
						Enable(current, entity, isAnimated, isEditorContainer);
						continue;
					}
				}
				Disable(current);
			}
			enumerator.Dispose();
		}

		private unsafe void Enable(EnabledAction enabledAction, Entity effectPrefab, bool isAnimated, bool isEditorContainer)
		{
			DynamicBuffer<EnabledEffect> dynamicBuffer = m_EffectOwners[enabledAction.m_Owner];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				ref EnabledEffect reference = ref dynamicBuffer.ElementAt(i);
				if (reference.m_EffectIndex != enabledAction.m_EffectIndex)
				{
					continue;
				}
				if (reference.m_EnabledIndex >= m_EnabledData.Length)
				{
					return;
				}
				ref EnabledEffectData reference2 = ref UnsafeUtility.ArrayElementAsRef<EnabledEffectData>(m_EnabledData.GetUnsafePtr(), reference.m_EnabledIndex);
				if (reference2.m_Prefab != effectPrefab)
				{
					continue;
				}
				if ((enabledAction.m_Flags & ActionFlags.OwnerUpdated) != 0)
				{
					if ((enabledAction.m_Flags & ActionFlags.IsStatic) == 0 || isAnimated || m_InterpolatedTransformData.HasComponent(enabledAction.m_Owner))
					{
						reference2.m_Flags |= EnabledEffectFlags.DynamicTransform;
					}
					else
					{
						reference2.m_Flags &= ~EnabledEffectFlags.DynamicTransform;
					}
					if (OwnerCollapsed(enabledAction.m_Owner))
					{
						reference2.m_Flags |= EnabledEffectFlags.OwnerCollapsed;
					}
					else
					{
						reference2.m_Flags &= ~EnabledEffectFlags.OwnerCollapsed;
					}
				}
				if ((reference2.m_Flags & EnabledEffectFlags.IsEnabled) == 0)
				{
					reference2.m_Flags |= EnabledEffectFlags.IsEnabled | EnabledEffectFlags.EnabledUpdated;
					if ((reference2.m_Flags & EnabledEffectFlags.IsVFX) != 0)
					{
						m_VFXUpdateQueue.Enqueue(new VFXUpdateInfo
						{
							m_Type = VFXUpdateType.Add,
							m_EnabledIndex = reference.m_EnabledIndex
						});
					}
				}
				else if ((enabledAction.m_Flags & ActionFlags.OwnerUpdated) != 0)
				{
					reference2.m_Flags |= EnabledEffectFlags.OwnerUpdated;
				}
				return;
			}
			int num = Interlocked.Increment(ref UnsafeUtility.AsRef<int>(m_EnabledDataIndex.GetUnsafePtr())) - 1;
			dynamicBuffer.Add(new EnabledEffect
			{
				m_EffectIndex = enabledAction.m_EffectIndex,
				m_EnabledIndex = num
			});
			EnabledEffectFlags enabledEffectFlags = EnabledEffectFlags.IsEnabled | EnabledEffectFlags.EnabledUpdated;
			if (isEditorContainer)
			{
				enabledEffectFlags |= EnabledEffectFlags.EditorContainer;
			}
			if (m_EffectControlData.m_LightEffectDatas.HasComponent(effectPrefab))
			{
				enabledEffectFlags |= EnabledEffectFlags.IsLight;
			}
			if (m_VFXDatas.HasComponent(effectPrefab))
			{
				enabledEffectFlags |= EnabledEffectFlags.IsVFX;
				m_VFXUpdateQueue.Enqueue(new VFXUpdateInfo
				{
					m_Type = VFXUpdateType.Add,
					m_EnabledIndex = num
				});
			}
			if (m_AudioSourceDatas.HasBuffer(effectPrefab))
			{
				enabledEffectFlags |= EnabledEffectFlags.IsAudio;
			}
			if (m_RandomTransformDatas.HasComponent(effectPrefab))
			{
				enabledEffectFlags |= EnabledEffectFlags.RandomTransform;
			}
			if (m_EffectControlData.m_Temps.HasComponent(enabledAction.m_Owner))
			{
				enabledEffectFlags |= EnabledEffectFlags.TempOwner;
			}
			if ((enabledAction.m_Flags & ActionFlags.IsStatic) == 0 || isAnimated || m_InterpolatedTransformData.HasComponent(enabledAction.m_Owner))
			{
				enabledEffectFlags |= EnabledEffectFlags.DynamicTransform;
			}
			if (OwnerCollapsed(enabledAction.m_Owner))
			{
				enabledEffectFlags |= EnabledEffectFlags.OwnerCollapsed;
			}
			if (num >= m_EnabledData.Capacity)
			{
				m_OverflowActions.Enqueue(new OverflowAction
				{
					m_Owner = enabledAction.m_Owner,
					m_Prefab = effectPrefab,
					m_DataIndex = num,
					m_EffectIndex = enabledAction.m_EffectIndex,
					m_Flags = enabledEffectFlags
				});
			}
			else
			{
				ref EnabledEffectData reference3 = ref UnsafeUtility.ArrayElementAsRef<EnabledEffectData>(m_EnabledData.GetUnsafePtr(), num);
				reference3 = default(EnabledEffectData);
				reference3.m_Owner = enabledAction.m_Owner;
				reference3.m_Prefab = effectPrefab;
				reference3.m_EffectIndex = enabledAction.m_EffectIndex;
				reference3.m_Flags = enabledEffectFlags;
			}
		}

		private bool OwnerCollapsed(Entity owner)
		{
			if (m_DestroyedData.HasComponent(owner) && m_PrefabRefs.TryGetComponent(owner, out var componentData) && m_ObjectGeometryDatas.TryGetComponent(componentData.m_Prefab, out var componentData2))
			{
				return (componentData2.m_Flags & (Game.Objects.GeometryFlags.Physical | Game.Objects.GeometryFlags.HasLot)) == (Game.Objects.GeometryFlags.Physical | Game.Objects.GeometryFlags.HasLot);
			}
			return false;
		}

		private unsafe void Disable(EnabledAction enabledAction)
		{
			DynamicBuffer<EnabledEffect> dynamicBuffer = m_EffectOwners[enabledAction.m_Owner];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				ref EnabledEffect reference = ref dynamicBuffer.ElementAt(i);
				if (reference.m_EffectIndex != enabledAction.m_EffectIndex)
				{
					continue;
				}
				if (reference.m_EnabledIndex >= m_EnabledData.Length)
				{
					break;
				}
				ref EnabledEffectData reference2 = ref UnsafeUtility.ArrayElementAsRef<EnabledEffectData>(m_EnabledData.GetUnsafePtr(), reference.m_EnabledIndex);
				if ((reference2.m_Flags & EnabledEffectFlags.IsEnabled) != 0)
				{
					reference2.m_Flags &= ~EnabledEffectFlags.IsEnabled;
					reference2.m_Flags |= EnabledEffectFlags.EnabledUpdated;
					if ((reference2.m_Flags & EnabledEffectFlags.IsVFX) != 0)
					{
						m_VFXUpdateQueue.Enqueue(new VFXUpdateInfo
						{
							m_Type = VFXUpdateType.Remove,
							m_EnabledIndex = reference.m_EnabledIndex
						});
					}
				}
				if ((enabledAction.m_Flags & ActionFlags.Deleted) != 0)
				{
					reference2.m_Flags |= EnabledEffectFlags.Deleted;
				}
				if ((enabledAction.m_Flags & ActionFlags.WrongPrefab) != 0)
				{
					reference2.m_Flags |= EnabledEffectFlags.WrongPrefab;
				}
				break;
			}
		}
	}

	[BurstCompile]
	private struct ResizeEnabledDataJob : IJob
	{
		[ReadOnly]
		public NativeReference<int> m_EnabledDataIndex;

		public NativeList<EnabledEffectData> m_EnabledData;

		public NativeQueue<OverflowAction> m_OverflowActions;

		public void Execute()
		{
			m_EnabledData.Resize(math.min(m_EnabledDataIndex.Value, m_EnabledData.Capacity), NativeArrayOptions.UninitializedMemory);
			m_EnabledData.Resize(m_EnabledDataIndex.Value, NativeArrayOptions.UninitializedMemory);
			OverflowAction item;
			while (m_OverflowActions.TryDequeue(out item))
			{
				ref EnabledEffectData reference = ref m_EnabledData.ElementAt(item.m_DataIndex);
				reference = default(EnabledEffectData);
				reference.m_Owner = item.m_Owner;
				reference.m_Prefab = item.m_Prefab;
				reference.m_EffectIndex = item.m_EffectIndex;
				reference.m_Flags = item.m_Flags;
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Object> __Game_Objects_Object_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Static> __Game_Objects_Static_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Events.Event> __Game_Events_Event_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<EnabledEffect> __Game_Effects_EnabledEffect_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrafficLights> __Game_Net_TrafficLights_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LightEffectData> __Game_Prefabs_LightEffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AudioEffectData> __Game_Prefabs_AudioEffectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Effect> __Game_Prefabs_Effect_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Static> __Game_Objects_Static_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<EnabledEffect> __Game_Effects_EnabledEffect_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VFXData> __Game_Prefabs_VFXData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RandomTransformData> __Game_Prefabs_RandomTransformData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<AudioSourceData> __Game_Prefabs_AudioSourceData_RO_BufferLookup;

		public BufferLookup<EnabledEffect> __Game_Effects_EnabledEffect_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Rendering_CullingInfo_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CullingInfo>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Tools.EditorContainer>(isReadOnly: true);
			__Game_Objects_Object_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Object>(isReadOnly: true);
			__Game_Objects_Static_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Static>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Events_Event_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Events.Event>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Effects_EnabledEffect_RO_BufferTypeHandle = state.GetBufferTypeHandle<EnabledEffect>(isReadOnly: true);
			__Game_Prefabs_EffectData_RO_ComponentLookup = state.GetComponentLookup<EffectData>(isReadOnly: true);
			__Game_Net_TrafficLights_RO_ComponentLookup = state.GetComponentLookup<TrafficLights>(isReadOnly: true);
			__Game_Prefabs_LightEffectData_RO_ComponentLookup = state.GetComponentLookup<LightEffectData>(isReadOnly: true);
			__Game_Prefabs_AudioEffectData_RO_ComponentLookup = state.GetComponentLookup<AudioEffectData>(isReadOnly: true);
			__Game_Prefabs_Effect_RO_BufferLookup = state.GetBufferLookup<Effect>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true);
			__Game_Objects_Static_RO_ComponentLookup = state.GetComponentLookup<Static>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Effects_EnabledEffect_RO_BufferLookup = state.GetBufferLookup<EnabledEffect>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Prefabs_VFXData_RO_ComponentLookup = state.GetComponentLookup<VFXData>(isReadOnly: true);
			__Game_Prefabs_RandomTransformData_RO_ComponentLookup = state.GetComponentLookup<RandomTransformData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_AudioSourceData_RO_BufferLookup = state.GetBufferLookup<AudioSourceData>(isReadOnly: true);
			__Game_Effects_EnabledEffect_RW_BufferLookup = state.GetBufferLookup<EnabledEffect>();
		}
	}

	private VFXSystem m_VFXSystem;

	private SearchSystem m_SearchSystem;

	private EffectFlagSystem m_EffectFlagSystem;

	private SimulationSystem m_SimulationSystem;

	private PreCullingSystem m_PreCullingSystem;

	private ToolSystem m_ToolSystem;

	private RenderingSystem m_RenderingSystem;

	private BatchDataSystem m_BatchDataSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private EffectControlData m_EffectControlData;

	private NativeList<EnabledEffectData> m_EnabledData;

	private EntityQuery m_UpdatedEffectsQuery;

	private EntityQuery m_AllEffectsQuery;

	private JobHandle m_EnabledWriteDependencies;

	private JobHandle m_EnabledReadDependencies;

	private float3 m_PrevCameraPosition;

	private float3 m_PrevCameraDirection;

	private float4 m_PrevLodParameters;

	private bool m_Loaded;

	private bool m_ResetPrevious;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_VFXSystem = base.World.GetOrCreateSystemManaged<VFXSystem>();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_EffectFlagSystem = base.World.GetOrCreateSystemManaged<EffectFlagSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_BatchDataSystem = base.World.GetOrCreateSystemManaged<BatchDataSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_EffectControlData = new EffectControlData(this);
		m_EnabledData = new NativeList<EnabledEffectData>(Allocator.Persistent);
		m_UpdatedEffectsQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<EnabledEffect>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<EffectsUpdated>(),
				ComponentType.ReadOnly<BatchesUpdated>()
			}
		});
		m_AllEffectsQuery = GetEntityQuery(ComponentType.ReadOnly<EnabledEffect>());
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_EnabledData.Dispose();
		base.OnDestroy();
	}

	public void PostDeserialize(Context context)
	{
		m_EnabledWriteDependencies.Complete();
		m_EnabledReadDependencies.Complete();
		m_EnabledData.Clear();
		m_ResetPrevious = true;
		m_Loaded = true;
	}

	public NativeList<EnabledEffectData> GetEnabledData(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_EnabledWriteDependencies : JobHandle.CombineDependencies(m_EnabledWriteDependencies, m_EnabledReadDependencies));
		return m_EnabledData;
	}

	public void AddEnabledDataReader(JobHandle dependencies)
	{
		m_EnabledReadDependencies = JobHandle.CombineDependencies(m_EnabledReadDependencies, dependencies);
	}

	public void AddEnabledDataWriter(JobHandle dependencies)
	{
		m_EnabledWriteDependencies = dependencies;
	}

	public void GetLodParameters(out float4 lodParameters, out float3 cameraPosition, out float3 cameraDirection)
	{
		lodParameters = m_PrevLodParameters;
		cameraPosition = m_PrevCameraPosition;
		cameraDirection = m_PrevCameraDirection;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool loaded = GetLoaded();
		EntityQuery query = (loaded ? m_AllEffectsQuery : m_UpdatedEffectsQuery);
		m_EffectControlData.Update(this, m_EffectFlagSystem.GetData(), m_SimulationSystem.frameIndex, m_ToolSystem.selected);
		m_EnabledWriteDependencies.Complete();
		m_EnabledReadDependencies.Complete();
		int length = m_EnabledData.Length;
		NativeParallelQueue<EnabledAction> nativeParallelQueue = new NativeParallelQueue<EnabledAction>(Allocator.TempJob);
		NativeQueue<OverflowAction> overflowActions = new NativeQueue<OverflowAction>(Allocator.TempJob);
		NativeReference<int> enabledDataIndex = new NativeReference<int>(length, Allocator.TempJob);
		float3 @float = m_PrevCameraPosition;
		float3 float2 = m_PrevCameraDirection;
		float4 float3 = m_PrevLodParameters;
		if (m_CameraUpdateSystem.TryGetLODParameters(out var lodParameters))
		{
			@float = lodParameters.cameraPosition;
			IGameCameraController activeCameraController = m_CameraUpdateSystem.activeCameraController;
			float3 = RenderingUtils.CalculateLodParameters(m_BatchDataSystem.GetLevelOfDetail(m_RenderingSystem.frameLod, activeCameraController), lodParameters);
			float2 = m_CameraUpdateSystem.activeViewer.forward;
		}
		if (m_ResetPrevious)
		{
			m_PrevCameraPosition = @float;
			m_PrevCameraDirection = float2;
			m_PrevLodParameters = float3;
		}
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new EffectControlJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EditorContainerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Object_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StaticType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Static_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Event_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EffectOwnerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Effects_EnabledEffect_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrafficLightsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrafficLights_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLightEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LightEffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAudioEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AudioEffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabEffects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_Effect_RO_BufferLookup, ref base.CheckedStateRef),
			m_LodParameters = float3,
			m_CameraPosition = @float,
			m_CameraDirection = float2,
			m_CullingData = m_PreCullingSystem.GetCullingData(readOnly: true, out dependencies),
			m_EnabledEffectData = m_EnabledData,
			m_ActionQueue = nativeParallelQueue.AsWriter()
		}, query, JobHandle.CombineDependencies(dependencies, base.Dependency));
		if (!loaded)
		{
			JobHandle dependencies2;
			NativeQuadTree<SourceInfo, QuadTreeBoundsXZ> searchTree = m_SearchSystem.GetSearchTree(readOnly: true, out dependencies2);
			NativeArray<int> nodeBuffer = new NativeArray<int>(256, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<int> subDataBuffer = new NativeArray<int>(256, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			TreeCullingJob1 jobData = new TreeCullingJob1
			{
				m_EffectSearchTree = searchTree,
				m_LodParameters = float3,
				m_PrevLodParameters = m_PrevLodParameters,
				m_CameraPosition = @float,
				m_PrevCameraPosition = m_PrevCameraPosition,
				m_CameraDirection = float2,
				m_PrevCameraDirection = m_PrevCameraDirection,
				m_NodeBuffer = nodeBuffer,
				m_SubDataBuffer = subDataBuffer,
				m_ActionQueue = nativeParallelQueue.AsWriter()
			};
			TreeCullingJob2 jobData2 = new TreeCullingJob2
			{
				m_EffectSearchTree = searchTree,
				m_LodParameters = float3,
				m_PrevLodParameters = m_PrevLodParameters,
				m_CameraPosition = @float,
				m_PrevCameraPosition = m_PrevCameraPosition,
				m_CameraDirection = float2,
				m_PrevCameraDirection = m_PrevCameraDirection,
				m_NodeBuffer = nodeBuffer,
				m_SubDataBuffer = subDataBuffer,
				m_ActionQueue = nativeParallelQueue.AsWriter()
			};
			JobHandle dependencies3;
			EffectCullingJob jobData3 = new EffectCullingJob
			{
				m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StaticData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Static_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EffectOwners = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Effects_EnabledEffect_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabEffects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_Effect_RO_BufferLookup, ref base.CheckedStateRef),
				m_CullingData = m_PreCullingSystem.GetUpdatedData(readOnly: true, out dependencies3),
				m_EnabledEffectData = m_EnabledData,
				m_ActionQueue = nativeParallelQueue.AsWriter()
			};
			JobHandle jobHandle2 = IJobParallelForExtensions.Schedule(dependsOn: IJobExtensions.Schedule(jobData, dependencies2), jobData: jobData2, arrayLength: nodeBuffer.Length, innerloopBatchCount: 1);
			JobHandle job = jobData3.Schedule(jobData3.m_CullingData, 16, JobHandle.CombineDependencies(base.Dependency, dependencies3));
			nodeBuffer.Dispose(jobHandle2);
			subDataBuffer.Dispose(jobHandle2);
			m_SearchSystem.AddSearchTreeReader(jobHandle2);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2, job);
		}
		EnabledActionJob jobData4 = new EnabledActionJob
		{
			m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VFXDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_VFXData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomTransformDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RandomTransformData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectGeometryDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AudioSourceDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AudioSourceData_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabEffects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_Effect_RO_BufferLookup, ref base.CheckedStateRef),
			m_EffectOwners = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Effects_EnabledEffect_RW_BufferLookup, ref base.CheckedStateRef),
			m_CullingActions = nativeParallelQueue.AsReader(),
			m_OverflowActions = overflowActions.AsParallelWriter(),
			m_VFXUpdateQueue = m_VFXSystem.GetSourceUpdateData().AsParallelWriter(),
			m_EnabledData = m_EnabledData,
			m_EnabledDataIndex = enabledDataIndex,
			m_EffectControlData = m_EffectControlData
		};
		ResizeEnabledDataJob jobData5 = new ResizeEnabledDataJob
		{
			m_EnabledDataIndex = enabledDataIndex,
			m_EnabledData = m_EnabledData,
			m_OverflowActions = overflowActions
		};
		JobHandle jobHandle3 = IJobParallelForExtensions.Schedule(jobData4, nativeParallelQueue.HashRange, 1, jobHandle);
		JobHandle inputDeps = (m_EnabledWriteDependencies = IJobExtensions.Schedule(jobData5, jobHandle3));
		nativeParallelQueue.Dispose(jobHandle3);
		overflowActions.Dispose(inputDeps);
		enabledDataIndex.Dispose(inputDeps);
		m_VFXSystem.AddSourceUpdateWriter(jobHandle3);
		m_PreCullingSystem.AddCullingDataReader(jobHandle);
		m_PrevCameraPosition = @float;
		m_PrevCameraDirection = float2;
		m_PrevLodParameters = float3;
		m_ResetPrevious = false;
		base.Dependency = jobHandle3;
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
	public EffectControlSystem()
	{
	}
}
