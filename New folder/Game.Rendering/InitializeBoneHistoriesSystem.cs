using System.Runtime.CompilerServices;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class InitializeBoneHistoriesSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeBoneHistoriesJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<Skeleton> m_Skeletons;

		[ReadOnly]
		public BufferLookup<Bone> m_Bones;

		[ReadOnly]
		public BufferLookup<ProceduralBone> m_ProceduralBones;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[NativeDisableParallelForRestriction]
		public BufferLookup<BoneHistory> m_BoneHistories;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public void Execute(int index)
		{
			PreCullingData cullingData = m_CullingData[index];
			if ((cullingData.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated)) != 0 && (cullingData.m_Flags & PreCullingFlags.Skeleton) != 0)
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

		private void Remove(PreCullingData cullingData)
		{
			m_BoneHistories[cullingData.m_Entity].Clear();
		}

		private void Update(PreCullingData cullingData)
		{
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			if (m_SubMeshes.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
			{
				DynamicBuffer<Skeleton> dynamicBuffer = m_Skeletons[cullingData.m_Entity];
				DynamicBuffer<Bone> bones = m_Bones[cullingData.m_Entity];
				DynamicBuffer<BoneHistory> dynamicBuffer2 = m_BoneHistories[cullingData.m_Entity];
				if (bones.Length == dynamicBuffer2.Length)
				{
					return;
				}
				dynamicBuffer2.ResizeUninitialized(bones.Length);
				DynamicBuffer<Skeleton> bufferData2 = default(DynamicBuffer<Skeleton>);
				DynamicBuffer<Bone> bones2 = default(DynamicBuffer<Bone>);
				bool flag = false;
				if ((cullingData.m_Flags & PreCullingFlags.Temp) != 0)
				{
					Temp temp = m_TempData[cullingData.m_Entity];
					flag = m_PrefabRefData.TryGetComponent(temp.m_Original, out var componentData) && m_Skeletons.TryGetBuffer(temp.m_Original, out bufferData2) && m_Bones.TryGetBuffer(temp.m_Original, out bones2) && componentData.m_Prefab == prefabRef.m_Prefab && bufferData2.Length == dynamicBuffer.Length && bones2.Length == bones.Length;
				}
				NativeList<float4x4> tempMatrices = default(NativeList<float4x4>);
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Skeleton skeleton = dynamicBuffer[i];
					if (skeleton.m_BoneOffset >= 0)
					{
						SubMesh subMesh = bufferData[i];
						DynamicBuffer<ProceduralBone> proceduralBones = m_ProceduralBones[subMesh.m_SubMesh];
						if (!tempMatrices.IsCreated)
						{
							tempMatrices = new NativeList<float4x4>(proceduralBones.Length * 2, Allocator.Temp);
						}
						tempMatrices.ResizeUninitialized(proceduralBones.Length * 2);
						if (flag)
						{
							ProceduralSkeletonSystem.GetSkinMatrices(bufferData2[i], in proceduralBones, in bones2, tempMatrices);
						}
						else
						{
							ProceduralSkeletonSystem.GetSkinMatrices(skeleton, in proceduralBones, in bones, tempMatrices);
						}
						for (int j = 0; j < proceduralBones.Length; j++)
						{
							ProceduralBone proceduralBone = proceduralBones[j];
							dynamicBuffer2[skeleton.m_BoneOffset + j] = new BoneHistory
							{
								m_Matrix = tempMatrices[proceduralBones.Length + proceduralBone.m_BindIndex]
							};
						}
					}
				}
				if (tempMatrices.IsCreated)
				{
					tempMatrices.Dispose();
				}
			}
			else
			{
				Remove(cullingData);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Skeleton> __Game_Rendering_Skeleton_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Bone> __Game_Rendering_Bone_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ProceduralBone> __Game_Prefabs_ProceduralBone_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		public BufferLookup<BoneHistory> __Game_Rendering_BoneHistory_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Rendering_Skeleton_RO_BufferLookup = state.GetBufferLookup<Skeleton>(isReadOnly: true);
			__Game_Rendering_Bone_RO_BufferLookup = state.GetBufferLookup<Bone>(isReadOnly: true);
			__Game_Prefabs_ProceduralBone_RO_BufferLookup = state.GetBufferLookup<ProceduralBone>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Rendering_BoneHistory_RW_BufferLookup = state.GetBufferLookup<BoneHistory>();
		}
	}

	private PreCullingSystem m_PreCullingSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		InitializeBoneHistoriesJob jobData = new InitializeBoneHistoriesJob
		{
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RO_BufferLookup, ref base.CheckedStateRef),
			m_Bones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Bone_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProceduralBones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_BoneHistories = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_BoneHistory_RW_BufferLookup, ref base.CheckedStateRef),
			m_CullingData = m_PreCullingSystem.GetUpdatedData(readOnly: true, out dependencies)
		};
		JobHandle jobHandle = jobData.Schedule(jobData.m_CullingData, 4, JobHandle.CombineDependencies(base.Dependency, dependencies));
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
	public InitializeBoneHistoriesSystem()
	{
	}
}
