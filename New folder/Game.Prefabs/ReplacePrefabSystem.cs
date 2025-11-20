using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Rendering;
using Game.Areas;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Game.Tools;
using Game.Tutorials;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class ReplacePrefabSystem : GameSystemBase
{
	private struct ReplaceMesh
	{
		public Entity m_OldMesh;

		public Entity m_NewMesh;
	}

	public struct ReplacePrefabData
	{
		public Entity m_OldPrefab;

		public Entity m_SourceInstance;

		public bool m_AreasUpdated;

		public bool m_NetsUpdated;

		public bool m_LanesUpdated;
	}

	[CompilerGenerated]
	public new class Finalize : GameSystemBase
	{
		public struct AreaKey : IEquatable<AreaKey>
		{
			public Entity m_Prefab;

			public float3 m_StartLocation;

			public int m_NodeCount;

			public bool Equals(AreaKey other)
			{
				if (m_Prefab == other.m_Prefab && m_StartLocation.Equals(other.m_StartLocation))
				{
					return m_NodeCount == other.m_NodeCount;
				}
				return false;
			}

			public override int GetHashCode()
			{
				return m_Prefab.GetHashCode() ^ m_StartLocation.GetHashCode() ^ m_NodeCount.GetHashCode();
			}
		}

		public struct NetKey : IEquatable<NetKey>
		{
			public Entity m_Prefab;

			public float3 m_StartLocation;

			public float3 m_EndLocation;

			public bool Equals(NetKey other)
			{
				if (m_Prefab == other.m_Prefab && m_StartLocation.Equals(other.m_StartLocation))
				{
					return m_EndLocation.Equals(other.m_EndLocation);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return m_Prefab.GetHashCode() ^ m_StartLocation.GetHashCode() ^ m_EndLocation.GetHashCode();
			}
		}

		[BurstCompile]
		private struct UpdateInstanceElementsJob : IJobParallelFor
		{
			[ReadOnly]
			public ComponentLookup<Deleted> m_DeletedData;

			[ReadOnly]
			public ComponentLookup<Owner> m_OwnerData;

			[ReadOnly]
			public ComponentLookup<Temp> m_TempData;

			[ReadOnly]
			public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerData;

			[ReadOnly]
			public ComponentLookup<Transform> m_TransformData;

			[ReadOnly]
			public ComponentLookup<Edge> m_EdgeData;

			[ReadOnly]
			public ComponentLookup<PrefabRef> m_PrefabRefData;

			[ReadOnly]
			public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

			[ReadOnly]
			public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

			[ReadOnly]
			public BufferLookup<Game.Objects.SubObject> m_SubObjects;

			[ReadOnly]
			public BufferLookup<Game.Areas.SubArea> m_SubAreas;

			[ReadOnly]
			public BufferLookup<Game.Net.SubNet> m_SubNets;

			[ReadOnly]
			public BufferLookup<ConnectedEdge> m_ConnectedEdges;

			[ReadOnly]
			public BufferLookup<SubArea> m_PrefabSubAreas;

			[ReadOnly]
			public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;

			[ReadOnly]
			public BufferLookup<SubNet> m_PrefabSubNets;

			[ReadOnly]
			public BufferLookup<SubLane> m_PrefabSubLanes;

			[ReadOnly]
			public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

			[ReadOnly]
			public bool m_EditorMode;

			[ReadOnly]
			public bool m_LefthandTraffic;

			[ReadOnly]
			public Entity m_LaneContainer;

			[ReadOnly]
			public RandomSeed m_RandomSeed;

			[ReadOnly]
			public NativeArray<Entity> m_Instances;

			[ReadOnly]
			public NativeHashMap<Entity, ReplacePrefabData> m_ReplacePrefabData;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public void Execute(int index)
			{
				Entity entity = m_Instances[index];
				Unity.Mathematics.Random random = m_RandomSeed.GetRandom(index);
				NativeParallelHashMap<Entity, int> selectedSpawnables = default(NativeParallelHashMap<Entity, int>);
				if (!m_TempData.HasComponent(entity) && m_PrefabRefData.TryGetComponent(entity, out var componentData) && m_ReplacePrefabData.TryGetValue(componentData.m_Prefab, out var item) && item.m_SourceInstance != entity && m_TransformData.TryGetComponent(entity, out var componentData2))
				{
					bool flag = !m_OwnerData.HasComponent(entity);
					bool flag2 = flag && m_EditorMode && item.m_LanesUpdated;
					if (item.m_AreasUpdated)
					{
						UpdateAreas(index, entity, componentData.m_Prefab, componentData2, flag, ref random, ref selectedSpawnables);
					}
					if (item.m_NetsUpdated || flag2)
					{
						UpdateNets(index, entity, componentData.m_Prefab, componentData2, flag, item.m_NetsUpdated, flag2, ref random);
					}
				}
				if (selectedSpawnables.IsCreated)
				{
					selectedSpawnables.Dispose();
				}
			}

			private void UpdateAreas(int jobIndex, Entity entity, Entity newPrefab, Transform transform, bool isTopLevel, ref Unity.Mathematics.Random random, ref NativeParallelHashMap<Entity, int> selectedSpawnables)
			{
				if (m_SubObjects.TryGetBuffer(entity, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						Entity subObject = bufferData[i].m_SubObject;
						m_CommandBuffer.AddComponent(jobIndex, subObject, default(Updated));
					}
				}
				if (m_SubAreas.TryGetBuffer(entity, out var bufferData2))
				{
					for (int j = 0; j < bufferData2.Length; j++)
					{
						m_CommandBuffer.AddComponent<Deleted>(jobIndex, bufferData2[j].m_Area);
					}
				}
				if (m_PrefabSubAreas.TryGetBuffer(newPrefab, out var bufferData3))
				{
					if (!bufferData2.IsCreated)
					{
						m_CommandBuffer.AddBuffer<Game.Areas.SubArea>(jobIndex, entity);
					}
					if (selectedSpawnables.IsCreated)
					{
						selectedSpawnables.Clear();
					}
					CreateAreas(jobIndex, entity, transform, bufferData3, m_PrefabSubAreaNodes[newPrefab], ref random, ref selectedSpawnables);
				}
				else if ((!m_EditorMode || !isTopLevel) && bufferData2.IsCreated)
				{
					m_CommandBuffer.RemoveComponent<Game.Areas.SubArea>(jobIndex, entity);
				}
			}

			private void UpdateNets(int jobIndex, Entity entity, Entity newPrefab, Transform transform, bool isTopLevel, bool updateNets, bool updateLanes, ref Unity.Mathematics.Random random)
			{
				if (m_SubNets.TryGetBuffer(entity, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						Game.Net.SubNet subNet = bufferData[i];
						if (m_EditorContainerData.HasComponent(subNet.m_SubNet))
						{
							if (!updateLanes)
							{
								continue;
							}
						}
						else if (!updateNets)
						{
							continue;
						}
						bool flag = true;
						if (m_ConnectedEdges.TryGetBuffer(subNet.m_SubNet, out var bufferData2))
						{
							for (int j = 0; j < bufferData2.Length; j++)
							{
								Entity edge = bufferData2[j].m_Edge;
								if ((!m_OwnerData.TryGetComponent(edge, out var componentData) || (!(componentData.m_Owner == entity) && !m_DeletedData.HasComponent(componentData.m_Owner))) && !m_DeletedData.HasComponent(edge))
								{
									Edge edge2 = m_EdgeData[edge];
									if (edge2.m_Start == subNet.m_SubNet || edge2.m_End == subNet.m_SubNet)
									{
										flag = false;
									}
									m_CommandBuffer.AddComponent(jobIndex, edge, default(Updated));
									if (!m_DeletedData.HasComponent(edge2.m_Start))
									{
										m_CommandBuffer.AddComponent(jobIndex, edge2.m_Start, default(Updated));
									}
									if (!m_DeletedData.HasComponent(edge2.m_End))
									{
										m_CommandBuffer.AddComponent(jobIndex, edge2.m_End, default(Updated));
									}
								}
							}
						}
						if (flag)
						{
							m_CommandBuffer.AddComponent<Deleted>(jobIndex, subNet.m_SubNet);
							continue;
						}
						m_CommandBuffer.RemoveComponent<Owner>(jobIndex, subNet.m_SubNet);
						m_CommandBuffer.AddComponent(jobIndex, subNet.m_SubNet, default(Updated));
					}
				}
				m_PrefabSubNets.TryGetBuffer(newPrefab, out var bufferData3);
				m_PrefabSubLanes.TryGetBuffer(newPrefab, out var bufferData4);
				if (bufferData3.IsCreated || (m_EditorMode && isTopLevel && bufferData4.IsCreated))
				{
					if (bufferData.IsCreated)
					{
						DynamicBuffer<Game.Net.SubNet> dynamicBuffer = m_CommandBuffer.SetBuffer<Game.Net.SubNet>(jobIndex, entity);
						if (!updateNets || !updateLanes)
						{
							for (int k = 0; k < bufferData.Length; k++)
							{
								Game.Net.SubNet elem = bufferData[k];
								if (m_EditorContainerData.HasComponent(elem.m_SubNet))
								{
									if (updateLanes)
									{
										continue;
									}
								}
								else if (updateNets)
								{
									continue;
								}
								dynamicBuffer.Add(elem);
							}
						}
					}
					else
					{
						m_CommandBuffer.AddBuffer<Game.Net.SubNet>(jobIndex, entity);
					}
					if (updateNets && bufferData3.IsCreated)
					{
						CreateNets(jobIndex, entity, transform, bufferData3, ref random);
					}
					if (updateLanes && bufferData4.IsCreated)
					{
						CreateLanes(jobIndex, entity, transform, bufferData4, ref random);
					}
				}
				else if (bufferData.IsCreated)
				{
					if (m_EditorMode && isTopLevel)
					{
						m_CommandBuffer.SetBuffer<Game.Net.SubNet>(jobIndex, entity);
					}
					else
					{
						m_CommandBuffer.RemoveComponent<Game.Net.SubNet>(jobIndex, entity);
					}
				}
			}

			private void CreateAreas(int jobIndex, Entity owner, Transform transform, DynamicBuffer<SubArea> subAreas, DynamicBuffer<SubAreaNode> subAreaNodes, ref Unity.Mathematics.Random random, ref NativeParallelHashMap<Entity, int> selectedSpawnables)
			{
				for (int i = 0; i < subAreas.Length; i++)
				{
					SubArea subArea = subAreas[i];
					int seed;
					if (!m_EditorMode && m_PrefabPlaceholderElements.TryGetBuffer(subArea.m_Prefab, out var bufferData))
					{
						if (!selectedSpawnables.IsCreated)
						{
							selectedSpawnables = new NativeParallelHashMap<Entity, int>(10, Allocator.Temp);
						}
						if (!AreaUtils.SelectAreaPrefab(bufferData, m_PrefabSpawnableObjectData, selectedSpawnables, ref random, out subArea.m_Prefab, out seed))
						{
							continue;
						}
					}
					else
					{
						seed = random.NextInt();
					}
					Entity e = m_CommandBuffer.CreateEntity(jobIndex);
					CreationDefinition component = new CreationDefinition
					{
						m_Prefab = subArea.m_Prefab,
						m_Owner = owner,
						m_RandomSeed = seed
					};
					component.m_Flags |= CreationFlags.Permanent;
					m_CommandBuffer.AddComponent(jobIndex, e, component);
					m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
					DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_CommandBuffer.AddBuffer<Game.Areas.Node>(jobIndex, e);
					dynamicBuffer.ResizeUninitialized(subArea.m_NodeRange.y - subArea.m_NodeRange.x + 1);
					DynamicBuffer<LocalNodeCache> dynamicBuffer2 = default(DynamicBuffer<LocalNodeCache>);
					if (m_EditorMode)
					{
						dynamicBuffer2 = m_CommandBuffer.AddBuffer<LocalNodeCache>(jobIndex, e);
						dynamicBuffer2.ResizeUninitialized(dynamicBuffer.Length);
					}
					int num = ObjectToolBaseSystem.GetFirstNodeIndex(subAreaNodes, subArea.m_NodeRange);
					int num2 = 0;
					for (int j = subArea.m_NodeRange.x; j <= subArea.m_NodeRange.y; j++)
					{
						float3 position = subAreaNodes[num].m_Position;
						float3 position2 = ObjectUtils.LocalToWorld(transform, position);
						int parentMesh = subAreaNodes[num].m_ParentMesh;
						float elevation = math.select(float.MinValue, position.y, parentMesh >= 0);
						dynamicBuffer[num2] = new Game.Areas.Node(position2, elevation);
						if (m_EditorMode)
						{
							dynamicBuffer2[num2] = new LocalNodeCache
							{
								m_Position = position,
								m_ParentMesh = parentMesh
							};
						}
						num2++;
						if (++num == subArea.m_NodeRange.y)
						{
							num = subArea.m_NodeRange.x;
						}
					}
				}
			}

			private void CreateNets(int jobIndex, Entity owner, Transform transform, DynamicBuffer<SubNet> subNets, ref Unity.Mathematics.Random random)
			{
				NativeList<float4> nodePositions = new NativeList<float4>(subNets.Length * 2, Allocator.Temp);
				for (int i = 0; i < subNets.Length; i++)
				{
					SubNet subNet = subNets[i];
					if (subNet.m_NodeIndex.x >= 0)
					{
						while (nodePositions.Length <= subNet.m_NodeIndex.x)
						{
							nodePositions.Add(default(float4));
						}
						nodePositions[subNet.m_NodeIndex.x] += new float4(subNet.m_Curve.a, 1f);
					}
					if (subNet.m_NodeIndex.y >= 0)
					{
						while (nodePositions.Length <= subNet.m_NodeIndex.y)
						{
							nodePositions.Add(default(float4));
						}
						nodePositions[subNet.m_NodeIndex.y] += new float4(subNet.m_Curve.d, 1f);
					}
				}
				for (int j = 0; j < nodePositions.Length; j++)
				{
					nodePositions[j] /= math.max(1f, nodePositions[j].w);
				}
				for (int k = 0; k < subNets.Length; k++)
				{
					SubNet subNet2 = NetUtils.GetSubNet(subNets, k, m_LefthandTraffic, ref m_PrefabNetGeometryData);
					CreateSubNet(jobIndex, subNet2.m_Prefab, Entity.Null, subNet2.m_Curve, subNet2.m_NodeIndex, subNet2.m_ParentMesh, subNet2.m_Upgrades, nodePositions, owner, transform, ref random);
				}
				nodePositions.Dispose();
			}

			private void CreateLanes(int jobIndex, Entity owner, Transform transform, DynamicBuffer<SubLane> subLanes, ref Unity.Mathematics.Random random)
			{
				NativeList<float4> nodePositions = new NativeList<float4>(subLanes.Length * 2, Allocator.Temp);
				for (int i = 0; i < subLanes.Length; i++)
				{
					SubLane subLane = subLanes[i];
					if (subLane.m_NodeIndex.x >= 0)
					{
						while (nodePositions.Length <= subLane.m_NodeIndex.x)
						{
							nodePositions.Add(default(float4));
						}
						nodePositions[subLane.m_NodeIndex.x] += new float4(subLane.m_Curve.a, 1f);
					}
					if (subLane.m_NodeIndex.y >= 0)
					{
						while (nodePositions.Length <= subLane.m_NodeIndex.y)
						{
							nodePositions.Add(default(float4));
						}
						nodePositions[subLane.m_NodeIndex.y] += new float4(subLane.m_Curve.d, 1f);
					}
				}
				for (int j = 0; j < nodePositions.Length; j++)
				{
					nodePositions[j] /= math.max(1f, nodePositions[j].w);
				}
				for (int k = 0; k < subLanes.Length; k++)
				{
					SubLane subLane2 = subLanes[k];
					CreateSubNet(jobIndex, m_LaneContainer, subLane2.m_Prefab, subLane2.m_Curve, subLane2.m_NodeIndex, subLane2.m_ParentMesh, default(CompositionFlags), nodePositions, owner, transform, ref random);
				}
				nodePositions.Dispose();
			}

			private void CreateSubNet(int jobIndex, Entity netPrefab, Entity lanePrefab, Bezier4x3 curve, int2 nodeIndex, int2 parentMesh, CompositionFlags upgrades, NativeList<float4> nodePositions, Entity owner, Transform transform, ref Unity.Mathematics.Random random)
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex);
				CreationDefinition component = new CreationDefinition
				{
					m_Prefab = netPrefab,
					m_SubPrefab = lanePrefab,
					m_Owner = owner,
					m_RandomSeed = random.NextInt()
				};
				component.m_Flags |= CreationFlags.Permanent;
				m_CommandBuffer.AddComponent(jobIndex, e, component);
				m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
				NetCourse component2 = default(NetCourse);
				component2.m_Curve = ObjectUtils.LocalToWorld(transform.m_Position, transform.m_Rotation, curve);
				component2.m_StartPosition.m_Position = component2.m_Curve.a;
				component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve), transform.m_Rotation);
				component2.m_StartPosition.m_CourseDelta = 0f;
				component2.m_StartPosition.m_Elevation = curve.a.y;
				component2.m_StartPosition.m_ParentMesh = parentMesh.x;
				if (nodeIndex.x >= 0)
				{
					component2.m_StartPosition.m_Position = ObjectUtils.LocalToWorld(transform.m_Position, transform.m_Rotation, nodePositions[nodeIndex.x].xyz);
				}
				component2.m_EndPosition.m_Position = component2.m_Curve.d;
				component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve), transform.m_Rotation);
				component2.m_EndPosition.m_CourseDelta = 1f;
				component2.m_EndPosition.m_Elevation = curve.d.y;
				component2.m_EndPosition.m_ParentMesh = parentMesh.y;
				if (nodeIndex.y >= 0)
				{
					component2.m_EndPosition.m_Position = ObjectUtils.LocalToWorld(transform.m_Position, transform.m_Rotation, nodePositions[nodeIndex.y].xyz);
				}
				component2.m_Length = MathUtils.Length(component2.m_Curve);
				component2.m_FixedIndex = -1;
				component2.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst | CoursePosFlags.DisableMerge;
				component2.m_EndPosition.m_Flags |= CoursePosFlags.IsLast | CoursePosFlags.DisableMerge;
				if (component2.m_StartPosition.m_Position.Equals(component2.m_EndPosition.m_Position))
				{
					component2.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
					component2.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
				}
				m_CommandBuffer.AddComponent(jobIndex, e, component2);
				if (upgrades != default(CompositionFlags))
				{
					Upgraded component3 = new Upgraded
					{
						m_Flags = upgrades
					};
					m_CommandBuffer.AddComponent(jobIndex, e, component3);
				}
				if (m_EditorMode)
				{
					LocalCurveCache component4 = new LocalCurveCache
					{
						m_Curve = curve
					};
					m_CommandBuffer.AddComponent(jobIndex, e, component4);
				}
			}
		}

		[BurstCompile]
		private struct CheckPrefabReplacesJob : IJob
		{
			[ReadOnly]
			public BufferLookup<SubArea> m_PrefabSubAreas;

			[ReadOnly]
			public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;

			[ReadOnly]
			public BufferLookup<SubNet> m_PrefabSubNets;

			[ReadOnly]
			public BufferLookup<SubLane> m_PrefabSubLanes;

			public NativeHashMap<Entity, ReplacePrefabData> m_ReplacePrefabData;

			public void Execute()
			{
				NativeArray<Entity> keyArray = m_ReplacePrefabData.GetKeyArray(Allocator.Temp);
				for (int i = 0; i < keyArray.Length; i++)
				{
					Entity entity = keyArray[i];
					ReplacePrefabData value = m_ReplacePrefabData[entity];
					value.m_AreasUpdated = CompareAreas(entity, value.m_OldPrefab);
					value.m_NetsUpdated = CompareNets(entity, value.m_OldPrefab);
					value.m_LanesUpdated = CompareLanes(entity, value.m_OldPrefab);
					m_ReplacePrefabData[entity] = value;
				}
				keyArray.Dispose();
			}

			private bool CompareAreas(Entity newPrefab, Entity oldPrefab)
			{
				m_PrefabSubAreas.TryGetBuffer(newPrefab, out var bufferData);
				m_PrefabSubAreas.TryGetBuffer(oldPrefab, out var bufferData2);
				if (!bufferData.IsCreated || !bufferData2.IsCreated)
				{
					if (!bufferData.IsCreated)
					{
						return bufferData2.IsCreated;
					}
					return true;
				}
				if (bufferData.Length != bufferData2.Length)
				{
					return true;
				}
				if (bufferData.Length == 0)
				{
					return false;
				}
				m_PrefabSubAreaNodes.TryGetBuffer(newPrefab, out var bufferData3);
				m_PrefabSubAreaNodes.TryGetBuffer(oldPrefab, out var bufferData4);
				if (bufferData3.Length != bufferData4.Length)
				{
					return true;
				}
				NativeParallelMultiHashMap<AreaKey, int> nativeParallelMultiHashMap = new NativeParallelMultiHashMap<AreaKey, int>(bufferData2.Length, Allocator.Temp);
				bool result = false;
				for (int i = 0; i < bufferData2.Length; i++)
				{
					SubArea subArea = bufferData2[i];
					int num = subArea.m_NodeRange.y - subArea.m_NodeRange.x;
					AreaKey key = new AreaKey
					{
						m_Prefab = subArea.m_Prefab,
						m_NodeCount = num
					};
					if (num != 0)
					{
						key.m_StartLocation = bufferData4[subArea.m_NodeRange.x].m_Position;
					}
					nativeParallelMultiHashMap.Add(key, i);
				}
				for (int j = 0; j < bufferData.Length; j++)
				{
					SubArea subArea2 = bufferData[j];
					int num2 = subArea2.m_NodeRange.y - subArea2.m_NodeRange.x;
					AreaKey key2 = new AreaKey
					{
						m_Prefab = subArea2.m_Prefab,
						m_NodeCount = num2
					};
					if (num2 != 0)
					{
						key2.m_StartLocation = bufferData3[subArea2.m_NodeRange.x].m_Position;
					}
					bool flag = false;
					if (nativeParallelMultiHashMap.TryGetFirstValue(key2, out var item, out var it))
					{
						do
						{
							flag = true;
							SubArea subArea3 = bufferData2[item];
							for (int k = 0; k < num2; k++)
							{
								SubAreaNode subAreaNode = bufferData3[subArea2.m_NodeRange.x + k];
								SubAreaNode subAreaNode2 = bufferData4[subArea3.m_NodeRange.x + k];
								flag &= subAreaNode.m_Position.Equals(subAreaNode2.m_Position);
								flag &= subAreaNode.m_ParentMesh == subAreaNode2.m_ParentMesh;
							}
							if (flag)
							{
								nativeParallelMultiHashMap.Remove(it);
								break;
							}
						}
						while (nativeParallelMultiHashMap.TryGetNextValue(out item, ref it));
					}
					if (!flag)
					{
						result = true;
						break;
					}
				}
				nativeParallelMultiHashMap.Dispose();
				return result;
			}

			private bool CompareNets(Entity newPrefab, Entity oldPrefab)
			{
				m_PrefabSubNets.TryGetBuffer(newPrefab, out var bufferData);
				m_PrefabSubNets.TryGetBuffer(oldPrefab, out var bufferData2);
				if (!bufferData.IsCreated || !bufferData2.IsCreated)
				{
					if (!bufferData.IsCreated)
					{
						return bufferData2.IsCreated;
					}
					return true;
				}
				if (bufferData.Length != bufferData2.Length)
				{
					return true;
				}
				if (bufferData.Length == 0)
				{
					return false;
				}
				NativeParallelMultiHashMap<NetKey, int> nativeParallelMultiHashMap = new NativeParallelMultiHashMap<NetKey, int>(bufferData2.Length, Allocator.Temp);
				bool result = false;
				for (int i = 0; i < bufferData2.Length; i++)
				{
					SubNet subNet = bufferData2[i];
					NetKey key = new NetKey
					{
						m_Prefab = subNet.m_Prefab,
						m_StartLocation = subNet.m_Curve.a,
						m_EndLocation = subNet.m_Curve.d
					};
					nativeParallelMultiHashMap.Add(key, i);
				}
				for (int j = 0; j < bufferData.Length; j++)
				{
					SubNet subNet2 = bufferData[j];
					NetKey key2 = new NetKey
					{
						m_Prefab = subNet2.m_Prefab,
						m_StartLocation = subNet2.m_Curve.a,
						m_EndLocation = subNet2.m_Curve.d
					};
					bool flag = false;
					if (nativeParallelMultiHashMap.TryGetFirstValue(key2, out var item, out var it))
					{
						do
						{
							SubNet subNet3 = bufferData2[item];
							flag = subNet2.m_Prefab == subNet3.m_Prefab && subNet2.m_Curve.Equals(subNet3.m_Curve) && subNet2.m_NodeIndex.Equals(subNet3.m_NodeIndex) && subNet2.m_ParentMesh.Equals(subNet3.m_ParentMesh) && subNet2.m_InvertMode == subNet3.m_InvertMode && subNet2.m_Upgrades == subNet3.m_Upgrades;
							if (flag)
							{
								nativeParallelMultiHashMap.Remove(it);
								break;
							}
						}
						while (nativeParallelMultiHashMap.TryGetNextValue(out item, ref it));
					}
					if (!flag)
					{
						result = true;
						break;
					}
				}
				nativeParallelMultiHashMap.Dispose();
				return result;
			}

			private bool CompareLanes(Entity newPrefab, Entity oldPrefab)
			{
				m_PrefabSubLanes.TryGetBuffer(newPrefab, out var bufferData);
				m_PrefabSubLanes.TryGetBuffer(oldPrefab, out var bufferData2);
				if (!bufferData.IsCreated || !bufferData2.IsCreated)
				{
					if (!bufferData.IsCreated)
					{
						return bufferData2.IsCreated;
					}
					return true;
				}
				if (bufferData.Length != bufferData2.Length)
				{
					return true;
				}
				if (bufferData.Length == 0)
				{
					return false;
				}
				NativeParallelMultiHashMap<NetKey, int> nativeParallelMultiHashMap = new NativeParallelMultiHashMap<NetKey, int>(bufferData2.Length, Allocator.Temp);
				bool result = false;
				for (int i = 0; i < bufferData2.Length; i++)
				{
					SubLane subLane = bufferData2[i];
					NetKey key = new NetKey
					{
						m_Prefab = subLane.m_Prefab,
						m_StartLocation = subLane.m_Curve.a,
						m_EndLocation = subLane.m_Curve.d
					};
					nativeParallelMultiHashMap.Add(key, i);
				}
				for (int j = 0; j < bufferData.Length; j++)
				{
					SubLane subLane2 = bufferData[j];
					NetKey key2 = new NetKey
					{
						m_Prefab = subLane2.m_Prefab,
						m_StartLocation = subLane2.m_Curve.a,
						m_EndLocation = subLane2.m_Curve.d
					};
					bool flag = false;
					if (nativeParallelMultiHashMap.TryGetFirstValue(key2, out var item, out var it))
					{
						do
						{
							SubLane subLane3 = bufferData2[item];
							flag = subLane2.m_Prefab == subLane3.m_Prefab && subLane2.m_Curve.Equals(subLane3.m_Curve) && subLane2.m_NodeIndex.Equals(subLane3.m_NodeIndex) && subLane2.m_ParentMesh.Equals(subLane3.m_ParentMesh);
							if (flag)
							{
								nativeParallelMultiHashMap.Remove(it);
								break;
							}
						}
						while (nativeParallelMultiHashMap.TryGetNextValue(out item, ref it));
					}
					if (!flag)
					{
						result = true;
						break;
					}
				}
				nativeParallelMultiHashMap.Dispose();
				return result;
			}
		}

		private struct TypeHandle
		{
			[ReadOnly]
			public BufferLookup<SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<SubLane> __Game_Prefabs_SubLane_RO_BufferLookup;

			[ReadOnly]
			public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<SubArea>(isReadOnly: true);
				__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(isReadOnly: true);
				__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<SubNet>(isReadOnly: true);
				__Game_Prefabs_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(isReadOnly: true);
				__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
				__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
				__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
				__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true);
				__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
				__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
				__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
				__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
				__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
				__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
				__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
				__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
				__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
				__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
			}
		}

		private ReplacePrefabSystem m_ReplacePrefabSystem;

		private EntityQuery m_LaneContainerQuery;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			m_ReplacePrefabSystem = base.World.GetOrCreateSystemManaged<ReplacePrefabSystem>();
			m_LaneContainerQuery = GetEntityQuery(ComponentType.ReadOnly<EditorContainerData>(), ComponentType.ReadOnly<NetData>());
		}

		[Preserve]
		protected override void OnUpdate()
		{
			NativeArray<Entity> instances = m_ReplacePrefabSystem.m_UpdateInstances.ToArray(Allocator.TempJob);
			if (m_ReplacePrefabSystem.m_MeshReplaces.Length != 0)
			{
				foreach (ReplaceMesh item2 in m_ReplacePrefabSystem.m_MeshReplaces)
				{
					m_ReplacePrefabSystem.m_ManagedBatchSystem.RemoveMesh(item2.m_OldMesh, item2.m_NewMesh);
				}
				m_ReplacePrefabSystem.m_MeshReplaces.Clear();
				m_ReplacePrefabSystem.m_ManagedBatchSystem.ResetSharedMeshes();
				HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
				HashSet<ComponentType> archetypeComponents = new HashSet<ComponentType>();
				hashSet.Add(ComponentType.ReadWrite<Stack>());
				hashSet.Add(ComponentType.ReadWrite<MeshColor>());
				hashSet.Add(ComponentType.ReadWrite<MeshGroup>());
				Entity item;
				while (m_ReplacePrefabSystem.m_UpdateInstances.TryDequeue(out item))
				{
					m_ReplacePrefabSystem.CheckInstanceComponents(item, hashSet, archetypeComponents);
				}
			}
			if (m_ReplacePrefabSystem.m_ReplacePrefabData.Count != 0)
			{
				EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
				CheckPrefabReplacesJob jobData = new CheckPrefabReplacesJob
				{
					m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
					m_PrefabSubAreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref base.CheckedStateRef),
					m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
					m_PrefabSubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
					m_ReplacePrefabData = m_ReplacePrefabSystem.m_ReplacePrefabData
				};
				UpdateInstanceElementsJob jobData2 = new UpdateInstanceElementsJob
				{
					m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
					m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
					m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
					m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
					m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
					m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
					m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
					m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
					m_PrefabSubAreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref base.CheckedStateRef),
					m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
					m_PrefabSubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
					m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
					m_EditorMode = m_ReplacePrefabSystem.m_ToolSystem.actionMode.IsEditor(),
					m_LefthandTraffic = m_ReplacePrefabSystem.m_CityConfigurationSystem.leftHandTraffic,
					m_RandomSeed = RandomSeed.Next(),
					m_Instances = instances,
					m_ReplacePrefabData = m_ReplacePrefabSystem.m_ReplacePrefabData,
					m_CommandBuffer = entityCommandBuffer.AsParallelWriter()
				};
				NativeArray<Entity> nativeArray = m_LaneContainerQuery.ToEntityArray(Allocator.Temp);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					if (base.EntityManager.HasComponent<NetData>(entity))
					{
						jobData2.m_LaneContainer = entity;
					}
				}
				nativeArray.Dispose();
				JobHandle dependsOn = IJobExtensions.Schedule(jobData, base.Dependency);
				IJobParallelForExtensions.Schedule(jobData2, instances.Length, 1, dependsOn).Complete();
				entityCommandBuffer.Playback(base.EntityManager);
				entityCommandBuffer.Dispose();
			}
			instances.Dispose();
			m_ReplacePrefabSystem.m_UpdateInstances.Clear();
			m_ReplacePrefabSystem.m_ReplacePrefabData.Clear();
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
		public Finalize()
		{
		}
	}

	[BurstCompile]
	private struct RemoveBatchGroupsJob : IJob
	{
		[ReadOnly]
		public Entity m_OldMeshEntity;

		[ReadOnly]
		public Entity m_NewMeshEntity;

		public BufferLookup<MeshBatch> m_MeshBatches;

		public BufferLookup<FadeBatch> m_FadeBatches;

		public BufferLookup<BatchGroup> m_BatchGroups;

		public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchGroups;

		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchInstances;

		public NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> m_NativeSubBatches;

		public EntityCommandBuffer m_EntityCommandBuffer;

		public NativeList<ReplaceMesh> m_ReplaceMeshes;

		public void Execute()
		{
			DynamicBuffer<BatchGroup> dynamicBuffer = m_BatchGroups[m_OldMeshEntity];
			NativeHashSet<Entity> updateBatches = new NativeHashSet<Entity>(10, Allocator.Temp);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				RemoveBatchGroup(dynamicBuffer[i].m_GroupIndex, dynamicBuffer[i].m_MergeIndex, updateBatches);
			}
			dynamicBuffer.Clear();
			ref NativeList<ReplaceMesh> reference = ref m_ReplaceMeshes;
			ReplaceMesh value = new ReplaceMesh
			{
				m_OldMesh = m_OldMeshEntity,
				m_NewMesh = m_NewMeshEntity
			};
			reference.Add(in value);
			NativeHashSet<Entity>.Enumerator enumerator = updateBatches.GetEnumerator();
			while (enumerator.MoveNext())
			{
				m_EntityCommandBuffer.AddComponent(enumerator.Current, default(BatchesUpdated));
			}
			enumerator.Dispose();
			updateBatches.Dispose();
		}

		private void RemoveBatchGroup(int groupIndex, int mergeIndex, NativeHashSet<Entity> updateBatches)
		{
			int groupIndex2 = groupIndex;
			if (mergeIndex != -1)
			{
				groupIndex2 = m_NativeBatchGroups.GetMergedGroupIndex(groupIndex, mergeIndex);
				m_NativeBatchGroups.RemoveMergedGroup(groupIndex, mergeIndex);
			}
			else
			{
				int mergedGroupCount = m_NativeBatchGroups.GetMergedGroupCount(groupIndex);
				if (mergedGroupCount != 0)
				{
					int mergedGroupIndex = m_NativeBatchGroups.GetMergedGroupIndex(groupIndex, 0);
					GroupData groupData = m_NativeBatchGroups.GetGroupData(mergedGroupIndex);
					DynamicBuffer<BatchGroup> dynamicBuffer = m_BatchGroups[groupData.m_Mesh];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						BatchGroup value = dynamicBuffer[i];
						if (value.m_GroupIndex == mergedGroupIndex)
						{
							value.m_MergeIndex = -1;
							dynamicBuffer[i] = value;
							break;
						}
					}
					for (int j = 1; j < mergedGroupCount; j++)
					{
						int mergedGroupIndex2 = m_NativeBatchGroups.GetMergedGroupIndex(groupIndex, j);
						groupData = m_NativeBatchGroups.GetGroupData(mergedGroupIndex2);
						dynamicBuffer = m_BatchGroups[groupData.m_Mesh];
						m_NativeBatchGroups.AddMergedGroup(mergedGroupIndex, mergedGroupIndex2);
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							BatchGroup value2 = dynamicBuffer[k];
							if (value2.m_GroupIndex == mergedGroupIndex2)
							{
								value2.m_MergeIndex = mergedGroupIndex;
								dynamicBuffer[j] = value2;
								break;
							}
						}
					}
				}
			}
			int instanceCount = m_NativeBatchInstances.GetInstanceCount(groupIndex);
			for (int l = 0; l < instanceCount; l++)
			{
				InstanceData instanceData = m_NativeBatchInstances.GetInstanceData(groupIndex, l);
				if (!m_MeshBatches.TryGetBuffer(instanceData.m_Entity, out var bufferData))
				{
					continue;
				}
				for (int m = 0; m < bufferData.Length; m++)
				{
					MeshBatch value3 = bufferData[m];
					if (value3.m_GroupIndex == groupIndex && value3.m_InstanceIndex == l)
					{
						if (m_FadeBatches.TryGetBuffer(instanceData.m_Entity, out var bufferData2))
						{
							bufferData.RemoveAtSwapBack(m);
							bufferData2.RemoveAtSwapBack(m);
							break;
						}
						value3.m_GroupIndex = -1;
						value3.m_InstanceIndex = -1;
						bufferData[m] = value3;
						updateBatches.Add(instanceData.m_Entity);
						break;
					}
				}
			}
			m_NativeBatchInstances.RemoveInstances(groupIndex, m_NativeSubBatches);
			m_NativeBatchGroups.DestroyGroup(groupIndex2, m_NativeBatchInstances, m_NativeSubBatches);
		}
	}

	[BurstCompile]
	private struct ReplacePrefabJob : IJobChunk
	{
		[ReadOnly]
		public Entity m_OldPrefab;

		[ReadOnly]
		public Entity m_NewPrefab;

		[ReadOnly]
		public Entity m_SourceInstance;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Game.Tools.EditorContainer> m_EditorContainerType;

		public BufferTypeHandle<SubObject> m_SubObjectType;

		public BufferTypeHandle<SubNet> m_SubNetType;

		public BufferTypeHandle<SubArea> m_SubAreaType;

		public BufferTypeHandle<PlaceholderObjectElement> m_PlaceholderObjectElementType;

		public BufferTypeHandle<ServiceUpgradeBuilding> m_ServiceUpgradeBuildingType;

		public BufferTypeHandle<BuildingUpgradeElement> m_BuildingUpgradeElementType;

		public BufferTypeHandle<Effect> m_EffectType;

		public BufferTypeHandle<ActivityLocationElement> m_ActivityLocationElementType;

		public BufferTypeHandle<SubMesh> m_SubMeshType;

		public BufferTypeHandle<LodMesh> m_LodMeshType;

		public BufferTypeHandle<UIGroupElement> m_UIGroupElementType;

		public BufferTypeHandle<TutorialPhaseRef> m_TutorialPhaseType;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<Entity>.ParallelWriter m_UpdateInstances;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			if (nativeArray2.Length != 0)
			{
				NativeArray<Game.Tools.EditorContainer> nativeArray3 = chunk.GetNativeArray(ref m_EditorContainerType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					PrefabRef value = nativeArray2[i];
					if (value.m_Prefab == m_OldPrefab)
					{
						value.m_Prefab = m_NewPrefab;
						nativeArray2[i] = value;
						Entity entity = nativeArray[i];
						if (m_SourceInstance != entity)
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Updated));
							m_UpdateInstances.Enqueue(entity);
						}
					}
				}
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					Game.Tools.EditorContainer value2 = nativeArray3[j];
					if (value2.m_Prefab == m_OldPrefab)
					{
						value2.m_Prefab = m_NewPrefab;
						nativeArray3[j] = value2;
					}
				}
				return;
			}
			BufferAccessor<SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			BufferAccessor<SubNet> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubNetType);
			BufferAccessor<SubArea> bufferAccessor3 = chunk.GetBufferAccessor(ref m_SubAreaType);
			BufferAccessor<PlaceholderObjectElement> bufferAccessor4 = chunk.GetBufferAccessor(ref m_PlaceholderObjectElementType);
			BufferAccessor<ServiceUpgradeBuilding> bufferAccessor5 = chunk.GetBufferAccessor(ref m_ServiceUpgradeBuildingType);
			BufferAccessor<BuildingUpgradeElement> bufferAccessor6 = chunk.GetBufferAccessor(ref m_BuildingUpgradeElementType);
			BufferAccessor<Effect> bufferAccessor7 = chunk.GetBufferAccessor(ref m_EffectType);
			BufferAccessor<ActivityLocationElement> bufferAccessor8 = chunk.GetBufferAccessor(ref m_ActivityLocationElementType);
			BufferAccessor<SubMesh> bufferAccessor9 = chunk.GetBufferAccessor(ref m_SubMeshType);
			BufferAccessor<LodMesh> bufferAccessor10 = chunk.GetBufferAccessor(ref m_LodMeshType);
			BufferAccessor<UIGroupElement> bufferAccessor11 = chunk.GetBufferAccessor(ref m_UIGroupElementType);
			BufferAccessor<TutorialPhaseRef> bufferAccessor12 = chunk.GetBufferAccessor(ref m_TutorialPhaseType);
			for (int k = 0; k < bufferAccessor.Length; k++)
			{
				DynamicBuffer<SubObject> dynamicBuffer = bufferAccessor[k];
				for (int l = 0; l < dynamicBuffer.Length; l++)
				{
					SubObject value3 = dynamicBuffer[l];
					if (value3.m_Prefab == m_OldPrefab)
					{
						value3.m_Prefab = m_NewPrefab;
						dynamicBuffer[l] = value3;
					}
				}
			}
			for (int m = 0; m < bufferAccessor2.Length; m++)
			{
				DynamicBuffer<SubNet> dynamicBuffer2 = bufferAccessor2[m];
				for (int n = 0; n < dynamicBuffer2.Length; n++)
				{
					SubNet value4 = dynamicBuffer2[n];
					if (value4.m_Prefab == m_OldPrefab)
					{
						value4.m_Prefab = m_NewPrefab;
						dynamicBuffer2[n] = value4;
					}
				}
			}
			for (int num = 0; num < bufferAccessor3.Length; num++)
			{
				DynamicBuffer<SubArea> dynamicBuffer3 = bufferAccessor3[num];
				for (int num2 = 0; num2 < dynamicBuffer3.Length; num2++)
				{
					SubArea value5 = dynamicBuffer3[num2];
					if (value5.m_Prefab == m_OldPrefab)
					{
						value5.m_Prefab = m_NewPrefab;
						dynamicBuffer3[num2] = value5;
					}
				}
			}
			for (int num3 = 0; num3 < bufferAccessor4.Length; num3++)
			{
				DynamicBuffer<PlaceholderObjectElement> dynamicBuffer4 = bufferAccessor4[num3];
				for (int num4 = 0; num4 < dynamicBuffer4.Length; num4++)
				{
					if (dynamicBuffer4[num4].m_Object == m_OldPrefab)
					{
						dynamicBuffer4.RemoveAtSwapBack(num4);
						num4--;
					}
				}
			}
			for (int num5 = 0; num5 < bufferAccessor5.Length; num5++)
			{
				DynamicBuffer<ServiceUpgradeBuilding> dynamicBuffer5 = bufferAccessor5[num5];
				for (int num6 = 0; num6 < dynamicBuffer5.Length; num6++)
				{
					ServiceUpgradeBuilding value6 = dynamicBuffer5[num6];
					if (value6.m_Building == m_OldPrefab)
					{
						value6.m_Building = m_NewPrefab;
						dynamicBuffer5[num6] = value6;
					}
				}
			}
			for (int num7 = 0; num7 < bufferAccessor6.Length; num7++)
			{
				DynamicBuffer<BuildingUpgradeElement> dynamicBuffer6 = bufferAccessor6[num7];
				for (int num8 = 0; num8 < dynamicBuffer6.Length; num8++)
				{
					if (dynamicBuffer6[num8].m_Upgrade == m_OldPrefab)
					{
						dynamicBuffer6.RemoveAtSwapBack(num8);
						num8--;
					}
				}
			}
			for (int num9 = 0; num9 < bufferAccessor7.Length; num9++)
			{
				DynamicBuffer<Effect> dynamicBuffer7 = bufferAccessor7[num9];
				for (int num10 = 0; num10 < dynamicBuffer7.Length; num10++)
				{
					Effect value7 = dynamicBuffer7[num10];
					if (value7.m_Effect == m_OldPrefab)
					{
						value7.m_Effect = m_NewPrefab;
						dynamicBuffer7[num10] = value7;
					}
				}
			}
			for (int num11 = 0; num11 < bufferAccessor8.Length; num11++)
			{
				DynamicBuffer<ActivityLocationElement> dynamicBuffer8 = bufferAccessor8[num11];
				for (int num12 = 0; num12 < dynamicBuffer8.Length; num12++)
				{
					ActivityLocationElement value8 = dynamicBuffer8[num12];
					if (value8.m_Prefab == m_OldPrefab)
					{
						value8.m_Prefab = m_NewPrefab;
						dynamicBuffer8[num12] = value8;
					}
				}
			}
			for (int num13 = 0; num13 < bufferAccessor9.Length; num13++)
			{
				DynamicBuffer<SubMesh> dynamicBuffer9 = bufferAccessor9[num13];
				for (int num14 = 0; num14 < dynamicBuffer9.Length; num14++)
				{
					SubMesh value9 = dynamicBuffer9[num14];
					if (value9.m_SubMesh == m_OldPrefab)
					{
						value9.m_SubMesh = m_NewPrefab;
						dynamicBuffer9[num14] = value9;
					}
				}
			}
			for (int num15 = 0; num15 < bufferAccessor10.Length; num15++)
			{
				DynamicBuffer<LodMesh> dynamicBuffer10 = bufferAccessor10[num15];
				for (int num16 = 0; num16 < dynamicBuffer10.Length; num16++)
				{
					LodMesh value10 = dynamicBuffer10[num16];
					if (value10.m_LodMesh == m_OldPrefab)
					{
						value10.m_LodMesh = m_NewPrefab;
						dynamicBuffer10[num16] = value10;
					}
				}
			}
			for (int num17 = 0; num17 < bufferAccessor11.Length; num17++)
			{
				DynamicBuffer<UIGroupElement> dynamicBuffer11 = bufferAccessor11[num17];
				for (int num18 = 0; num18 < dynamicBuffer11.Length; num18++)
				{
					if (dynamicBuffer11[num18].m_Prefab == m_OldPrefab)
					{
						dynamicBuffer11.RemoveAtSwapBack(num18);
						num18--;
					}
				}
			}
			for (int num19 = 0; num19 < bufferAccessor12.Length; num19++)
			{
				DynamicBuffer<TutorialPhaseRef> dynamicBuffer12 = bufferAccessor12[num19];
				for (int num20 = 0; num20 < dynamicBuffer12.Length; num20++)
				{
					TutorialPhaseRef value11 = dynamicBuffer12[num20];
					if (value11.m_Phase == m_OldPrefab)
					{
						value11.m_Phase = m_NewPrefab;
						dynamicBuffer12[num20] = value11;
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
		public BufferLookup<MeshBatch> __Game_Rendering_MeshBatch_RW_BufferLookup;

		public BufferLookup<FadeBatch> __Game_Rendering_FadeBatch_RW_BufferLookup;

		public BufferLookup<BatchGroup> __Game_Prefabs_BatchGroup_RW_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RW_ComponentTypeHandle;

		public BufferTypeHandle<SubObject> __Game_Prefabs_SubObject_RW_BufferTypeHandle;

		public BufferTypeHandle<SubNet> __Game_Prefabs_SubNet_RW_BufferTypeHandle;

		public BufferTypeHandle<SubArea> __Game_Prefabs_SubArea_RW_BufferTypeHandle;

		public BufferTypeHandle<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RW_BufferTypeHandle;

		public BufferTypeHandle<ServiceUpgradeBuilding> __Game_Prefabs_ServiceUpgradeBuilding_RW_BufferTypeHandle;

		public BufferTypeHandle<BuildingUpgradeElement> __Game_Prefabs_BuildingUpgradeElement_RW_BufferTypeHandle;

		public BufferTypeHandle<Effect> __Game_Prefabs_Effect_RW_BufferTypeHandle;

		public BufferTypeHandle<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RW_BufferTypeHandle;

		public BufferTypeHandle<SubMesh> __Game_Prefabs_SubMesh_RW_BufferTypeHandle;

		public BufferTypeHandle<LodMesh> __Game_Prefabs_LodMesh_RW_BufferTypeHandle;

		public BufferTypeHandle<UIGroupElement> __Game_Prefabs_UIGroupElement_RW_BufferTypeHandle;

		public BufferTypeHandle<TutorialPhaseRef> __Game_Tutorials_TutorialPhaseRef_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Rendering_MeshBatch_RW_BufferLookup = state.GetBufferLookup<MeshBatch>();
			__Game_Rendering_FadeBatch_RW_BufferLookup = state.GetBufferLookup<FadeBatch>();
			__Game_Prefabs_BatchGroup_RW_BufferLookup = state.GetBufferLookup<BatchGroup>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>();
			__Game_Tools_EditorContainer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Tools.EditorContainer>();
			__Game_Prefabs_SubObject_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>();
			__Game_Prefabs_SubNet_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubNet>();
			__Game_Prefabs_SubArea_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubArea>();
			__Game_Prefabs_PlaceholderObjectElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PlaceholderObjectElement>();
			__Game_Prefabs_ServiceUpgradeBuilding_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceUpgradeBuilding>();
			__Game_Prefabs_BuildingUpgradeElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<BuildingUpgradeElement>();
			__Game_Prefabs_Effect_RW_BufferTypeHandle = state.GetBufferTypeHandle<Effect>();
			__Game_Prefabs_ActivityLocationElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<ActivityLocationElement>();
			__Game_Prefabs_SubMesh_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubMesh>();
			__Game_Prefabs_LodMesh_RW_BufferTypeHandle = state.GetBufferTypeHandle<LodMesh>();
			__Game_Prefabs_UIGroupElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<UIGroupElement>();
			__Game_Tutorials_TutorialPhaseRef_RW_BufferTypeHandle = state.GetBufferTypeHandle<TutorialPhaseRef>();
		}
	}

	private ToolSystem m_ToolSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private Finalize m_FinalizeSystem;

	private BatchManagerSystem m_BatchManagerSystem;

	private ManagedBatchSystem m_ManagedBatchSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_PrefabRefQuery;

	private Entity m_OldPrefab;

	private Entity m_NewPrefab;

	private Entity m_SourceInstance;

	private NativeList<ReplaceMesh> m_MeshReplaces;

	private NativeQueue<Entity> m_UpdateInstances;

	private NativeHashMap<Entity, ReplacePrefabData> m_ReplacePrefabData;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_FinalizeSystem = base.World.GetOrCreateSystemManaged<Finalize>();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_ManagedBatchSystem = base.World.GetOrCreateSystemManaged<ManagedBatchSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabRefQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[13]
			{
				ComponentType.ReadWrite<PrefabRef>(),
				ComponentType.ReadWrite<SubObject>(),
				ComponentType.ReadWrite<SubNet>(),
				ComponentType.ReadWrite<SubArea>(),
				ComponentType.ReadWrite<PlaceholderObjectElement>(),
				ComponentType.ReadWrite<ServiceUpgradeBuilding>(),
				ComponentType.ReadWrite<BuildingUpgradeElement>(),
				ComponentType.ReadWrite<Effect>(),
				ComponentType.ReadWrite<ActivityLocationElement>(),
				ComponentType.ReadWrite<SubMesh>(),
				ComponentType.ReadWrite<LodMesh>(),
				ComponentType.ReadWrite<UIGroupElement>(),
				ComponentType.ReadWrite<TutorialPhaseRef>()
			}
		});
		m_MeshReplaces = new NativeList<ReplaceMesh>(1, Allocator.Persistent);
		m_UpdateInstances = new NativeQueue<Entity>(Allocator.Persistent);
		m_ReplacePrefabData = new NativeHashMap<Entity, ReplacePrefabData>(1, Allocator.Persistent);
		RequireForUpdate(m_PrefabRefQuery);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_MeshReplaces.Dispose();
		m_UpdateInstances.Dispose();
		base.OnDestroy();
	}

	public void ReplacePrefab(Entity oldPrefab, Entity newPrefab, Entity sourceInstance)
	{
		m_OldPrefab = oldPrefab;
		m_NewPrefab = newPrefab;
		m_SourceInstance = sourceInstance;
		try
		{
			base.Enabled = true;
			Update();
		}
		finally
		{
			base.Enabled = false;
		}
	}

	public void FinalizeReplaces()
	{
		m_FinalizeSystem.Update();
	}

	private void CheckInstanceComponents(Entity instance, HashSet<ComponentType> checkedComponents, HashSet<ComponentType> archetypeComponents)
	{
		NativeArray<ComponentType> componentTypes = base.EntityManager.GetChunk(instance).Archetype.GetComponentTypes();
		PrefabRef componentData = base.EntityManager.GetComponentData<PrefabRef>(instance);
		m_PrefabSystem.GetPrefab<PrefabBase>(componentData).GetArchetypeComponents(archetypeComponents);
		foreach (ComponentType item in componentTypes)
		{
			if (checkedComponents.Contains(item))
			{
				if (archetypeComponents.Contains(item))
				{
					archetypeComponents.Remove(item);
				}
				else
				{
					base.EntityManager.RemoveComponent(instance, item);
				}
			}
		}
		foreach (ComponentType archetypeComponent in archetypeComponents)
		{
			if (checkedComponents.Contains(archetypeComponent))
			{
				base.EntityManager.AddComponent(instance, archetypeComponent);
			}
		}
		archetypeComponents.Clear();
		componentTypes.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
		EntityCommandBuffer entityCommandBuffer2 = default(EntityCommandBuffer);
		m_ToolSystem.RequireFullUpdate();
		JobHandle jobHandle = default(JobHandle);
		if (base.EntityManager.HasComponent<MeshData>(m_OldPrefab))
		{
			entityCommandBuffer2 = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
			JobHandle dependencies;
			JobHandle dependencies2;
			JobHandle dependencies3;
			RemoveBatchGroupsJob jobData = new RemoveBatchGroupsJob
			{
				m_OldMeshEntity = m_OldPrefab,
				m_NewMeshEntity = m_NewPrefab,
				m_MeshBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshBatch_RW_BufferLookup, ref base.CheckedStateRef),
				m_FadeBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_FadeBatch_RW_BufferLookup, ref base.CheckedStateRef),
				m_BatchGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_BatchGroup_RW_BufferLookup, ref base.CheckedStateRef),
				m_NativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies),
				m_NativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: false, out dependencies2),
				m_NativeSubBatches = m_BatchManagerSystem.GetNativeSubBatches(readOnly: false, out dependencies3),
				m_EntityCommandBuffer = entityCommandBuffer2,
				m_ReplaceMeshes = m_MeshReplaces
			};
			jobHandle = JobHandle.CombineDependencies(jobHandle, IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, dependencies, dependencies2, dependencies3)));
			m_BatchManagerSystem.AddNativeBatchGroupsWriter(jobHandle);
			m_BatchManagerSystem.AddNativeBatchInstancesWriter(jobHandle);
			m_BatchManagerSystem.AddNativeSubBatchesWriter(jobHandle);
		}
		else
		{
			m_ReplacePrefabData[m_NewPrefab] = new ReplacePrefabData
			{
				m_OldPrefab = m_OldPrefab,
				m_SourceInstance = m_SourceInstance
			};
		}
		if (m_SourceInstance != Entity.Null)
		{
			m_UpdateInstances.Enqueue(m_SourceInstance);
		}
		ReplacePrefabJob jobData2 = new ReplacePrefabJob
		{
			m_OldPrefab = m_OldPrefab,
			m_NewPrefab = m_NewPrefab,
			m_SourceInstance = m_SourceInstance,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EditorContainerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_EditorContainer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubObject_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubNetType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubNet_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubAreaType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubArea_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PlaceholderObjectElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceUpgradeBuildingType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingUpgradeElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingUpgradeElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EffectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_Effect_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ActivityLocationElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubMeshType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubMesh_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_LodMeshType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_LodMesh_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_UIGroupElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_UIGroupElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_TutorialPhaseType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tutorials_TutorialPhaseRef_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_CommandBuffer = entityCommandBuffer.AsParallelWriter(),
			m_UpdateInstances = m_UpdateInstances.AsParallelWriter()
		};
		JobHandle.CombineDependencies(jobHandle, JobChunkExtensions.ScheduleParallel(jobData2, m_PrefabRefQuery, base.Dependency)).Complete();
		if (base.EntityManager.HasComponent<BuildingUpgradeElement>(m_OldPrefab))
		{
			DynamicBuffer<BuildingUpgradeElement> dynamicBuffer = base.EntityManager.AddBuffer<BuildingUpgradeElement>(m_NewPrefab);
			DynamicBuffer<BuildingUpgradeElement> buffer = base.EntityManager.GetBuffer<BuildingUpgradeElement>(m_OldPrefab, isReadOnly: true);
			dynamicBuffer.CopyFrom(buffer);
		}
		entityCommandBuffer.Playback(base.EntityManager);
		entityCommandBuffer.Dispose();
		if (entityCommandBuffer2.IsCreated)
		{
			entityCommandBuffer2.Playback(base.EntityManager);
			entityCommandBuffer2.Dispose();
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
	public ReplacePrefabSystem()
	{
	}
}
