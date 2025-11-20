using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class AttachSystem : GameSystemBase
{
	public struct RemovedAttached
	{
		public Entity m_Entity;

		public Entity m_Parent;
	}

	[BurstCompile]
	private struct FindAttachedParentsJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_AttachedChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_ParentChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Elevation> m_ElevationType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceholderBuildingData> m_PlaceholderBuildingData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Attached> m_AttachedData;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_AttachedChunks[index];
			if (archetypeChunk.Has(ref m_DeletedType))
			{
				return;
			}
			NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
			NativeArray<Transform> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TransformType);
			NativeArray<Owner> nativeArray3 = archetypeChunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Temp> nativeArray5 = archetypeChunk.GetNativeArray(ref m_TempType);
			bool flag = nativeArray5.Length != 0;
			bool flag2 = nativeArray3.Length != 0;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Attached attached = m_AttachedData[entity];
				if (attached.m_Parent != Entity.Null)
				{
					if (m_PlaceholderBuildingData.HasComponent(attached.m_Parent))
					{
						attached.m_Parent = FindParent(attached.m_Parent, nativeArray2[i], flag);
						m_AttachedData[entity] = attached;
					}
					continue;
				}
				Attached componentData = default(Attached);
				bool relocating = false;
				if (flag)
				{
					Temp temp = nativeArray5[i];
					relocating = (temp.m_Flags & TempFlags.Modify) != 0;
					if (m_AttachedData.TryGetComponent(temp.m_Original, out componentData) && !m_HiddenData.HasComponent(componentData.m_Parent))
					{
						attached.m_Parent = componentData.m_Parent;
						attached.m_CurvePosition = componentData.m_CurvePosition;
						m_AttachedData[entity] = attached;
						continue;
					}
				}
				PrefabRef prefabRef = nativeArray4[i];
				PlaceableObjectData placeableObjectData = default(PlaceableObjectData);
				if (m_PlaceableObjectData.HasComponent(prefabRef.m_Prefab))
				{
					placeableObjectData = m_PlaceableObjectData[prefabRef.m_Prefab];
					if ((placeableObjectData.m_Flags & (PlacementFlags.RoadSide | PlacementFlags.OwnerSide | PlacementFlags.Shoreline | PlacementFlags.Floating)) != PlacementFlags.None)
					{
						continue;
					}
				}
				if (!m_ObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					continue;
				}
				if ((componentData2.m_Flags & GeometryFlags.OptionalAttach) != GeometryFlags.None)
				{
					componentData = default(Attached);
				}
				Entity entity2 = entity;
				Entity ignoreParent = Entity.Null;
				if (flag2)
				{
					ignoreParent = nativeArray3[i].m_Owner;
					if (!m_BuildingData.HasComponent(entity) && (componentData2.m_Flags & GeometryFlags.OptionalAttach) == 0)
					{
						entity2 = nativeArray3[i].m_Owner;
						while (m_OwnerData.HasComponent(entity2) && !m_BuildingData.HasComponent(entity2))
						{
							entity2 = m_OwnerData[entity2].m_Owner;
						}
					}
				}
				Transform transform = nativeArray2[i];
				if (FindParent(ref attached, componentData, transform, flag, relocating, entity2, ignoreParent, componentData2, placeableObjectData.m_Flags))
				{
					m_AttachedData[entity] = attached;
				}
			}
		}

		private Entity FindParent(Entity prefab, Transform transform, bool isTemp)
		{
			float num = float.MaxValue;
			Entity result = Entity.Null;
			for (int i = 0; i < m_ParentChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_ParentChunks[i];
				if (isTemp != archetypeChunk.Has(ref m_TempType))
				{
					continue;
				}
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Transform> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TransformType);
				NativeArray<PrefabRef> nativeArray3 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (!(nativeArray3[j].m_Prefab != prefab))
					{
						Transform transform2 = nativeArray2[j];
						float num2 = math.distance(transform.m_Position, transform2.m_Position);
						if (num2 < num)
						{
							num = num2;
							result = nativeArray[j];
						}
					}
				}
			}
			return result;
		}

		private bool FindParent(ref Attached attached, Attached originalAttached, Transform transform, bool isTemp, bool relocating, Entity topLevelOwner, Entity ignoreParent, ObjectGeometryData objectGeometryData, PlacementFlags placementFlags)
		{
			Bounds3 bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, objectGeometryData);
			float2 x = new float2(0f, 10f);
			bool result = false;
			Bounds3 bounds2 = default(Bounds3);
			for (int i = 0; i < m_ParentChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_ParentChunks[i];
				NativeArray<Temp> nativeArray = archetypeChunk.GetNativeArray(ref m_TempType);
				bool flag = nativeArray.Length != 0;
				if (isTemp != flag)
				{
					continue;
				}
				if ((placementFlags & PlacementFlags.RoadNode) != PlacementFlags.None)
				{
					NativeArray<Node> nativeArray2 = archetypeChunk.GetNativeArray(ref m_NodeType);
					if (nativeArray2.Length != 0)
					{
						NativeArray<Entity> nativeArray3 = archetypeChunk.GetNativeArray(m_EntityType);
						NativeArray<Game.Net.Elevation> nativeArray4 = archetypeChunk.GetNativeArray(ref m_ElevationType);
						NativeArray<PrefabRef> nativeArray5 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
						for (int j = 0; j < nativeArray2.Length; j++)
						{
							Entity entity = nativeArray3[j];
							if (entity == ignoreParent || (CollectionUtils.TryGet(nativeArray, j, out var value) && (value.m_Flags & TempFlags.Delete) != 0))
							{
								continue;
							}
							PrefabRef prefabRef = nativeArray5[j];
							if (!m_NetGeometryData.HasComponent(prefabRef.m_Prefab))
							{
								continue;
							}
							NetGeometryData netGeometryData = m_NetGeometryData[prefabRef.m_Prefab];
							int num = 0;
							EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, entity, m_Edges, m_EdgeData, m_TempData, m_HiddenData);
							while (true)
							{
								if (edgeIterator.GetNext(out var value2))
								{
									Entity edge = value2.m_Edge;
									if (edge == ignoreParent)
									{
										Edge edge2 = m_EdgeData[edge];
										if (edge2.m_Start == entity || edge2.m_End == entity)
										{
											break;
										}
									}
									PrefabRef prefabRef2 = m_PrefabRefData[edge];
									if ((m_NetGeometryData[prefabRef2.m_Prefab].m_MergeLayers & netGeometryData.m_MergeLayers) != Layer.None)
									{
										num++;
									}
									continue;
								}
								if ((placementFlags & (PlacementFlags.RoadNode | PlacementFlags.RoadEdge)) != PlacementFlags.RoadNode && num < 3)
								{
									break;
								}
								Node node = nativeArray2[j];
								Entity entity2 = entity;
								if ((objectGeometryData.m_Flags & GeometryFlags.OptionalAttach) == 0)
								{
									while (m_OwnerData.HasComponent(entity2) && !m_BuildingData.HasComponent(entity2))
									{
										entity2 = m_OwnerData[entity2].m_Owner;
									}
								}
								float num2 = math.select(x.x, x.y, topLevelOwner == entity2);
								float num3 = netGeometryData.m_DefaultWidth * 0.5f;
								bounds2.min = node.m_Position - new float3(num3, 0f - netGeometryData.m_DefaultHeightRange.min, num3);
								bounds2.max = node.m_Position + new float3(num3, netGeometryData.m_DefaultHeightRange.max, num3);
								if (num2 > 0f)
								{
									bounds2.min -= num2;
									bounds2.max += num2;
								}
								if ((objectGeometryData.m_Flags & GeometryFlags.BaseCollision) != GeometryFlags.None)
								{
									bounds.y = bounds2.y;
									transform.m_Position.y = node.m_Position.y;
									if (nativeArray4.Length != 0)
									{
										float num4 = math.csum(nativeArray4[j].m_Elevation) * 0.5f;
										bounds.y -= num4;
										transform.m_Position.y -= num4;
									}
								}
								if (MathUtils.Intersect(bounds2, bounds))
								{
									float num5 = math.distance(node.m_Position, transform.m_Position) - num3;
									num5 += math.clamp(num5, 0f - num3, 0f);
									if (num5 < num2)
									{
										x = math.min(x, num5);
										attached.m_Parent = entity;
										attached.m_CurvePosition = 0f;
										result = true;
									}
								}
								if (flag && !relocating && originalAttached.m_Parent != Entity.Null && attached.m_Parent == Entity.Null && value.m_Original == originalAttached.m_Parent)
								{
									attached.m_Parent = entity;
									attached.m_CurvePosition = 0f;
									result = true;
								}
								break;
							}
						}
					}
				}
				if ((placementFlags & (PlacementFlags.RoadNode | PlacementFlags.RoadEdge)) == PlacementFlags.RoadNode)
				{
					continue;
				}
				NativeArray<Edge> nativeArray6 = archetypeChunk.GetNativeArray(ref m_EdgeType);
				if (nativeArray6.Length == 0)
				{
					continue;
				}
				NativeArray<Entity> nativeArray7 = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Curve> nativeArray8 = archetypeChunk.GetNativeArray(ref m_CurveType);
				NativeArray<Game.Net.Elevation> nativeArray9 = archetypeChunk.GetNativeArray(ref m_ElevationType);
				NativeArray<PrefabRef> nativeArray10 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				for (int k = 0; k < nativeArray6.Length; k++)
				{
					Entity entity3 = nativeArray7[k];
					if (entity3 == ignoreParent)
					{
						continue;
					}
					Edge edge3 = nativeArray6[k];
					if (edge3.m_Start == ignoreParent || edge3.m_End == ignoreParent || (CollectionUtils.TryGet(nativeArray, k, out var value3) && (value3.m_Flags & TempFlags.Delete) != 0))
					{
						continue;
					}
					PrefabRef prefabRef3 = nativeArray10[k];
					if (!m_NetGeometryData.HasComponent(prefabRef3.m_Prefab))
					{
						continue;
					}
					Curve curve = nativeArray8[k];
					NetGeometryData netGeometryData2 = m_NetGeometryData[prefabRef3.m_Prefab];
					Entity entity4 = entity3;
					if ((objectGeometryData.m_Flags & GeometryFlags.OptionalAttach) == 0)
					{
						while (m_OwnerData.HasComponent(entity4) && !m_BuildingData.HasComponent(entity4))
						{
							entity4 = m_OwnerData[entity4].m_Owner;
						}
					}
					float num6 = math.select(x.x, x.y, topLevelOwner == entity4);
					float num7 = netGeometryData2.m_DefaultWidth * 0.5f;
					Bounds3 bounds3 = MathUtils.Bounds(curve.m_Bezier);
					bounds3.min -= new float3(num7, 0f - netGeometryData2.m_DefaultHeightRange.min, num7);
					bounds3.max += new float3(num7, netGeometryData2.m_DefaultHeightRange.max, num7);
					if (num6 > 0f)
					{
						bounds3.min -= num6;
						bounds3.max += num6;
					}
					if ((placementFlags & PlacementFlags.Waterway) != PlacementFlags.None)
					{
						if ((netGeometryData2.m_Flags & Game.Net.GeometryFlags.OnWater) == 0)
						{
							continue;
						}
						if (MathUtils.Intersect(bounds3, bounds))
						{
							float t;
							float num8 = MathUtils.Distance(curve.m_Bezier.xz, transform.m_Position.xz, out t) - num7;
							if (num8 < num6)
							{
								x = math.min(x, num8);
								attached.m_Parent = entity3;
								attached.m_CurvePosition = t;
								result = true;
							}
						}
					}
					else
					{
						if ((netGeometryData2.m_Flags & Game.Net.GeometryFlags.OnWater) != 0)
						{
							continue;
						}
						if ((objectGeometryData.m_Flags & GeometryFlags.BaseCollision) != GeometryFlags.None)
						{
							bounds.y = bounds3.y;
							float num9 = 0f;
							if (nativeArray9.Length != 0)
							{
								num9 = math.csum(nativeArray9[k].m_Elevation) * 0.5f;
								if (num9 > 0f && (netGeometryData2.m_Flags & Game.Net.GeometryFlags.RaisedIsElevated) == 0 && (!math.all(nativeArray9[k].m_Elevation >= netGeometryData2.m_ElevationLimit * 2f) || (netGeometryData2.m_Flags & Game.Net.GeometryFlags.ElevatedIsRaised) != 0) && (netGeometryData2.m_Flags & Game.Net.GeometryFlags.RequireElevated) == 0)
								{
									num9 = 0f;
								}
								bounds.y -= num9;
							}
							if (MathUtils.Intersect(bounds3, bounds))
							{
								float y = MathUtils.Distance(curve.m_Bezier.xz, transform.m_Position.xz, out var t2);
								y = math.length(new float2(num9, y)) - num7;
								if (y < num6)
								{
									x = math.min(x, y);
									attached.m_Parent = entity3;
									attached.m_CurvePosition = t2;
									result = true;
								}
							}
						}
						else if (MathUtils.Intersect(bounds3, bounds))
						{
							float t3;
							float num10 = MathUtils.Distance(curve.m_Bezier, transform.m_Position, out t3) - num7;
							if (num10 < num6)
							{
								x = math.min(x, num10);
								attached.m_Parent = entity3;
								attached.m_CurvePosition = t3;
								result = true;
							}
						}
					}
					if (flag && !relocating && originalAttached.m_Parent != Entity.Null && attached.m_Parent == Entity.Null && value3.m_Original == originalAttached.m_Parent)
					{
						MathUtils.Distance(curve.m_Bezier.xz, transform.m_Position.xz, out var t4);
						attached.m_Parent = entity3;
						attached.m_CurvePosition = t4;
						result = true;
					}
				}
			}
			return result;
		}
	}

	[BurstCompile]
	private struct UpdateAttachedReferencesJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_AttachedChunks;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Recent> m_RecentData;

		[ReadOnly]
		public ComponentLookup<Placeholder> m_PlaceholderData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		public ComponentTypeHandle<Attached> m_AttachedType;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameterData;

		public BufferLookup<SubObject> m_SubObjects;

		public ComponentLookup<Attachment> m_AttachmentData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeHashMap<Entity, Entity> newAttachments = new NativeHashMap<Entity, Entity>(32, Allocator.Temp);
			for (int i = 0; i < m_AttachedChunks.Length; i++)
			{
				Execute(m_AttachedChunks[i], newAttachments);
			}
			if (newAttachments.Count != 0)
			{
				NativeArray<Entity> keyArray = newAttachments.GetKeyArray(Allocator.Temp);
				for (int j = 0; j < keyArray.Length; j++)
				{
					Entity entity = keyArray[j];
					Entity entity2 = newAttachments[entity];
					if (entity2 != Entity.Null)
					{
						if (m_AttachmentData.HasComponent(entity))
						{
							m_AttachmentData[entity] = new Attachment(entity2);
						}
						else
						{
							m_CommandBuffer.AddComponent(entity, new Attachment(entity2));
						}
					}
					else if (m_AttachmentData.HasComponent(entity))
					{
						m_CommandBuffer.RemoveComponent<Attachment>(entity);
					}
				}
				keyArray.Dispose();
			}
			newAttachments.Dispose();
		}

		private void Execute(ArchetypeChunk chunk, NativeHashMap<Entity, Entity> newAttachments)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Attached> nativeArray2 = chunk.GetNativeArray(ref m_AttachedType);
			if (chunk.Has(ref m_DeletedType))
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					ref Attached reference = ref nativeArray2.ElementAt(i);
					if (reference.m_OldParent != Entity.Null && reference.m_OldParent != reference.m_Parent && m_SubObjects.TryGetBuffer(reference.m_OldParent, out var bufferData))
					{
						CollectionUtils.RemoveValue(bufferData, new SubObject(entity));
					}
					reference.m_OldParent = Entity.Null;
					if (reference.m_Parent == entity)
					{
						reference.m_Parent = Entity.Null;
						m_CommandBuffer.RemoveComponent<Attached>(entity);
						continue;
					}
					if (m_SubObjects.TryGetBuffer(reference.m_Parent, out var bufferData2))
					{
						CollectionUtils.RemoveValue(bufferData2, new SubObject(entity));
					}
					if (!m_PlaceholderData.HasComponent(reference.m_Parent))
					{
						continue;
					}
					if (newAttachments.TryGetValue(reference.m_Parent, out var item))
					{
						if (item == entity)
						{
							newAttachments.Remove(reference.m_Parent);
							newAttachments.TryAdd(reference.m_Parent, Entity.Null);
						}
					}
					else if (m_AttachmentData.HasComponent(reference.m_Parent))
					{
						item = m_AttachmentData[reference.m_Parent].m_Attached;
						if (item == entity)
						{
							newAttachments.TryAdd(reference.m_Parent, Entity.Null);
						}
					}
				}
				return;
			}
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			if (nativeArray3.Length != 0)
			{
				NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
				bool flag = chunk.Has(ref m_OwnerType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					ref Attached reference2 = ref nativeArray2.ElementAt(j);
					Temp component = nativeArray3[j];
					PrefabRef prefabRef = nativeArray4[j];
					if (reference2.m_OldParent != Entity.Null && reference2.m_OldParent != reference2.m_Parent && m_SubObjects.TryGetBuffer(reference2.m_OldParent, out var bufferData3))
					{
						CollectionUtils.RemoveValue(bufferData3, new SubObject(entity2));
					}
					reference2.m_OldParent = Entity.Null;
					if (reference2.m_Parent == entity2)
					{
						reference2.m_Parent = Entity.Null;
						m_CommandBuffer.RemoveComponent<Attached>(entity2);
						continue;
					}
					bool flag2 = reference2.m_Parent != Entity.Null;
					if (m_TempData.TryGetComponent(reference2.m_Parent, out var componentData))
					{
						flag2 = (componentData.m_Flags & TempFlags.Delete) == 0;
						if (m_SubObjects.TryGetBuffer(reference2.m_Parent, out var bufferData4))
						{
							CollectionUtils.TryAddUniqueValue(bufferData4, new SubObject(entity2));
						}
						if (m_PlaceholderData.HasComponent(reference2.m_Parent))
						{
							newAttachments.Remove(reference2.m_Parent);
							newAttachments.TryAdd(reference2.m_Parent, entity2);
						}
					}
					if (flag || flag2 || (component.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Upgrade | TempFlags.Parent | TempFlags.Duplicate)) != 0)
					{
						continue;
					}
					ObjectGeometryData objectGeometryData = default(ObjectGeometryData);
					if (m_ObjectGeometryData.HasComponent(prefabRef.m_Prefab))
					{
						objectGeometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
					}
					if ((objectGeometryData.m_Flags & GeometryFlags.OptionalAttach) == 0)
					{
						component.m_Flags |= TempFlags.Delete;
						if (m_RecentData.TryGetComponent(component.m_Original, out var componentData2))
						{
							component.m_Cost = -ObjectUtils.GetRefundAmount(componentData2, m_SimulationFrame, m_EconomyParameterData);
						}
						m_CommandBuffer.SetComponent(entity2, component);
					}
				}
				return;
			}
			for (int k = 0; k < nativeArray2.Length; k++)
			{
				Entity entity3 = nativeArray[k];
				ref Attached reference3 = ref nativeArray2.ElementAt(k);
				if (reference3.m_OldParent != Entity.Null && reference3.m_OldParent != reference3.m_Parent && m_SubObjects.TryGetBuffer(reference3.m_OldParent, out var bufferData5))
				{
					CollectionUtils.RemoveValue(bufferData5, new SubObject(entity3));
				}
				reference3.m_OldParent = Entity.Null;
				if (reference3.m_Parent == entity3)
				{
					reference3.m_Parent = Entity.Null;
					m_CommandBuffer.RemoveComponent<Attached>(entity3);
					continue;
				}
				if (m_SubObjects.TryGetBuffer(reference3.m_Parent, out var bufferData6))
				{
					CollectionUtils.TryAddUniqueValue(bufferData6, new SubObject(entity3));
				}
				if (m_PlaceholderData.HasComponent(reference3.m_Parent))
				{
					newAttachments.Remove(reference3.m_Parent);
					newAttachments.TryAdd(reference3.m_Parent, entity3);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceholderBuildingData> __Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		public ComponentLookup<Attached> __Game_Objects_Attached_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Recent> __Game_Tools_Recent_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Placeholder> __Game_Objects_Placeholder_RO_ComponentLookup;

		public ComponentTypeHandle<Attached> __Game_Objects_Attached_RW_ComponentTypeHandle;

		public BufferLookup<SubObject> __Game_Objects_SubObject_RW_BufferLookup;

		public ComponentLookup<Attachment> __Game_Objects_Attachment_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Elevation>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup = state.GetComponentLookup<PlaceholderBuildingData>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Objects_Attached_RW_ComponentLookup = state.GetComponentLookup<Attached>();
			__Game_Tools_Recent_RO_ComponentLookup = state.GetComponentLookup<Recent>(isReadOnly: true);
			__Game_Objects_Placeholder_RO_ComponentLookup = state.GetComponentLookup<Placeholder>(isReadOnly: true);
			__Game_Objects_Attached_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Attached>();
			__Game_Objects_SubObject_RW_BufferLookup = state.GetBufferLookup<SubObject>();
			__Game_Objects_Attachment_RW_ComponentLookup = state.GetComponentLookup<Attachment>();
		}
	}

	private ModificationBarrier3 m_ModificationBarrier;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_ObjectQuery;

	private EntityQuery m_TempQuery;

	private EntityQuery m_EconomyParameterQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier3>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Object>(),
				ComponentType.ReadOnly<Attached>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_TempQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<SubObject>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Placeholder>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		RequireForUpdate(m_ObjectQuery);
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> nativeList = m_ObjectQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = JobHandle.CombineDependencies(outJobHandle, base.Dependency);
		if (!m_TempQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle2;
			NativeList<ArchetypeChunk> parentChunks = m_TempQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2);
			JobHandle jobHandle2 = new FindAttachedParentsJob
			{
				m_AttachedChunks = nativeList.AsDeferredJobArray(),
				m_ParentChunks = parentChunks,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ElevationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceholderBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RW_ComponentLookup, ref base.CheckedStateRef)
			}.Schedule(nativeList, 1, JobHandle.CombineDependencies(jobHandle, outJobHandle2));
			parentChunks.Dispose(jobHandle2);
			jobHandle = jobHandle2;
		}
		JobHandle jobHandle3 = IJobExtensions.Schedule(new UpdateAttachedReferencesJob
		{
			m_AttachedChunks = nativeList,
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RecentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Recent_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PlaceholderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Attached_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RW_BufferLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RW_ComponentLookup, ref base.CheckedStateRef),
			m_EconomyParameterData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, jobHandle);
		nativeList.Dispose(jobHandle3);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle3);
		base.Dependency = jobHandle3;
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
	public AttachSystem()
	{
	}
}
