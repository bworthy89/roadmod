using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class NotificationIconBufferSystem : GameSystemBase
{
	public struct IconData
	{
		public NativeArray<InstanceData> m_InstanceData;

		public NativeValue<Bounds3> m_IconBounds;
	}

	public struct InstanceData : IComparable<InstanceData>
	{
		public float3 m_Position;

		public float4 m_Params;

		public float m_Icon;

		public float m_Distance;

		public int CompareTo(InstanceData other)
		{
			return (int)math.sign(other.m_Distance - m_Distance);
		}
	}

	private struct HiddenPositionData
	{
		public float3 m_Position;

		public float m_Radius;

		public float m_Distance;
	}

	[BurstCompile]
	private struct NotificationIconBufferJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<Icon> m_IconType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Notifications.Animation> m_AnimationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Hidden> m_HiddenType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NotificationIconDisplayData> m_IconDisplayData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Icon> m_IconData;

		[ReadOnly]
		public ComponentLookup<Game.Notifications.Animation> m_AnimationData;

		[ReadOnly]
		public ComponentLookup<DisallowCluster> m_DisallowClusterData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[ReadOnly]
		public BufferLookup<IconAnimationElement> m_IconAnimations;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_CameraUp;

		[ReadOnly]
		public float3 m_CameraRight;

		[ReadOnly]
		public Entity m_ConfigurationEntity;

		[ReadOnly]
		public uint m_CategoryMask;

		public IconClusterSystem.ClusterData m_ClusterData;

		public NativeList<InstanceData> m_InstanceData;

		public NativeValue<Bounds3> m_IconBounds;

		public void Execute()
		{
			m_InstanceData.Clear();
			Bounds3 value = new Bounds3(float.MaxValue, float.MinValue);
			DynamicBuffer<IconAnimationElement> iconAnimations = m_IconAnimations[m_ConfigurationEntity];
			NativeParallelHashMap<Entity, HiddenPositionData> nativeParallelHashMap = default(NativeParallelHashMap<Entity, HiddenPositionData>);
			if (!m_ClusterData.isEmpty)
			{
				NativeList<IconClusterSystem.IconCluster> nativeList = new NativeList<IconClusterSystem.IconCluster>(64, Allocator.Temp);
				int index = 0;
				IconClusterSystem.IconCluster cluster;
				while (m_ClusterData.GetRoot(ref index, out cluster))
				{
					float distance = math.distance(m_CameraPosition, cluster.center);
					value |= cluster.GetBounds(distance, m_CameraUp);
					nativeList.Add(in cluster);
				}
				while (nativeList.Length != 0)
				{
					IconClusterSystem.IconCluster iconCluster = nativeList[nativeList.Length - 1];
					nativeList.RemoveAtSwapBack(nativeList.Length - 1);
					float num = math.distance(m_CameraPosition, iconCluster.center);
					int2 subClusters;
					if (iconCluster.KeepCluster(num))
					{
						float radius = iconCluster.GetRadius(num);
						NativeArray<IconClusterSystem.ClusterIcon> icons = iconCluster.GetIcons(m_ClusterData);
						bool flag;
						do
						{
							flag = false;
							IconClusterSystem.ClusterIcon value2 = icons[0];
							for (int i = 1; i < icons.Length; i++)
							{
								IconClusterSystem.ClusterIcon clusterIcon = icons[i];
								if (value2.priority == clusterIcon.priority)
								{
									float num2 = math.dot(m_CameraRight.xz, value2.order);
									float num3 = math.dot(m_CameraRight.xz, clusterIcon.order);
									if (num2 > num3)
									{
										icons[i] = value2;
										icons[i - 1] = clusterIcon;
										flag = true;
										continue;
									}
								}
								value2 = clusterIcon;
							}
						}
						while (flag);
						float3 @float = m_CameraRight * (radius * 0.5f);
						float3 center = iconCluster.center;
						if (iconCluster.isMoving)
						{
							IconClusterSystem.ClusterIcon clusterIcon2 = icons[0];
							if (m_OwnerData.HasComponent(clusterIcon2.icon))
							{
								Owner owner = m_OwnerData[clusterIcon2.icon];
								if (m_InterpolatedTransformData.HasComponent(owner.m_Owner))
								{
									PrefabRef prefabRef = m_PrefabRefData[owner.m_Owner];
									Game.Objects.Transform transform = m_InterpolatedTransformData[owner.m_Owner].ToTransform();
									Bounds3 bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, m_ObjectGeometryData[prefabRef.m_Prefab]);
									center.xz = transform.m_Position.xz;
									center.y = bounds.max.y;
								}
							}
						}
						center += @float * ((float)(icons.Length - 1) * 0.5f);
						for (int num4 = icons.Length - 1; num4 >= 0; num4--)
						{
							IconClusterSystem.ClusterIcon clusterIcon3 = icons[num4];
							float num5 = 1E-06f * (float)(num4 - (int)clusterIcon3.priority);
							if (m_HiddenData.HasComponent(clusterIcon3.icon))
							{
								if (!nativeParallelHashMap.IsCreated)
								{
									nativeParallelHashMap = new NativeParallelHashMap<Entity, HiddenPositionData>(16, Allocator.Temp);
								}
								nativeParallelHashMap.TryAdd(clusterIcon3.icon, new HiddenPositionData
								{
									m_Position = center,
									m_Radius = radius,
									m_Distance = num * (1f + num5)
								});
							}
							else
							{
								NotificationIconDisplayData notificationIconDisplayData = m_IconDisplayData[clusterIcon3.prefab];
								float t = (float)(int)clusterIcon3.priority * 0.003921569f;
								float4 float2 = new float4(math.lerp(notificationIconDisplayData.m_MinParams, notificationIconDisplayData.m_MaxParams, t), math.select(1f, new float2(0.5f, 0f), (notificationIconDisplayData.m_CategoryMask & m_CategoryMask) == 0 && !iconCluster.isTemp));
								m_InstanceData.Add(new InstanceData
								{
									m_Position = center,
									m_Params = float2,
									m_Icon = notificationIconDisplayData.m_IconIndex,
									m_Distance = num * (1f + num5)
								});
							}
							center -= @float;
						}
					}
					else if (iconCluster.GetSubClusters(out subClusters))
					{
						nativeList.Add(m_ClusterData.GetCluster(subClusters.x));
						nativeList.Add(m_ClusterData.GetCluster(subClusters.y));
					}
				}
			}
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[j];
				if (archetypeChunk.Has(ref m_HiddenType))
				{
					continue;
				}
				NativeArray<Icon> nativeArray = archetypeChunk.GetNativeArray(ref m_IconType);
				NativeArray<Game.Notifications.Animation> nativeArray2 = archetypeChunk.GetNativeArray(ref m_AnimationType);
				NativeArray<PrefabRef> nativeArray3 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Temp> nativeArray4 = archetypeChunk.GetNativeArray(ref m_TempType);
				if (nativeArray4.Length != 0)
				{
					for (int k = 0; k < nativeArray.Length; k++)
					{
						Icon icon = nativeArray[k];
						PrefabRef prefabRef2 = nativeArray3[k];
						Temp temp = nativeArray4[k];
						if (!m_IconDisplayData.IsComponentEnabled(prefabRef2.m_Prefab))
						{
							continue;
						}
						NotificationIconDisplayData notificationIconDisplayData2 = m_IconDisplayData[prefabRef2.m_Prefab];
						float num6 = math.distance(icon.m_Location, m_CameraPosition);
						if (temp.m_Original != Entity.Null)
						{
							if (nativeParallelHashMap.IsCreated && nativeParallelHashMap.TryGetValue(temp.m_Original, out var item))
							{
								Icon icon2 = m_IconData[temp.m_Original];
								if (math.distance(icon.m_Location, icon2.m_Location) < item.m_Radius * 0.1f)
								{
									icon.m_Location = item.m_Position;
									num6 = item.m_Distance;
								}
							}
							else
							{
								if (!m_DisallowClusterData.HasComponent(temp.m_Original) || m_DeletedData.HasComponent(temp.m_Original))
								{
									continue;
								}
								icon.m_Location = m_IconData[temp.m_Original].m_Location;
								num6 = math.distance(icon.m_Location, m_CameraPosition);
							}
						}
						float t2 = (float)(int)icon.m_Priority * 0.003921569f;
						float4 iconParams = new float4(math.lerp(notificationIconDisplayData2.m_MinParams, notificationIconDisplayData2.m_MaxParams, t2), math.select(1f, new float2(0.5f, 0f), (notificationIconDisplayData2.m_CategoryMask & m_CategoryMask) == 0));
						if ((temp.m_Flags & (TempFlags.Delete | TempFlags.Select)) != 0)
						{
							iconParams.x *= 1.1f;
							iconParams.y = 0f;
						}
						float num7 = IconClusterSystem.IconCluster.CalculateRadius(iconParams.x, num6);
						if (temp.m_Original != Entity.Null && m_AnimationData.HasComponent(temp.m_Original))
						{
							Animate(ref icon.m_Location, ref iconParams, num7, m_AnimationData[temp.m_Original], iconAnimations);
						}
						if ((temp.m_Flags & (TempFlags.Delete | TempFlags.Select)) != 0)
						{
							num6 *= 0.99f;
						}
						else if ((icon.m_Flags & IconFlags.OnTop) != 0)
						{
							num6 *= 0.995f;
						}
						m_InstanceData.Add(new InstanceData
						{
							m_Position = icon.m_Location,
							m_Params = iconParams,
							m_Icon = notificationIconDisplayData2.m_IconIndex,
							m_Distance = num6
						});
						value |= new Bounds3(icon.m_Location - num7, icon.m_Location + num7);
					}
					continue;
				}
				for (int l = 0; l < nativeArray.Length; l++)
				{
					Icon icon3 = nativeArray[l];
					PrefabRef prefabRef3 = nativeArray3[l];
					if (!m_IconDisplayData.IsComponentEnabled(prefabRef3.m_Prefab))
					{
						continue;
					}
					NotificationIconDisplayData notificationIconDisplayData3 = m_IconDisplayData[prefabRef3.m_Prefab];
					float t3 = (float)(int)icon3.m_Priority * 0.003921569f;
					float4 iconParams2 = new float4(math.lerp(notificationIconDisplayData3.m_MinParams, notificationIconDisplayData3.m_MaxParams, t3), math.select(1f, new float2(0.5f, 0f), (notificationIconDisplayData3.m_CategoryMask & m_CategoryMask) == 0));
					float num8 = math.distance(icon3.m_Location, m_CameraPosition);
					float num9 = IconClusterSystem.IconCluster.CalculateRadius(iconParams2.x, num8);
					if (nativeArray2.Length != 0)
					{
						Game.Notifications.Animation animation = nativeArray2[l];
						if (animation.m_Timer <= 0f)
						{
							continue;
						}
						Animate(ref icon3.m_Location, ref iconParams2, num9, animation, iconAnimations);
					}
					if ((icon3.m_Flags & IconFlags.OnTop) != 0)
					{
						num8 *= 0.995f;
					}
					m_InstanceData.Add(new InstanceData
					{
						m_Position = icon3.m_Location,
						m_Params = iconParams2,
						m_Icon = notificationIconDisplayData3.m_IconIndex,
						m_Distance = num8
					});
					value |= new Bounds3(icon3.m_Location - num9, icon3.m_Location + num9);
				}
			}
			m_IconBounds.value = value;
		}

		private void Animate(ref float3 location, ref float4 iconParams, float radius, Game.Notifications.Animation animation, DynamicBuffer<IconAnimationElement> iconAnimations)
		{
			float3 @float = iconAnimations[(int)animation.m_Type].m_AnimationCurve.Evaluate(animation.m_Timer / animation.m_Duration);
			iconParams.xz *= @float.xy;
			location += m_CameraUp * (radius * @float.z);
		}
	}

	[BurstCompile]
	private struct NotificationIconSortJob : IJob
	{
		public NativeList<InstanceData> m_InstanceData;

		public void Execute()
		{
			if (m_InstanceData.Length >= 2)
			{
				m_InstanceData.Sort();
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Icon> __Game_Notifications_Icon_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Notifications.Animation> __Game_Notifications_Animation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Hidden> __Game_Tools_Hidden_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NotificationIconDisplayData> __Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Icon> __Game_Notifications_Icon_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Notifications.Animation> __Game_Notifications_Animation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DisallowCluster> __Game_Notifications_DisallowCluster_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<IconAnimationElement> __Game_Prefabs_IconAnimationElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Notifications_Icon_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Icon>(isReadOnly: true);
			__Game_Notifications_Animation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Notifications.Animation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Hidden>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup = state.GetComponentLookup<NotificationIconDisplayData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentLookup = state.GetComponentLookup<Icon>(isReadOnly: true);
			__Game_Notifications_Animation_RO_ComponentLookup = state.GetComponentLookup<Game.Notifications.Animation>(isReadOnly: true);
			__Game_Notifications_DisallowCluster_RO_ComponentLookup = state.GetComponentLookup<DisallowCluster>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>(isReadOnly: true);
			__Game_Prefabs_IconAnimationElement_RO_BufferLookup = state.GetBufferLookup<IconAnimationElement>(isReadOnly: true);
		}
	}

	private EntityQuery m_IconQuery;

	private EntityQuery m_ConfigurationQuery;

	private IconClusterSystem m_IconClusterSystem;

	private ToolSystem m_ToolSystem;

	private PrefabSystem m_PrefabSystem;

	private NativeList<InstanceData> m_InstanceData;

	private NativeValue<Bounds3> m_IconBounds;

	private JobHandle m_InstanceDataDeps;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_IconClusterSystem = base.World.GetOrCreateSystemManaged<IconClusterSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_IconQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Icon>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<DisallowCluster>(),
				ComponentType.ReadOnly<Game.Notifications.Animation>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Hidden>()
			}
		});
		m_ConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<IconConfigurationData>());
	}

	[Preserve]
	protected override void OnDestroy()
	{
		if (m_InstanceData.IsCreated)
		{
			m_InstanceDataDeps.Complete();
			m_InstanceData.Dispose();
			m_IconBounds.Dispose();
		}
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		Camera main = Camera.main;
		if (!(main == null) && !m_ConfigurationQuery.IsEmptyIgnoreFilter)
		{
			if (!m_InstanceData.IsCreated)
			{
				m_InstanceData = new NativeList<InstanceData>(64, Allocator.Persistent);
				m_IconBounds = new NativeValue<Bounds3>(Allocator.Persistent);
			}
			UnityEngine.Transform transform = main.transform;
			uint categoryMask = uint.MaxValue;
			if (m_ToolSystem.activeInfoview != null)
			{
				categoryMask = m_PrefabSystem.GetComponentData<InfoviewData>(m_ToolSystem.activeInfoview).m_NotificationMask;
				categoryMask |= 0x80000000u;
			}
			m_InstanceDataDeps.Complete();
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> chunks = m_IconQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			JobHandle dependencies;
			NotificationIconBufferJob jobData = new NotificationIconBufferJob
			{
				m_IconType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AnimationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_Animation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HiddenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IconDisplayData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AnimationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Animation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DisallowClusterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_DisallowCluster_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IconAnimations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_IconAnimationElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_Chunks = chunks,
				m_CameraPosition = transform.position,
				m_CameraUp = transform.up,
				m_CameraRight = transform.right,
				m_ConfigurationEntity = m_ConfigurationQuery.GetSingletonEntity(),
				m_CategoryMask = categoryMask,
				m_ClusterData = m_IconClusterSystem.GetIconClusterData(readOnly: false, out dependencies),
				m_InstanceData = m_InstanceData,
				m_IconBounds = m_IconBounds
			};
			NotificationIconSortJob jobData2 = new NotificationIconSortJob
			{
				m_InstanceData = m_InstanceData
			};
			JobHandle jobHandle = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle, dependencies));
			JobHandle instanceDataDeps = IJobExtensions.Schedule(jobData2, jobHandle);
			chunks.Dispose(jobHandle);
			m_IconClusterSystem.AddIconClusterWriter(jobHandle);
			m_InstanceDataDeps = instanceDataDeps;
			base.Dependency = jobHandle;
		}
	}

	public IconData GetIconData()
	{
		m_InstanceDataDeps.Complete();
		m_InstanceDataDeps = default(JobHandle);
		if (m_InstanceData.IsCreated)
		{
			return new IconData
			{
				m_InstanceData = m_InstanceData.AsArray(),
				m_IconBounds = m_IconBounds
			};
		}
		return default(IconData);
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
	public NotificationIconBufferSystem()
	{
	}
}
