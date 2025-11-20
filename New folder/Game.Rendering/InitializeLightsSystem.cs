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
public class InitializeLightsSystem : GameSystemBase
{
	[BurstCompile]
	private struct ProceduralInitializeJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<ProceduralLight> m_ProceduralLights;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		public BufferLookup<Emissive> m_Emissives;

		public BufferLookup<LightState> m_Lights;

		[ReadOnly]
		public int m_CurrentTime;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public NativeHeapAllocator m_HeapAllocator;

		public NativeReference<ProceduralEmissiveSystem.AllocationInfo> m_AllocationInfo;

		public NativeQueue<ProceduralEmissiveSystem.AllocationRemove> m_AllocationRemoves;

		public void Execute()
		{
			ref ProceduralEmissiveSystem.AllocationInfo allocationInfo = ref m_AllocationInfo.ValueAsRef();
			for (int i = 0; i < m_CullingData.Length; i++)
			{
				PreCullingData cullingData = m_CullingData[i];
				if ((cullingData.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated)) != 0 && (cullingData.m_Flags & PreCullingFlags.Emissive) != 0)
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
			DynamicBuffer<Emissive> emissives = m_Emissives[cullingData.m_Entity];
			DynamicBuffer<LightState> dynamicBuffer = m_Lights[cullingData.m_Entity];
			Deallocate(emissives);
			emissives.Clear();
			dynamicBuffer.Clear();
		}

		private unsafe void Update(PreCullingData cullingData, ref ProceduralEmissiveSystem.AllocationInfo allocationInfo)
		{
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			if (m_SubMeshes.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
			{
				DynamicBuffer<Emissive> emissives = m_Emissives[cullingData.m_Entity];
				DynamicBuffer<LightState> dynamicBuffer = m_Lights[cullingData.m_Entity];
				int num = 0;
				for (int i = 0; i < bufferData.Length; i++)
				{
					SubMesh subMesh = bufferData[i];
					if (m_ProceduralLights.HasBuffer(subMesh.m_SubMesh))
					{
						num += m_ProceduralLights[subMesh.m_SubMesh].Length;
					}
				}
				if (emissives.Length == bufferData.Length && dynamicBuffer.Length == num)
				{
					return;
				}
				Deallocate(emissives);
				emissives.ResizeUninitialized(bufferData.Length);
				dynamicBuffer.ResizeUninitialized(num);
				num = 0;
				for (int j = 0; j < bufferData.Length; j++)
				{
					SubMesh subMesh2 = bufferData[j];
					if (m_ProceduralLights.HasBuffer(subMesh2.m_SubMesh))
					{
						DynamicBuffer<ProceduralLight> dynamicBuffer2 = m_ProceduralLights[subMesh2.m_SubMesh];
						NativeHeapBlock bufferAllocation = m_HeapAllocator.Allocate((uint)(dynamicBuffer2.Length + 1));
						if (bufferAllocation.Empty)
						{
							m_HeapAllocator.Resize(m_HeapAllocator.Size + 1048576u / (uint)sizeof(float4));
							bufferAllocation = m_HeapAllocator.Allocate((uint)(dynamicBuffer2.Length + 1));
						}
						allocationInfo.m_AllocationCount++;
						emissives[j] = new Emissive
						{
							m_BufferAllocation = bufferAllocation,
							m_LightOffset = num,
							m_Updated = true
						};
						for (int k = 0; k < dynamicBuffer2.Length; k++)
						{
							dynamicBuffer[num++] = new LightState
							{
								m_Intensity = 0f,
								m_Color = 0f
							};
						}
					}
					else
					{
						emissives[j] = new Emissive
						{
							m_LightOffset = -1
						};
					}
				}
			}
			else
			{
				Remove(cullingData);
			}
		}

		private void Deallocate(DynamicBuffer<Emissive> emissives)
		{
			for (int i = 0; i < emissives.Length; i++)
			{
				Emissive emissive = emissives[i];
				if (!emissive.m_BufferAllocation.Empty)
				{
					m_AllocationRemoves.Enqueue(new ProceduralEmissiveSystem.AllocationRemove
					{
						m_Allocation = emissive.m_BufferAllocation,
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
		public BufferLookup<ProceduralLight> __Game_Prefabs_ProceduralLight_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		public BufferLookup<Emissive> __Game_Rendering_Emissive_RW_BufferLookup;

		public BufferLookup<LightState> __Game_Rendering_LightState_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ProceduralLight_RO_BufferLookup = state.GetBufferLookup<ProceduralLight>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Rendering_Emissive_RW_BufferLookup = state.GetBufferLookup<Emissive>();
			__Game_Rendering_LightState_RW_BufferLookup = state.GetBufferLookup<LightState>();
		}
	}

	private ProceduralEmissiveSystem m_ProceduralEmissiveSystem;

	private PreCullingSystem m_PreCullingSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ProceduralEmissiveSystem = base.World.GetOrCreateSystemManaged<ProceduralEmissiveSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeReference<ProceduralEmissiveSystem.AllocationInfo> allocationInfo;
		NativeQueue<ProceduralEmissiveSystem.AllocationRemove> allocationRemoves;
		int currentTime;
		JobHandle dependencies;
		NativeHeapAllocator heapAllocator = m_ProceduralEmissiveSystem.GetHeapAllocator(out allocationInfo, out allocationRemoves, out currentTime, out dependencies);
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new ProceduralInitializeJob
		{
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ProceduralLights = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralLight_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_Emissives = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Emissive_RW_BufferLookup, ref base.CheckedStateRef),
			m_Lights = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_LightState_RW_BufferLookup, ref base.CheckedStateRef),
			m_CurrentTime = currentTime,
			m_CullingData = m_PreCullingSystem.GetUpdatedData(readOnly: true, out dependencies2),
			m_HeapAllocator = heapAllocator,
			m_AllocationInfo = allocationInfo,
			m_AllocationRemoves = allocationRemoves
		}, JobHandle.CombineDependencies(base.Dependency, dependencies2, dependencies));
		m_ProceduralEmissiveSystem.AddHeapWriter(jobHandle);
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
	public InitializeLightsSystem()
	{
	}
}
