using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Common;
using Game.Objects;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class AreaColorSystem : GameSystemBase
{
	[BurstCompile]
	private struct FillColorDataJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Batch> m_BatchType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Lot> m_LotType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Color> m_ColorData;

		[NativeDisableParallelForRestriction]
		public NativeList<AreaColorData> m_AreaColorData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Batch> nativeArray = chunk.GetNativeArray(ref m_BatchType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			BufferAccessor<Triangle> bufferAccessor = chunk.GetBufferAccessor(ref m_TriangleType);
			bool flag = chunk.Has(ref m_LotType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Batch batch = nativeArray[i];
				if (batch.m_BatchAllocation.Empty)
				{
					continue;
				}
				DynamicBuffer<Triangle> dynamicBuffer = bufferAccessor[i];
				Vector3 vector = default(Vector3);
				if (nativeArray2.Length != 0)
				{
					Owner owner = nativeArray2[i];
					while (true)
					{
						if (m_ColorData.HasComponent(owner.m_Owner))
						{
							Game.Objects.Color color = m_ColorData[owner.m_Owner];
							if (flag)
							{
								if (color.m_Index != 0)
								{
									vector.x = (float)(int)color.m_Index + 0.5f;
									vector.y = (float)(int)color.m_Value * 0.003921569f;
									vector.z = 1f;
								}
							}
							else if (color.m_SubColor)
							{
								vector.x = (float)(int)color.m_Index + 0.5f;
								vector.y = (float)(int)color.m_Value * 0.003921569f;
								vector.z = 1f;
							}
							break;
						}
						if (!m_OwnerData.TryGetComponent(owner.m_Owner, out var componentData))
						{
							break;
						}
						owner = componentData;
					}
				}
				int begin = (int)batch.m_BatchAllocation.Begin;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					m_AreaColorData[begin++] = new AreaColorData
					{
						m_Color = vector
					};
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Batch> __Game_Areas_Batch_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Lot> __Game_Areas_Lot_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Color> __Game_Objects_Color_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_Batch_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Batch>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lot>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Color_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Color>(isReadOnly: true);
		}
	}

	private AreaBatchSystem m_AreaBatchSystem;

	private ToolSystem m_ToolSystem;

	private EntityQuery m_AreaQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AreaBatchSystem = base.World.GetOrCreateSystemManaged<AreaBatchSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_AreaQuery = GetEntityQuery(ComponentType.ReadOnly<Batch>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeInfoview != null)
		{
			JobHandle dependencies;
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new FillColorDataJob
			{
				m_BatchType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Batch_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Color_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AreaColorData = m_AreaBatchSystem.GetColorData(out dependencies)
			}, m_AreaQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
			m_AreaBatchSystem.AddColorWriter(jobHandle);
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
	public AreaColorSystem()
	{
	}
}
