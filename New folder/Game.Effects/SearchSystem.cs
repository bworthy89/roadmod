using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Effects;

[CompilerGenerated]
public class SearchSystem : GameSystemBase, IPreDeserialize
{
	private struct AddedSource
	{
		public Entity m_Prefab;

		public int m_EffectIndex;
	}

	[BurstCompile]
	private struct UpdateSearchTreeJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Tools.EditorContainer> m_EditorContainerType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;

		[ReadOnly]
		public ComponentLookup<EffectData> m_PrefabEffectData;

		[ReadOnly]
		public ComponentLookup<LightEffectData> m_PrefabLightEffectData;

		[ReadOnly]
		public ComponentLookup<AudioEffectData> m_PrefabAudioEffectData;

		[ReadOnly]
		public BufferLookup<Effect> m_PrefabEffects;

		[ReadOnly]
		public bool m_Loaded;

		public NativeQuadTree<SourceInfo, QuadTreeBoundsXZ> m_SearchTree;

		public NativeParallelMultiHashMap<Entity, AddedSource> m_AddedSources;

		public EffectControlData m_EffectControlData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Tools.EditorContainer> nativeArray2 = chunk.GetNativeArray(ref m_EditorContainerType);
			NativeArray<PrefabRef> nativeArray3 = default(NativeArray<PrefabRef>);
			if (nativeArray2.Length == 0)
			{
				nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			}
			if (chunk.Has(ref m_DeletedType) || chunk.Has(ref m_InterpolatedTransformType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					if (m_AddedSources.TryGetFirstValue(entity, out var item, out var it))
					{
						do
						{
							m_SearchTree.TryRemove(new SourceInfo(entity, item.m_EffectIndex));
						}
						while (m_AddedSources.TryGetNextValue(out item, ref it));
						m_AddedSources.Remove(entity);
					}
				}
				return;
			}
			NativeArray<Transform> nativeArray4 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Curve> nativeArray5 = chunk.GetNativeArray(ref m_CurveType);
			bool flag = m_Loaded || chunk.Has(ref m_CreatedType);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Entity entity2 = nativeArray[j];
				if (CollectionUtils.TryGet(nativeArray2, j, out var value))
				{
					if (m_AddedSources.TryGetFirstValue(entity2, out var item2, out var it2))
					{
						do
						{
							if (value.m_Prefab != item2.m_Prefab)
							{
								m_SearchTree.TryRemove(new SourceInfo(entity2, item2.m_EffectIndex));
							}
						}
						while (m_AddedSources.TryGetNextValue(out item2, ref it2));
						m_AddedSources.Remove(entity2);
					}
					if (m_PrefabEffectData.TryGetComponent(value.m_Prefab, out var componentData) && !componentData.m_OwnerCulling)
					{
						if (m_EffectControlData.ShouldBeEnabled(entity2, value.m_Prefab, checkEnabled: true, isEditorContainer: true))
						{
							Effect effect = new Effect
							{
								m_Effect = value.m_Prefab
							};
							QuadTreeBoundsXZ bounds = GetBounds(nativeArray4, nativeArray5, j, effect);
							m_SearchTree.AddOrUpdate(new SourceInfo(entity2, 0), bounds);
							m_AddedSources.Add(entity2, new AddedSource
							{
								m_Prefab = value.m_Prefab,
								m_EffectIndex = 0
							});
						}
						else if (!flag)
						{
							m_SearchTree.TryRemove(new SourceInfo(entity2, 0));
						}
					}
					continue;
				}
				PrefabRef prefabRef = nativeArray3[j];
				m_PrefabEffects.TryGetBuffer(prefabRef.m_Prefab, out var bufferData);
				if (m_AddedSources.TryGetFirstValue(entity2, out var item3, out var it3))
				{
					do
					{
						if (!bufferData.IsCreated || bufferData.Length <= item3.m_EffectIndex || bufferData[item3.m_EffectIndex].m_Effect != item3.m_Prefab)
						{
							m_SearchTree.TryRemove(new SourceInfo(entity2, item3.m_EffectIndex));
						}
					}
					while (m_AddedSources.TryGetNextValue(out item3, ref it3));
					m_AddedSources.Remove(entity2);
				}
				if (!bufferData.IsCreated)
				{
					continue;
				}
				for (int k = 0; k < bufferData.Length; k++)
				{
					Effect effect2 = bufferData[k];
					if (m_PrefabEffectData.TryGetComponent(effect2.m_Effect, out var componentData2) && !componentData2.m_OwnerCulling)
					{
						if (m_EffectControlData.ShouldBeEnabled(entity2, effect2.m_Effect, checkEnabled: true, isEditorContainer: false))
						{
							QuadTreeBoundsXZ bounds2 = GetBounds(nativeArray4, nativeArray5, j, effect2);
							m_SearchTree.AddOrUpdate(new SourceInfo(entity2, k), bounds2);
							m_AddedSources.Add(entity2, new AddedSource
							{
								m_Prefab = effect2.m_Effect,
								m_EffectIndex = k
							});
						}
						else if (!flag)
						{
							m_SearchTree.TryRemove(new SourceInfo(entity2, k));
						}
					}
				}
			}
		}

		private QuadTreeBoundsXZ GetBounds(NativeArray<Transform> transforms, NativeArray<Curve> curves, int index, Effect effect)
		{
			return SearchSystem.GetBounds(transforms, curves, index, effect, ref m_PrefabLightEffectData, ref m_PrefabAudioEffectData);
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

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LightEffectData> __Game_Prefabs_LightEffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AudioEffectData> __Game_Prefabs_AudioEffectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Effect> __Game_Prefabs_Effect_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Tools.EditorContainer>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InterpolatedTransform>(isReadOnly: true);
			__Game_Prefabs_EffectData_RO_ComponentLookup = state.GetComponentLookup<EffectData>(isReadOnly: true);
			__Game_Prefabs_LightEffectData_RO_ComponentLookup = state.GetComponentLookup<LightEffectData>(isReadOnly: true);
			__Game_Prefabs_AudioEffectData_RO_ComponentLookup = state.GetComponentLookup<AudioEffectData>(isReadOnly: true);
			__Game_Prefabs_Effect_RO_BufferLookup = state.GetBufferLookup<Effect>(isReadOnly: true);
		}
	}

	private EffectFlagSystem m_EffectFlagSystem;

	private SimulationSystem m_SimulationSystem;

	private ToolSystem m_ToolSystem;

	private EffectControlData m_EffectControlData;

	private NativeQuadTree<SourceInfo, QuadTreeBoundsXZ> m_SearchTree;

	private NativeParallelMultiHashMap<Entity, AddedSource> m_AddedSources;

	private EntityQuery m_UpdatedEffectsQuery;

	private EntityQuery m_AllEffectsQuery;

	private JobHandle m_ReadDependencies;

	private JobHandle m_WriteDependencies;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EffectFlagSystem = base.World.GetOrCreateSystemManaged<EffectFlagSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_EffectControlData = new EffectControlData(this);
		m_SearchTree = new NativeQuadTree<SourceInfo, QuadTreeBoundsXZ>(1f, Allocator.Persistent);
		m_AddedSources = new NativeParallelMultiHashMap<Entity, AddedSource>(1000, Allocator.Persistent);
		m_UpdatedEffectsQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<EnabledEffect>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<EffectsUpdated>(),
				ComponentType.ReadOnly<BatchesUpdated>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Object>(),
				ComponentType.ReadOnly<Game.Events.Event>(),
				ComponentType.ReadOnly<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<EnabledEffect>(),
				ComponentType.ReadOnly<Object>(),
				ComponentType.ReadOnly<Static>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<EffectsUpdated>(),
				ComponentType.ReadOnly<BatchesUpdated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_AllEffectsQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<EnabledEffect>() },
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Object>(),
				ComponentType.ReadOnly<Game.Events.Event>(),
				ComponentType.ReadOnly<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<EnabledEffect>(),
				ComponentType.ReadOnly<Object>(),
				ComponentType.ReadOnly<Static>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_SearchTree.Dispose();
		m_AddedSources.Dispose();
		base.OnDestroy();
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool loaded = GetLoaded();
		EntityQuery query = (loaded ? m_AllEffectsQuery : m_UpdatedEffectsQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			m_EffectControlData.Update(this, m_EffectFlagSystem.GetData(), m_SimulationSystem.frameIndex, m_ToolSystem.selected);
			JobHandle dependencies;
			JobHandle jobHandle = JobChunkExtensions.Schedule(new UpdateSearchTreeJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EditorContainerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabLightEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LightEffectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAudioEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AudioEffectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabEffects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_Effect_RO_BufferLookup, ref base.CheckedStateRef),
				m_Loaded = loaded,
				m_SearchTree = GetSearchTree(readOnly: false, out dependencies),
				m_AddedSources = m_AddedSources,
				m_EffectControlData = m_EffectControlData
			}, query, JobHandle.CombineDependencies(base.Dependency, dependencies));
			AddSearchTreeWriter(jobHandle);
			base.Dependency = jobHandle;
		}
	}

	public NativeQuadTree<SourceInfo, QuadTreeBoundsXZ> GetSearchTree(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_WriteDependencies : JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies));
		return m_SearchTree;
	}

	public void AddSearchTreeReader(JobHandle jobHandle)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, jobHandle);
	}

	public void AddSearchTreeWriter(JobHandle jobHandle)
	{
		m_WriteDependencies = jobHandle;
	}

	public void PreDeserialize(Context context)
	{
		JobHandle dependencies;
		NativeQuadTree<SourceInfo, QuadTreeBoundsXZ> searchTree = GetSearchTree(readOnly: false, out dependencies);
		dependencies.Complete();
		searchTree.Clear();
		m_AddedSources.Clear();
		m_Loaded = true;
	}

	public static QuadTreeBoundsXZ GetBounds(NativeArray<Transform> transforms, NativeArray<Curve> curves, int index, Effect effect, ref ComponentLookup<LightEffectData> prefabLightEffectData, ref ComponentLookup<AudioEffectData> prefabAudioEffectData)
	{
		float3 @float = effect.m_Position;
		quaternion rotation = effect.m_Rotation;
		Transform value2;
		if (CollectionUtils.TryGet(curves, index, out var value))
		{
			@float = MathUtils.Position(value.m_Bezier, 0.5f);
		}
		else if (CollectionUtils.TryGet(transforms, index, out value2))
		{
			Transform transform = ObjectUtils.LocalToWorld(value2, @float, rotation);
			@float = transform.m_Position;
			rotation = transform.m_Rotation;
		}
		Bounds3 bounds = new Bounds3(@float - 1f, @float + 1f);
		int num = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(1f)));
		if (prefabLightEffectData.TryGetComponent(effect.m_Effect, out var componentData))
		{
			bounds |= new Bounds3(@float - componentData.m_Range, @float + componentData.m_Range);
			num = math.min(num, componentData.m_MinLod);
		}
		if (prefabAudioEffectData.TryGetComponent(effect.m_Effect, out var componentData2) && math.any(componentData2.m_SourceSize > 0f))
		{
			Bounds3 bounds2 = new Bounds3(-componentData2.m_SourceSize, componentData2.m_SourceSize);
			bounds |= ObjectUtils.CalculateBounds(@float, rotation, bounds2);
			num = math.min(num, RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(componentData2.m_SourceSize)));
		}
		return new QuadTreeBoundsXZ(bounds, (BoundsMask)0, num);
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
	public SearchSystem()
	{
	}
}
