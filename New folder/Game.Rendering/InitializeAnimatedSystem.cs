using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class InitializeAnimatedSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeAnimatedJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<CharacterStyleData> m_CharacterStyleData;

		[ReadOnly]
		public ComponentLookup<ActivityPropData> m_ActivityPropData;

		[ReadOnly]
		public BufferLookup<MeshGroup> m_MeshGroups;

		[ReadOnly]
		public BufferLookup<MeshColor> m_MeshColors;

		[ReadOnly]
		public BufferLookup<AnimationClip> m_AnimationClips;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_SubMeshGroups;

		[ReadOnly]
		public BufferLookup<CharacterElement> m_CharacterElements;

		[ReadOnly]
		public BufferLookup<OverlayElement> m_OverlayElements;

		public BufferLookup<Animated> m_Animateds;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public AnimatedSystem.AllocationData m_AllocationData;

		public void Execute()
		{
			for (int i = 0; i < m_CullingData.Length; i++)
			{
				PreCullingData cullingData = m_CullingData[i];
				if ((cullingData.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated | PreCullingFlags.BatchesUpdated)) != 0 && (cullingData.m_Flags & PreCullingFlags.Animated) != 0)
				{
					if ((cullingData.m_Flags & PreCullingFlags.NearCamera) == 0)
					{
						Remove(cullingData);
					}
					else
					{
						Update(cullingData);
					}
				}
			}
		}

		private void Remove(PreCullingData cullingData)
		{
			DynamicBuffer<Animated> animateds = m_Animateds[cullingData.m_Entity];
			Deallocate(animateds);
			animateds.Clear();
		}

		private unsafe void Update(PreCullingData cullingData)
		{
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			if (m_SubMeshes.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
			{
				DynamicBuffer<Animated> animateds = m_Animateds[cullingData.m_Entity];
				DynamicBuffer<MeshGroup> bufferData2 = default(DynamicBuffer<MeshGroup>);
				DynamicBuffer<MeshColor> bufferData3 = default(DynamicBuffer<MeshColor>);
				DynamicBuffer<CharacterElement> bufferData4 = default(DynamicBuffer<CharacterElement>);
				int num = bufferData.Length;
				if (m_SubMeshGroups.TryGetBuffer(prefabRef.m_Prefab, out var bufferData5))
				{
					if (m_MeshGroups.TryGetBuffer(cullingData.m_Entity, out bufferData2))
					{
						num = bufferData2.Length;
						m_MeshColors.TryGetBuffer(cullingData.m_Entity, out bufferData3);
					}
					else
					{
						num = 1;
					}
					m_CharacterElements.TryGetBuffer(prefabRef.m_Prefab, out bufferData4);
				}
				bool flag = animateds.Length != num;
				if (!flag && (cullingData.m_Flags & (PreCullingFlags.Updated | PreCullingFlags.BatchesUpdated)) == 0)
				{
					return;
				}
				Deallocate(animateds);
				animateds.ResizeUninitialized(num);
				OverlayIndex* ptr = stackalloc OverlayIndex[8];
				for (int i = 0; i < num; i++)
				{
					Animated value = ((!flag) ? animateds[i] : new Animated
					{
						m_ClipIndexBody0 = -1,
						m_ClipIndexBody0I = -1,
						m_ClipIndexBody1 = -1,
						m_ClipIndexBody1I = -1,
						m_ClipIndexFace0 = -1,
						m_ClipIndexFace1 = -1
					});
					Entity entity;
					AnimationLayerMask animationLayerMask;
					ActivityPropData componentData;
					if (bufferData4.IsCreated)
					{
						CollectionUtils.TryGet(bufferData2, i, out var value2);
						SubMeshGroup subMeshGroup = bufferData5[value2.m_SubMeshGroup];
						CharacterElement characterElement = bufferData4[value2.m_SubMeshGroup];
						CharacterStyleData characterStyleData = m_CharacterStyleData[characterElement.m_Style];
						entity = characterElement.m_Style;
						animationLayerMask = characterStyleData.m_AnimationLayerMask;
						value.m_BoneAllocation = m_AllocationData.AllocateBones(characterStyleData.m_BoneCount);
						MetaBufferData metaBufferData = new MetaBufferData
						{
							m_BoneOffset = (int)value.m_BoneAllocation.Begin,
							m_BoneCount = characterStyleData.m_BoneCount,
							m_ShapeCount = characterStyleData.m_ShapeCount,
							m_MetaIndexLink = -1,
							m_BoneLink = -1,
							m_ShapeWeights = characterElement.m_ShapeWeights,
							m_TextureWeights = characterElement.m_TextureWeights,
							m_OverlayWeights = characterElement.m_OverlayWeights,
							m_MaskWeights = characterElement.m_MaskWeights
						};
						DynamicBuffer<OverlayElement> bufferData6 = default(DynamicBuffer<OverlayElement>);
						for (int j = subMeshGroup.m_SubMeshRange.x; j < subMeshGroup.m_SubMeshRange.y; j++)
						{
							SubMesh subMesh = bufferData[j];
							if (m_OverlayElements.TryGetBuffer(subMesh.m_SubMesh, out bufferData6))
							{
								break;
							}
						}
						AddOverlayIndex(ptr, 0, bufferData6, characterElement.m_OverlayWeights.m_Weight0);
						AddOverlayIndex(ptr, 1, bufferData6, characterElement.m_OverlayWeights.m_Weight1);
						AddOverlayIndex(ptr, 2, bufferData6, characterElement.m_OverlayWeights.m_Weight2);
						AddOverlayIndex(ptr, 3, bufferData6, characterElement.m_OverlayWeights.m_Weight3);
						AddOverlayIndex(ptr, 4, bufferData6, characterElement.m_OverlayWeights.m_Weight4);
						AddOverlayIndex(ptr, 5, bufferData6, characterElement.m_OverlayWeights.m_Weight5);
						AddOverlayIndex(ptr, 6, bufferData6, characterElement.m_OverlayWeights.m_Weight6);
						AddOverlayIndex(ptr, 7, bufferData6, characterElement.m_OverlayWeights.m_Weight7);
						NativeSortExtension.Sort(ptr, 8);
						metaBufferData.m_OverlayWeights.m_Weight0 = ptr->m_Weight;
						metaBufferData.m_OverlayWeights.m_Weight1 = ptr[1].m_Weight;
						metaBufferData.m_OverlayWeights.m_Weight2 = ptr[2].m_Weight;
						metaBufferData.m_OverlayWeights.m_Weight3 = ptr[3].m_Weight;
						metaBufferData.m_OverlayWeights.m_Weight4 = ptr[4].m_Weight;
						metaBufferData.m_OverlayWeights.m_Weight5 = ptr[5].m_Weight;
						metaBufferData.m_OverlayWeights.m_Weight6 = ptr[6].m_Weight;
						metaBufferData.m_OverlayWeights.m_Weight7 = ptr[7].m_Weight;
						int colorOffset = value2.m_ColorOffset + (subMeshGroup.m_SubMeshRange.y - subMeshGroup.m_SubMeshRange.x);
						metaBufferData.m_OverlayColors1.m_Color0 = GetOverlayColor(ptr, 0, bufferData3, colorOffset);
						metaBufferData.m_OverlayColors1.m_Color1 = GetOverlayColor(ptr, 1, bufferData3, colorOffset);
						metaBufferData.m_OverlayColors1.m_Color2 = GetOverlayColor(ptr, 2, bufferData3, colorOffset);
						metaBufferData.m_OverlayColors1.m_Color3 = GetOverlayColor(ptr, 3, bufferData3, colorOffset);
						metaBufferData.m_OverlayColors1.m_Color4 = GetOverlayColor(ptr, 4, bufferData3, colorOffset);
						metaBufferData.m_OverlayColors1.m_Color5 = GetOverlayColor(ptr, 5, bufferData3, colorOffset);
						metaBufferData.m_OverlayColors1.m_Color6 = GetOverlayColor(ptr, 6, bufferData3, colorOffset);
						metaBufferData.m_OverlayColors1.m_Color7 = GetOverlayColor(ptr, 7, bufferData3, colorOffset);
						value.m_MetaIndex = m_AllocationData.AddMetaBufferData(metaBufferData);
					}
					else if (m_ActivityPropData.TryGetComponent(prefabRef.m_Prefab, out componentData))
					{
						value.m_BoneAllocation = m_AllocationData.AllocateBones(componentData.m_BoneCount);
						MetaBufferData metaBufferData2 = new MetaBufferData
						{
							m_BoneOffset = (int)value.m_BoneAllocation.Begin,
							m_BoneCount = componentData.m_BoneCount,
							m_ShapeCount = 1,
							m_MetaIndexLink = -1,
							m_BoneLink = -1
						};
						entity = prefabRef.m_Prefab;
						animationLayerMask = new AnimationLayerMask(AnimationLayer.Prop);
						value.m_MetaIndex = m_AllocationData.AddMetaBufferData(metaBufferData2);
					}
					else
					{
						int index = i;
						if (bufferData5.IsCreated)
						{
							CollectionUtils.TryGet(bufferData2, i, out var value3);
							index = bufferData5[value3.m_SubMeshGroup].m_SubMeshRange.x;
						}
						entity = bufferData[index].m_SubMesh;
						animationLayerMask = new AnimationLayerMask(AnimationLayer.Body);
					}
					if (flag && m_AnimationClips.TryGetBuffer(entity, out var bufferData7) && bufferData7.Length != 0)
					{
						if ((animationLayerMask.m_Mask & new AnimationLayerMask(AnimationLayer.Body).m_Mask) != 0)
						{
							value.m_ClipIndexBody0 = 0;
						}
						if ((animationLayerMask.m_Mask & new AnimationLayerMask(AnimationLayer.Facial).m_Mask) != 0)
						{
							value.m_ClipIndexFace0 = 0;
						}
						if ((animationLayerMask.m_Mask & new AnimationLayerMask(AnimationLayer.Prop).m_Mask) != 0)
						{
							value.m_ClipIndexBody0 = 0;
						}
					}
					animateds[i] = value;
				}
			}
			else
			{
				Remove(cullingData);
			}
		}

		private void Deallocate(DynamicBuffer<Animated> animateds)
		{
			for (int i = 0; i < animateds.Length; i++)
			{
				Animated animated = animateds[i];
				if (!animated.m_BoneAllocation.Empty)
				{
					m_AllocationData.ReleaseBones(animated.m_BoneAllocation);
				}
				if (animated.m_MetaIndex != 0)
				{
					m_AllocationData.RemoveMetaBufferData(animated.m_MetaIndex);
				}
			}
		}

		private unsafe void AddOverlayIndex(OverlayIndex* overlayIndex, int index, DynamicBuffer<OverlayElement> overlayElements, BlendWeight weight)
		{
			int sortOrder = 0;
			if (overlayElements.IsCreated && weight.m_Index >= 0 && weight.m_Index < overlayElements.Length)
			{
				sortOrder = overlayElements[weight.m_Index].m_SortOrder;
			}
			overlayIndex[index] = new OverlayIndex
			{
				m_Weight = weight,
				m_OriginalIndex = index,
				m_SortOrder = sortOrder
			};
		}

		private unsafe Color GetOverlayColor(OverlayIndex* overlayIndex, int index, DynamicBuffer<MeshColor> meshColors, int colorOffset)
		{
			if (meshColors.IsCreated && meshColors.Length >= colorOffset + 8)
			{
				return meshColors[colorOffset + overlayIndex[index].m_OriginalIndex].m_ColorSet.m_Channel0.linear;
			}
			return Color.white;
		}
	}

	private struct OverlayIndex : IComparable<OverlayIndex>
	{
		public BlendWeight m_Weight;

		public int m_OriginalIndex;

		public int m_SortOrder;

		public int CompareTo(OverlayIndex other)
		{
			return math.select(m_OriginalIndex - other.m_OriginalIndex, m_SortOrder - other.m_SortOrder, m_SortOrder != other.m_SortOrder);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CharacterStyleData> __Game_Prefabs_CharacterStyleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ActivityPropData> __Game_Prefabs_ActivityPropData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshColor> __Game_Rendering_MeshColor_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationClip> __Game_Prefabs_AnimationClip_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CharacterElement> __Game_Prefabs_CharacterElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<OverlayElement> __Game_Prefabs_OverlayElement_RO_BufferLookup;

		public BufferLookup<Animated> __Game_Rendering_Animated_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CharacterStyleData_RO_ComponentLookup = state.GetComponentLookup<CharacterStyleData>(isReadOnly: true);
			__Game_Prefabs_ActivityPropData_RO_ComponentLookup = state.GetComponentLookup<ActivityPropData>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferLookup = state.GetBufferLookup<MeshGroup>(isReadOnly: true);
			__Game_Rendering_MeshColor_RO_BufferLookup = state.GetBufferLookup<MeshColor>(isReadOnly: true);
			__Game_Prefabs_AnimationClip_RO_BufferLookup = state.GetBufferLookup<AnimationClip>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_CharacterElement_RO_BufferLookup = state.GetBufferLookup<CharacterElement>(isReadOnly: true);
			__Game_Prefabs_OverlayElement_RO_BufferLookup = state.GetBufferLookup<OverlayElement>(isReadOnly: true);
			__Game_Rendering_Animated_RW_BufferLookup = state.GetBufferLookup<Animated>();
		}
	}

	private AnimatedSystem m_AnimatedSystem;

	private PreCullingSystem m_PreCullingSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AnimatedSystem = base.World.GetOrCreateSystemManaged<AnimatedSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new InitializeAnimatedJob
		{
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CharacterStyleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CharacterStyleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ActivityPropData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ActivityPropData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshColors = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshColor_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_CharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_OverlayElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_OverlayElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_Animateds = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Animated_RW_BufferLookup, ref base.CheckedStateRef),
			m_CullingData = m_PreCullingSystem.GetUpdatedData(readOnly: true, out dependencies),
			m_AllocationData = m_AnimatedSystem.GetAllocationData(out dependencies2)
		}, JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
		m_AnimatedSystem.AddAllocationWriter(jobHandle);
		m_PreCullingSystem.AddCullingDataReader(jobHandle);
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
	public InitializeAnimatedSystem()
	{
	}
}
