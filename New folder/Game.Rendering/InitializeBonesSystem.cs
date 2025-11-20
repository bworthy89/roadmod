using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class InitializeBonesSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeBonesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<ProceduralBone> m_ProceduralBones;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		public BufferLookup<Skeleton> m_Skeletons;

		public BufferLookup<Bone> m_Bones;

		public BufferLookup<Momentum> m_Momentums;

		public BufferLookup<PlaybackLayer> m_PlaybackLayers;

		[ReadOnly]
		public int m_CurrentTime;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public NativeHeapAllocator m_HeapAllocator;

		public NativeReference<ProceduralSkeletonSystem.AllocationInfo> m_AllocationInfo;

		public NativeQueue<ProceduralSkeletonSystem.AllocationRemove> m_AllocationRemoves;

		public void Execute()
		{
			ref ProceduralSkeletonSystem.AllocationInfo allocationInfo = ref m_AllocationInfo.ValueAsRef();
			for (int i = 0; i < m_CullingData.Length; i++)
			{
				PreCullingData cullingData = m_CullingData[i];
				if ((cullingData.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated)) != 0 && (cullingData.m_Flags & PreCullingFlags.Skeleton) != 0)
				{
					if ((cullingData.m_Flags & PreCullingFlags.NearCamera) == 0)
					{
						Remove(cullingData);
					}
					else
					{
						Update(cullingData, ref allocationInfo);
					}
				}
			}
		}

		private void Remove(PreCullingData cullingData)
		{
			DynamicBuffer<Skeleton> skeletons = m_Skeletons[cullingData.m_Entity];
			DynamicBuffer<Bone> dynamicBuffer = m_Bones[cullingData.m_Entity];
			Deallocate(skeletons);
			skeletons.Clear();
			dynamicBuffer.Clear();
			if (m_Momentums.TryGetBuffer(cullingData.m_Entity, out var bufferData))
			{
				bufferData.Clear();
			}
			if (m_PlaybackLayers.TryGetBuffer(cullingData.m_Entity, out var bufferData2))
			{
				bufferData2.Clear();
			}
		}

		private unsafe void Update(PreCullingData cullingData, ref ProceduralSkeletonSystem.AllocationInfo allocationInfo)
		{
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			if (m_SubMeshes.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
			{
				DynamicBuffer<Skeleton> skeletons = m_Skeletons[cullingData.m_Entity];
				DynamicBuffer<Bone> dynamicBuffer = m_Bones[cullingData.m_Entity];
				m_Momentums.TryGetBuffer(cullingData.m_Entity, out var bufferData2);
				m_PlaybackLayers.TryGetBuffer(cullingData.m_Entity, out var bufferData3);
				int num = 0;
				int num2 = 0;
				for (int i = 0; i < bufferData.Length; i++)
				{
					SubMesh subMesh = bufferData[i];
					if (!m_ProceduralBones.TryGetBuffer(subMesh.m_SubMesh, out var bufferData4))
					{
						continue;
					}
					num += bufferData4.Length;
					if (!bufferData3.IsCreated)
					{
						continue;
					}
					int num3 = 0;
					for (int j = 0; j < bufferData4.Length; j++)
					{
						ProceduralBone proceduralBone = bufferData4[j];
						BoneType type = bufferData4[j].m_Type;
						if ((uint)(type - 35) <= 7u)
						{
							int num4 = 1 << (int)(proceduralBone.m_Type - 35);
							if ((num3 & num4) == 0)
							{
								num3 |= num4;
								num2++;
							}
						}
					}
				}
				if (skeletons.Length == bufferData.Length && dynamicBuffer.Length == num && (!bufferData3.IsCreated || bufferData3.Length == num2))
				{
					return;
				}
				Deallocate(skeletons);
				skeletons.ResizeUninitialized(bufferData.Length);
				dynamicBuffer.ResizeUninitialized(num);
				if (bufferData2.IsCreated)
				{
					bufferData2.ResizeUninitialized(num);
					for (int k = 0; k < bufferData2.Length; k++)
					{
						bufferData2[k] = default(Momentum);
					}
				}
				if (bufferData3.IsCreated)
				{
					bufferData3.ResizeUninitialized(num2);
				}
				num = 0;
				num2 = 0;
				for (int l = 0; l < bufferData.Length; l++)
				{
					SubMesh subMesh2 = bufferData[l];
					if (m_ProceduralBones.TryGetBuffer(subMesh2.m_SubMesh, out var bufferData5))
					{
						NativeHeapBlock bufferAllocation = m_HeapAllocator.Allocate((uint)bufferData5.Length);
						if (bufferAllocation.Empty)
						{
							m_HeapAllocator.Resize(m_HeapAllocator.Size + 1048576u / (uint)sizeof(float4x4));
							bufferAllocation = m_HeapAllocator.Allocate((uint)bufferData5.Length);
						}
						allocationInfo.m_AllocationCount++;
						Skeleton value = new Skeleton
						{
							m_BufferAllocation = bufferAllocation,
							m_BoneOffset = num,
							m_LayerOffset = num2,
							m_CurrentUpdated = true,
							m_HistoryUpdated = true
						};
						int num5 = 0;
						for (int m = 0; m < bufferData5.Length; m++)
						{
							ProceduralBone proceduralBone2 = bufferData5[m];
							value.m_RequireHistory |= proceduralBone2.m_ConnectionID != 0;
							if (bufferData3.IsCreated)
							{
								BoneType type = proceduralBone2.m_Type;
								if ((uint)(type - 35) <= 7u)
								{
									int num6 = (int)(proceduralBone2.m_Type - 35);
									int num7 = 1 << num6;
									if ((num5 & num7) == 0)
									{
										num5 |= num7;
										bufferData3[num2++] = new PlaybackLayer
										{
											m_ClipIndex = -1,
											m_LayerIndex = (byte)num6
										};
									}
								}
							}
							dynamicBuffer[num++] = new Bone
							{
								m_Position = proceduralBone2.m_Position,
								m_Rotation = proceduralBone2.m_Rotation,
								m_Scale = proceduralBone2.m_Scale
							};
						}
						skeletons[l] = value;
					}
					else
					{
						skeletons[l] = new Skeleton
						{
							m_BoneOffset = -1
						};
					}
				}
			}
			else
			{
				Remove(cullingData);
			}
		}

		private void Deallocate(DynamicBuffer<Skeleton> skeletons)
		{
			for (int i = 0; i < skeletons.Length; i++)
			{
				Skeleton skeleton = skeletons[i];
				if (!skeleton.m_BufferAllocation.Empty)
				{
					m_AllocationRemoves.Enqueue(new ProceduralSkeletonSystem.AllocationRemove
					{
						m_Allocation = skeleton.m_BufferAllocation,
						m_RemoveTime = m_CurrentTime
					});
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ProceduralBone> __Game_Prefabs_ProceduralBone_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		public BufferLookup<Skeleton> __Game_Rendering_Skeleton_RW_BufferLookup;

		public BufferLookup<Bone> __Game_Rendering_Bone_RW_BufferLookup;

		public BufferLookup<Momentum> __Game_Rendering_Momentum_RW_BufferLookup;

		public BufferLookup<PlaybackLayer> __Game_Rendering_PlaybackLayer_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ProceduralBone_RO_BufferLookup = state.GetBufferLookup<ProceduralBone>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Rendering_Skeleton_RW_BufferLookup = state.GetBufferLookup<Skeleton>();
			__Game_Rendering_Bone_RW_BufferLookup = state.GetBufferLookup<Bone>();
			__Game_Rendering_Momentum_RW_BufferLookup = state.GetBufferLookup<Momentum>();
			__Game_Rendering_PlaybackLayer_RW_BufferLookup = state.GetBufferLookup<PlaybackLayer>();
		}
	}

	private ProceduralSkeletonSystem m_ProceduralSkeletonSystem;

	private PreCullingSystem m_PreCullingSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ProceduralSkeletonSystem = base.World.GetOrCreateSystemManaged<ProceduralSkeletonSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeReference<ProceduralSkeletonSystem.AllocationInfo> allocationInfo;
		NativeQueue<ProceduralSkeletonSystem.AllocationRemove> allocationRemoves;
		int currentTime;
		JobHandle dependencies;
		NativeHeapAllocator heapAllocator = m_ProceduralSkeletonSystem.GetHeapAllocator(out allocationInfo, out allocationRemoves, out currentTime, out dependencies);
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new InitializeBonesJob
		{
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ProceduralBones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RW_BufferLookup, ref base.CheckedStateRef),
			m_Bones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Bone_RW_BufferLookup, ref base.CheckedStateRef),
			m_Momentums = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Momentum_RW_BufferLookup, ref base.CheckedStateRef),
			m_PlaybackLayers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_PlaybackLayer_RW_BufferLookup, ref base.CheckedStateRef),
			m_CurrentTime = currentTime,
			m_CullingData = m_PreCullingSystem.GetUpdatedData(readOnly: true, out dependencies2),
			m_HeapAllocator = heapAllocator,
			m_AllocationInfo = allocationInfo,
			m_AllocationRemoves = allocationRemoves
		}, JobHandle.CombineDependencies(base.Dependency, dependencies2, dependencies));
		m_ProceduralSkeletonSystem.AddHeapWriter(jobHandle);
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
	public InitializeBonesSystem()
	{
	}
}
