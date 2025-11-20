using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TreeGrowthSystem : GameSystemBase
{
	[BurstCompile]
	private struct TreeGrowthJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<Tree> m_TreeType;

		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		public ComponentTypeHandle<Damaged> m_DamagedType;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Tree> nativeArray2 = chunk.GetNativeArray(ref m_TreeType);
			NativeArray<Destroyed> nativeArray3 = chunk.GetNativeArray(ref m_DestroyedType);
			NativeArray<Damaged> nativeArray4 = chunk.GetNativeArray(ref m_DamagedType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			if (nativeArray3.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Tree tree = nativeArray2[i];
					Destroyed destroyed = nativeArray3[i];
					if (TickTree(ref tree, ref destroyed, ref random))
					{
						Entity e = nativeArray[i];
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(BatchesUpdated));
						m_CommandBuffer.RemoveComponent<Destroyed>(unfilteredChunkIndex, e);
						m_CommandBuffer.RemoveComponent<Damaged>(unfilteredChunkIndex, e);
					}
					nativeArray2[i] = tree;
					nativeArray3[i] = destroyed;
				}
				return;
			}
			if (nativeArray4.Length != 0)
			{
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Tree tree2 = nativeArray2[j];
					Damaged damaged = nativeArray4[j];
					if (TickTree(ref tree2, ref damaged, ref random, out var stateChanged))
					{
						Entity e2 = nativeArray[j];
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e2, default(BatchesUpdated));
						m_CommandBuffer.RemoveComponent<Damaged>(unfilteredChunkIndex, e2);
					}
					if (stateChanged)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[j], default(BatchesUpdated));
					}
					nativeArray2[j] = tree2;
					nativeArray4[j] = damaged;
				}
				return;
			}
			for (int k = 0; k < nativeArray2.Length; k++)
			{
				Tree tree3 = nativeArray2[k];
				if (TickTree(ref tree3, ref random))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[k], default(BatchesUpdated));
				}
				nativeArray2[k] = tree3;
			}
		}

		private bool TickTree(ref Tree tree, ref Random random)
		{
			switch (tree.m_State & (TreeState.Teen | TreeState.Adult | TreeState.Elderly | TreeState.Dead | TreeState.Stump))
			{
			case TreeState.Teen:
				return TickTeen(ref tree, ref random);
			case TreeState.Adult:
				return TickAdult(ref tree, ref random);
			case TreeState.Elderly:
				return TickElderly(ref tree, ref random);
			case TreeState.Dead:
			case TreeState.Stump:
				return TickDead(ref tree, ref random);
			default:
				return TickChild(ref tree, ref random);
			}
		}

		private bool TickTree(ref Tree tree, ref Damaged damaged, ref Random random, out bool stateChanged)
		{
			switch (tree.m_State & (TreeState.Teen | TreeState.Adult | TreeState.Elderly | TreeState.Dead | TreeState.Stump))
			{
			case TreeState.Elderly:
				stateChanged = TickElderly(ref tree, ref random);
				damaged.m_Damage -= random.NextFloat3(0.03137255f);
				damaged.m_Damage = math.max(damaged.m_Damage, float3.zero);
				return damaged.m_Damage.Equals(float3.zero);
			case TreeState.Dead:
			case TreeState.Stump:
				stateChanged = TickDead(ref tree, ref random);
				return stateChanged;
			default:
				stateChanged = false;
				damaged.m_Damage -= random.NextFloat3(0.03137255f);
				damaged.m_Damage = math.max(damaged.m_Damage, float3.zero);
				return damaged.m_Damage.Equals(float3.zero);
			}
		}

		private bool TickTree(ref Tree tree, ref Destroyed destroyed, ref Random random)
		{
			destroyed.m_Cleared += random.NextFloat(0.03137255f);
			if (destroyed.m_Cleared < 1f)
			{
				return false;
			}
			tree.m_State &= ~(TreeState.Teen | TreeState.Adult | TreeState.Elderly | TreeState.Dead | TreeState.Stump);
			tree.m_Growth = 0;
			destroyed.m_Cleared = 1f;
			return true;
		}

		private bool TickChild(ref Tree tree, ref Random random)
		{
			int num = tree.m_Growth + (random.NextInt(1280) >> 8);
			if (num < 256)
			{
				tree.m_Growth = (byte)num;
				return false;
			}
			tree.m_State |= TreeState.Teen;
			tree.m_Growth = 0;
			return true;
		}

		private bool TickTeen(ref Tree tree, ref Random random)
		{
			int num = tree.m_Growth + (random.NextInt(938) >> 8);
			if (num < 256)
			{
				tree.m_Growth = (byte)num;
				return false;
			}
			tree.m_State = (tree.m_State & ~TreeState.Teen) | TreeState.Adult;
			tree.m_Growth = 0;
			return true;
		}

		private bool TickAdult(ref Tree tree, ref Random random)
		{
			int num = tree.m_Growth + (random.NextInt(548) >> 8);
			if (num < 256)
			{
				tree.m_Growth = (byte)num;
				return false;
			}
			tree.m_State = (tree.m_State & ~TreeState.Adult) | TreeState.Elderly;
			tree.m_Growth = 0;
			return true;
		}

		private bool TickElderly(ref Tree tree, ref Random random)
		{
			int num = tree.m_Growth + (random.NextInt(548) >> 8);
			if (num < 256)
			{
				tree.m_Growth = (byte)num;
				return false;
			}
			tree.m_State = (tree.m_State & ~TreeState.Elderly) | TreeState.Dead;
			tree.m_Growth = 0;
			return true;
		}

		private bool TickDead(ref Tree tree, ref Random random)
		{
			int num = tree.m_Growth + (random.NextInt(2304) >> 8);
			if (num < 256)
			{
				tree.m_Growth = (byte)num;
				return false;
			}
			tree.m_State &= ~(TreeState.Dead | TreeState.Stump);
			tree.m_Growth = 0;
			return true;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentTypeHandle<Tree> __Game_Objects_Tree_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Damaged> __Game_Objects_Damaged_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Tree_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Tree>();
			__Game_Common_Destroyed_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>();
			__Game_Objects_Damaged_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Damaged>();
		}
	}

	public const int UPDATES_PER_DAY = 32;

	public const int TICK_SPEED_CHILD = 1280;

	public const int TICK_SPEED_TEEN = 938;

	public const int TICK_SPEED_ADULT = 548;

	public const int TICK_SPEED_ELDERLY = 548;

	public const int TICK_SPEED_DEAD = 2304;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_TreeQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 512;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TreeQuery = GetEntityQuery(ComponentType.ReadWrite<Tree>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Overridden>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_TreeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, 32, 16);
		m_TreeQuery.ResetFilter();
		m_TreeQuery.SetSharedComponentFilter(new UpdateFrame(updateFrame));
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new TreeGrowthJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TreeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Tree_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DamagedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Damaged_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_TreeQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public TreeGrowthSystem()
	{
	}
}
