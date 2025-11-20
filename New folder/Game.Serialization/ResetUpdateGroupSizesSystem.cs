using System.Runtime.CompilerServices;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class ResetUpdateGroupSizesSystem : GameSystemBase
{
	[BurstCompile]
	private struct ResetUpdateGroupSizesJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public UpdateGroupSystem.UpdateGroupTypes m_UpdateGroupTypes;

		public UpdateGroupSystem.UpdateGroupSizes m_UpdateGroupSizes;

		public void Execute()
		{
			m_UpdateGroupSizes.Clear();
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk chunk = m_Chunks[i];
				NativeArray<int> nativeArray = m_UpdateGroupSizes.Get(chunk, m_UpdateGroupTypes);
				if (nativeArray.IsCreated)
				{
					uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
					if (index < nativeArray.Length)
					{
						nativeArray[(int)index] += chunk.Count;
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
		}
	}

	private UpdateGroupSystem m_UpdateGroupSystem;

	private UpdateGroupSystem.UpdateGroupTypes m_UpdateGroupTypes;

	private EntityQuery m_UpdateFrameQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateGroupSystem = base.World.GetOrCreateSystemManaged<UpdateGroupSystem>();
		m_UpdateFrameQuery = GetEntityQuery(ComponentType.ReadOnly<UpdateFrame>());
		m_UpdateGroupTypes = new UpdateGroupSystem.UpdateGroupTypes(this);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_UpdateFrameQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		m_UpdateGroupTypes.Update(this);
		JobHandle jobHandle = IJobExtensions.Schedule(new ResetUpdateGroupSizesJob
		{
			m_Chunks = chunks,
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateGroupTypes = m_UpdateGroupTypes,
			m_UpdateGroupSizes = m_UpdateGroupSystem.GetUpdateGroupSizes()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		chunks.Dispose(jobHandle);
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
	public ResetUpdateGroupSizesSystem()
	{
	}
}
