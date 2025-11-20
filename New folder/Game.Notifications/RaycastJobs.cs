using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Notifications;

public static class RaycastJobs
{
	[BurstCompile]
	public struct RaycastIconsJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public float3 m_CameraUp;

		[ReadOnly]
		public float3 m_CameraRight;

		[ReadOnly]
		public IconClusterSystem.ClusterData m_ClusterData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_IconChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Icon> m_IconType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Static> m_StaticData;

		[ReadOnly]
		public ComponentLookup<Object> m_ObjectData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Placeholder> m_PlaceholderData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<CullingInfo> m_CullingInfoData;

		[ReadOnly]
		public ComponentLookup<NotificationIconDisplayData> m_IconDisplayData;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(int index)
		{
			RaycastInput input = m_Input[index];
			if ((input.m_TypeMask & (TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Icons)) != TypeMask.None)
			{
				if (!m_ClusterData.isEmpty)
				{
					CheckClusters(index, input);
				}
				if (m_IconChunks.Length != 0)
				{
					CheckChunks(index, input);
				}
			}
		}

		private void CheckClusters(int raycastIndex, RaycastInput input)
		{
			IconLayerMask iconLayerMask = input.m_IconLayerMask;
			if ((input.m_TypeMask & TypeMask.StaticObjects) != TypeMask.None)
			{
				iconLayerMask |= IconLayerMask.Marker;
			}
			float3 a = input.m_Line.a;
			NativeList<IconClusterSystem.IconCluster> nativeList = new NativeList<IconClusterSystem.IconCluster>(64, Allocator.Temp);
			int index = 0;
			IconClusterSystem.IconCluster cluster;
			while (m_ClusterData.GetRoot(ref index, out cluster))
			{
				if ((NotificationsUtils.GetIconLayerMask(cluster.layer) & iconLayerMask) != IconLayerMask.None)
				{
					nativeList.Add(in cluster);
				}
			}
			while (nativeList.Length != 0)
			{
				IconClusterSystem.IconCluster iconCluster = nativeList[nativeList.Length - 1];
				nativeList.RemoveAtSwapBack(nativeList.Length - 1);
				float distance = math.distance(a, iconCluster.center);
				int2 subClusters;
				float2 t2;
				if (iconCluster.KeepCluster(distance))
				{
					float radius = iconCluster.GetRadius(distance);
					NativeArray<IconClusterSystem.ClusterIcon> icons = iconCluster.GetIcons(m_ClusterData);
					float3 @float = m_CameraRight * (radius * 0.5f);
					float3 float2 = iconCluster.center + @float * ((float)(icons.Length - 1) * 0.5f);
					float3 float3 = m_CameraUp * radius;
					RaycastResult result = default(RaycastResult);
					float num = radius;
					for (int num2 = icons.Length - 1; num2 >= 0; num2--)
					{
						IconClusterSystem.ClusterIcon clusterIcon = icons[num2];
						if (!m_TempData.HasComponent(clusterIcon.icon))
						{
							float t;
							float num3 = MathUtils.Distance(input.m_Line, float2 + float3, out t);
							if (num3 < num)
							{
								num = num3;
								result.m_Owner = clusterIcon.icon;
								result.m_Hit.m_HitEntity = clusterIcon.icon;
								result.m_Hit.m_Position = float2;
								result.m_Hit.m_HitPosition = MathUtils.Position(input.m_Line, t);
								result.m_Hit.m_NormalizedDistance = t - 100f / math.max(1f, MathUtils.Length(input.m_Line));
							}
						}
						float2 -= @float;
					}
					if (result.m_Owner != Entity.Null)
					{
						ValidateResult(raycastIndex, input, result, iconCluster.layer);
					}
				}
				else if (iconCluster.GetSubClusters(out subClusters) && MathUtils.Intersect(iconCluster.GetBounds(distance, m_CameraUp), input.m_Line, out t2))
				{
					nativeList.Add(m_ClusterData.GetCluster(subClusters.x));
					nativeList.Add(m_ClusterData.GetCluster(subClusters.y));
				}
			}
			nativeList.Dispose();
		}

		private void CheckChunks(int raycastIndex, RaycastInput input)
		{
			IconLayerMask iconLayerMask = input.m_IconLayerMask;
			if ((input.m_TypeMask & (TypeMask.StaticObjects | TypeMask.MovingObjects)) != TypeMask.None)
			{
				iconLayerMask |= IconLayerMask.Marker;
			}
			float3 a = input.m_Line.a;
			for (int i = 0; i < m_IconChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_IconChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Icon> nativeArray2 = archetypeChunk.GetNativeArray(ref m_IconType);
				NativeArray<PrefabRef> nativeArray3 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Icon icon = nativeArray2[j];
					if ((NotificationsUtils.GetIconLayerMask(icon.m_ClusterLayer) & iconLayerMask) == 0)
					{
						continue;
					}
					PrefabRef prefabRef = nativeArray3[j];
					if (!m_IconDisplayData.IsComponentEnabled(prefabRef.m_Prefab))
					{
						continue;
					}
					NotificationIconDisplayData notificationIconDisplayData = m_IconDisplayData[prefabRef.m_Prefab];
					float t = (float)(int)icon.m_Priority * 0.003921569f;
					float2 @float = math.lerp(notificationIconDisplayData.m_MinParams, notificationIconDisplayData.m_MaxParams, t);
					float num = IconClusterSystem.IconCluster.CalculateRadius(distance: math.distance(icon.m_Location, a), radius: @float.x);
					float3 float2 = m_CameraUp * num;
					if (MathUtils.Distance(input.m_Line, icon.m_Location + float2, out var t2) < num)
					{
						RaycastResult result = default(RaycastResult);
						result.m_Owner = nativeArray[j];
						result.m_Hit.m_HitEntity = result.m_Owner;
						result.m_Hit.m_Position = icon.m_Location;
						result.m_Hit.m_HitPosition = MathUtils.Position(input.m_Line, t2);
						result.m_Hit.m_NormalizedDistance = t2 - 100f / math.max(1f, MathUtils.Length(input.m_Line));
						if ((icon.m_Flags & IconFlags.OnTop) != 0)
						{
							result.m_Hit.m_NormalizedDistance *= 0.999f;
						}
						ValidateResult(raycastIndex, input, result, icon.m_ClusterLayer);
					}
				}
			}
		}

		private void ValidateResult(int raycastIndex, RaycastInput input, RaycastResult result, IconClusterLayer layer)
		{
			if ((input.m_IconLayerMask & NotificationsUtils.GetIconLayerMask(layer)) != IconLayerMask.None)
			{
				m_Results.Accumulate(raycastIndex, result);
			}
			while (m_OwnerData.HasComponent(result.m_Owner))
			{
				result.m_Owner = m_OwnerData[result.m_Owner].m_Owner;
				if (!m_ObjectData.HasComponent(result.m_Owner))
				{
					continue;
				}
				if (m_StaticData.HasComponent(result.m_Owner))
				{
					if ((input.m_TypeMask & TypeMask.StaticObjects) == 0)
					{
						break;
					}
					if (CheckPlaceholder(input, ref result.m_Owner))
					{
						result.m_Hit.m_Position = m_TransformData[result.m_Owner].m_Position;
						m_Results.Accumulate(raycastIndex, result);
						break;
					}
					continue;
				}
				if ((input.m_TypeMask & TypeMask.MovingObjects) != TypeMask.None)
				{
					result.m_Hit.m_Position = MathUtils.Center(m_CullingInfoData[result.m_Owner].m_Bounds);
					m_Results.Accumulate(raycastIndex, result);
				}
				break;
			}
		}

		private bool CheckPlaceholder(RaycastInput input, ref Entity entity)
		{
			if ((input.m_Flags & RaycastFlags.Placeholders) != 0)
			{
				return true;
			}
			if (m_PlaceholderData.HasComponent(entity))
			{
				if (m_AttachmentData.HasComponent(entity))
				{
					Attachment attachment = m_AttachmentData[entity];
					if (m_TransformData.HasComponent(attachment.m_Attached))
					{
						entity = attachment.m_Attached;
						return true;
					}
				}
				return false;
			}
			return true;
		}
	}
}
