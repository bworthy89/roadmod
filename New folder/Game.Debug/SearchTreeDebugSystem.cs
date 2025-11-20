using System;
using Colossal;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Effects;
using Game.Net;
using Game.Objects;
using Game.Routes;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

public class SearchTreeDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct NativeQuadTreeGizmoJob<TItem, TBounds, TIterator> : IJob where TItem : unmanaged, IEquatable<TItem> where TBounds : unmanaged, IEquatable<TBounds>, IBounds2<TBounds> where TIterator : unmanaged, INativeQuadTreeIterator<TItem, TBounds>
	{
		[ReadOnly]
		public NativeQuadTree<TItem, TBounds> m_Tree;

		public TIterator m_Iterator;

		public void Execute()
		{
			m_Tree.Iterate(ref m_Iterator);
		}
	}

	public struct Bounds2DebugIterator<TItem> : INativeQuadTreeIterator<TItem, Bounds2>, IUnsafeQuadTreeIterator<TItem, Bounds2> where TItem : unmanaged, IEquatable<TItem>
	{
		private Bounds2 m_Bounds;

		private GizmoBatcher m_GizmoBatcher;

		public Bounds2DebugIterator(Bounds2 bounds, GizmoBatcher gizmoBatcher)
		{
			m_Bounds = bounds;
			m_GizmoBatcher = gizmoBatcher;
		}

		public bool Intersect(Bounds2 bounds)
		{
			if (!MathUtils.Intersect(bounds, m_Bounds))
			{
				return false;
			}
			float2 @float = MathUtils.Center(bounds);
			float2 size = MathUtils.Size(bounds) * 0.5f;
			m_GizmoBatcher.DrawWireRect(new float3(@float.x, 0f, @float.y), size, UnityEngine.Color.white);
			return true;
		}

		public void Iterate(Bounds2 bounds, TItem edgeEntity)
		{
			if (MathUtils.Intersect(bounds, m_Bounds))
			{
				float2 @float = MathUtils.Center(bounds);
				float2 size = MathUtils.Size(bounds) * 0.5f;
				m_GizmoBatcher.DrawWireRect(new float3(@float.x, 0f, @float.y), size, UnityEngine.Color.red);
			}
		}
	}

	public struct LocalEffectDebugIterator : INativeQuadTreeIterator<LocalEffectSystem.EffectItem, LocalEffectSystem.EffectBounds>, IUnsafeQuadTreeIterator<LocalEffectSystem.EffectItem, LocalEffectSystem.EffectBounds>
	{
		private Bounds2 m_Bounds;

		private GizmoBatcher m_GizmoBatcher;

		public LocalEffectDebugIterator(Bounds2 bounds, GizmoBatcher gizmoBatcher)
		{
			m_Bounds = bounds;
			m_GizmoBatcher = gizmoBatcher;
		}

		public bool Intersect(LocalEffectSystem.EffectBounds bounds)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds))
			{
				return false;
			}
			float2 @float = MathUtils.Center(bounds.m_Bounds);
			float2 size = MathUtils.Size(bounds.m_Bounds) * 0.5f;
			m_GizmoBatcher.DrawWireRect(new float3(@float.x, 0f, @float.y), size, UnityEngine.Color.white);
			return true;
		}

		public void Iterate(LocalEffectSystem.EffectBounds bounds, LocalEffectSystem.EffectItem item)
		{
			if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds))
			{
				float2 @float = MathUtils.Center(bounds.m_Bounds);
				float2 size = MathUtils.Size(bounds.m_Bounds) * 0.5f;
				m_GizmoBatcher.DrawWireRect(new float3(@float.x, 0f, @float.y), size, UnityEngine.Color.red);
			}
		}
	}

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Zones.SearchSystem m_ZoneSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Routes.SearchSystem m_RouteSearchSystem;

	private Game.Effects.SearchSystem m_EffectSearchSystem;

	private LocalEffectSystem m_LocalEffectSystem;

	private GizmosSystem m_GizmosSystem;

	private Option m_StaticObjectOption;

	private Option m_MovingObjectOption;

	private Option m_NetOption;

	private Option m_LaneOption;

	private Option m_ZoneOption;

	private Option m_AreaOption;

	private Option m_RouteOption;

	private Option m_EffectOption;

	private Option m_LocalEffectOption;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ZoneSearchSystem = base.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_RouteSearchSystem = base.World.GetOrCreateSystemManaged<Game.Routes.SearchSystem>();
		m_EffectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Effects.SearchSystem>();
		m_LocalEffectSystem = base.World.GetOrCreateSystemManaged<LocalEffectSystem>();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_StaticObjectOption = AddOption("Static Objects", defaultEnabled: true);
		m_MovingObjectOption = AddOption("Moving Objects", defaultEnabled: true);
		m_NetOption = AddOption("Nets", defaultEnabled: false);
		m_LaneOption = AddOption("Lanes", defaultEnabled: false);
		m_ZoneOption = AddOption("Zones", defaultEnabled: false);
		m_AreaOption = AddOption("Areas", defaultEnabled: false);
		m_RouteOption = AddOption("Routes", defaultEnabled: false);
		m_EffectOption = AddOption("Effects", defaultEnabled: false);
		m_LocalEffectOption = AddOption("Local Effects", defaultEnabled: false);
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		Bounds3 bounds = new Bounds3(float.MinValue, float.MaxValue);
		JobHandle jobHandle = inputDeps;
		if (m_StaticObjectOption.enabled)
		{
			JobHandle job = StaticObjectSearchTreeDebug(inputDeps, bounds);
			jobHandle = JobHandle.CombineDependencies(jobHandle, job);
		}
		if (m_MovingObjectOption.enabled)
		{
			JobHandle job2 = MovingObjectSearchTreeDebug(inputDeps, bounds);
			jobHandle = JobHandle.CombineDependencies(jobHandle, job2);
		}
		if (m_NetOption.enabled)
		{
			JobHandle job3 = NetSearchTreeDebug(inputDeps, bounds);
			jobHandle = JobHandle.CombineDependencies(jobHandle, job3);
		}
		if (m_LaneOption.enabled)
		{
			JobHandle job4 = LaneSearchTreeDebug(inputDeps, bounds);
			jobHandle = JobHandle.CombineDependencies(jobHandle, job4);
		}
		if (m_ZoneOption.enabled)
		{
			JobHandle job5 = ZoneSearchTreeDebug(inputDeps, bounds);
			jobHandle = JobHandle.CombineDependencies(jobHandle, job5);
		}
		if (m_AreaOption.enabled)
		{
			JobHandle job6 = AreaSearchTreeDebug(inputDeps, bounds);
			jobHandle = JobHandle.CombineDependencies(jobHandle, job6);
		}
		if (m_RouteOption.enabled)
		{
			JobHandle job7 = RouteSearchTreeDebug(inputDeps, bounds);
			jobHandle = JobHandle.CombineDependencies(jobHandle, job7);
		}
		if (m_EffectOption.enabled)
		{
			JobHandle job8 = EffectSearchTreeDebug(inputDeps, bounds);
			jobHandle = JobHandle.CombineDependencies(jobHandle, job8);
		}
		if (m_LocalEffectOption.enabled)
		{
			JobHandle job9 = LocalEffectSearchTreeDebug(inputDeps, bounds);
			jobHandle = JobHandle.CombineDependencies(jobHandle, job9);
		}
		return jobHandle;
	}

	private JobHandle StaticObjectSearchTreeDebug(JobHandle inputDeps, Bounds3 bounds)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new NativeQuadTreeGizmoJob<Entity, QuadTreeBoundsXZ, QuadTreeBoundsXZ.DebugIterator<Entity>>
		{
			m_Tree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
			m_Iterator = new QuadTreeBoundsXZ.DebugIterator<Entity>(bounds, m_GizmosSystem.GetGizmosBatcher(out dependencies2))
		}, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	private JobHandle MovingObjectSearchTreeDebug(JobHandle inputDeps, Bounds3 bounds)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new NativeQuadTreeGizmoJob<Entity, QuadTreeBoundsXZ, QuadTreeBoundsXZ.DebugIterator<Entity>>
		{
			m_Tree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out dependencies),
			m_Iterator = new QuadTreeBoundsXZ.DebugIterator<Entity>(bounds, m_GizmosSystem.GetGizmosBatcher(out dependencies2))
		}, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	private JobHandle NetSearchTreeDebug(JobHandle inputDeps, Bounds3 bounds)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new NativeQuadTreeGizmoJob<Entity, QuadTreeBoundsXZ, QuadTreeBoundsXZ.DebugIterator<Entity>>
		{
			m_Tree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_Iterator = new QuadTreeBoundsXZ.DebugIterator<Entity>(bounds, m_GizmosSystem.GetGizmosBatcher(out dependencies2))
		}, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	private JobHandle LaneSearchTreeDebug(JobHandle inputDeps, Bounds3 bounds)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new NativeQuadTreeGizmoJob<Entity, QuadTreeBoundsXZ, QuadTreeBoundsXZ.DebugIterator<Entity>>
		{
			m_Tree = m_NetSearchSystem.GetLaneSearchTree(readOnly: true, out dependencies),
			m_Iterator = new QuadTreeBoundsXZ.DebugIterator<Entity>(bounds, m_GizmosSystem.GetGizmosBatcher(out dependencies2))
		}, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_NetSearchSystem.AddLaneSearchTreeReader(jobHandle);
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	private JobHandle ZoneSearchTreeDebug(JobHandle inputDeps, Bounds3 bounds)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new NativeQuadTreeGizmoJob<Entity, Bounds2, Bounds2DebugIterator<Entity>>
		{
			m_Tree = m_ZoneSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_Iterator = new Bounds2DebugIterator<Entity>(bounds.xz, m_GizmosSystem.GetGizmosBatcher(out dependencies2))
		}, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_ZoneSearchSystem.AddSearchTreeReader(jobHandle);
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	private JobHandle AreaSearchTreeDebug(JobHandle inputDeps, Bounds3 bounds)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new NativeQuadTreeGizmoJob<AreaSearchItem, QuadTreeBoundsXZ, QuadTreeBoundsXZ.DebugIterator<AreaSearchItem>>
		{
			m_Tree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_Iterator = new QuadTreeBoundsXZ.DebugIterator<AreaSearchItem>(bounds, m_GizmosSystem.GetGizmosBatcher(out dependencies2))
		}, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	private JobHandle RouteSearchTreeDebug(JobHandle inputDeps, Bounds3 bounds)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new NativeQuadTreeGizmoJob<RouteSearchItem, QuadTreeBoundsXZ, QuadTreeBoundsXZ.DebugIterator<RouteSearchItem>>
		{
			m_Tree = m_RouteSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_Iterator = new QuadTreeBoundsXZ.DebugIterator<RouteSearchItem>(bounds, m_GizmosSystem.GetGizmosBatcher(out dependencies2))
		}, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_RouteSearchSystem.AddSearchTreeReader(jobHandle);
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	private JobHandle EffectSearchTreeDebug(JobHandle inputDeps, Bounds3 bounds)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new NativeQuadTreeGizmoJob<SourceInfo, QuadTreeBoundsXZ, QuadTreeBoundsXZ.DebugIterator<SourceInfo>>
		{
			m_Tree = m_EffectSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_Iterator = new QuadTreeBoundsXZ.DebugIterator<SourceInfo>(bounds, m_GizmosSystem.GetGizmosBatcher(out dependencies2))
		}, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_EffectSearchSystem.AddSearchTreeReader(jobHandle);
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	private JobHandle LocalEffectSearchTreeDebug(JobHandle inputDeps, Bounds3 bounds)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new NativeQuadTreeGizmoJob<LocalEffectSystem.EffectItem, LocalEffectSystem.EffectBounds, LocalEffectDebugIterator>
		{
			m_Tree = m_LocalEffectSystem.GetSearchTree(readOnly: true, out dependencies),
			m_Iterator = new LocalEffectDebugIterator(bounds.xz, m_GizmosSystem.GetGizmosBatcher(out dependencies2))
		}, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_LocalEffectSystem.AddLocalEffectReader(jobHandle);
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	[Preserve]
	public SearchTreeDebugSystem()
	{
	}
}
