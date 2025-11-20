using System.Runtime.CompilerServices;
using Game.Common;
using Game.Creatures;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Pathfind;

[CompilerGenerated]
public class PathOwnerTargetMovedSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckPathOwnerTargetsJob : IJobChunk
	{
		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public Entity m_MovedEntity;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public BufferTypeHandle<PathElement> m_PathElementType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Target> nativeArray = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray2 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (nativeArray[i].m_Target == m_MovedEntity)
				{
					PathOwner value = nativeArray2[i];
					DynamicBuffer<PathElement> dynamicBuffer = bufferAccessor[i];
					if (value.m_ElementIndex < dynamicBuffer.Length)
					{
						int index = random.NextInt(value.m_ElementIndex, dynamicBuffer.Length);
						PathElement value2 = dynamicBuffer[index];
						value2.m_Target = Entity.Null;
						dynamicBuffer[index] = value2;
					}
					else
					{
						value.m_State |= PathFlags.Obsolete;
						nativeArray2[i] = value;
					}
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
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Pathfind_PathElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>();
		}
	}

	private EntityQuery m_EventQuery;

	private EntityQuery m_PathOwnerQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<PathTargetMoved>(), ComponentType.ReadOnly<Event>());
		m_PathOwnerQuery = GetEntityQuery(ComponentType.ReadOnly<PathOwner>(), ComponentType.ReadOnly<PathElement>(), ComponentType.ReadOnly<Target>(), ComponentType.Exclude<GroupMember>());
		RequireForUpdate(m_EventQuery);
		RequireForUpdate(m_PathOwnerQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<PathTargetMoved> nativeArray = m_EventQuery.ToComponentDataArray<PathTargetMoved>(Allocator.TempJob);
		try
		{
			CheckPathOwnerTargetsJob jobData = new CheckPathOwnerTargetsJob
			{
				m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferTypeHandle, ref base.CheckedStateRef)
			};
			for (int i = 0; i < nativeArray.Length; i++)
			{
				jobData.m_RandomSeed = RandomSeed.Next();
				jobData.m_MovedEntity = nativeArray[i].m_Target;
				base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_PathOwnerQuery, base.Dependency);
			}
		}
		finally
		{
			nativeArray.Dispose();
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
	public PathOwnerTargetMovedSystem()
	{
	}
}
