using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Colossal.Rendering;
using Game.Common;
using Game.Rendering;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class MeshSystem : GameSystemBase
{
	[BurstCompile]
	private struct RemoveBatchGroupsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> m_PrefabDataType;

		public BufferLookup<MeshBatch> m_MeshBatches;

		public BufferLookup<FadeBatch> m_FadeBatches;

		public BufferLookup<BatchGroup> m_BatchGroups;

		public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchGroups;

		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchInstances;

		public NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> m_NativeSubBatches;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (!chunk.Has(ref m_DeletedType))
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabData> nativeArray2 = chunk.GetNativeArray(ref m_PrefabDataType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (nativeArray2[i].m_Index < 0)
				{
					DynamicBuffer<BatchGroup> dynamicBuffer = m_BatchGroups[nativeArray[i]];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						RemoveBatchGroup(dynamicBuffer[j].m_GroupIndex, dynamicBuffer[j].m_MergeIndex);
					}
					dynamicBuffer.Clear();
				}
			}
		}

		private void RemoveBatchGroup(int groupIndex, int mergeIndex)
		{
			int groupIndex2 = groupIndex;
			if (mergeIndex != -1)
			{
				groupIndex2 = m_NativeBatchGroups.GetMergedGroupIndex(groupIndex, mergeIndex);
				m_NativeBatchGroups.RemoveMergedGroup(groupIndex, mergeIndex);
			}
			else
			{
				int mergedGroupCount = m_NativeBatchGroups.GetMergedGroupCount(groupIndex);
				if (mergedGroupCount != 0)
				{
					int mergedGroupIndex = m_NativeBatchGroups.GetMergedGroupIndex(groupIndex, 0);
					GroupData groupData = m_NativeBatchGroups.GetGroupData(mergedGroupIndex);
					DynamicBuffer<BatchGroup> dynamicBuffer = m_BatchGroups[groupData.m_Mesh];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						BatchGroup value = dynamicBuffer[i];
						if (value.m_GroupIndex == mergedGroupIndex)
						{
							value.m_MergeIndex = -1;
							dynamicBuffer[i] = value;
							break;
						}
					}
					for (int j = 1; j < mergedGroupCount; j++)
					{
						int mergedGroupIndex2 = m_NativeBatchGroups.GetMergedGroupIndex(groupIndex, j);
						groupData = m_NativeBatchGroups.GetGroupData(mergedGroupIndex2);
						dynamicBuffer = m_BatchGroups[groupData.m_Mesh];
						m_NativeBatchGroups.AddMergedGroup(mergedGroupIndex, mergedGroupIndex2);
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							BatchGroup value2 = dynamicBuffer[k];
							if (value2.m_GroupIndex == mergedGroupIndex2)
							{
								value2.m_MergeIndex = mergedGroupIndex;
								dynamicBuffer[j] = value2;
								break;
							}
						}
					}
				}
			}
			int instanceCount = m_NativeBatchInstances.GetInstanceCount(groupIndex);
			for (int l = 0; l < instanceCount; l++)
			{
				InstanceData instanceData = m_NativeBatchInstances.GetInstanceData(groupIndex, l);
				if (!m_MeshBatches.TryGetBuffer(instanceData.m_Entity, out var bufferData))
				{
					continue;
				}
				for (int m = 0; m < bufferData.Length; m++)
				{
					MeshBatch value3 = bufferData[m];
					if (value3.m_GroupIndex == groupIndex && value3.m_InstanceIndex == l)
					{
						if (m_FadeBatches.TryGetBuffer(instanceData.m_Entity, out var bufferData2))
						{
							bufferData.RemoveAtSwapBack(m);
							bufferData2.RemoveAtSwapBack(m);
						}
						else
						{
							value3.m_GroupIndex = -1;
							value3.m_InstanceIndex = -1;
							bufferData[m] = value3;
						}
						break;
					}
				}
			}
			m_NativeBatchInstances.RemoveInstances(groupIndex, m_NativeSubBatches);
			m_NativeBatchGroups.DestroyGroup(groupIndex2, m_NativeBatchInstances, m_NativeSubBatches);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct InitializeMeshJob : IJobParallelFor
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct BoneParentComparer : IComparer<ProceduralBone>
		{
			public int Compare(ProceduralBone x, ProceduralBone y)
			{
				return math.select(math.select(x.m_SourceIndex - y.m_SourceIndex, x.m_ParentIndex - y.m_ParentIndex, x.m_ParentIndex != y.m_ParentIndex), x.m_HierarchyDepth - y.m_HierarchyDepth, x.m_HierarchyDepth != y.m_HierarchyDepth);
			}
		}

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		public BufferTypeHandle<ProceduralBone> m_ProceduralBoneType;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_Chunks[index];
			if (archetypeChunk.Has(ref m_DeletedType))
			{
				return;
			}
			BufferAccessor<ProceduralBone> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_ProceduralBoneType);
			if (bufferAccessor.Length == 0)
			{
				return;
			}
			NativeList<int> nativeList = new NativeList<int>(100, Allocator.Temp);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<ProceduralBone> dynamicBuffer = bufferAccessor[i];
				if (dynamicBuffer.Length == 0)
				{
					continue;
				}
				nativeList.ResizeUninitialized(dynamicBuffer.Length);
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					ProceduralBone value = dynamicBuffer[j];
					int sourceIndex = value.m_SourceIndex;
					value.m_SourceIndex = -1;
					if (sourceIndex != 0)
					{
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							if (dynamicBuffer[k].m_ConnectionID == sourceIndex)
							{
								value.m_SourceIndex = k;
								break;
							}
						}
					}
					if (value.m_ParentIndex >= 0 && value.m_HierarchyDepth == 0)
					{
						int num = j;
						int num2 = 0;
						do
						{
							value.m_HierarchyDepth = 1;
							dynamicBuffer[num] = value;
							nativeList[num2++] = num;
							num = value.m_ParentIndex;
							value = dynamicBuffer[num];
						}
						while (value.m_ParentIndex >= 0 && value.m_HierarchyDepth == 0);
						while (num2 != 0)
						{
							int hierarchyDepth = value.m_HierarchyDepth;
							num = nativeList[--num2];
							value = dynamicBuffer[num];
							value.m_HierarchyDepth += hierarchyDepth;
							dynamicBuffer[num] = value;
						}
					}
				}
				dynamicBuffer.AsNativeArray().Sort(default(BoneParentComparer));
				for (int l = 0; l < dynamicBuffer.Length; l++)
				{
					nativeList[dynamicBuffer[l].m_BindIndex] = l;
				}
				for (int m = 0; m < dynamicBuffer.Length; m++)
				{
					ProceduralBone value2 = dynamicBuffer[m];
					if (value2.m_ParentIndex >= 0)
					{
						value2.m_ParentIndex = nativeList[value2.m_ParentIndex];
						ProceduralBone proceduralBone = dynamicBuffer[value2.m_ParentIndex];
						value2.m_ObjectPosition = proceduralBone.m_ObjectPosition + math.mul(proceduralBone.m_ObjectRotation, value2.m_Position);
						value2.m_ObjectRotation = math.mul(proceduralBone.m_ObjectRotation, value2.m_Rotation);
					}
					else
					{
						value2.m_ObjectPosition = value2.m_Position;
						value2.m_ObjectRotation = value2.m_Rotation;
					}
					if (value2.m_SourceIndex >= 0)
					{
						value2.m_SourceIndex = nativeList[value2.m_SourceIndex];
					}
					dynamicBuffer[m] = value2;
				}
			}
			nativeList.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<MeshData> __Game_Prefabs_MeshData_RW_ComponentTypeHandle;

		public BufferTypeHandle<LodMesh> __Game_Prefabs_LodMesh_RW_BufferTypeHandle;

		public BufferTypeHandle<ProceduralBone> __Game_Prefabs_ProceduralBone_RW_BufferTypeHandle;

		public BufferTypeHandle<AnimationClip> __Game_Prefabs_AnimationClip_RW_BufferTypeHandle;

		public BufferTypeHandle<ProceduralLight> __Game_Prefabs_ProceduralLight_RW_BufferTypeHandle;

		public BufferTypeHandle<LightAnimation> __Game_Prefabs_LightAnimation_RW_BufferTypeHandle;

		public BufferTypeHandle<MeshMaterial> __Game_Prefabs_MeshMaterial_RW_BufferTypeHandle;

		public BufferLookup<MeshBatch> __Game_Rendering_MeshBatch_RW_BufferLookup;

		public BufferLookup<FadeBatch> __Game_Rendering_FadeBatch_RW_BufferLookup;

		public BufferLookup<BatchGroup> __Game_Prefabs_BatchGroup_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_MeshData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<MeshData>();
			__Game_Prefabs_LodMesh_RW_BufferTypeHandle = state.GetBufferTypeHandle<LodMesh>();
			__Game_Prefabs_ProceduralBone_RW_BufferTypeHandle = state.GetBufferTypeHandle<ProceduralBone>();
			__Game_Prefabs_AnimationClip_RW_BufferTypeHandle = state.GetBufferTypeHandle<AnimationClip>();
			__Game_Prefabs_ProceduralLight_RW_BufferTypeHandle = state.GetBufferTypeHandle<ProceduralLight>();
			__Game_Prefabs_LightAnimation_RW_BufferTypeHandle = state.GetBufferTypeHandle<LightAnimation>();
			__Game_Prefabs_MeshMaterial_RW_BufferTypeHandle = state.GetBufferTypeHandle<MeshMaterial>();
			__Game_Rendering_MeshBatch_RW_BufferLookup = state.GetBufferLookup<MeshBatch>();
			__Game_Rendering_FadeBatch_RW_BufferLookup = state.GetBufferLookup<FadeBatch>();
			__Game_Prefabs_BatchGroup_RW_BufferLookup = state.GetBufferLookup<BatchGroup>();
		}
	}

	private PrefabSystem m_PrefabSystem;

	private BatchManagerSystem m_BatchManagerSystem;

	private ManagedBatchSystem m_ManagedBatchSystem;

	private EntityQuery m_PrefabQuery;

	private Dictionary<ManagedBatchSystem.MaterialKey, int> m_MaterialIndex;

	private ManagedBatchSystem.MaterialKey m_CachedMaterialKey;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_ManagedBatchSystem = base.World.GetOrCreateSystemManaged<ManagedBatchSystem>();
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadWrite<MeshData>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_MaterialIndex = new Dictionary<ManagedBatchSystem.MaterialKey, int>();
		RequireForUpdate(m_PrefabQuery);
	}

	public int GetMaterialIndex(SurfaceAsset surface)
	{
		ManagedBatchSystem.MaterialKey materialKey;
		if (m_CachedMaterialKey != null)
		{
			materialKey = m_CachedMaterialKey;
			m_CachedMaterialKey = null;
		}
		else
		{
			materialKey = new ManagedBatchSystem.MaterialKey();
		}
		surface.LoadProperties(useVT: true);
		materialKey.Initialize(surface);
		if (m_MaterialIndex.TryGetValue(materialKey, out var value))
		{
			materialKey.Clear();
			m_CachedMaterialKey = materialKey;
		}
		else
		{
			value = m_MaterialIndex.Count;
			m_MaterialIndex.Add(materialKey, value);
		}
		surface.Unload();
		return value;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> chunks = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<Deleted> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<PrefabData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<MeshData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_MeshData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<LodMesh> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_LodMesh_RW_BufferTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<ProceduralBone> bufferTypeHandle2 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RW_BufferTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<AnimationClip> bufferTypeHandle3 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_AnimationClip_RW_BufferTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<ProceduralLight> bufferTypeHandle4 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_ProceduralLight_RW_BufferTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<LightAnimation> bufferTypeHandle5 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_LightAnimation_RW_BufferTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<MeshMaterial> bufferTypeHandle6 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_MeshMaterial_RW_BufferTypeHandle, ref base.CheckedStateRef);
		bool flag = false;
		CompleteDependency();
		LodMesh value2 = default(LodMesh);
		for (int i = 0; i < chunks.Length; i++)
		{
			ArchetypeChunk archetypeChunk = chunks[i];
			NativeArray<PrefabData> nativeArray = archetypeChunk.GetNativeArray(ref typeHandle2);
			if (archetypeChunk.Has(ref typeHandle))
			{
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (nativeArray[j].m_Index < 0)
					{
						m_ManagedBatchSystem.RemoveMesh(nativeArray2[j]);
						flag = true;
					}
				}
				continue;
			}
			NativeArray<MeshData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle3);
			BufferAccessor<LodMesh> bufferAccessor = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle);
			BufferAccessor<ProceduralBone> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle2);
			BufferAccessor<AnimationClip> bufferAccessor3 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle3);
			BufferAccessor<ProceduralLight> bufferAccessor4 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle4);
			BufferAccessor<LightAnimation> bufferAccessor5 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle5);
			BufferAccessor<MeshMaterial> bufferAccessor6 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle6);
			for (int k = 0; k < nativeArray.Length; k++)
			{
				RenderPrefab prefab = m_PrefabSystem.GetPrefab<RenderPrefab>(nativeArray[k]);
				MeshData value = nativeArray3[k];
				value.m_Bounds = prefab.bounds;
				value.m_SubMeshCount = prefab.meshCount;
				value.m_IndexCount = prefab.indexCount;
				value.m_SmoothingDistance = 0.001f;
				value.m_ShadowBias = 0.5f;
				if (prefab.meshCount != prefab.materialCount)
				{
					COSystemBase.baseLog.WarnFormat(prefab, "{0}: subMeshCount ({1}) != materialCount ({2})", prefab.name, prefab.meshCount, prefab.materialCount);
				}
				if (bufferAccessor6.Length != 0)
				{
					int materialCount = prefab.materialCount;
					DynamicBuffer<MeshMaterial> dynamicBuffer = bufferAccessor6[k];
					dynamicBuffer.ResizeUninitialized(materialCount);
					int num = 0;
					foreach (SurfaceAsset surfaceAsset in prefab.surfaceAssets)
					{
						dynamicBuffer[num++] = new MeshMaterial
						{
							m_MaterialIndex = GetMaterialIndex(surfaceAsset)
						};
					}
				}
				if (prefab.isImpostor)
				{
					value.m_State |= MeshFlags.Impostor;
				}
				if (bufferAccessor.Length != 0)
				{
					LodProperties component = prefab.GetComponent<LodProperties>();
					DynamicBuffer<LodMesh> dynamicBuffer2 = bufferAccessor[k];
					value.m_LodBias = component.m_Bias;
					value.m_ShadowBias += component.m_Bias + component.m_ShadowBias;
					if (component.m_LodMeshes != null)
					{
						dynamicBuffer2.ResizeUninitialized(component.m_LodMeshes.Length);
						for (int l = 0; l < component.m_LodMeshes.Length; l++)
						{
							RenderPrefab renderPrefab = component.m_LodMeshes[l];
							int index = l;
							for (int num2 = l - 1; num2 >= 0; num2--)
							{
								RenderPrefab renderPrefab2 = component.m_LodMeshes[num2];
								if (renderPrefab.indexCount <= renderPrefab2.indexCount)
								{
									break;
								}
								dynamicBuffer2[index] = dynamicBuffer2[num2];
								index = num2;
							}
							value2.m_LodMesh = m_PrefabSystem.GetEntity(renderPrefab);
							dynamicBuffer2[index] = value2;
						}
					}
				}
				if (prefab.surfaceArea > 0f)
				{
					float3 @float = value.m_Bounds.max - value.m_Bounds.min;
					float num3 = math.log2(math.sqrt(math.clamp(prefab.surfaceArea / (math.csum(@float * @float.yzx) * 2f), 1E-06f, 1f)));
					float num4 = math.log2(math.sqrt(math.clamp(math.cmax(math.min(@float, @float.yzx)) * 3f / math.csum(@float), 1E-06f, 1f)));
					value.m_LodBias -= num3;
					value.m_ShadowBias -= 1.5f * num3 + num4;
				}
				if (bufferAccessor2.Length != 0)
				{
					ProceduralAnimationProperties component2 = prefab.GetComponent<ProceduralAnimationProperties>();
					if (component2.m_Bones != null)
					{
						DynamicBuffer<ProceduralBone> dynamicBuffer3 = bufferAccessor2[k];
						dynamicBuffer3.ResizeUninitialized(component2.m_Bones.Length);
						for (int m = 0; m < component2.m_Bones.Length; m++)
						{
							ProceduralAnimationProperties.BoneInfo boneInfo = component2.m_Bones[m];
							float speed;
							float acceleration;
							switch (boneInfo.m_Type)
							{
							case BoneType.LookAtDirection:
							case BoneType.WindTurbineRotation:
							case BoneType.WindSpeedRotation:
							case BoneType.PoweredRotation:
							case BoneType.TrafficBarrierDirection:
							case BoneType.RollingRotation:
							case BoneType.PropellerRotation:
							case BoneType.LookAtRotation:
							case BoneType.LookAtAim:
							case BoneType.PropellerAngle:
							case BoneType.PantographRotation:
							case BoneType.WorkingRotation:
							case BoneType.OperatingRotation:
							case BoneType.TimeRotation:
							case BoneType.LookAtRotationSide:
							case BoneType.RotationXFromMovementY:
							case BoneType.LookAtAimForward:
								speed = boneInfo.m_Speed * (MathF.PI * 2f);
								acceleration = boneInfo.m_Acceleration * (MathF.PI * 2f);
								break;
							default:
								speed = boneInfo.m_Speed;
								acceleration = boneInfo.m_Acceleration;
								break;
							}
							int num5 = boneInfo.m_ConnectionID;
							if (num5 < 0 || num5 > 900)
							{
								COSystemBase.baseLog.ErrorFormat(prefab, "{0}: boneInfo[{1}].ConnectionID ({2}) != 0->900", prefab.name, m, num5);
								num5 = 0;
							}
							dynamicBuffer3[m] = new ProceduralBone
							{
								m_Position = boneInfo.position,
								m_Rotation = boneInfo.rotation,
								m_Scale = boneInfo.scale,
								m_BindPose = boneInfo.bindPose,
								m_ParentIndex = boneInfo.parentId,
								m_BindIndex = m,
								m_Type = boneInfo.m_Type,
								m_ConnectionID = num5,
								m_SourceIndex = boneInfo.m_SourceID,
								m_Speed = speed,
								m_Acceleration = acceleration
							};
						}
					}
				}
				if (bufferAccessor3.Length != 0)
				{
					ProceduralAnimationProperties component3 = prefab.GetComponent<ProceduralAnimationProperties>();
					if (component3.m_Animations != null)
					{
						DynamicBuffer<AnimationClip> dynamicBuffer4 = bufferAccessor3[k];
						dynamicBuffer4.ResizeUninitialized(component3.m_Animations.Length);
						for (int n = 0; n < component3.m_Animations.Length; n++)
						{
							ProceduralAnimationProperties.AnimationInfo animationInfo = component3.m_Animations[n];
							ref AnimationClip reference = ref dynamicBuffer4.ElementAt(n);
							reference = default(AnimationClip);
							reference.m_InfoIndex = -1;
							reference.m_RootMotionBone = -1;
							reference.m_Layer = animationInfo.layer;
							reference.m_ClipState = animationInfo.state;
							reference.m_Playback = animationInfo.playback;
							reference.m_Acceleration = animationInfo.acceleration;
							reference.m_TargetValue = float.MinValue;
							reference.m_VariationCount = 1;
							if (reference.m_Playback == AnimationPlayback.RandomLoop || reference.m_Type == AnimationType.Move || reference.m_Playback == AnimationPlayback.SyncToRelative)
							{
								reference.m_AnimationLength = (float)animationInfo.frameCount / (float)animationInfo.frameRate;
								reference.m_FrameRate = animationInfo.frameRate;
								continue;
							}
							float num6 = (float)(animationInfo.frameCount - 1) * (60f / (float)animationInfo.frameRate);
							num6 = math.max(1f, math.round(num6 / 16f)) * 16f;
							reference.m_AnimationLength = num6 * (1f / 60f);
							reference.m_FrameRate = (float)math.max(1, animationInfo.frameCount - 1) / reference.m_AnimationLength;
							reference.m_AnimationLength -= 0.001f;
						}
					}
				}
				if (bufferAccessor4.Length != 0)
				{
					EmissiveProperties component4 = prefab.GetComponent<EmissiveProperties>();
					if (component4.hasAnyLights)
					{
						DynamicBuffer<ProceduralLight> dynamicBuffer5 = bufferAccessor4[k];
						dynamicBuffer5.ResizeUninitialized(component4.lightsCount);
						int num7 = 0;
						int num8 = 0;
						if (bufferAccessor5.Length != 0)
						{
							DynamicBuffer<LightAnimation> dynamicBuffer6 = bufferAccessor5[k];
							int num9 = 0;
							if (component4.m_SignalGroupAnimations != null)
							{
								num9 += component4.m_SignalGroupAnimations.Count;
							}
							num7 = num9;
							if (component4.m_AnimationCurves != null)
							{
								num9 += component4.m_AnimationCurves.Count;
								num8 = component4.m_AnimationCurves.Count;
							}
							dynamicBuffer6.ResizeUninitialized(num9);
							if (component4.m_SignalGroupAnimations != null)
							{
								for (int num10 = 0; num10 < component4.m_SignalGroupAnimations.Count; num10++)
								{
									EmissiveProperties.SignalGroupAnimation signalGroupAnimation = component4.m_SignalGroupAnimations[num10];
									dynamicBuffer6[num10] = new LightAnimation
									{
										m_DurationFrames = (uint)math.max(1, Mathf.RoundToInt(signalGroupAnimation.m_Duration * 60f)),
										m_SignalAnimation = new SignalAnimation(signalGroupAnimation.m_SignalGroupMasks)
									};
								}
							}
							if (component4.m_AnimationCurves != null)
							{
								for (int num11 = 0; num11 < component4.m_AnimationCurves.Count; num11++)
								{
									EmissiveProperties.AnimationProperties animationProperties = component4.m_AnimationCurves[num11];
									dynamicBuffer6[num7 + num11] = new LightAnimation
									{
										m_DurationFrames = (uint)math.max(1, Mathf.RoundToInt(animationProperties.m_Duration * 60f)),
										m_AnimationCurve = new AnimationCurve1(animationProperties.m_Curve)
									};
								}
							}
						}
						int num12 = 0;
						if (component4.hasMultiLights)
						{
							num12 = component4.m_MultiLights.Count;
							for (int num13 = 0; num13 < component4.m_MultiLights.Count; num13++)
							{
								EmissiveProperties.MultiLightMapping multiLightMapping = component4.m_MultiLights[num13];
								Color linear = multiLightMapping.color.linear;
								Color linear2 = multiLightMapping.colorOff.linear;
								dynamicBuffer5[num13] = new ProceduralLight
								{
									m_Color = new float4(linear.r, linear.g, linear.b, multiLightMapping.intensity * 100f),
									m_Color2 = new float4(linear2.r, linear2.g, linear2.b, multiLightMapping.intensity * 100f),
									m_Purpose = multiLightMapping.purpose,
									m_ResponseSpeed = 1f / math.max(0.001f, multiLightMapping.responseTime),
									m_AnimationIndex = math.select(-1, num7 + multiLightMapping.animationIndex, multiLightMapping.animationIndex >= 0 && multiLightMapping.animationIndex < num8)
								};
							}
						}
						if (component4.hasSingleLights)
						{
							for (int num14 = 0; num14 < component4.m_SingleLights.Count; num14++)
							{
								EmissiveProperties.SingleLightMapping singleLightMapping = component4.m_SingleLights[num14];
								Color linear3 = singleLightMapping.color.linear;
								Color linear4 = singleLightMapping.colorOff.linear;
								dynamicBuffer5[num12 + num14] = new ProceduralLight
								{
									m_Color = new float4(linear3.r, linear3.g, linear3.b, singleLightMapping.intensity * 100f),
									m_Color2 = new float4(linear4.r, linear4.g, linear4.b, singleLightMapping.intensity * 100f),
									m_Purpose = singleLightMapping.purpose,
									m_ResponseSpeed = 1f / math.max(0.001f, singleLightMapping.responseTime),
									m_AnimationIndex = math.select(-1, num7 + singleLightMapping.animationIndex, singleLightMapping.animationIndex >= 0 && singleLightMapping.animationIndex < num8)
								};
							}
						}
					}
				}
				UndergroundMesh component5 = prefab.GetComponent<UndergroundMesh>();
				if (component5 != null)
				{
					if (component5.m_IsTunnel)
					{
						value.m_DefaultLayers |= MeshLayer.Tunnel;
					}
					if (component5.m_IsPipeline)
					{
						value.m_DefaultLayers |= MeshLayer.Pipeline;
					}
					if (component5.m_IsSubPipeline)
					{
						value.m_DefaultLayers |= MeshLayer.SubPipeline;
					}
				}
				OverlayProperties component6 = prefab.GetComponent<OverlayProperties>();
				if (component6 != null && component6.m_IsWaterway)
				{
					value.m_DefaultLayers |= MeshLayer.Waterway;
				}
				if (prefab.GetComponent<DecalProperties>() != null)
				{
					value.m_State |= MeshFlags.Decal;
				}
				StackProperties component7 = prefab.GetComponent<StackProperties>();
				if (component7 != null)
				{
					switch (component7.m_Direction)
					{
					case StackDirection.Right:
						value.m_State |= MeshFlags.StackX;
						break;
					case StackDirection.Up:
						value.m_State |= MeshFlags.StackY;
						break;
					case StackDirection.Forward:
						value.m_State |= MeshFlags.StackZ;
						break;
					}
				}
				if (prefab.GetComponent<ProceduralAnimationProperties>() != null)
				{
					value.m_State |= MeshFlags.Skeleton;
				}
				CurveProperties component8 = prefab.GetComponent<CurveProperties>();
				if (component8 != null)
				{
					value.m_TilingCount = component8.m_TilingCount;
					if (component8.m_OverrideLength != 0f)
					{
						value.m_Bounds.min.z = component8.m_OverrideLength * -0.5f;
						value.m_Bounds.max.z = component8.m_OverrideLength * 0.5f;
					}
					if (component8.m_SmoothingDistance > value.m_SmoothingDistance)
					{
						value.m_SmoothingDistance = component8.m_SmoothingDistance;
					}
					if (component8.m_GeometryTiling)
					{
						value.m_State |= MeshFlags.Tiling;
					}
					if (component8.m_InvertCurve)
					{
						value.m_State |= MeshFlags.Invert;
					}
				}
				BaseProperties component9 = prefab.GetComponent<BaseProperties>();
				if (component9 != null && component9.m_BaseType != null)
				{
					value.m_State |= MeshFlags.Base;
					if (component9.m_UseMinBounds)
					{
						value.m_State |= MeshFlags.MinBounds;
					}
				}
				if (prefab.Has<DefaultMesh>())
				{
					float renderingSize = RenderingUtils.GetRenderingSize(MathUtils.Size(value.m_Bounds));
					value.m_State |= MeshFlags.Default;
					value.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(renderingSize, value.m_LodBias);
					value.m_ShadowLod = (byte)RenderingUtils.CalculateLodLimit(renderingSize, value.m_ShadowBias);
				}
				nativeArray3[k] = value;
			}
		}
		InitializeMeshJob jobData = new InitializeMeshJob
		{
			m_Chunks = chunks,
			m_DeletedType = typeHandle,
			m_ProceduralBoneType = bufferTypeHandle2
		};
		base.Dependency = IJobParallelForExtensions.Schedule(jobData, chunks.Length, 1, base.Dependency);
		if (flag)
		{
			JobHandle dependencies;
			JobHandle dependencies2;
			JobHandle dependencies3;
			JobHandle jobHandle = JobChunkExtensions.Schedule(new RemoveBatchGroupsJob
			{
				m_EntityType = entityTypeHandle,
				m_DeletedType = typeHandle,
				m_PrefabDataType = typeHandle2,
				m_MeshBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshBatch_RW_BufferLookup, ref base.CheckedStateRef),
				m_FadeBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_FadeBatch_RW_BufferLookup, ref base.CheckedStateRef),
				m_BatchGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_BatchGroup_RW_BufferLookup, ref base.CheckedStateRef),
				m_NativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies),
				m_NativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: false, out dependencies2),
				m_NativeSubBatches = m_BatchManagerSystem.GetNativeSubBatches(readOnly: false, out dependencies3)
			}, m_PrefabQuery, JobUtils.CombineDependencies(base.Dependency, dependencies, dependencies2, dependencies3));
			m_BatchManagerSystem.AddNativeBatchGroupsWriter(jobHandle);
			m_BatchManagerSystem.AddNativeBatchInstancesWriter(jobHandle);
			m_BatchManagerSystem.AddNativeSubBatchesWriter(jobHandle);
			base.Dependency = jobHandle;
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
	public MeshSystem()
	{
	}
}
