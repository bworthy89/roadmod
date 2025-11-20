using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class LocalEffectSystem : GameSystemBase
{
	public struct EffectItem : IEquatable<EffectItem>
	{
		public Entity m_Provider;

		public LocalModifierType m_Type;

		public EffectItem(Entity provider, LocalModifierType type)
		{
			m_Provider = provider;
			m_Type = type;
		}

		public bool Equals(EffectItem other)
		{
			if (m_Provider.Equals(other.m_Provider))
			{
				return m_Type == other.m_Type;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return m_Provider.GetHashCode() ^ (int)m_Type;
		}
	}

	public struct EffectBounds : IEquatable<EffectBounds>, IBounds2<EffectBounds>
	{
		public Bounds2 m_Bounds;

		public uint m_TypeMask;

		public float2 m_Delta;

		public EffectBounds(Bounds2 bounds, uint typeMask, float2 delta)
		{
			m_Bounds = bounds;
			m_TypeMask = typeMask;
			m_Delta = delta;
		}

		public bool Equals(EffectBounds other)
		{
			if (m_Bounds.Equals(other.m_Bounds) && m_TypeMask == other.m_TypeMask)
			{
				return m_Delta.Equals(other.m_Delta);
			}
			return false;
		}

		public void Reset()
		{
			m_Bounds.Reset();
			m_TypeMask = 0u;
			m_Delta = default(float2);
		}

		public float2 Center()
		{
			return m_Bounds.Center();
		}

		public float2 Size()
		{
			return m_Bounds.Size();
		}

		public EffectBounds Merge(EffectBounds other)
		{
			return new EffectBounds
			{
				m_Bounds = m_Bounds.Merge(other.m_Bounds),
				m_TypeMask = (m_TypeMask | other.m_TypeMask)
			};
		}

		public bool Intersect(EffectBounds other)
		{
			if (m_Bounds.Intersect(other.m_Bounds))
			{
				return (m_TypeMask & other.m_TypeMask) != 0;
			}
			return false;
		}
	}

	public struct ReadData
	{
		private struct Iterator : INativeQuadTreeIterator<EffectItem, EffectBounds>, IUnsafeQuadTreeIterator<EffectItem, EffectBounds>
		{
			public float2 m_Position;

			public float2 m_Delta;

			public uint m_TypeMask;

			public bool Intersect(EffectBounds bounds)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Position))
				{
					return (bounds.m_TypeMask & m_TypeMask) != 0;
				}
				return false;
			}

			public void Iterate(EffectBounds bounds, EffectItem entity2)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Position) && (bounds.m_TypeMask & m_TypeMask) != 0)
				{
					float2 x = MathUtils.Center(bounds.m_Bounds);
					float num = (bounds.m_Bounds.max.x - bounds.m_Bounds.min.x) * 0.5f;
					float num2 = 1f - math.distancesq(x, m_Position) / (num * num);
					if (num2 > 0f)
					{
						float2 @float = bounds.m_Delta * num2;
						m_Delta.y *= 1f + @float.y;
						m_Delta += @float;
					}
				}
			}
		}

		private NativeQuadTree<EffectItem, EffectBounds> m_SearchTree;

		public ReadData(NativeQuadTree<EffectItem, EffectBounds> searchTree)
		{
			m_SearchTree = searchTree;
		}

		public void ApplyModifier(ref float value, float3 position, LocalModifierType type)
		{
			Iterator iterator = new Iterator
			{
				m_Position = position.xz,
				m_TypeMask = (uint)(1 << (int)type)
			};
			m_SearchTree.Iterate(ref iterator);
			value += iterator.m_Delta.x;
			value += value * iterator.m_Delta.y;
		}
	}

	[BurstCompile]
	private struct UpdateLocalEffectsJob : IJob
	{
		[ReadOnly]
		public bool m_Loaded;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		public NativeQuadTree<EffectItem, EffectBounds> m_SearchTree;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> m_AbandonedType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<Signature> m_SignatureType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<LocalModifierData> m_LocalModifierData;

		public void Execute()
		{
			NativeList<LocalModifierData> tempModifierList = new NativeList<LocalModifierData>(10, Allocator.Temp);
			DynamicBuffer<LocalModifierData> dynamicBuffer = m_LocalModifierData[m_BuildingConfigurationData.m_AbandonedBuildingLocalEffects];
			DynamicBuffer<LocalModifierData> dynamicBuffer2 = m_LocalModifierData[m_BuildingConfigurationData.m_AbandonedCollapsedBuildingLocalEffects];
			if (m_Loaded)
			{
				m_SearchTree.Clear();
			}
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<InstalledUpgrade> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_InstalledUpgradeType);
				BufferAccessor<Efficiency> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_EfficiencyType);
				NativeArray<Transform> nativeArray3 = archetypeChunk.GetNativeArray(ref m_TransformType);
				bool flag = archetypeChunk.Has(ref m_SignatureType);
				bool flag2 = archetypeChunk.Has(ref m_AbandonedType);
				bool flag3 = archetypeChunk.Has(ref m_DestroyedType);
				bool flag4 = archetypeChunk.Has(ref m_DeletedType);
				bool created = m_Loaded || archetypeChunk.Has(ref m_CreatedType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity = nativeArray[j];
					Transform transform = nativeArray3[j];
					PrefabRef prefabRef = nativeArray2[j];
					float efficiency = ((bufferAccessor2.Length == 0 || flag) ? 1f : BuildingUtils.GetEfficiency(bufferAccessor2[j]));
					InitializeTempList(tempModifierList, prefabRef.m_Prefab);
					if (bufferAccessor.Length != 0)
					{
						AddToTempList(tempModifierList, bufferAccessor[j]);
					}
					for (int k = 0; k < tempModifierList.Length; k++)
					{
						LocalModifierData localModifier = tempModifierList[k];
						UpdateEffect(entity, localModifier, flag4 || flag3, created, transform, efficiency);
					}
					if (flag2)
					{
						DynamicBuffer<LocalModifierData> dynamicBuffer3 = (flag3 ? dynamicBuffer2 : dynamicBuffer);
						for (int l = 0; l < dynamicBuffer3.Length; l++)
						{
							LocalModifierData localModifier2 = dynamicBuffer3[l];
							UpdateEffect(entity, localModifier2, flag4, created, transform, 1f);
						}
					}
				}
			}
			tempModifierList.Dispose();
		}

		private void UpdateEffect(Entity entity, LocalModifierData localModifier, bool deleted, bool created, Transform transform, float efficiency)
		{
			EffectBounds effectBounds2;
			if (deleted)
			{
				m_SearchTree.TryRemove(new EffectItem(entity, localModifier.m_Type));
			}
			else if (created)
			{
				if (GetEffectBounds(transform, efficiency, localModifier, out var effectBounds))
				{
					m_SearchTree.Add(new EffectItem(entity, localModifier.m_Type), effectBounds);
				}
			}
			else if (GetEffectBounds(transform, efficiency, localModifier, out effectBounds2))
			{
				m_SearchTree.AddOrUpdate(new EffectItem(entity, localModifier.m_Type), effectBounds2);
			}
			else
			{
				m_SearchTree.TryRemove(new EffectItem(entity, localModifier.m_Type));
			}
		}

		private void InitializeTempList(NativeList<LocalModifierData> tempModifierList, Entity prefab)
		{
			if (m_LocalModifierData.TryGetBuffer(prefab, out var bufferData))
			{
				LocalEffectSystem.InitializeTempList(tempModifierList, bufferData);
			}
			else
			{
				tempModifierList.Clear();
			}
		}

		private void AddToTempList(NativeList<LocalModifierData> tempModifierList, DynamicBuffer<InstalledUpgrade> upgrades)
		{
			for (int i = 0; i < upgrades.Length; i++)
			{
				InstalledUpgrade installedUpgrade = upgrades[i];
				if (m_LocalModifierData.TryGetBuffer(m_PrefabRefData[installedUpgrade.m_Upgrade].m_Prefab, out var bufferData))
				{
					LocalEffectSystem.AddToTempList(tempModifierList, bufferData, BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> __Game_Buildings_Abandoned_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Signature> __Game_Buildings_Signature_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LocalModifierData> __Game_Prefabs_LocalModifierData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Abandoned>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_Signature_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Signature>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_LocalModifierData_RO_BufferLookup = state.GetBufferLookup<LocalModifierData>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdatedProvidersQuery;

	private EntityQuery m_AllProvidersQuery;

	private NativeQuadTree<EffectItem, EffectBounds> m_SearchTree;

	private JobHandle m_ReadDependencies;

	private JobHandle m_WriteDependencies;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_547405150_0;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdatedProvidersQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<LocalEffectProvider>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Abandoned>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_AllProvidersQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<LocalEffectProvider>() },
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Abandoned>() },
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_SearchTree = new NativeQuadTree<EffectItem, EffectBounds>(1f, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_SearchTree.Dispose();
		base.OnDestroy();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
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
		EntityQuery entityQuery = (loaded ? m_AllProvidersQuery : m_UpdatedProvidersQuery);
		if (!entityQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> chunks = entityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			JobHandle dependencies;
			JobHandle jobHandle = IJobExtensions.Schedule(new UpdateLocalEffectsJob
			{
				m_Loaded = loaded,
				m_Chunks = chunks,
				m_SearchTree = GetSearchTree(readOnly: false, out dependencies),
				m_BuildingConfigurationData = __query_547405150_0.GetSingleton<BuildingConfigurationData>(),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AbandonedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_SignatureType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Signature_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LocalModifierData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LocalModifierData_RO_BufferLookup, ref base.CheckedStateRef)
			}, JobHandle.CombineDependencies(base.Dependency, outJobHandle, dependencies));
			chunks.Dispose(jobHandle);
			AddLocalEffectWriter(jobHandle);
			base.Dependency = jobHandle;
		}
	}

	public ReadData GetReadData(out JobHandle dependencies)
	{
		dependencies = m_WriteDependencies;
		return new ReadData(m_SearchTree);
	}

	public NativeQuadTree<EffectItem, EffectBounds> GetSearchTree(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_WriteDependencies : JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies));
		return m_SearchTree;
	}

	public void AddLocalEffectReader(JobHandle jobHandle)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, jobHandle);
	}

	public void AddLocalEffectWriter(JobHandle jobHandle)
	{
		m_WriteDependencies = JobHandle.CombineDependencies(m_WriteDependencies, jobHandle);
	}

	public static void InitializeTempList(NativeList<LocalModifierData> tempModifierList, DynamicBuffer<LocalModifierData> localModifiers)
	{
		tempModifierList.Clear();
		tempModifierList.AddRange(localModifiers.AsNativeArray());
	}

	public static void AddToTempList(NativeList<LocalModifierData> tempModifierList, DynamicBuffer<LocalModifierData> localModifiers, bool disabled)
	{
		for (int i = 0; i < localModifiers.Length; i++)
		{
			LocalModifierData value = localModifiers[i];
			if (disabled)
			{
				value.m_Delta = default(Bounds1);
				value.m_Radius = default(Bounds1);
			}
			int num = 0;
			while (true)
			{
				if (num < tempModifierList.Length)
				{
					LocalModifierData value2 = tempModifierList[num];
					if (value2.m_Type == value.m_Type)
					{
						if (value2.m_Mode != value.m_Mode)
						{
							throw new Exception($"Modifier mode mismatch (type: {value.m_Type})");
						}
						value2.m_Delta.min += value.m_Delta.min;
						value2.m_Delta.max += value.m_Delta.max;
						switch (value2.m_RadiusCombineMode)
						{
						case ModifierRadiusCombineMode.Additive:
							value2.m_Radius.min += value.m_Radius.min;
							value2.m_Radius.max += value.m_Radius.max;
							break;
						case ModifierRadiusCombineMode.Maximal:
							value2.m_Radius.min = math.max(value2.m_Radius.min, value.m_Radius.min);
							value2.m_Radius.max = math.max(value2.m_Radius.max, value.m_Radius.max);
							break;
						}
						tempModifierList[num] = value2;
						break;
					}
					num++;
					continue;
				}
				tempModifierList.Add(in value);
				break;
			}
		}
	}

	public static bool GetEffectBounds(Transform transform, LocalModifierData localModifier, out EffectBounds effectBounds)
	{
		return GetEffectBounds(transform, 1f, localModifier, out effectBounds);
	}

	public static bool GetEffectBounds(Transform transform, float efficiency, LocalModifierData localModifier, out EffectBounds effectBounds)
	{
		float num = localModifier.m_Radius.max;
		float num2 = localModifier.m_Delta.max;
		if (efficiency != 1f)
		{
			efficiency = math.sqrt(efficiency);
			num = math.lerp(localModifier.m_Radius.min, localModifier.m_Radius.max, math.sqrt(efficiency));
			num2 = math.lerp(localModifier.m_Delta.min, localModifier.m_Delta.max, efficiency);
		}
		Bounds2 bounds = new Bounds2(transform.m_Position.xz - num, transform.m_Position.xz + num);
		uint typeMask = (uint)(1 << (int)localModifier.m_Type);
		num2 = math.select(num2, 1f / math.max(0.001f, 1f + num2) - 1f, localModifier.m_Mode == ModifierValueMode.InverseRelative);
		float2 delta = math.select(new float2(0f, num2), new float2(num2, 0f), localModifier.m_Mode == ModifierValueMode.Absolute);
		effectBounds = new EffectBounds(bounds, typeMask, delta);
		if (num >= 1f)
		{
			return num2 != 0f;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_547405150_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public LocalEffectSystem()
	{
	}
}
