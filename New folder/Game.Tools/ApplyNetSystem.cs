using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Triggers;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ApplyNetSystem : GameSystemBase
{
	[BurstCompile]
	private struct PatchTempReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		public ComponentLookup<Owner> m_OwnerData;

		public BufferLookup<Game.Net.SubNet> m_SubNets;

		public BufferLookup<ConnectedEdge> m_Edges;

		public BufferLookup<ConnectedNode> m_Nodes;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Edge> nativeArray3 = chunk.GetNativeArray(ref m_EdgeType);
			NativeArray<Node> nativeArray4 = chunk.GetNativeArray(ref m_NodeType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity entity = nativeArray[i];
				Edge value = nativeArray3[i];
				Temp temp = nativeArray2[i];
				if ((temp.m_Flags & TempFlags.Delete) != 0)
				{
					continue;
				}
				if (temp.m_Original != Entity.Null && (temp.m_Flags & (TempFlags.Replace | TempFlags.Combine)) == 0)
				{
					Temp temp2 = m_TempData[value.m_Start];
					if (temp2.m_Original != Entity.Null && (temp2.m_Flags & (TempFlags.Delete | TempFlags.Replace)) == 0)
					{
						value.m_Start = temp2.m_Original;
					}
					Temp temp3 = m_TempData[value.m_End];
					if (temp3.m_Original != Entity.Null && (temp3.m_Flags & (TempFlags.Delete | TempFlags.Replace)) == 0)
					{
						value.m_End = temp3.m_Original;
					}
					DynamicBuffer<ConnectedNode> buffer = m_Nodes[temp.m_Original];
					DynamicBuffer<ConnectedNode> dynamicBuffer = m_Nodes[entity];
					for (int j = 0; j < buffer.Length; j++)
					{
						ConnectedNode connectedNode = buffer[j];
						CollectionUtils.RemoveValue(m_Edges[connectedNode.m_Node], new ConnectedEdge(temp.m_Original));
					}
					buffer.Clear();
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						ConnectedNode value2 = dynamicBuffer[k];
						if (m_TempData.HasComponent(value2.m_Node))
						{
							Temp temp4 = m_TempData[value2.m_Node];
							if (temp4.m_Original != Entity.Null && (temp4.m_Flags & (TempFlags.Delete | TempFlags.Replace)) == 0)
							{
								value2.m_Node = temp4.m_Original;
							}
						}
						DynamicBuffer<ConnectedEdge> buffer2 = m_Edges[value2.m_Node];
						CollectionUtils.TryAddUniqueValue(buffer, value2);
						CollectionUtils.TryAddUniqueValue(buffer2, new ConnectedEdge(temp.m_Original));
					}
				}
				else
				{
					Temp temp5 = m_TempData[value.m_Start];
					if (temp5.m_Original != Entity.Null && (temp5.m_Flags & (TempFlags.Delete | TempFlags.Replace)) == 0)
					{
						CollectionUtils.RemoveValue(m_Edges[value.m_Start], new ConnectedEdge(entity));
						value.m_Start = temp5.m_Original;
						CollectionUtils.TryAddUniqueValue(m_Edges[value.m_Start], new ConnectedEdge(entity));
					}
					Temp temp6 = m_TempData[value.m_End];
					if (temp6.m_Original != Entity.Null && (temp6.m_Flags & (TempFlags.Delete | TempFlags.Replace)) == 0)
					{
						CollectionUtils.RemoveValue(m_Edges[value.m_End], new ConnectedEdge(entity));
						value.m_End = temp6.m_Original;
						CollectionUtils.TryAddUniqueValue(m_Edges[value.m_End], new ConnectedEdge(entity));
					}
					DynamicBuffer<ConnectedNode> dynamicBuffer2 = m_Nodes[entity];
					for (int l = 0; l < dynamicBuffer2.Length; l++)
					{
						ConnectedNode value3 = dynamicBuffer2[l];
						if (m_TempData.HasComponent(value3.m_Node))
						{
							Temp temp7 = m_TempData[value3.m_Node];
							if (temp7.m_Original != Entity.Null && (temp7.m_Flags & (TempFlags.Delete | TempFlags.Replace)) == 0)
							{
								value3.m_Node = temp7.m_Original;
								dynamicBuffer2[l] = value3;
							}
						}
						CollectionUtils.TryAddUniqueValue(m_Edges[value3.m_Node], new ConnectedEdge(entity));
					}
				}
				nativeArray3[i] = value;
			}
			if (nativeArray4.Length == 0 && nativeArray3.Length == 0)
			{
				return;
			}
			for (int m = 0; m < nativeArray2.Length; m++)
			{
				Entity entity2 = nativeArray[m];
				Temp temp8 = nativeArray2[m];
				Entity entity3 = Entity.Null;
				Entity entity4 = Entity.Null;
				bool flag = false;
				if (m_OwnerData.HasComponent(entity2))
				{
					entity4 = m_OwnerData[entity2].m_Owner;
					entity3 = entity4;
					if (m_TempData.HasComponent(entity4))
					{
						Temp temp9 = m_TempData[entity4];
						if (temp9.m_Original != Entity.Null && (temp9.m_Flags & (TempFlags.Replace | TempFlags.Combine)) == 0)
						{
							flag = true;
							entity4 = temp9.m_Original;
							m_OwnerData[entity2] = new Owner(entity4);
						}
						else if ((temp9.m_Flags & (TempFlags.Delete | TempFlags.Cancel)) == 0)
						{
							flag = true;
						}
					}
				}
				if (temp8.m_Original != Entity.Null && (temp8.m_Flags & (TempFlags.Delete | TempFlags.Replace)) == 0)
				{
					if (flag && m_SubNets.HasBuffer(entity3))
					{
						CollectionUtils.RemoveValue(m_SubNets[entity3], new Game.Net.SubNet(entity2));
					}
					entity2 = temp8.m_Original;
					entity3 = ((!m_OwnerData.HasComponent(entity2)) ? Entity.Null : m_OwnerData[entity2].m_Owner);
				}
				if (entity3 != entity4)
				{
					if (m_SubNets.HasBuffer(entity3))
					{
						CollectionUtils.RemoveValue(m_SubNets[entity3], new Game.Net.SubNet(entity2));
					}
					if (m_SubNets.HasBuffer(entity4))
					{
						CollectionUtils.TryAddUniqueValue(m_SubNets[entity4], new Game.Net.SubNet(entity2));
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixConnectedEdgesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_Nodes;

		[NativeDisableParallelForRestriction]
		public BufferLookup<ConnectedEdge> m_Edges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (!chunk.Has(ref m_NodeType))
			{
				return;
			}
			NativeArray<Temp> nativeArray = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Temp temp = nativeArray[i];
				if (!(temp.m_Original != Entity.Null) || (temp.m_Flags & (TempFlags.Delete | TempFlags.Replace)) != 0 || !m_Edges.HasBuffer(temp.m_Original))
				{
					continue;
				}
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_Edges[temp.m_Original];
				for (int num = dynamicBuffer.Length - 1; num >= 0; num--)
				{
					Entity edge = dynamicBuffer[num].m_Edge;
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_Start == temp.m_Original || edge2.m_End == temp.m_Original)
					{
						continue;
					}
					if (m_Nodes.HasBuffer(edge))
					{
						DynamicBuffer<ConnectedNode> dynamicBuffer2 = m_Nodes[edge];
						int num2 = 0;
						while (num2 < dynamicBuffer2.Length)
						{
							if (!(dynamicBuffer2[num2].m_Node == temp.m_Original))
							{
								num2++;
								continue;
							}
							goto IL_0129;
						}
					}
					dynamicBuffer.RemoveAt(num);
					IL_0129:;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct HandleTempEntitiesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NetNodeType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_NetEdgeType;

		[ReadOnly]
		public ComponentTypeHandle<Lane> m_NetLaneType;

		[ReadOnly]
		public ComponentLookup<Edge> m_NetEdgeData;

		[ReadOnly]
		public ComponentLookup<LandValue> m_LandValueData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<LocalConnect> m_LocalConnectData;

		[ReadOnly]
		public ComponentLookup<TramTrack> m_TramTrackData;

		[ReadOnly]
		public ComponentLookup<TrainTrack> m_TrainTrackData;

		[ReadOnly]
		public ComponentLookup<Waterway> m_WaterwayData;

		[ReadOnly]
		public ComponentLookup<Road> m_RoadData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Pollution> m_PollutionData;

		[ReadOnly]
		public ComponentLookup<TrafficLights> m_TrafficLightsData;

		[ReadOnly]
		public ComponentLookup<Orphan> m_OrphanData;

		[ReadOnly]
		public ComponentLookup<EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<Aggregated> m_AggregatedData;

		[ReadOnly]
		public ComponentLookup<Standalone> m_StandaloneData;

		[ReadOnly]
		public ComponentLookup<Marker> m_MarkerData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Recent> m_RecentData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		[ReadOnly]
		public BufferLookup<SubReplacement> m_SubReplacements;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameterData;

		[ReadOnly]
		public ComponentTypeSet m_ApplyCreatedTypes;

		[ReadOnly]
		public ComponentTypeSet m_ApplyUpdatedTypes;

		[ReadOnly]
		public ComponentTypeSet m_ApplyDeletedTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Node> nativeArray3 = chunk.GetNativeArray(ref m_NetNodeType);
			if (nativeArray3.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					Temp temp = nativeArray2[i];
					if ((temp.m_Flags & TempFlags.Delete) != 0)
					{
						Delete(unfilteredChunkIndex, entity, temp);
					}
					else if ((temp.m_Flags & TempFlags.Replace) != 0)
					{
						if (temp.m_Original != Entity.Null)
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, temp.m_Original, default(Deleted));
						}
						Create(unfilteredChunkIndex, entity, temp);
					}
					else if (temp.m_Original != Entity.Null)
					{
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, temp.m_Original, nativeArray3[i]);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_OwnerData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_PrefabRefData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_UpgradedData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_LocalConnectData, updateValue: false);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_TrafficLightsData, updateValue: false);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_OrphanData, updateValue: false);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_EditorContainerData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_NativeData, updateValue: false);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_StandaloneData, updateValue: false);
						if (m_PrefabData.IsComponentEnabled(m_PrefabRefData[entity].m_Prefab))
						{
							UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_LandValueData, updateValue: false);
							UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_TramTrackData, updateValue: false);
							UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_TrainTrackData, updateValue: false);
							UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_WaterwayData, updateValue: false);
							UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_RoadData);
							UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_PollutionData, updateValue: false);
							UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_MarkerData, updateValue: false);
						}
						Update(unfilteredChunkIndex, entity, temp);
					}
					else
					{
						Create(unfilteredChunkIndex, entity, temp);
					}
				}
				return;
			}
			if (chunk.GetNativeArray(ref m_NetEdgeType).Length != 0)
			{
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					Temp temp2 = nativeArray2[j];
					if (m_AggregatedData.HasComponent(entity2))
					{
						Aggregated aggregated = m_AggregatedData[entity2];
						if (m_TempData.HasComponent(aggregated.m_Aggregate))
						{
							m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity2, default(Aggregated));
						}
					}
					if ((temp2.m_Flags & TempFlags.Delete) != 0)
					{
						Delete(unfilteredChunkIndex, entity2, temp2);
					}
					else if ((temp2.m_Flags & (TempFlags.Replace | TempFlags.Combine)) != 0)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, temp2.m_Original, default(Deleted));
						Create(unfilteredChunkIndex, entity2, temp2);
					}
					else if (temp2.m_Original != Entity.Null)
					{
						UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_OwnerData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_PrefabRefData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_ElevationData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_UpgradedData, updateValue: true);
						UpdateBuffer(unfilteredChunkIndex, entity2, temp2.m_Original, m_SubReplacements, out var _, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_CurveData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_NetEdgeData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_EditorContainerData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_NativeData, updateValue: false);
						if (m_PrefabData.IsComponentEnabled(m_PrefabRefData[entity2].m_Prefab))
						{
							UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_RoadData);
							UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_PollutionData, updateValue: false);
							UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_TramTrackData, updateValue: false);
							UpdateComponent(unfilteredChunkIndex, entity2, temp2.m_Original, m_LandValueData, updateValue: false);
						}
						Update(unfilteredChunkIndex, entity2, temp2);
					}
					else
					{
						Create(unfilteredChunkIndex, entity2, temp2);
					}
				}
				return;
			}
			if (chunk.GetNativeArray(ref m_NetLaneType).Length != 0)
			{
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity entity3 = nativeArray[k];
					Temp temp3 = nativeArray2[k];
					if ((temp3.m_Flags & TempFlags.Cancel) != 0)
					{
						Cancel(unfilteredChunkIndex, entity3, temp3);
						continue;
					}
					if ((temp3.m_Flags & TempFlags.Delete) != 0)
					{
						Delete(unfilteredChunkIndex, entity3, temp3);
						continue;
					}
					if ((temp3.m_Flags & TempFlags.Replace) != 0)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, temp3.m_Original, default(Deleted));
						Create(unfilteredChunkIndex, entity3, temp3);
						continue;
					}
					if (temp3.m_Original != Entity.Null)
					{
						if (m_OwnerData.HasComponent(entity3))
						{
							Owner owner = m_OwnerData[entity3];
							if (m_TempData.HasComponent(owner.m_Owner))
							{
								Temp temp4 = m_TempData[owner.m_Owner];
								if (temp4.m_Original != Entity.Null && (temp4.m_Flags & (TempFlags.Replace | TempFlags.Combine)) != 0)
								{
									m_CommandBuffer.AddComponent(unfilteredChunkIndex, temp3.m_Original, default(Deleted));
									Create(unfilteredChunkIndex, entity3, temp3);
									continue;
								}
							}
						}
						Update(unfilteredChunkIndex, entity3, temp3);
						continue;
					}
					if (m_OwnerData.HasComponent(entity3))
					{
						Owner owner2 = m_OwnerData[entity3];
						if (m_TempData.HasComponent(owner2.m_Owner))
						{
							Temp temp5 = m_TempData[owner2.m_Owner];
							if (temp5.m_Original != Entity.Null && (temp5.m_Flags & (TempFlags.Replace | TempFlags.Combine)) == 0)
							{
								if ((temp5.m_Flags & TempFlags.Upgrade) != 0)
								{
									m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity3, in m_ApplyDeletedTypes);
								}
								else
								{
									m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity3, default(Deleted));
								}
								continue;
							}
						}
					}
					Create(unfilteredChunkIndex, entity3, temp3);
				}
				return;
			}
			for (int l = 0; l < nativeArray.Length; l++)
			{
				Entity e = nativeArray[l];
				Temp temp6 = nativeArray2[l];
				if ((temp6.m_Flags & TempFlags.Delete) == 0 && temp6.m_Original != Entity.Null && m_HiddenData.HasComponent(temp6.m_Original))
				{
					m_CommandBuffer.RemoveComponent<Hidden>(unfilteredChunkIndex, temp6.m_Original);
				}
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(Deleted));
			}
		}

		private void Cancel(int chunkIndex, Entity entity, Temp temp)
		{
			if (m_HiddenData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(BatchesUpdated));
				m_CommandBuffer.RemoveComponent<Hidden>(chunkIndex, temp.m_Original);
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Delete(int chunkIndex, Entity entity, Temp temp)
		{
			if (temp.m_Original != Entity.Null)
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Deleted));
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void UpdateComponent<T>(int chunkIndex, Entity entity, Entity original, ComponentLookup<T> data, bool updateValue) where T : unmanaged, IComponentData
		{
			if (data.HasComponent(entity))
			{
				if (data.HasComponent(original))
				{
					if (updateValue)
					{
						m_CommandBuffer.SetComponent(chunkIndex, original, data[entity]);
					}
				}
				else if (updateValue)
				{
					m_CommandBuffer.AddComponent(chunkIndex, original, data[entity]);
				}
				else
				{
					m_CommandBuffer.AddComponent(chunkIndex, original, default(T));
				}
			}
			else if (data.HasComponent(original))
			{
				m_CommandBuffer.RemoveComponent<T>(chunkIndex, original);
			}
		}

		private bool UpdateBuffer<T>(int chunkIndex, Entity entity, Entity original, BufferLookup<T> data, out DynamicBuffer<T> oldBuffer, bool updateValue) where T : unmanaged, IBufferElementData
		{
			if (data.HasBuffer(entity))
			{
				if (data.HasBuffer(original))
				{
					if (updateValue)
					{
						m_CommandBuffer.SetBuffer<T>(chunkIndex, original).CopyFrom(data[entity]);
					}
				}
				else if (updateValue)
				{
					m_CommandBuffer.AddBuffer<T>(chunkIndex, original).CopyFrom(data[entity]);
				}
				else
				{
					m_CommandBuffer.AddBuffer<T>(chunkIndex, original);
				}
			}
			else if (data.TryGetBuffer(original, out oldBuffer))
			{
				m_CommandBuffer.RemoveComponent<T>(chunkIndex, original);
				return true;
			}
			oldBuffer = default(DynamicBuffer<T>);
			return false;
		}

		private void UpdateComponent(int chunkIndex, Entity entity, Entity original, ComponentLookup<Road> data)
		{
			if (data.HasComponent(entity))
			{
				if (data.HasComponent(original))
				{
					Road component = data[original];
					component.m_Flags = data[entity].m_Flags;
					m_CommandBuffer.SetComponent(chunkIndex, original, component);
				}
				else
				{
					m_CommandBuffer.AddComponent(chunkIndex, original, data[entity]);
				}
			}
			else if (data.HasComponent(original))
			{
				m_CommandBuffer.RemoveComponent<Road>(chunkIndex, original);
			}
		}

		private void Update(int chunkIndex, Entity entity, Temp temp)
		{
			if (m_HiddenData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.RemoveComponent<Hidden>(chunkIndex, temp.m_Original);
			}
			if (temp.m_Cost != 0)
			{
				Recent component = new Recent
				{
					m_ModificationFrame = m_SimulationFrame,
					m_ModificationCost = temp.m_Cost
				};
				if (m_RecentData.TryGetComponent(temp.m_Original, out var componentData))
				{
					component.m_ModificationCost += componentData.m_ModificationCost;
					component.m_ModificationCost += NetUtils.GetRefundAmount(componentData, m_SimulationFrame, m_EconomyParameterData);
					componentData.m_ModificationFrame = m_SimulationFrame;
					component.m_ModificationCost -= NetUtils.GetRefundAmount(componentData, m_SimulationFrame, m_EconomyParameterData);
					component.m_ModificationCost = math.min(component.m_ModificationCost, temp.m_Value);
					if (component.m_ModificationCost > 0)
					{
						m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, component);
					}
					else
					{
						m_CommandBuffer.RemoveComponent<Recent>(chunkIndex, temp.m_Original);
					}
				}
				else if (component.m_ModificationCost > 0)
				{
					m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, component);
				}
			}
			if (((m_UpgradedData.TryGetComponent(entity, out var componentData2) && (componentData2.m_Flags.m_Left & CompositionFlags.Side.ForbidSecondary) != 0) || (componentData2.m_Flags.m_Right & CompositionFlags.Side.ForbidSecondary) != 0) && (!m_UpgradedData.TryGetComponent(temp.m_Original, out var componentData3) || (componentData3.m_Flags.m_Left & CompositionFlags.Side.ForbidSecondary) == 0 || (componentData3.m_Flags.m_Right & CompositionFlags.Side.ForbidSecondary) == 0))
			{
				m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.BicycleRoadBan, Entity.Null, 0f));
			}
			m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, in m_ApplyUpdatedTypes);
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Create(int chunkIndex, Entity entity, Temp temp)
		{
			m_CommandBuffer.RemoveComponent<Temp>(chunkIndex, entity);
			if (temp.m_Cost > 0)
			{
				Recent component = new Recent
				{
					m_ModificationFrame = m_SimulationFrame,
					m_ModificationCost = temp.m_Cost
				};
				m_CommandBuffer.AddComponent(chunkIndex, entity, component);
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, in m_ApplyCreatedTypes);
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
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Edge> __Game_Net_Edge_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		public ComponentLookup<Owner> __Game_Common_Owner_RW_ComponentLookup;

		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RW_BufferLookup;

		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RW_BufferLookup;

		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Lane> __Game_Net_Lane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalConnect> __Game_Net_LocalConnect_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TramTrack> __Game_Net_TramTrack_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainTrack> __Game_Net_TrainTrack_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Waterway> __Game_Net_Waterway_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Pollution> __Game_Net_Pollution_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrafficLights> __Game_Net_TrafficLights_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Aggregated> __Game_Net_Aggregated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Standalone> __Game_Net_Standalone_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Marker> __Game_Net_Marker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Recent> __Game_Tools_Recent_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubReplacement> __Game_Net_SubReplacement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
			__Game_Net_Edge_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>();
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Common_Owner_RW_ComponentLookup = state.GetComponentLookup<Owner>();
			__Game_Net_SubNet_RW_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>();
			__Game_Net_ConnectedEdge_RW_BufferLookup = state.GetBufferLookup<ConnectedEdge>();
			__Game_Net_ConnectedNode_RW_BufferLookup = state.GetBufferLookup<ConnectedNode>();
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lane>(isReadOnly: true);
			__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentLookup = state.GetComponentLookup<Upgraded>(isReadOnly: true);
			__Game_Net_LocalConnect_RO_ComponentLookup = state.GetComponentLookup<LocalConnect>(isReadOnly: true);
			__Game_Net_TramTrack_RO_ComponentLookup = state.GetComponentLookup<TramTrack>(isReadOnly: true);
			__Game_Net_TrainTrack_RO_ComponentLookup = state.GetComponentLookup<TrainTrack>(isReadOnly: true);
			__Game_Net_Waterway_RO_ComponentLookup = state.GetComponentLookup<Waterway>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Pollution_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Pollution>(isReadOnly: true);
			__Game_Net_TrafficLights_RO_ComponentLookup = state.GetComponentLookup<TrafficLights>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<EditorContainer>(isReadOnly: true);
			__Game_Net_Aggregated_RO_ComponentLookup = state.GetComponentLookup<Aggregated>(isReadOnly: true);
			__Game_Net_Standalone_RO_ComponentLookup = state.GetComponentLookup<Standalone>(isReadOnly: true);
			__Game_Net_Marker_RO_ComponentLookup = state.GetComponentLookup<Marker>(isReadOnly: true);
			__Game_Tools_Recent_RO_ComponentLookup = state.GetComponentLookup<Recent>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Net_SubReplacement_RO_BufferLookup = state.GetBufferLookup<SubReplacement>(isReadOnly: true);
		}
	}

	private ToolOutputBarrier m_ToolOutputBarrier;

	private SimulationSystem m_SimulationSystem;

	private TriggerSystem m_TriggerSystem;

	private EntityQuery m_TempQuery;

	private EntityQuery m_EconomyParameterQuery;

	private ComponentTypeSet m_ApplyCreatedTypes;

	private ComponentTypeSet m_ApplyUpdatedTypes;

	private ComponentTypeSet m_ApplyDeletedTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_TempQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Temp>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Lane>(),
				ComponentType.ReadOnly<Aggregate>()
			},
			None = new ComponentType[0]
		});
		m_ApplyCreatedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		m_ApplyUpdatedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Updated>());
		m_ApplyDeletedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Deleted>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		RequireForUpdate(m_TempQuery);
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PatchTempReferencesJob jobData = new PatchTempReferencesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RW_ComponentLookup, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RW_BufferLookup, ref base.CheckedStateRef),
			m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RW_BufferLookup, ref base.CheckedStateRef),
			m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RW_BufferLookup, ref base.CheckedStateRef)
		};
		FixConnectedEdgesJob jobData2 = new FixConnectedEdgesJob
		{
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RW_BufferLookup, ref base.CheckedStateRef)
		};
		NativeQueue<TriggerAction> nativeQueue = (m_TriggerSystem.Enabled ? m_TriggerSystem.CreateActionBuffer() : new NativeQueue<TriggerAction>(Allocator.TempJob));
		HandleTempEntitiesJob jobData3 = new HandleTempEntitiesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetNodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetEdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LandValueData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LandValue_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LocalConnect_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TramTrackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TramTrack_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainTrackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrainTrack_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterwayData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Waterway_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PollutionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Pollution_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrafficLightsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrafficLights_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AggregatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Aggregated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StandaloneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Standalone_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MarkerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Marker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RecentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Recent_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubReplacements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubReplacement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_EconomyParameterData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_ApplyCreatedTypes = m_ApplyCreatedTypes,
			m_ApplyUpdatedTypes = m_ApplyUpdatedTypes,
			m_ApplyDeletedTypes = m_ApplyDeletedTypes,
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_TriggerBuffer = nativeQueue.AsParallelWriter()
		};
		JobHandle dependsOn = JobChunkExtensions.Schedule(jobData, m_TempQuery, base.Dependency);
		JobHandle job = JobChunkExtensions.ScheduleParallel(jobData2, m_TempQuery, dependsOn);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData3, m_TempQuery, dependsOn);
		if (m_TriggerSystem.Enabled)
		{
			m_TriggerSystem.AddActionBufferWriter(jobHandle);
		}
		else
		{
			nativeQueue.Dispose(jobHandle);
		}
		base.Dependency = JobHandle.CombineDependencies(job, jobHandle);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
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
	public ApplyNetSystem()
	{
	}
}
