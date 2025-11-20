using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Achievements;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class BulldozeToolSystem : ToolBaseSystem
{
	public enum Mode
	{
		MainElements,
		SubElements,
		Everything
	}

	private enum State
	{
		Default,
		Applying,
		Waiting,
		Confirmed,
		Cancelled
	}

	private struct PathEdge
	{
		public Entity m_Edge;

		public bool m_Invert;
	}

	public struct PathItem : ILessThan<PathItem>
	{
		public Entity m_Node;

		public Entity m_Edge;

		public float m_Cost;

		public bool LessThan(PathItem other)
		{
			return m_Cost < other.m_Cost;
		}
	}

	[BurstCompile]
	private struct SnapJob : IJob
	{
		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public Mode m_Mode;

		[ReadOnly]
		public State m_State;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Static> m_StaticData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		public NativeList<ControlPoint> m_ControlPoints;

		public void Execute()
		{
			if (!m_EditorMode && m_OutsideConnectionData.HasComponent(m_ControlPoints[m_ControlPoints.Length - 1].m_OriginalEntity))
			{
				m_ControlPoints.RemoveAt(m_ControlPoints.Length - 1);
				return;
			}
			if (m_Mode == Mode.MainElements)
			{
				ControlPoint value = m_ControlPoints[m_ControlPoints.Length - 1];
				if (m_StaticData.HasComponent(value.m_OriginalEntity) && !m_ServiceUpgradeData.HasComponent(value.m_OriginalEntity) && m_OwnerData.TryGetComponent(value.m_OriginalEntity, out var componentData))
				{
					value.m_OriginalEntity = componentData.m_Owner;
					while (m_StaticData.HasComponent(value.m_OriginalEntity) && !m_ServiceUpgradeData.HasComponent(value.m_OriginalEntity) && m_OwnerData.TryGetComponent(value.m_OriginalEntity, out componentData))
					{
						value.m_OriginalEntity = componentData.m_Owner;
					}
					m_ControlPoints[m_ControlPoints.Length - 1] = value;
				}
			}
			if (m_State != State.Applying)
			{
				return;
			}
			ControlPoint startPoint = m_ControlPoints[0];
			ControlPoint endPoint = m_ControlPoints[m_ControlPoints.Length - 1];
			if (m_EdgeData.HasComponent(startPoint.m_OriginalEntity) || m_NodeData.HasComponent(startPoint.m_OriginalEntity))
			{
				m_ControlPoints.Clear();
				NativeList<PathEdge> path = new NativeList<PathEdge>(Allocator.Temp);
				CreatePath(startPoint, endPoint, path);
				AddControlPoints(startPoint, endPoint, path);
				return;
			}
			if (m_EdgeData.HasComponent(endPoint.m_OriginalEntity) || m_NodeData.HasComponent(endPoint.m_OriginalEntity))
			{
				m_ControlPoints.RemoveAt(m_ControlPoints.Length - 1);
				return;
			}
			Entity entity = Entity.Null;
			Entity entity2 = Entity.Null;
			if (m_OwnerData.HasComponent(startPoint.m_OriginalEntity))
			{
				entity = m_OwnerData[startPoint.m_OriginalEntity].m_Owner;
			}
			if (m_OwnerData.HasComponent(endPoint.m_OriginalEntity))
			{
				entity2 = m_OwnerData[endPoint.m_OriginalEntity].m_Owner;
			}
			if (entity != entity2)
			{
				m_ControlPoints.RemoveAt(m_ControlPoints.Length - 1);
				return;
			}
			for (int i = 0; i < m_ControlPoints.Length - 1; i++)
			{
				if (m_ControlPoints[i].m_OriginalEntity == endPoint.m_OriginalEntity)
				{
					m_ControlPoints.RemoveAt(m_ControlPoints.Length - 1);
					break;
				}
			}
		}

		private void CreatePath(ControlPoint startPoint, ControlPoint endPoint, NativeList<PathEdge> path)
		{
			if (math.distance(startPoint.m_Position, endPoint.m_Position) < 4f)
			{
				endPoint = startPoint;
			}
			PrefabRef prefabRef = m_PrefabRefData[startPoint.m_OriginalEntity];
			NetData netData = m_PrefabNetData[prefabRef.m_Prefab];
			if (startPoint.m_OriginalEntity == endPoint.m_OriginalEntity)
			{
				if (m_EdgeData.HasComponent(endPoint.m_OriginalEntity))
				{
					PathEdge value = new PathEdge
					{
						m_Edge = endPoint.m_OriginalEntity,
						m_Invert = (endPoint.m_CurvePosition < startPoint.m_CurvePosition)
					};
					path.Add(in value);
				}
				return;
			}
			NativeMinHeap<PathItem> nativeMinHeap = new NativeMinHeap<PathItem>(100, Allocator.Temp);
			NativeParallelHashMap<Entity, Entity> nativeParallelHashMap = new NativeParallelHashMap<Entity, Entity>(100, Allocator.Temp);
			if (m_EdgeData.HasComponent(endPoint.m_OriginalEntity))
			{
				Edge edge = m_EdgeData[endPoint.m_OriginalEntity];
				PrefabRef prefabRef2 = m_PrefabRefData[endPoint.m_OriginalEntity];
				NetData netData2 = m_PrefabNetData[prefabRef2.m_Prefab];
				if ((netData.m_RequiredLayers & netData2.m_RequiredLayers) != Layer.None)
				{
					nativeMinHeap.Insert(new PathItem
					{
						m_Node = edge.m_Start,
						m_Edge = endPoint.m_OriginalEntity,
						m_Cost = 0f
					});
					nativeMinHeap.Insert(new PathItem
					{
						m_Node = edge.m_End,
						m_Edge = endPoint.m_OriginalEntity,
						m_Cost = 0f
					});
				}
			}
			else if (m_NodeData.HasComponent(endPoint.m_OriginalEntity))
			{
				nativeMinHeap.Insert(new PathItem
				{
					m_Node = endPoint.m_OriginalEntity,
					m_Edge = Entity.Null,
					m_Cost = 0f
				});
			}
			Entity entity = Entity.Null;
			while (nativeMinHeap.Length != 0)
			{
				PathItem pathItem = nativeMinHeap.Extract();
				if (pathItem.m_Edge == startPoint.m_OriginalEntity)
				{
					nativeParallelHashMap[pathItem.m_Node] = pathItem.m_Edge;
					entity = pathItem.m_Node;
					break;
				}
				if (!nativeParallelHashMap.TryAdd(pathItem.m_Node, pathItem.m_Edge))
				{
					continue;
				}
				if (pathItem.m_Node == startPoint.m_OriginalEntity)
				{
					entity = pathItem.m_Node;
					break;
				}
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[pathItem.m_Node];
				PrefabRef prefabRef3 = default(PrefabRef);
				if (pathItem.m_Edge != Entity.Null)
				{
					prefabRef3 = m_PrefabRefData[pathItem.m_Edge];
				}
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity edge2 = dynamicBuffer[i].m_Edge;
					if (edge2 == pathItem.m_Edge)
					{
						continue;
					}
					Edge edge3 = m_EdgeData[edge2];
					Entity entity2;
					if (edge3.m_Start == pathItem.m_Node)
					{
						entity2 = edge3.m_End;
					}
					else
					{
						if (!(edge3.m_End == pathItem.m_Node))
						{
							continue;
						}
						entity2 = edge3.m_Start;
					}
					if (!nativeParallelHashMap.ContainsKey(entity2) || !(edge2 != startPoint.m_OriginalEntity))
					{
						PrefabRef prefabRef4 = m_PrefabRefData[edge2];
						NetData netData3 = m_PrefabNetData[prefabRef4.m_Prefab];
						if ((netData.m_RequiredLayers & netData3.m_RequiredLayers) != Layer.None)
						{
							Curve curve = m_CurveData[edge2];
							float num = pathItem.m_Cost + curve.m_Length;
							num += math.select(0f, 9.9f, prefabRef.m_Prefab != prefabRef3.m_Prefab);
							num += math.select(0f, 10f, dynamicBuffer.Length > 2);
							nativeMinHeap.Insert(new PathItem
							{
								m_Node = entity2,
								m_Edge = edge2,
								m_Cost = num
							});
						}
					}
				}
			}
			Entity item;
			while (nativeParallelHashMap.TryGetValue(entity, out item) && !(item == Entity.Null))
			{
				Edge edge4 = m_EdgeData[item];
				bool flag = edge4.m_End == entity;
				path.Add(new PathEdge
				{
					m_Edge = item,
					m_Invert = flag
				});
				if (!(item == endPoint.m_OriginalEntity))
				{
					entity = (flag ? edge4.m_Start : edge4.m_End);
					continue;
				}
				break;
			}
		}

		private void AddControlPoints(ControlPoint startPoint, ControlPoint endPoint, NativeList<PathEdge> path)
		{
			m_ControlPoints.Add(in startPoint);
			for (int i = 0; i < path.Length; i++)
			{
				PathEdge pathEdge = path[i];
				Edge edge = m_EdgeData[pathEdge.m_Edge];
				Curve curve = m_CurveData[pathEdge.m_Edge];
				if (pathEdge.m_Invert)
				{
					CommonUtils.Swap(ref edge.m_Start, ref edge.m_End);
					curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
				}
				ControlPoint value = endPoint;
				value.m_OriginalEntity = edge.m_Start;
				value.m_Position = curve.m_Bezier.a;
				ControlPoint value2 = endPoint;
				value2.m_OriginalEntity = edge.m_End;
				value2.m_Position = curve.m_Bezier.d;
				m_ControlPoints.Add(in value);
				m_ControlPoints.Add(in value2);
			}
			m_ControlPoints.Add(in endPoint);
		}
	}

	[BurstCompile]
	private struct CreateDefinitionsJob : IJob
	{
		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public Mode m_Mode;

		[ReadOnly]
		public State m_State;

		[ReadOnly]
		public NativeList<ControlPoint> m_ControlPoints;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> m_CachedNodes;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Fixed> m_FixedData;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_LotData;

		[ReadOnly]
		public ComponentLookup<EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Placeholder> m_PlaceholderData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeHashSet<Entity> bulldozeEntities = new NativeHashSet<Entity>(16, Allocator.Temp);
			if (m_State == State.Applying && m_ControlPoints.Length >= 2 && (m_EdgeData.HasComponent(m_ControlPoints[0].m_OriginalEntity) || m_NodeData.HasComponent(m_ControlPoints[0].m_OriginalEntity)))
			{
				int num = m_ControlPoints.Length / 2 - 1;
				if (num == 0 && m_ControlPoints[0].m_OriginalEntity == m_ControlPoints[1].m_OriginalEntity)
				{
					if (m_Mode != Mode.MainElements || !m_OwnerData.HasComponent(m_ControlPoints[0].m_OriginalEntity) || m_ServiceUpgradeData.HasComponent(m_ControlPoints[0].m_OriginalEntity))
					{
						bulldozeEntities.Add(m_ControlPoints[0].m_OriginalEntity);
					}
				}
				else
				{
					for (int i = 0; i < num; i++)
					{
						ControlPoint controlPoint = m_ControlPoints[i * 2 + 1];
						ControlPoint controlPoint2 = m_ControlPoints[i * 2 + 2];
						DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[controlPoint.m_OriginalEntity];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							Entity edge = dynamicBuffer[j].m_Edge;
							Edge edge2 = m_EdgeData[edge];
							if (m_Mode != Mode.MainElements || !m_OwnerData.HasComponent(edge) || m_ServiceUpgradeData.HasComponent(edge))
							{
								if (edge2.m_Start == controlPoint.m_OriginalEntity && edge2.m_End == controlPoint2.m_OriginalEntity)
								{
									bulldozeEntities.Add(edge);
								}
								else if (edge2.m_End == controlPoint.m_OriginalEntity && edge2.m_Start == controlPoint2.m_OriginalEntity)
								{
									bulldozeEntities.Add(edge);
								}
							}
						}
					}
				}
			}
			else
			{
				for (int k = 0; k < m_ControlPoints.Length; k++)
				{
					bulldozeEntities.Add(m_ControlPoints[k].m_OriginalEntity);
				}
			}
			if (!bulldozeEntities.IsEmpty)
			{
				NativeHashMap<Entity, OwnerDefinition> ownerDefinitions = new NativeHashMap<Entity, OwnerDefinition>(16, Allocator.Temp);
				NativeHashSet<Entity> attachedEntities = new NativeHashSet<Entity>(16, Allocator.Temp);
				NativeArray<Entity> nativeArray = bulldozeEntities.ToNativeArray(Allocator.Temp);
				for (int l = 0; l < nativeArray.Length; l++)
				{
					Execute(nativeArray[l], ownerDefinitions, bulldozeEntities, attachedEntities);
				}
				nativeArray.Dispose();
				ownerDefinitions.Dispose();
				attachedEntities.Dispose();
			}
			bulldozeEntities.Dispose();
		}

		private void Execute(Entity bulldozeEntity, NativeHashMap<Entity, OwnerDefinition> ownerDefinitions, NativeHashSet<Entity> bulldozeEntities, NativeHashSet<Entity> attachedEntities)
		{
			Entity entity = Entity.Null;
			Entity entity2 = Entity.Null;
			OwnerDefinition item = default(OwnerDefinition);
			bool parent = false;
			if (m_OwnerData.HasComponent(bulldozeEntity))
			{
				if (m_EditorMode)
				{
					Entity entity3 = bulldozeEntity;
					while (m_OwnerData.HasComponent(entity3) && !m_BuildingData.HasComponent(entity3))
					{
						entity3 = m_OwnerData[entity3].m_Owner;
						if (m_ServiceUpgradeData.HasComponent(entity3))
						{
							entity2 = entity3;
						}
					}
					if (m_TransformData.HasComponent(entity3) && m_SubObjects.HasBuffer(entity3))
					{
						entity = entity3;
					}
				}
				else if (m_ServiceUpgradeData.HasComponent(bulldozeEntity))
				{
					entity = m_OwnerData[bulldozeEntity].m_Owner;
					parent = true;
				}
			}
			if (m_TransformData.HasComponent(entity))
			{
				if (!ownerDefinitions.TryGetValue(entity, out item))
				{
					Transform transform = m_TransformData[entity];
					Entity owner = Entity.Null;
					if (m_OwnerData.HasComponent(entity))
					{
						owner = m_OwnerData[entity].m_Owner;
					}
					AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, entity, owner, default(OwnerDefinition), entity2 == Entity.Null, parent, delete: false);
					item.m_Prefab = m_PrefabRefData[entity].m_Prefab;
					item.m_Position = transform.m_Position;
					item.m_Rotation = transform.m_Rotation;
					if (m_InstalledUpgrades.HasBuffer(entity))
					{
						DynamicBuffer<InstalledUpgrade> dynamicBuffer = m_InstalledUpgrades[entity];
						for (int i = 0; i < dynamicBuffer.Length; i++)
						{
							Entity upgrade = dynamicBuffer[i].m_Upgrade;
							if (upgrade != bulldozeEntity && !bulldozeEntities.Contains(upgrade))
							{
								AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, upgrade, Entity.Null, item, entity2 == upgrade, parent, delete: false);
							}
						}
					}
					if (m_AttachmentData.TryGetComponent(entity, out var componentData) && m_TransformData.HasComponent(componentData.m_Attached))
					{
						AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, componentData.m_Attached, Entity.Null, default(OwnerDefinition), upgrade: true, parent, delete: false);
					}
					ownerDefinitions.Add(entity, item);
					if (m_TransformData.HasComponent(entity2))
					{
						transform = m_TransformData[entity2];
						item.m_Prefab = m_PrefabRefData[entity2].m_Prefab;
						item.m_Position = transform.m_Position;
						item.m_Rotation = transform.m_Rotation;
					}
				}
			}
			else if (m_AttachmentData.HasComponent(bulldozeEntity))
			{
				Attachment attachment = m_AttachmentData[bulldozeEntity];
				if (!bulldozeEntities.Contains(attachment.m_Attached) && m_TransformData.HasComponent(attachment.m_Attached) && m_PlaceholderData.HasComponent(bulldozeEntity) && !m_OwnerData.HasComponent(attachment.m_Attached))
				{
					AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, attachment.m_Attached, Entity.Null, default(OwnerDefinition), upgrade: false, parent: false, delete: true);
				}
			}
			else if (m_AttachedData.HasComponent(bulldozeEntity))
			{
				Attached attached = m_AttachedData[bulldozeEntity];
				if (!bulldozeEntities.Contains(attached.m_Parent) && m_AttachmentData.HasComponent(attached.m_Parent) && m_AttachmentData[attached.m_Parent].m_Attached == bulldozeEntity)
				{
					AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, attached.m_Parent, Entity.Null, default(OwnerDefinition), upgrade: false, parent: false, delete: true);
					if (m_InstalledUpgrades.HasBuffer(attached.m_Parent))
					{
						Transform transform2 = m_TransformData[attached.m_Parent];
						DynamicBuffer<InstalledUpgrade> dynamicBuffer2 = m_InstalledUpgrades[attached.m_Parent];
						OwnerDefinition ownerDefinition = new OwnerDefinition
						{
							m_Prefab = m_PrefabRefData[attached.m_Parent].m_Prefab,
							m_Position = transform2.m_Position,
							m_Rotation = transform2.m_Rotation
						};
						for (int j = 0; j < dynamicBuffer2.Length; j++)
						{
							Entity upgrade2 = dynamicBuffer2[j].m_Upgrade;
							AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, upgrade2, Entity.Null, ownerDefinition, upgrade: false, parent: false, delete: true);
						}
					}
				}
			}
			if (m_ConnectedEdges.HasBuffer(bulldozeEntity))
			{
				DynamicBuffer<ConnectedEdge> dynamicBuffer3 = m_ConnectedEdges[bulldozeEntity];
				PrefabRef prefabRef = m_PrefabRefData[bulldozeEntity];
				m_PrefabNetData.TryGetComponent(prefabRef.m_Prefab, out var componentData2);
				bool flag = true;
				bool flag2 = false;
				for (int k = 0; k < dynamicBuffer3.Length; k++)
				{
					Entity edge = dynamicBuffer3[k].m_Edge;
					Edge edge2 = m_EdgeData[edge];
					if (!(edge2.m_Start == bulldozeEntity) && !(edge2.m_End == bulldozeEntity))
					{
						continue;
					}
					PrefabRef prefabRef2 = m_PrefabRefData[edge];
					m_PrefabNetData.TryGetComponent(prefabRef2.m_Prefab, out var componentData3);
					if ((componentData2.m_RequiredLayers & componentData3.m_RequiredLayers) == 0)
					{
						flag2 = true;
						continue;
					}
					if (!bulldozeEntities.Contains(edge))
					{
						AddEdge(ownerDefinitions, bulldozeEntities, attachedEntities, edge, Entity.Null, item, upgrade: false, delete: true);
					}
					flag = false;
				}
				if (flag && flag2)
				{
					for (int l = 0; l < dynamicBuffer3.Length; l++)
					{
						Entity edge3 = dynamicBuffer3[l].m_Edge;
						Edge edge4 = m_EdgeData[edge3];
						if (edge4.m_Start == bulldozeEntity || edge4.m_End == bulldozeEntity)
						{
							if (!bulldozeEntities.Contains(edge3))
							{
								AddEdge(ownerDefinitions, bulldozeEntities, attachedEntities, edge3, Entity.Null, item, upgrade: false, delete: true);
							}
							flag = false;
						}
					}
				}
				if (flag)
				{
					AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, bulldozeEntity, Entity.Null, item, upgrade: false, parent: false, delete: true);
				}
				return;
			}
			if (m_EdgeData.HasComponent(bulldozeEntity))
			{
				AddEdge(ownerDefinitions, bulldozeEntities, attachedEntities, bulldozeEntity, Entity.Null, item, upgrade: false, delete: true);
				return;
			}
			if (m_AreaNodes.HasBuffer(bulldozeEntity))
			{
				AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, bulldozeEntity, Entity.Null, item, upgrade: false, parent: false, delete: true);
				if (!m_SubObjects.TryGetBuffer(bulldozeEntity, out var bufferData))
				{
					return;
				}
				for (int m = 0; m < bufferData.Length; m++)
				{
					Game.Objects.SubObject subObject = bufferData[m];
					if (m_BuildingData.HasComponent(subObject.m_SubObject))
					{
						AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, subObject.m_SubObject, Entity.Null, default(OwnerDefinition), upgrade: false, parent: false, delete: true);
					}
				}
				return;
			}
			AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, bulldozeEntity, Entity.Null, item, upgrade: false, parent: false, delete: true);
			if (m_InstalledUpgrades.HasBuffer(bulldozeEntity) && m_TransformData.HasComponent(bulldozeEntity))
			{
				Transform transform3 = m_TransformData[bulldozeEntity];
				item.m_Prefab = m_PrefabRefData[bulldozeEntity].m_Prefab;
				item.m_Position = transform3.m_Position;
				item.m_Rotation = transform3.m_Rotation;
				DynamicBuffer<InstalledUpgrade> dynamicBuffer4 = m_InstalledUpgrades[bulldozeEntity];
				for (int n = 0; n < dynamicBuffer4.Length; n++)
				{
					AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, dynamicBuffer4[n].m_Upgrade, Entity.Null, item, upgrade: false, parent: false, delete: true);
				}
			}
		}

		private void AddEdge(NativeHashMap<Entity, OwnerDefinition> ownerDefinitions, NativeHashSet<Entity> bulldozeEntities, NativeHashSet<Entity> attachedEntities, Entity entity, Entity owner, OwnerDefinition ownerDefinition, bool upgrade, bool delete)
		{
			AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, entity, owner, ownerDefinition, upgrade, parent: false, delete);
			if (m_FixedData.HasComponent(entity))
			{
				Edge edge = m_EdgeData[entity];
				AddFixedEdges(ownerDefinitions, bulldozeEntities, attachedEntities, entity, edge.m_Start, owner, ownerDefinition, upgrade, delete);
				AddFixedEdges(ownerDefinitions, bulldozeEntities, attachedEntities, entity, edge.m_End, owner, ownerDefinition, upgrade, delete);
			}
		}

		private void AddFixedEdges(NativeHashMap<Entity, OwnerDefinition> ownerDefinitions, NativeHashSet<Entity> bulldozeEntities, NativeHashSet<Entity> attachedEntities, Entity lastEdge, Entity lastNode, Entity owner, OwnerDefinition ownerDefinition, bool upgrade, bool delete)
		{
			while (m_FixedData.HasComponent(lastNode) && !bulldozeEntities.Contains(lastNode))
			{
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[lastNode];
				Entity entity = Entity.Null;
				Entity entity2 = Entity.Null;
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity edge = dynamicBuffer[i].m_Edge;
					if (!(edge != lastEdge) || !m_FixedData.HasComponent(edge))
					{
						continue;
					}
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_Start == lastNode)
					{
						if (bulldozeEntities.Add(edge))
						{
							AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, edge, owner, ownerDefinition, upgrade, parent: false, delete);
							entity = edge;
							entity2 = edge2.m_End;
						}
						break;
					}
					if (edge2.m_End == lastNode)
					{
						if (bulldozeEntities.Add(edge))
						{
							AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, edge, owner, ownerDefinition, upgrade, parent: false, delete);
							entity = edge;
							entity2 = edge2.m_Start;
						}
						break;
					}
				}
				lastEdge = entity;
				lastNode = entity2;
			}
		}

		private void AddEntity(NativeHashMap<Entity, OwnerDefinition> ownerDefinitions, NativeHashSet<Entity> bulldozeEntities, NativeHashSet<Entity> attachedEntities, Entity entity, Entity owner, OwnerDefinition ownerDefinition, bool upgrade, bool parent, bool delete)
		{
			Entity e = m_CommandBuffer.CreateEntity();
			CreationDefinition component = new CreationDefinition
			{
				m_Original = entity,
				m_Owner = owner
			};
			if (upgrade)
			{
				component.m_Flags |= CreationFlags.Upgrade;
			}
			if (parent)
			{
				component.m_Flags |= CreationFlags.Upgrade | CreationFlags.Parent;
			}
			if (delete)
			{
				component.m_Flags |= CreationFlags.Delete;
			}
			m_CommandBuffer.AddComponent(e, default(Updated));
			if (ownerDefinition.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
			}
			if (m_EdgeData.HasComponent(entity))
			{
				if (m_EditorContainerData.HasComponent(entity))
				{
					component.m_SubPrefab = m_EditorContainerData[entity].m_Prefab;
				}
				Edge edge = m_EdgeData[entity];
				NetCourse component2 = default(NetCourse);
				component2.m_Curve = m_CurveData[entity].m_Bezier;
				component2.m_Length = MathUtils.Length(component2.m_Curve);
				component2.m_FixedIndex = -1;
				if (m_FixedData.HasComponent(entity))
				{
					component2.m_FixedIndex = m_FixedData[entity].m_Index;
				}
				component2.m_StartPosition.m_Entity = edge.m_Start;
				component2.m_StartPosition.m_Position = component2.m_Curve.a;
				component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve));
				component2.m_StartPosition.m_CourseDelta = 0f;
				component2.m_EndPosition.m_Entity = edge.m_End;
				component2.m_EndPosition.m_Position = component2.m_Curve.d;
				component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve));
				component2.m_EndPosition.m_CourseDelta = 1f;
				m_CommandBuffer.AddComponent(e, component2);
				ownerDefinition.m_Prefab = m_PrefabRefData[entity].m_Prefab;
				ownerDefinition.m_Position = component2.m_Curve.a;
				ownerDefinition.m_Rotation = new float4(component2.m_Curve.d, 0f);
			}
			else if (m_NodeData.HasComponent(entity))
			{
				if (m_EditorContainerData.HasComponent(entity))
				{
					component.m_SubPrefab = m_EditorContainerData[entity].m_Prefab;
				}
				Game.Net.Node node = m_NodeData[entity];
				NetCourse component3 = new NetCourse
				{
					m_Curve = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position),
					m_Length = 0f,
					m_FixedIndex = -1,
					m_StartPosition = 
					{
						m_Entity = entity,
						m_Position = node.m_Position,
						m_Rotation = node.m_Rotation,
						m_CourseDelta = 0f
					},
					m_EndPosition = 
					{
						m_Entity = entity,
						m_Position = node.m_Position,
						m_Rotation = node.m_Rotation,
						m_CourseDelta = 1f
					}
				};
				m_CommandBuffer.AddComponent(e, component3);
			}
			else if (m_TransformData.HasComponent(entity))
			{
				Transform transform = m_TransformData[entity];
				ObjectDefinition component4 = new ObjectDefinition
				{
					m_Position = transform.m_Position,
					m_Rotation = transform.m_Rotation
				};
				if (m_ElevationData.TryGetComponent(entity, out var componentData))
				{
					component4.m_Elevation = componentData.m_Elevation;
					component4.m_ParentMesh = ObjectUtils.GetSubParentMesh(componentData.m_Flags);
				}
				else
				{
					component4.m_ParentMesh = -1;
				}
				if (parent && m_AttachedData.TryGetComponent(entity, out var componentData2) && m_PrefabRefData.TryGetComponent(componentData2.m_Parent, out var componentData3))
				{
					component.m_Attached = componentData3.m_Prefab;
					component.m_Flags |= CreationFlags.Attach;
				}
				component4.m_Probability = 100;
				component4.m_PrefabSubIndex = -1;
				if (m_LocalTransformCacheData.HasComponent(entity))
				{
					LocalTransformCache localTransformCache = m_LocalTransformCacheData[entity];
					component4.m_LocalPosition = localTransformCache.m_Position;
					component4.m_LocalRotation = localTransformCache.m_Rotation;
					component4.m_ParentMesh = localTransformCache.m_ParentMesh;
					component4.m_GroupIndex = localTransformCache.m_GroupIndex;
					component4.m_Probability = localTransformCache.m_Probability;
					component4.m_PrefabSubIndex = localTransformCache.m_PrefabSubIndex;
				}
				else if (ownerDefinition.m_Prefab != Entity.Null)
				{
					Transform transform2 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(new Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation)), transform);
					component4.m_LocalPosition = transform2.m_Position;
					component4.m_LocalRotation = transform2.m_Rotation;
				}
				else if (m_TransformData.HasComponent(owner))
				{
					Transform transform3 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(m_TransformData[owner]), transform);
					component4.m_LocalPosition = transform3.m_Position;
					component4.m_LocalRotation = transform3.m_Rotation;
				}
				else
				{
					component4.m_LocalPosition = transform.m_Position;
					component4.m_LocalRotation = transform.m_Rotation;
				}
				if (m_EditorContainerData.HasComponent(entity))
				{
					EditorContainer editorContainer = m_EditorContainerData[entity];
					component.m_SubPrefab = editorContainer.m_Prefab;
					component4.m_Scale = editorContainer.m_Scale;
					component4.m_Intensity = editorContainer.m_Intensity;
					component4.m_GroupIndex = editorContainer.m_GroupIndex;
				}
				m_CommandBuffer.AddComponent(e, component4);
				ownerDefinition.m_Prefab = m_PrefabRefData[entity].m_Prefab;
				ownerDefinition.m_Position = transform.m_Position;
				ownerDefinition.m_Rotation = transform.m_Rotation;
			}
			else if (m_AreaNodes.HasBuffer(entity))
			{
				DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_AreaNodes[entity];
				m_CommandBuffer.AddBuffer<Game.Areas.Node>(e).CopyFrom(dynamicBuffer.AsNativeArray());
				if (m_CachedNodes.HasBuffer(entity))
				{
					DynamicBuffer<LocalNodeCache> dynamicBuffer2 = m_CachedNodes[entity];
					m_CommandBuffer.AddBuffer<LocalNodeCache>(e).CopyFrom(dynamicBuffer2.AsNativeArray());
				}
			}
			m_CommandBuffer.AddComponent(e, component);
			if (delete && m_AttachedData.HasComponent(entity))
			{
				Attached attached = m_AttachedData[entity];
				AddAttachedParent(bulldozeEntities, ownerDefinitions, attachedEntities, attached);
			}
			if (delete && m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (m_AttachedData.TryGetComponent(bufferData[i].m_SubObject, out var componentData4))
					{
						AddAttachedParent(bulldozeEntities, ownerDefinitions, attachedEntities, componentData4);
					}
				}
			}
			if (m_SubNets.HasBuffer(entity))
			{
				AddSubNets(bulldozeEntities, entity, ownerDefinition, delete);
			}
			if (!m_SubAreas.HasBuffer(entity))
			{
				return;
			}
			DynamicBuffer<Game.Areas.SubArea> dynamicBuffer3 = m_SubAreas[entity];
			for (int j = 0; j < dynamicBuffer3.Length; j++)
			{
				Entity area = dynamicBuffer3[j].m_Area;
				e = m_CommandBuffer.CreateEntity();
				CreationDefinition component5 = new CreationDefinition
				{
					m_Original = area
				};
				if (delete)
				{
					component5.m_Flags |= CreationFlags.Delete;
					if (!m_LotData.HasComponent(area))
					{
						component5.m_Flags |= CreationFlags.Hidden;
					}
				}
				m_CommandBuffer.AddComponent(e, component5);
				m_CommandBuffer.AddComponent(e, default(Updated));
				if (ownerDefinition.m_Prefab != Entity.Null)
				{
					m_CommandBuffer.AddComponent(e, ownerDefinition);
				}
				DynamicBuffer<Game.Areas.Node> dynamicBuffer4 = m_AreaNodes[area];
				m_CommandBuffer.AddBuffer<Game.Areas.Node>(e).CopyFrom(dynamicBuffer4.AsNativeArray());
				if (m_CachedNodes.HasBuffer(area))
				{
					DynamicBuffer<LocalNodeCache> dynamicBuffer5 = m_CachedNodes[area];
					m_CommandBuffer.AddBuffer<LocalNodeCache>(e).CopyFrom(dynamicBuffer5.AsNativeArray());
				}
				if (!m_SubObjects.TryGetBuffer(area, out bufferData))
				{
					continue;
				}
				for (int k = 0; k < bufferData.Length; k++)
				{
					Game.Objects.SubObject subObject = bufferData[k];
					if (m_BuildingData.HasComponent(subObject.m_SubObject))
					{
						AddEntity(ownerDefinitions, bulldozeEntities, attachedEntities, subObject.m_SubObject, Entity.Null, default(OwnerDefinition), upgrade: false, parent: false, delete: true);
					}
				}
			}
		}

		private void AddAttachedParent(NativeHashSet<Entity> bulldozeEntities, NativeHashMap<Entity, OwnerDefinition> ownerDefinitions, NativeHashSet<Entity> attachedEntities, Attached attached)
		{
			Entity entity = attached.m_Parent;
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity, out componentData))
			{
				entity = componentData.m_Owner;
				if (bulldozeEntities.Contains(entity) || ownerDefinitions.ContainsKey(entity))
				{
					return;
				}
			}
			if (m_EdgeData.HasComponent(attached.m_Parent))
			{
				Edge edge = m_EdgeData[attached.m_Parent];
				if (!bulldozeEntities.Contains(attached.m_Parent) && !bulldozeEntities.Contains(edge.m_Start) && !bulldozeEntities.Contains(edge.m_End) && attachedEntities.Add(attached.m_Parent))
				{
					Entity e = m_CommandBuffer.CreateEntity();
					CreationDefinition component = new CreationDefinition
					{
						m_Original = attached.m_Parent
					};
					component.m_Flags |= CreationFlags.Align;
					m_CommandBuffer.AddComponent(e, component);
					m_CommandBuffer.AddComponent(e, default(Updated));
					NetCourse component2 = default(NetCourse);
					component2.m_Curve = m_CurveData[attached.m_Parent].m_Bezier;
					component2.m_Length = MathUtils.Length(component2.m_Curve);
					component2.m_FixedIndex = -1;
					component2.m_StartPosition.m_Entity = edge.m_Start;
					component2.m_StartPosition.m_Position = component2.m_Curve.a;
					component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve));
					component2.m_StartPosition.m_CourseDelta = 0f;
					component2.m_EndPosition.m_Entity = edge.m_End;
					component2.m_EndPosition.m_Position = component2.m_Curve.d;
					component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve));
					component2.m_EndPosition.m_CourseDelta = 1f;
					m_CommandBuffer.AddComponent(e, component2);
				}
			}
			else if (m_NodeData.HasComponent(attached.m_Parent) && !bulldozeEntities.Contains(attached.m_Parent) && attachedEntities.Add(attached.m_Parent))
			{
				Game.Net.Node node = m_NodeData[attached.m_Parent];
				Entity e2 = m_CommandBuffer.CreateEntity();
				CreationDefinition component3 = new CreationDefinition
				{
					m_Original = attached.m_Parent
				};
				m_CommandBuffer.AddComponent(e2, component3);
				m_CommandBuffer.AddComponent(e2, default(Updated));
				NetCourse component4 = new NetCourse
				{
					m_Curve = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position),
					m_Length = 0f,
					m_FixedIndex = -1,
					m_StartPosition = 
					{
						m_Entity = attached.m_Parent,
						m_Position = node.m_Position,
						m_Rotation = node.m_Rotation,
						m_CourseDelta = 0f
					},
					m_EndPosition = 
					{
						m_Entity = attached.m_Parent,
						m_Position = node.m_Position,
						m_Rotation = node.m_Rotation,
						m_CourseDelta = 1f
					}
				};
				m_CommandBuffer.AddComponent(e2, component4);
			}
		}

		private void AddSubNets(NativeHashSet<Entity> bulldozeEntities, Entity entity, OwnerDefinition ownerDefinition, bool delete)
		{
			DynamicBuffer<Game.Net.SubNet> dynamicBuffer = m_SubNets[entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subNet = dynamicBuffer[i].m_SubNet;
				if (m_NodeData.HasComponent(subNet))
				{
					if (!HasEdgeStartOrEnd(subNet, entity) && !bulldozeEntities.Contains(subNet))
					{
						Game.Net.Node node = m_NodeData[subNet];
						Entity e = m_CommandBuffer.CreateEntity();
						CreationDefinition component = new CreationDefinition
						{
							m_Original = subNet
						};
						if (delete)
						{
							component.m_Flags |= CreationFlags.Delete;
						}
						if (m_EditorContainerData.HasComponent(subNet))
						{
							component.m_SubPrefab = m_EditorContainerData[subNet].m_Prefab;
						}
						m_CommandBuffer.AddComponent(e, component);
						m_CommandBuffer.AddComponent(e, default(Updated));
						if (ownerDefinition.m_Prefab != Entity.Null)
						{
							m_CommandBuffer.AddComponent(e, ownerDefinition);
						}
						NetCourse component2 = new NetCourse
						{
							m_Curve = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position),
							m_Length = 0f,
							m_FixedIndex = -1,
							m_StartPosition = 
							{
								m_Entity = subNet,
								m_Position = node.m_Position,
								m_Rotation = node.m_Rotation,
								m_CourseDelta = 0f
							},
							m_EndPosition = 
							{
								m_Entity = subNet,
								m_Position = node.m_Position,
								m_Rotation = node.m_Rotation,
								m_CourseDelta = 1f
							}
						};
						m_CommandBuffer.AddComponent(e, component2);
					}
				}
				else
				{
					if (!m_EdgeData.HasComponent(subNet))
					{
						continue;
					}
					Edge edge = m_EdgeData[subNet];
					if (!bulldozeEntities.Contains(subNet) && !bulldozeEntities.Contains(edge.m_Start) && !bulldozeEntities.Contains(edge.m_End))
					{
						Entity e2 = m_CommandBuffer.CreateEntity();
						CreationDefinition component3 = new CreationDefinition
						{
							m_Original = subNet
						};
						if (delete)
						{
							component3.m_Flags |= CreationFlags.Delete;
						}
						if (m_EditorContainerData.HasComponent(subNet))
						{
							component3.m_SubPrefab = m_EditorContainerData[subNet].m_Prefab;
						}
						m_CommandBuffer.AddComponent(e2, component3);
						m_CommandBuffer.AddComponent(e2, default(Updated));
						if (ownerDefinition.m_Prefab != Entity.Null)
						{
							m_CommandBuffer.AddComponent(e2, ownerDefinition);
						}
						NetCourse component4 = default(NetCourse);
						component4.m_Curve = m_CurveData[subNet].m_Bezier;
						component4.m_Length = MathUtils.Length(component4.m_Curve);
						component4.m_FixedIndex = -1;
						component4.m_StartPosition.m_Entity = edge.m_Start;
						component4.m_StartPosition.m_Position = component4.m_Curve.a;
						component4.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component4.m_Curve));
						component4.m_StartPosition.m_CourseDelta = 0f;
						component4.m_EndPosition.m_Entity = edge.m_End;
						component4.m_EndPosition.m_Position = component4.m_Curve.d;
						component4.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component4.m_Curve));
						component4.m_EndPosition.m_CourseDelta = 1f;
						m_CommandBuffer.AddComponent(e2, component4);
						if (m_SubNets.HasBuffer(subNet))
						{
							AddSubNets(ownerDefinition: new OwnerDefinition
							{
								m_Prefab = m_PrefabRefData[subNet].m_Prefab,
								m_Position = component4.m_Curve.a,
								m_Rotation = new float4(component4.m_Curve.d, 0f)
							}, bulldozeEntities: bulldozeEntities, entity: subNet, delete: delete);
						}
					}
				}
			}
		}

		private bool HasEdgeStartOrEnd(Entity node, Entity owner)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				if ((edge2.m_Start == node || edge2.m_End == node) && m_OwnerData.HasComponent(edge) && m_OwnerData[edge].m_Owner == owner)
				{
					return true;
				}
			}
			return false;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Static> __Game_Objects_Static_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Fixed> __Game_Net_Fixed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Placeholder> __Game_Objects_Placeholder_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Static_RO_ComponentLookup = state.GetComponentLookup<Static>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Tools_LocalNodeCache_RO_BufferLookup = state.GetBufferLookup<LocalNodeCache>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
			__Game_Net_Fixed_RO_ComponentLookup = state.GetComponentLookup<Fixed>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<EditorContainer>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Objects_Placeholder_RO_ComponentLookup = state.GetComponentLookup<Placeholder>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
		}
	}

	public const string kToolID = "Bulldoze Tool";

	public Action EventConfirmationRequested;

	private ToolOutputBarrier m_ToolOutputBarrier;

	private AudioManager m_AudioManager;

	private AchievementTriggerSystem m_AchievementTriggerSystem;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_RoadQuery;

	private EntityQuery m_PlantQuery;

	private EntityQuery m_SoundQuery;

	private ControlPoint m_LastRaycastPoint;

	private State m_State;

	private NativeList<ControlPoint> m_ControlPoints;

	private IProxyAction m_Bulldoze;

	private IProxyAction m_BulldozeDiscard;

	private bool m_ApplyBlocked;

	private TypeHandle __TypeHandle;

	public override string toolID => "Bulldoze Tool";

	public override int uiModeIndex => (int)actualMode;

	public override bool allowUnderground => true;

	public Mode mode { get; set; }

	public Mode actualMode
	{
		get
		{
			if (!m_ToolSystem.actionMode.IsEditor())
			{
				return Mode.MainElements;
			}
			return mode;
		}
	}

	public bool underground { get; set; }

	public bool allowManipulation { get; set; }

	public bool debugBypassBulldozeConfirmation { get; set; }

	public BulldozePrefab prefab { get; set; }

	private protected override IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield return m_Bulldoze;
			yield return m_BulldozeDiscard;
		}
	}

	public override void GetUIModes(List<ToolMode> modes)
	{
		modes.Add(new ToolMode(Mode.MainElements.ToString(), 0));
		if (m_ToolSystem.actionMode.IsEditor())
		{
			modes.Add(new ToolMode(Mode.SubElements.ToString(), 1));
			modes.Add(new ToolMode(Mode.Everything.ToString(), 2));
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_AchievementTriggerSystem = base.World.GetOrCreateSystemManaged<AchievementTriggerSystem>();
		m_ControlPoints = new NativeList<ControlPoint>(4, Allocator.Persistent);
		m_DefinitionQuery = GetDefinitionQuery();
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Temp>(), ComponentType.Exclude<Deleted>());
		m_RoadQuery = GetEntityQuery(ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Temp>(), ComponentType.Exclude<Deleted>());
		m_PlantQuery = GetEntityQuery(ComponentType.ReadOnly<Plant>(), ComponentType.ReadOnly<Temp>(), ComponentType.Exclude<Deleted>());
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_Bulldoze = InputManager.instance.toolActionCollection.GetActionState("Bulldoze", "BulldozeToolSystem");
		m_BulldozeDiscard = InputManager.instance.toolActionCollection.GetActionState("Bulldoze Discard", "BulldozeToolSystem");
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ControlPoints.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_ControlPoints.Clear();
		m_LastRaycastPoint = default(ControlPoint);
		m_State = State.Default;
		m_ApplyBlocked = false;
		base.requireUnderground = false;
		base.requireStopIcons = false;
		base.requireAreas = AreaTypeMask.None;
		base.requireNet = Layer.None;
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			base.applyActionOverride = m_Bulldoze;
			base.applyAction.shouldBeEnabled = base.actionsEnabled && m_State != State.Waiting && m_ControlPoints.Length != 0;
			base.cancelActionOverride = m_BulldozeDiscard;
			base.cancelAction.shouldBeEnabled = base.actionsEnabled && m_State == State.Applying && IsMultiSelection();
		}
	}

	private bool IsMultiSelection()
	{
		if (m_ControlPoints.Length == 0)
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Net.Node>(m_ControlPoints[0].m_OriginalEntity) || base.EntityManager.HasComponent<Edge>(m_ControlPoints[0].m_OriginalEntity))
		{
			return m_ControlPoints.Length > 4;
		}
		return m_ControlPoints.Length > 1;
	}

	public override PrefabBase GetPrefab()
	{
		return prefab;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		if (prefab is BulldozePrefab bulldozePrefab)
		{
			this.prefab = bulldozePrefab;
			return true;
		}
		return false;
	}

	public override void SetUnderground(bool underground)
	{
		this.underground = underground;
	}

	public override void ElevationUp()
	{
		underground = false;
	}

	public override void ElevationDown()
	{
		underground = true;
	}

	public override void ElevationScroll()
	{
		underground = !underground;
	}

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
		m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Net;
		m_ToolRaycastSystem.netLayerMask = Layer.All;
		m_ToolRaycastSystem.raycastFlags |= RaycastFlags.BuildingLots;
		if (underground)
		{
			m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
		}
		else
		{
			m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
		}
		switch (actualMode)
		{
		case Mode.SubElements:
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements | RaycastFlags.NoMainElements;
			break;
		case Mode.Everything:
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;
			break;
		}
		if (m_ToolSystem.actionMode.IsEditor())
		{
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Markers | RaycastFlags.UpgradeIsMain | RaycastFlags.EditorContainers;
			m_ToolRaycastSystem.typeMask |= TypeMask.Areas;
			if (underground)
			{
				m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.Spaces;
			}
			else
			{
				m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.Lots | AreaTypeMask.Spaces | AreaTypeMask.Surfaces;
			}
		}
		else
		{
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubBuildings;
			if (!underground)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.Areas;
				m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.Lots | AreaTypeMask.Surfaces;
			}
		}
		if (allowManipulation)
		{
			m_ToolRaycastSystem.typeMask |= TypeMask.MovingObjects;
		}
		m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders | RaycastFlags.Decals;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		base.requireUnderground = underground;
		base.requireStopIcons = true;
		if (underground)
		{
			base.requireAreas = (m_ToolSystem.actionMode.IsEditor() ? AreaTypeMask.Spaces : AreaTypeMask.None);
			base.requireNet = Layer.None;
		}
		else
		{
			base.requireAreas = (m_ToolSystem.actionMode.IsEditor() ? (AreaTypeMask.Lots | AreaTypeMask.Spaces | AreaTypeMask.Surfaces) : AreaTypeMask.None);
			base.requireNet = Layer.Waterway;
		}
		UpdateActions();
		if (m_State == State.Applying && !base.applyAction.enabled)
		{
			m_State = State.Default;
			m_ControlPoints.Clear();
			base.applyMode = ApplyMode.Clear;
			inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		}
		switch (m_State)
		{
		case State.Default:
			if (m_ApplyBlocked)
			{
				if (base.applyAction.WasReleasedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame())
				{
					m_ApplyBlocked = false;
				}
				return Update(inputDeps, fullUpdate: false);
			}
			if (base.cancelAction.IsInProgress())
			{
				base.applyMode = ApplyMode.None;
				return inputDeps;
			}
			if (m_ControlPoints.Length > 0 && base.applyAction.WasPressedThisFrame())
			{
				m_State = State.Applying;
				return Update(inputDeps, fullUpdate: true);
			}
			return Update(inputDeps, fullUpdate: false);
		case State.Applying:
			if (base.cancelAction.IsInProgress())
			{
				m_State = State.Default;
				m_ApplyBlocked = true;
				if (m_ControlPoints.Length >= 2)
				{
					m_ControlPoints.RemoveRange(0, m_ControlPoints.Length - 1);
				}
				return Update(inputDeps, fullUpdate: true);
			}
			if (!base.applyAction.IsInProgress())
			{
				if (!m_BuildingQuery.IsEmptyIgnoreFilter && !m_ToolSystem.actionMode.IsEditor() && EventConfirmationRequested != null && !debugBypassBulldozeConfirmation && ConfirmationNeeded())
				{
					m_State = State.Waiting;
					base.applyMode = ApplyMode.None;
					EventConfirmationRequested();
					return inputDeps;
				}
				m_State = State.Default;
				return Apply(inputDeps);
			}
			return Update(inputDeps, fullUpdate: false);
		case State.Confirmed:
			m_State = State.Default;
			return Apply(inputDeps);
		case State.Cancelled:
			m_State = State.Default;
			if (m_ControlPoints.Length >= 2)
			{
				m_ControlPoints.RemoveRange(0, m_ControlPoints.Length - 1);
			}
			return Update(inputDeps, fullUpdate: true);
		default:
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
	}

	private bool ConfirmationNeeded()
	{
		NativeArray<Entity> nativeArray = m_BuildingQuery.ToEntityArray(Allocator.TempJob);
		bool result = false;
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity entity = nativeArray[i];
			if ((base.EntityManager.GetComponentData<Temp>(entity).m_Flags & TempFlags.Delete) != 0 && base.EntityManager.TryGetComponent<PrefabRef>(entity, out var component) && (!base.EntityManager.HasComponent<SpawnableBuildingData>(component.m_Prefab) || base.EntityManager.HasComponent<SignatureBuildingData>(component.m_Prefab)))
			{
				result = true;
			}
		}
		nativeArray.Dispose();
		return result;
	}

	public void ConfirmAction(bool confirm)
	{
		if (m_State == State.Waiting)
		{
			m_State = (confirm ? State.Confirmed : State.Cancelled);
		}
	}

	private JobHandle Apply(JobHandle inputDeps)
	{
		if (GetAllowApply())
		{
			int num = m_BuildingQuery.CalculateEntityCount();
			if (num > 0 || m_RoadQuery.CalculateEntityCount() > 0)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_BulldozeSound);
			}
			else
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PropPlantBulldozeSound);
			}
			if (num > 0 && m_ToolSystem.actionMode.IsGame())
			{
				m_AchievementTriggerSystem.m_SquasherDownerBuffer.AddProgress(num);
			}
			base.applyMode = ApplyMode.Apply;
			m_LastRaycastPoint = default(ControlPoint);
			m_ControlPoints.Clear();
			return DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		}
		if (m_ControlPoints.Length >= 2)
		{
			m_ControlPoints.RemoveRange(0, m_ControlPoints.Length - 1);
		}
		return Update(inputDeps, fullUpdate: true);
	}

	protected override bool GetRaycastResult(out ControlPoint controlPoint)
	{
		if (GetRaycastResult(out Entity entity, out RaycastHit hit))
		{
			if (m_ToolSystem.actionMode.IsEditor() && base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(entity) && base.EntityManager.TryGetComponent<Owner>(entity, out var component))
			{
				controlPoint.m_OriginalEntity = component.m_Owner;
			}
			if (base.EntityManager.HasComponent<Game.Net.Node>(entity) && base.EntityManager.HasComponent<Edge>(hit.m_HitEntity))
			{
				entity = hit.m_HitEntity;
			}
			controlPoint = new ControlPoint(entity, hit);
			return true;
		}
		controlPoint = default(ControlPoint);
		return false;
	}

	protected override bool GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate)
	{
		if (GetRaycastResult(out var entity, out var hit, out forceUpdate))
		{
			if (m_ToolSystem.actionMode.IsEditor() && base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(entity) && base.EntityManager.TryGetComponent<Owner>(entity, out var component))
			{
				controlPoint.m_OriginalEntity = component.m_Owner;
			}
			if (base.EntityManager.HasComponent<Game.Net.Node>(entity) && base.EntityManager.HasComponent<Edge>(hit.m_HitEntity))
			{
				entity = hit.m_HitEntity;
			}
			controlPoint = new ControlPoint(entity, hit);
			return true;
		}
		controlPoint = default(ControlPoint);
		return false;
	}

	private JobHandle Update(JobHandle inputDeps, bool fullUpdate)
	{
		if (GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate))
		{
			fullUpdate = fullUpdate || forceUpdate;
			if (m_ControlPoints.Length == 0)
			{
				base.applyMode = ApplyMode.Clear;
				m_ControlPoints.Add(in controlPoint);
				inputDeps = SnapControlPoints(inputDeps);
				inputDeps = UpdateDefinitions(inputDeps);
			}
			else
			{
				base.applyMode = ApplyMode.None;
				if (fullUpdate || !m_LastRaycastPoint.Equals(controlPoint))
				{
					m_LastRaycastPoint = controlPoint;
					ControlPoint controlPoint2 = m_ControlPoints[m_ControlPoints.Length - 1];
					if (m_State == State.Applying && controlPoint.m_OriginalEntity != m_ControlPoints[m_ControlPoints.Length - 1].m_OriginalEntity)
					{
						m_ControlPoints.Add(in controlPoint);
					}
					else
					{
						m_ControlPoints[m_ControlPoints.Length - 1] = controlPoint;
					}
					inputDeps = SnapControlPoints(inputDeps);
					JobHandle.ScheduleBatchedJobs();
					inputDeps.Complete();
					ControlPoint other = default(ControlPoint);
					if (m_ControlPoints.Length != 0)
					{
						other = m_ControlPoints[m_ControlPoints.Length - 1];
					}
					if (fullUpdate || !controlPoint2.EqualsIgnoreHit(other))
					{
						base.applyMode = ApplyMode.Clear;
						inputDeps = UpdateDefinitions(inputDeps);
					}
				}
			}
		}
		else
		{
			if (m_State == State.Default)
			{
				m_ControlPoints.Clear();
			}
			base.applyMode = ApplyMode.Clear;
			inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		}
		return inputDeps;
	}

	private JobHandle SnapControlPoints(JobHandle inputDeps)
	{
		return IJobExtensions.Schedule(new SnapJob
		{
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_Mode = actualMode,
			m_State = m_State,
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StaticData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Static_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_ControlPoints = m_ControlPoints
		}, inputDeps);
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps)
	{
		JobHandle job = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		JobHandle jobHandle = IJobExtensions.Schedule(new CreateDefinitionsJob
		{
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_Mode = actualMode,
			m_State = m_State,
			m_ControlPoints = m_ControlPoints,
			m_CachedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FixedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Fixed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceholderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
		}, inputDeps);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
		return JobHandle.CombineDependencies(job, jobHandle);
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
	public BulldozeToolSystem()
	{
	}
}
