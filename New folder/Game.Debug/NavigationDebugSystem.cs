using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Areas;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class NavigationDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct NavigationGizmoJob : IJobChunk
	{
		[ReadOnly]
		public bool m_HumanOption;

		[ReadOnly]
		public bool m_AnimalOption;

		[ReadOnly]
		public bool m_CarOption;

		[ReadOnly]
		public bool m_TrainOption;

		[ReadOnly]
		public bool m_WatercraftOption;

		[ReadOnly]
		public bool m_AircraftOption;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public float m_TimeOffset;

		[ReadOnly]
		public Entity m_Selected;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> m_CarCurrentLaneType;

		[ReadOnly]
		public BufferTypeHandle<CarNavigationLane> m_CarNavigationLaneType;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftCurrentLane> m_WatercraftCurrentLaneType;

		[ReadOnly]
		public BufferTypeHandle<WatercraftNavigationLane> m_WatercraftNavigationLaneType;

		[ReadOnly]
		public ComponentTypeHandle<AircraftCurrentLane> m_AircraftCurrentLaneType;

		[ReadOnly]
		public BufferTypeHandle<AircraftNavigationLane> m_AircraftNavigationLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Train> m_TrainType;

		[ReadOnly]
		public ComponentTypeHandle<TrainCurrentLane> m_TrainCurrentLaneType;

		[ReadOnly]
		public BufferTypeHandle<TrainNavigationLane> m_TrainNavigationLaneType;

		[ReadOnly]
		public ComponentTypeHandle<HumanCurrentLane> m_HumanCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<AnimalCurrentLane> m_AnimalCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<TrainData> m_PrefabTrainData;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Objects.Transform> nativeArray = chunk.GetNativeArray(ref m_TransformType);
			int num;
			int num2;
			if (m_Selected != Entity.Null)
			{
				num = (num2 = -1);
				NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					if (nativeArray2[i] == m_Selected)
					{
						num = i;
						num2 = i + 1;
						break;
					}
				}
				if (num == -1)
				{
					return;
				}
			}
			else
			{
				num = 0;
				num2 = chunk.Count;
			}
			if (m_CarOption)
			{
				NativeArray<CarCurrentLane> nativeArray3 = chunk.GetNativeArray(ref m_CarCurrentLaneType);
				if (nativeArray3.Length != 0)
				{
					BufferAccessor<CarNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_CarNavigationLaneType);
					float timeOffset = m_TimeOffset * 10f;
					for (int j = num; j < num2; j++)
					{
						CarCurrentLane carCurrentLane = nativeArray3[j];
						Game.Objects.Transform transform = nativeArray[j];
						DynamicBuffer<CarNavigationLane> dynamicBuffer = bufferAccessor[j];
						if (!m_CurveData.HasComponent(carCurrentLane.m_Lane))
						{
							continue;
						}
						Curve curve = m_CurveData[carCurrentLane.m_Lane];
						Bezier4x3 curve2 = MathUtils.Cut(curve.m_Bezier, carCurrentLane.m_CurvePosition.xy);
						Bezier4x3 curve3 = MathUtils.Cut(curve.m_Bezier, carCurrentLane.m_CurvePosition.yz);
						float3 d = curve3.d;
						DrawNavigationCurve(curve2, curve.m_Length, timeOffset, new UnityEngine.Color(1f, 0.5f, 0f, 1f), carCurrentLane.m_CurvePosition.xy);
						DrawNavigationCurve(curve3, curve.m_Length, timeOffset, UnityEngine.Color.yellow, carCurrentLane.m_CurvePosition.yz);
						if (m_CurveData.HasComponent(carCurrentLane.m_ChangeLane))
						{
							curve = m_CurveData[carCurrentLane.m_ChangeLane];
							Bezier4x3 curve4 = MathUtils.Cut(curve.m_Bezier, carCurrentLane.m_CurvePosition.xz);
							d = curve4.d;
							DrawNavigationCurve(curve4, curve.m_Length, timeOffset, UnityEngine.Color.magenta, carCurrentLane.m_CurvePosition.xz);
							m_GizmoBatcher.DrawLine(curve2.a, curve4.a, UnityEngine.Color.magenta);
							m_GizmoBatcher.DrawLine(transform.m_Position, math.lerp(curve2.a, curve4.a, math.saturate(carCurrentLane.m_ChangeProgress)), UnityEngine.Color.red);
						}
						else
						{
							m_GizmoBatcher.DrawLine(transform.m_Position, curve2.a, UnityEngine.Color.red);
						}
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							CarNavigationLane carNavigationLane = dynamicBuffer[k];
							if (!m_CurveData.HasComponent(carNavigationLane.m_Lane))
							{
								break;
							}
							curve = m_CurveData[carNavigationLane.m_Lane];
							curve2 = MathUtils.Cut(curve.m_Bezier, carNavigationLane.m_CurvePosition);
							DrawNavigationCurve(curve2, curve.m_Length, timeOffset, UnityEngine.Color.green, carNavigationLane.m_CurvePosition);
							if (math.lengthsq(curve2.a - d) > 1f)
							{
								m_GizmoBatcher.DrawLine(d, curve2.a, UnityEngine.Color.magenta);
							}
							d = curve2.d;
						}
					}
				}
			}
			if (m_WatercraftOption)
			{
				NativeArray<WatercraftCurrentLane> nativeArray4 = chunk.GetNativeArray(ref m_WatercraftCurrentLaneType);
				if (nativeArray4.Length != 0)
				{
					BufferAccessor<WatercraftNavigationLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_WatercraftNavigationLaneType);
					float timeOffset2 = m_TimeOffset * 10f;
					for (int l = num; l < num2; l++)
					{
						WatercraftCurrentLane watercraftCurrentLane = nativeArray4[l];
						Game.Objects.Transform transform2 = nativeArray[l];
						DynamicBuffer<WatercraftNavigationLane> dynamicBuffer2 = bufferAccessor2[l];
						if (!m_CurveData.HasComponent(watercraftCurrentLane.m_Lane))
						{
							continue;
						}
						Curve curve5 = m_CurveData[watercraftCurrentLane.m_Lane];
						Bezier4x3 curve6 = MathUtils.Cut(curve5.m_Bezier, watercraftCurrentLane.m_CurvePosition.xy);
						Bezier4x3 curve7 = MathUtils.Cut(curve5.m_Bezier, watercraftCurrentLane.m_CurvePosition.yz);
						float3 d2 = curve7.d;
						DrawNavigationCurve(curve6, curve5.m_Length, timeOffset2, new UnityEngine.Color(1f, 0.5f, 0f, 1f), watercraftCurrentLane.m_CurvePosition.xy);
						DrawNavigationCurve(curve7, curve5.m_Length, timeOffset2, UnityEngine.Color.yellow, watercraftCurrentLane.m_CurvePosition.yz);
						if (m_CurveData.HasComponent(watercraftCurrentLane.m_ChangeLane))
						{
							curve5 = m_CurveData[watercraftCurrentLane.m_ChangeLane];
							Bezier4x3 curve8 = MathUtils.Cut(curve5.m_Bezier, watercraftCurrentLane.m_CurvePosition.xz);
							d2 = curve8.d;
							DrawNavigationCurve(curve8, curve5.m_Length, timeOffset2, UnityEngine.Color.magenta, watercraftCurrentLane.m_CurvePosition.xz);
							m_GizmoBatcher.DrawLine(curve6.a, curve8.a, UnityEngine.Color.magenta);
							m_GizmoBatcher.DrawLine(transform2.m_Position, math.lerp(curve6.a, curve8.a, math.saturate(watercraftCurrentLane.m_ChangeProgress)), UnityEngine.Color.red);
						}
						else
						{
							m_GizmoBatcher.DrawLine(transform2.m_Position, curve6.a, UnityEngine.Color.red);
						}
						for (int m = 0; m < dynamicBuffer2.Length; m++)
						{
							WatercraftNavigationLane watercraftNavigationLane = dynamicBuffer2[m];
							if (!m_CurveData.HasComponent(watercraftNavigationLane.m_Lane))
							{
								break;
							}
							curve5 = m_CurveData[watercraftNavigationLane.m_Lane];
							curve6 = MathUtils.Cut(curve5.m_Bezier, watercraftNavigationLane.m_CurvePosition);
							DrawNavigationCurve(curve6, curve5.m_Length, timeOffset2, UnityEngine.Color.green, watercraftNavigationLane.m_CurvePosition);
							if (math.lengthsq(curve6.a - d2) > 1f)
							{
								m_GizmoBatcher.DrawLine(d2, curve6.a, UnityEngine.Color.magenta);
							}
							d2 = curve6.d;
						}
					}
				}
			}
			if (m_AircraftOption)
			{
				NativeArray<AircraftCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_AircraftCurrentLaneType);
				if (nativeArray5.Length != 0)
				{
					BufferAccessor<AircraftNavigationLane> bufferAccessor3 = chunk.GetBufferAccessor(ref m_AircraftNavigationLaneType);
					float timeOffset3 = m_TimeOffset * 10f;
					for (int n = num; n < num2; n++)
					{
						AircraftCurrentLane aircraftCurrentLane = nativeArray5[n];
						Game.Objects.Transform transform3 = nativeArray[n];
						DynamicBuffer<AircraftNavigationLane> dynamicBuffer3 = bufferAccessor3[n];
						float3 @float;
						if (m_CurveData.HasComponent(aircraftCurrentLane.m_Lane))
						{
							Curve curve9 = m_CurveData[aircraftCurrentLane.m_Lane];
							Bezier4x3 curve10 = MathUtils.Cut(curve9.m_Bezier, aircraftCurrentLane.m_CurvePosition.xy);
							Bezier4x3 curve11 = MathUtils.Cut(curve9.m_Bezier, aircraftCurrentLane.m_CurvePosition.yz);
							@float = curve11.d;
							DrawNavigationCurve(curve10, curve9.m_Length, timeOffset3, new UnityEngine.Color(1f, 0.5f, 0f, 1f), aircraftCurrentLane.m_CurvePosition.xy);
							DrawNavigationCurve(curve11, curve9.m_Length, timeOffset3, UnityEngine.Color.yellow, aircraftCurrentLane.m_CurvePosition.yz);
							m_GizmoBatcher.DrawLine(transform3.m_Position, curve10.a, UnityEngine.Color.red);
						}
						else
						{
							if (!m_TransformData.HasComponent(aircraftCurrentLane.m_Lane))
							{
								continue;
							}
							@float = m_TransformData[aircraftCurrentLane.m_Lane].m_Position;
							m_GizmoBatcher.DrawLine(transform3.m_Position, @float, UnityEngine.Color.red);
						}
						for (int num3 = 0; num3 < dynamicBuffer3.Length; num3++)
						{
							AircraftNavigationLane aircraftNavigationLane = dynamicBuffer3[num3];
							if (m_CurveData.HasComponent(aircraftNavigationLane.m_Lane))
							{
								Curve curve12 = m_CurveData[aircraftNavigationLane.m_Lane];
								Bezier4x3 curve13 = MathUtils.Cut(curve12.m_Bezier, aircraftNavigationLane.m_CurvePosition);
								DrawNavigationCurve(curve13, curve12.m_Length, timeOffset3, UnityEngine.Color.green, aircraftNavigationLane.m_CurvePosition);
								if (math.lengthsq(curve13.a - @float) > 1f)
								{
									m_GizmoBatcher.DrawLine(@float, curve13.a, UnityEngine.Color.magenta);
								}
								@float = curve13.d;
								continue;
							}
							if (!m_TransformData.HasComponent(aircraftCurrentLane.m_Lane))
							{
								break;
							}
							float3 position = m_TransformData[aircraftCurrentLane.m_Lane].m_Position;
							if (math.lengthsq(position - @float) > 1f)
							{
								m_GizmoBatcher.DrawLine(@float, position, UnityEngine.Color.magenta);
							}
							@float = position;
						}
					}
				}
			}
			if (m_TrainOption)
			{
				NativeArray<TrainCurrentLane> nativeArray6 = chunk.GetNativeArray(ref m_TrainCurrentLaneType);
				if (nativeArray6.Length != 0)
				{
					NativeArray<Train> nativeArray7 = chunk.GetNativeArray(ref m_TrainType);
					NativeArray<PrefabRef> nativeArray8 = chunk.GetNativeArray(ref m_PrefabRefType);
					BufferAccessor<TrainNavigationLane> bufferAccessor4 = chunk.GetBufferAccessor(ref m_TrainNavigationLaneType);
					float timeOffset4 = m_TimeOffset * 10f;
					for (int num4 = num; num4 < num2; num4++)
					{
						Train train = nativeArray7[num4];
						TrainCurrentLane trainCurrentLane = nativeArray6[num4];
						Game.Objects.Transform transform4 = nativeArray[num4];
						PrefabRef prefabRef = nativeArray8[num4];
						TrainData prefabTrainData = m_PrefabTrainData[prefabRef.m_Prefab];
						if (!m_CurveData.HasComponent(trainCurrentLane.m_Front.m_Lane) || !m_CurveData.HasComponent(trainCurrentLane.m_Rear.m_Lane))
						{
							continue;
						}
						Curve curve14 = m_CurveData[trainCurrentLane.m_Front.m_Lane];
						Curve curve15 = m_CurveData[trainCurrentLane.m_Rear.m_Lane];
						Bezier4x3 curve16 = MathUtils.Cut(curve14.m_Bezier, trainCurrentLane.m_Front.m_CurvePosition.yw);
						Bezier4x3 bezier4x = MathUtils.Cut(curve15.m_Bezier, trainCurrentLane.m_Rear.m_CurvePosition.yw);
						VehicleUtils.CalculateTrainNavigationPivots(transform4, prefabTrainData, out var pivot, out var pivot2);
						if ((train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0)
						{
							CommonUtils.Swap(ref pivot, ref pivot2);
						}
						DrawNavigationCurve(curve16, curve14.m_Length, timeOffset4, new UnityEngine.Color(1f, 0.5f, 0f, 1f), trainCurrentLane.m_Front.m_CurvePosition.yw);
						m_GizmoBatcher.DrawLine(pivot, curve16.a, UnityEngine.Color.red);
						m_GizmoBatcher.DrawLine(pivot2, bezier4x.a, UnityEngine.Color.red);
						if (bufferAccessor4.Length == 0)
						{
							continue;
						}
						DynamicBuffer<TrainNavigationLane> dynamicBuffer4 = bufferAccessor4[num4];
						float3 d3 = curve16.d;
						for (int num5 = 0; num5 < dynamicBuffer4.Length; num5++)
						{
							TrainNavigationLane trainNavigationLane = dynamicBuffer4[num5];
							if (!m_CurveData.HasComponent(trainNavigationLane.m_Lane))
							{
								break;
							}
							curve14 = m_CurveData[trainNavigationLane.m_Lane];
							curve16 = MathUtils.Cut(curve14.m_Bezier, trainNavigationLane.m_CurvePosition);
							DrawNavigationCurve(curve16, curve14.m_Length, timeOffset4, UnityEngine.Color.green, trainNavigationLane.m_CurvePosition);
							if (num5 != 0 && math.lengthsq(curve16.a - d3) > 1f)
							{
								m_GizmoBatcher.DrawLine(d3, curve16.a, UnityEngine.Color.magenta);
							}
							d3 = curve16.d;
						}
					}
				}
			}
			if (m_HumanOption)
			{
				NativeArray<HumanCurrentLane> nativeArray9 = chunk.GetNativeArray(ref m_HumanCurrentLaneType);
				if (nativeArray9.Length != 0)
				{
					float timeOffset5 = m_TimeOffset * 5f;
					for (int num6 = num; num6 < num2; num6++)
					{
						HumanCurrentLane humanCurrentLane = nativeArray9[num6];
						Game.Objects.Transform transform5 = nativeArray[num6];
						if (m_CurveData.HasComponent(humanCurrentLane.m_Lane))
						{
							Curve curve17 = m_CurveData[humanCurrentLane.m_Lane];
							Bezier4x3 curve18 = MathUtils.Cut(curve17.m_Bezier, humanCurrentLane.m_CurvePosition);
							DrawNavigationCurve(curve18, curve17.m_Length, timeOffset5, UnityEngine.Color.yellow, humanCurrentLane.m_CurvePosition);
							m_GizmoBatcher.DrawLine(transform5.m_Position, curve18.a, UnityEngine.Color.red);
						}
					}
				}
			}
			if (!m_AnimalOption)
			{
				return;
			}
			NativeArray<AnimalCurrentLane> nativeArray10 = chunk.GetNativeArray(ref m_AnimalCurrentLaneType);
			if (nativeArray10.Length == 0)
			{
				return;
			}
			float timeOffset6 = m_TimeOffset * 5f;
			for (int num7 = num; num7 < num2; num7++)
			{
				AnimalCurrentLane animalCurrentLane = nativeArray10[num7];
				Game.Objects.Transform transform6 = nativeArray[num7];
				if (m_CurveData.HasComponent(animalCurrentLane.m_Lane))
				{
					Curve curve19 = m_CurveData[animalCurrentLane.m_Lane];
					Bezier4x3 curve20 = MathUtils.Cut(curve19.m_Bezier, animalCurrentLane.m_CurvePosition);
					DrawNavigationCurve(curve20, curve19.m_Length, timeOffset6, UnityEngine.Color.yellow, animalCurrentLane.m_CurvePosition);
					m_GizmoBatcher.DrawLine(transform6.m_Position, curve20.a, UnityEngine.Color.red);
				}
			}
		}

		private void DrawNavigationCurve(Bezier4x3 curve, float totalLength, float timeOffset, UnityEngine.Color color, float2 curveDelta)
		{
			float num = totalLength * math.abs(curveDelta.x - curveDelta.y);
			if (num >= 1f)
			{
				m_GizmoBatcher.DrawFlowCurve(curve, num, color, timeOffset, reverse: false, 1, -1, 1f);
			}
			else
			{
				m_GizmoBatcher.DrawCurve(curve, num, color);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SelectedNavigationGizmoJob : IJob
	{
		[ReadOnly]
		public Entity m_Selected;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> m_CarCurrentLaneType;

		[ReadOnly]
		public BufferLookup<CarNavigationLane> m_CarNavigationLaneType;

		[ReadOnly]
		public ComponentLookup<CarNavigation> m_CarNavigationType;

		[ReadOnly]
		public ComponentLookup<WatercraftCurrentLane> m_WatercraftCurrentLaneType;

		[ReadOnly]
		public BufferLookup<WatercraftNavigationLane> m_WatercraftNavigationLaneType;

		[ReadOnly]
		public ComponentLookup<WatercraftNavigation> m_WatercraftNavigationType;

		[ReadOnly]
		public ComponentLookup<AircraftCurrentLane> m_AircraftCurrentLaneType;

		[ReadOnly]
		public BufferLookup<AircraftNavigationLane> m_AircraftNavigationLaneType;

		[ReadOnly]
		public ComponentLookup<AircraftNavigation> m_AircraftNavigationType;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> m_TrainCurrentLaneType;

		[ReadOnly]
		public BufferLookup<TrainNavigationLane> m_TrainNavigationLaneType;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElementType;

		[ReadOnly]
		public ComponentLookup<Blocker> m_BlockerType;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> m_HumanCurrentLaneType;

		[ReadOnly]
		public ComponentLookup<AnimalCurrentLane> m_AnimalCurrentLaneType;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetType;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> m_TrackLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<LaneReservation> m_LaneReservationData;

		[ReadOnly]
		public ComponentLookup<LaneCondition> m_LaneConditionData;

		[ReadOnly]
		public ComponentLookup<LaneSignal> m_LaneSignalData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<AreaLane> m_AreaLaneData;

		[ReadOnly]
		public ComponentLookup<NodeLane> m_NodeLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Car> m_CarDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Bicycle> m_BicycleDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Watercraft> m_WatercraftDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Aircraft> m_AircraftDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> m_TrainCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<Creature> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefDataFromEntity;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<TrainData> m_PrefabTrainData;

		[ReadOnly]
		public ComponentLookup<WatercraftData> m_PrefabWatercraftData;

		[ReadOnly]
		public ComponentLookup<AircraftData> m_PrefabAircraftData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute()
		{
			float num = 4f / 15f;
			NativeList<Entity> tempBuffer = default(NativeList<Entity>);
			CarLaneSelectBuffer buffer;
			DynamicBuffer<CarNavigationLane> dynamicBuffer;
			CarData carData2;
			CarLaneSpeedIterator carLaneSpeedIterator;
			float num4;
			Game.Net.CarLaneFlags laneFlags;
			if (m_CarCurrentLaneType.TryGetComponent(m_Selected, out var componentData) && m_MovingDataFromEntity.TryGetComponent(m_Selected, out var componentData2))
			{
				buffer = default(CarLaneSelectBuffer);
				Game.Objects.Transform transform = m_TransformDataFromEntity[m_Selected];
				_ = m_TargetType[m_Selected];
				Car carData = m_CarDataFromEntity[m_Selected];
				PseudoRandomSeed randomSeed = m_PseudoRandomSeedType[m_Selected];
				PrefabRef prefabRef = m_PrefabRefDataFromEntity[m_Selected];
				CarNavigation carNavigation = m_CarNavigationType[m_Selected];
				Blocker blocker = m_BlockerType[m_Selected];
				dynamicBuffer = m_CarNavigationLaneType[m_Selected];
				carData2 = m_PrefabCarData[prefabRef.m_Prefab];
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
				float num2 = math.length(componentData2.m_Velocity);
				int priority = VehicleUtils.GetPriority(carData);
				bool flag = m_BicycleDataFromEntity.HasComponent(m_Selected);
				VehicleUtils.GetDrivingStyle(m_SimulationFrame, randomSeed, flag, out var safetyTime);
				if ((componentData.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0)
				{
					carData2.m_MaxSpeed = 277.77777f;
					carData2.m_Acceleration = 277.77777f;
					carData2.m_Braking = 277.77777f;
				}
				else
				{
					num2 = math.min(num2, carData2.m_MaxSpeed);
				}
				Bounds1 speedRange = (((componentData.m_LaneFlags & (Game.Vehicles.CarLaneFlags.Connection | Game.Vehicles.CarLaneFlags.ResetSpeed)) == 0) ? VehicleUtils.CalculateSpeedRange(carData2, num2, num) : new Bounds1(0f, carData2.m_MaxSpeed));
				if (dynamicBuffer.Length != 0)
				{
					CarLaneSelectIterator carLaneSelectIterator = new CarLaneSelectIterator
					{
						m_OwnerData = m_OwnerDataFromEntity,
						m_LaneData = m_LaneData,
						m_CarLaneData = m_CarLaneData,
						m_SlaveLaneData = m_SlaveLaneData,
						m_LaneReservationData = m_LaneReservationData,
						m_MovingData = m_MovingDataFromEntity,
						m_CarData = m_CarDataFromEntity,
						m_ControllerData = m_ControllerDataFromEntity,
						m_Lanes = m_SubLanes,
						m_LaneObjects = m_LaneObjects,
						m_Entity = m_Selected,
						m_Blocker = blocker.m_Blocker,
						m_Priority = priority,
						m_LeftHandTraffic = m_LeftHandTraffic,
						m_ForbidLaneFlags = VehicleUtils.GetForbiddenLaneFlags(carData, flag),
						m_PreferLaneFlags = VehicleUtils.GetPreferredLaneFlags(carData),
						m_PathMethods = (flag ? PathMethod.Bicycle : PathMethod.Road)
					};
					carLaneSelectIterator.SetBuffer(ref buffer);
					CarNavigationLane carNavigationLane = dynamicBuffer[dynamicBuffer.Length - 1];
					carLaneSelectIterator.CalculateLaneCosts(carNavigationLane, dynamicBuffer.Length - 1);
					for (int num3 = dynamicBuffer.Length - 2; num3 >= 0; num3--)
					{
						CarNavigationLane carNavigationLane2 = dynamicBuffer[num3];
						if (m_LaneData.HasComponent(carNavigationLane.m_Lane))
						{
							carLaneSelectIterator.CalculateLaneCosts(carNavigationLane2, carNavigationLane, num3);
						}
						carNavigationLane = carNavigationLane2;
					}
					CarCurrentLane currentLaneData = componentData;
					carLaneSelectIterator.DrawLaneCosts(currentLaneData, dynamicBuffer[0], m_CurveData, m_GizmoBatcher);
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						CarNavigationLane navLaneData = dynamicBuffer[i];
						carLaneSelectIterator.DrawLaneCosts(navLaneData, m_CurveData, m_GizmoBatcher);
					}
				}
				carLaneSpeedIterator = new CarLaneSpeedIterator
				{
					m_TransformData = m_TransformDataFromEntity,
					m_MovingData = m_MovingDataFromEntity,
					m_CarData = m_CarDataFromEntity,
					m_BicycleData = m_BicycleDataFromEntity,
					m_TrainData = m_TrainDataFromEntity,
					m_ControllerData = m_ControllerDataFromEntity,
					m_LaneReservationData = m_LaneReservationData,
					m_LaneConditionData = m_LaneConditionData,
					m_LaneSignalData = m_LaneSignalData,
					m_CurveData = m_CurveData,
					m_CarLaneData = m_CarLaneData,
					m_PedestrianLaneData = m_PedestrianLaneData,
					m_ParkingLaneData = m_ParkingLaneData,
					m_UnspawnedData = m_UnspawnedData,
					m_CreatureData = m_CreatureData,
					m_PrefabRefData = m_PrefabRefDataFromEntity,
					m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
					m_PrefabCarData = m_PrefabCarData,
					m_PrefabTrainData = m_PrefabTrainData,
					m_PrefabParkingLaneData = m_PrefabParkingLaneData,
					m_LaneOverlapData = m_LaneOverlaps,
					m_LaneObjectData = m_LaneObjects,
					m_Entity = m_Selected,
					m_Ignore = (((componentData.m_LaneFlags & Game.Vehicles.CarLaneFlags.IgnoreBlocker) != 0) ? blocker.m_Blocker : Entity.Null),
					m_TempBuffer = tempBuffer,
					m_Priority = priority,
					m_TimeStep = num,
					m_SafeTimeStep = num + safetyTime,
					m_DistanceOffset = math.select(0f, math.max(-0.5f, -0.5f * math.lengthsq(1.5f - num2)), num2 < 1.5f),
					m_SpeedLimitFactor = VehicleUtils.GetSpeedLimitFactor(carData),
					m_CurrentSpeed = num2,
					m_PrefabCar = carData2,
					m_PrefabObjectGeometry = objectGeometryData,
					m_SpeedRange = speedRange,
					m_PushBlockers = ((componentData.m_LaneFlags & Game.Vehicles.CarLaneFlags.PushBlockers) != 0),
					m_IsBicycle = flag,
					m_MaxSpeed = speedRange.max,
					m_CanChangeLane = 1f,
					m_CurrentPosition = transform.m_Position
				};
				if ((componentData.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TransformTarget | Game.Vehicles.CarLaneFlags.ParkingSpace)) != 0)
				{
					carLaneSpeedIterator.IterateParkingTarget(componentData.m_Lane, componentData.m_CurvePosition.xz);
					carLaneSpeedIterator.IterateTarget(carNavigation.m_TargetPosition);
					DrawBlocker(componentData.m_Lane, carLaneSpeedIterator.m_MaxSpeed / carData2.m_MaxSpeed);
					return;
				}
				if ((componentData.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) != 0)
				{
					float maxLaneSpeed = 11.111112f;
					if (flag && m_ConnectionLaneData.TryGetComponent(componentData.m_Lane, out var componentData3) && (componentData3.m_Flags & ConnectionLaneFlags.Pedestrian) != 0)
					{
						maxLaneSpeed = 5.555556f;
					}
					carLaneSpeedIterator.IterateTarget(carNavigation.m_TargetPosition, maxLaneSpeed);
					if (m_AreaLaneData.HasComponent(componentData.m_Lane))
					{
						Entity owner = m_OwnerDataFromEntity[componentData.m_Lane].m_Owner;
						AreaLane areaLane = m_AreaLaneData[componentData.m_Lane];
						DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = m_AreaNodes[owner];
						if (areaLane.m_Nodes.y == areaLane.m_Nodes.z)
						{
							Triangle3 triangle = new Triangle3(dynamicBuffer2[areaLane.m_Nodes.x].m_Position, dynamicBuffer2[areaLane.m_Nodes.y].m_Position, dynamicBuffer2[areaLane.m_Nodes.w].m_Position);
							m_GizmoBatcher.DrawLine(triangle.a, triangle.b, UnityEngine.Color.cyan);
							m_GizmoBatcher.DrawLine(triangle.b, triangle.c, UnityEngine.Color.cyan);
							m_GizmoBatcher.DrawLine(triangle.c, triangle.a, UnityEngine.Color.cyan);
						}
						else
						{
							bool4 @bool = new bool4(componentData.m_CurvePosition.yz < 0.5f, componentData.m_CurvePosition.yz > 0.5f);
							Triangle3 triangle2;
							Triangle3 triangle3;
							if (@bool.w)
							{
								triangle2 = new Triangle3(dynamicBuffer2[areaLane.m_Nodes.z].m_Position, dynamicBuffer2[areaLane.m_Nodes.y].m_Position, dynamicBuffer2[areaLane.m_Nodes.x].m_Position);
								triangle3 = new Triangle3(dynamicBuffer2[areaLane.m_Nodes.y].m_Position, dynamicBuffer2[areaLane.m_Nodes.z].m_Position, dynamicBuffer2[areaLane.m_Nodes.w].m_Position);
							}
							else
							{
								triangle2 = new Triangle3(dynamicBuffer2[areaLane.m_Nodes.y].m_Position, dynamicBuffer2[areaLane.m_Nodes.z].m_Position, dynamicBuffer2[areaLane.m_Nodes.w].m_Position);
								triangle3 = new Triangle3(dynamicBuffer2[areaLane.m_Nodes.z].m_Position, dynamicBuffer2[areaLane.m_Nodes.y].m_Position, dynamicBuffer2[areaLane.m_Nodes.x].m_Position);
							}
							if (math.any(@bool.xy & @bool.wz))
							{
								m_GizmoBatcher.DrawLine(triangle2.a, triangle2.b, UnityEngine.Color.blue);
								m_GizmoBatcher.DrawLine(triangle2.b, triangle2.c, UnityEngine.Color.cyan);
								m_GizmoBatcher.DrawLine(triangle2.c, triangle2.a, UnityEngine.Color.cyan);
								m_GizmoBatcher.DrawLine(triangle3.b, triangle3.c, UnityEngine.Color.green);
								m_GizmoBatcher.DrawLine(triangle3.c, triangle3.a, UnityEngine.Color.green);
							}
							else
							{
								m_GizmoBatcher.DrawLine(triangle2.b, triangle2.c, UnityEngine.Color.yellow);
								m_GizmoBatcher.DrawLine(triangle2.c, triangle2.a, UnityEngine.Color.yellow);
								m_GizmoBatcher.DrawLine(triangle3.a, triangle3.b, UnityEngine.Color.blue);
								m_GizmoBatcher.DrawLine(triangle3.b, triangle3.c, UnityEngine.Color.cyan);
								m_GizmoBatcher.DrawLine(triangle3.c, triangle3.a, UnityEngine.Color.cyan);
							}
						}
					}
					m_GizmoBatcher.DrawLine(transform.m_Position, carNavigation.m_TargetPosition, UnityEngine.Color.red);
					DrawBlocker(componentData.m_Lane, carLaneSpeedIterator.m_MaxSpeed / carData2.m_MaxSpeed);
					return;
				}
				if (componentData.m_Lane == Entity.Null)
				{
					return;
				}
				PrefabRef prefabRef2 = m_PrefabRefDataFromEntity[componentData.m_Lane];
				NetLaneData prefabLaneData = m_PrefabNetLaneData[prefabRef2.m_Prefab];
				m_NodeLaneData.TryGetComponent(componentData.m_Lane, out var componentData4);
				float laneOffset = VehicleUtils.GetLaneOffset(objectGeometryData, prefabLaneData, componentData4, componentData.m_CurvePosition.x, componentData.m_LanePosition, flag);
				num4 = laneOffset;
				Entity nextLane = Entity.Null;
				float2 nextOffset = 0f;
				if (dynamicBuffer.Length > 0)
				{
					CarNavigationLane carNavigationLane3 = dynamicBuffer[0];
					nextLane = carNavigationLane3.m_Lane;
					nextOffset = carNavigationLane3.m_CurvePosition;
				}
				if (componentData.m_ChangeLane != Entity.Null)
				{
					float4 @float = math.select(new float4(-0.5f, 0.5f, 0.002f, 0.1f), new float4(-0.5f, 0.5f, 0.02f, 0.2f), flag);
					float4 float2 = math.select(new float4(0f, 0f, 0.01f, 0.1f), new float4(0.25f, -0.25f, 0.1f, 0.2f), flag);
					float2 x = math.select(@float.xy, float2.xy, new bool2((componentData.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight)) == Game.Vehicles.CarLaneFlags.TurnRight, (componentData.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight)) == Game.Vehicles.CarLaneFlags.TurnLeft));
					float num5 = math.clamp(componentData.m_LanePosition, math.cmin(x), math.cmax(x));
					num5 = 0f - num5;
					PrefabRef prefabRef3 = m_PrefabRefDataFromEntity[componentData.m_ChangeLane];
					NetLaneData prefabLaneData2 = m_PrefabNetLaneData[prefabRef3.m_Prefab];
					m_NodeLaneData.TryGetComponent(componentData.m_ChangeLane, out var componentData5);
					num4 = VehicleUtils.GetLaneOffset(objectGeometryData, prefabLaneData2, componentData5, componentData.m_CurvePosition.x, num5, flag);
					if (!carLaneSpeedIterator.IterateFirstLane(componentData.m_Lane, componentData.m_ChangeLane, componentData.m_CurvePosition, nextLane, nextOffset, componentData.m_ChangeProgress, laneOffset, num4, (componentData.m_LaneFlags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0, out laneFlags))
					{
						goto IL_0cc5;
					}
				}
				else if (!carLaneSpeedIterator.IterateFirstLane(componentData.m_Lane, componentData.m_CurvePosition, nextLane, nextOffset, laneOffset, (componentData.m_LaneFlags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0, out laneFlags))
				{
					goto IL_0cc5;
				}
				goto IL_0e4e;
			}
			goto IL_0ea1;
			IL_186b:
			int num6 = 0;
			DynamicBuffer<WatercraftNavigationLane> dynamicBuffer3;
			WatercraftLaneSpeedIterator watercraftLaneSpeedIterator;
			WatercraftCurrentLane componentData6;
			bool needSignal;
			while (true)
			{
				if (num6 < dynamicBuffer3.Length)
				{
					WatercraftNavigationLane watercraftNavigationLane = dynamicBuffer3[num6];
					if ((watercraftNavigationLane.m_Flags & (WatercraftLaneFlags.TransformTarget | WatercraftLaneFlags.Area)) == 0)
					{
						if ((watercraftNavigationLane.m_Flags & WatercraftLaneFlags.Connection) != 0)
						{
							watercraftLaneSpeedIterator.m_PrefabWatercraft.m_MaxSpeed = 277.77777f;
							watercraftLaneSpeedIterator.m_PrefabWatercraft.m_Acceleration = 277.77777f;
							watercraftLaneSpeedIterator.m_PrefabWatercraft.m_Braking = 277.77777f;
							watercraftLaneSpeedIterator.m_SpeedRange = new Bounds1(0f, 277.77777f);
						}
						else if ((componentData6.m_LaneFlags & WatercraftLaneFlags.Connection) != 0)
						{
							goto IL_19b0;
						}
						bool test = (watercraftNavigationLane.m_Lane == componentData6.m_Lane) | (watercraftNavigationLane.m_Lane == componentData6.m_ChangeLane);
						bool ignoreSignal = (watercraftNavigationLane.m_Flags & WatercraftLaneFlags.IgnoreSignal) != 0;
						float minOffset = math.select(-1f, componentData6.m_CurvePosition.y, test);
						if (watercraftLaneSpeedIterator.IterateNextLane(watercraftNavigationLane.m_Lane, watercraftNavigationLane.m_CurvePosition, minOffset, ignoreSignal, out needSignal))
						{
							break;
						}
						num6++;
						continue;
					}
					VehicleUtils.CalculateTransformPosition(ref watercraftLaneSpeedIterator.m_CurrentPosition, watercraftNavigationLane.m_Lane, m_TransformDataFromEntity, m_PositionData, m_PrefabRefDataFromEntity, m_PrefabBuildingData);
				}
				goto IL_19b0;
				IL_19b0:
				watercraftLaneSpeedIterator.IterateTarget(watercraftLaneSpeedIterator.m_CurrentPosition);
				break;
			}
			goto IL_19be;
			IL_0ea1:
			WatercraftLaneSelectBuffer buffer2;
			WatercraftData watercraftData;
			if (m_WatercraftCurrentLaneType.TryGetComponent(m_Selected, out componentData6) && m_MovingDataFromEntity.TryGetComponent(m_Selected, out componentData2))
			{
				buffer2 = default(WatercraftLaneSelectBuffer);
				Game.Objects.Transform transform2 = m_TransformDataFromEntity[m_Selected];
				_ = m_TargetType[m_Selected];
				_ = m_WatercraftDataFromEntity[m_Selected];
				PrefabRef prefabRef4 = m_PrefabRefDataFromEntity[m_Selected];
				WatercraftNavigation watercraftNavigation = m_WatercraftNavigationType[m_Selected];
				Blocker blocker2 = m_BlockerType[m_Selected];
				dynamicBuffer3 = m_WatercraftNavigationLaneType[m_Selected];
				watercraftData = m_PrefabWatercraftData[prefabRef4.m_Prefab];
				ObjectGeometryData prefabObjectGeometry = m_PrefabObjectGeometryData[prefabRef4.m_Prefab];
				float num7 = math.length(componentData2.m_Velocity);
				int priority2 = VehicleUtils.GetPriority(watercraftData);
				if ((componentData6.m_LaneFlags & WatercraftLaneFlags.Connection) != 0)
				{
					watercraftData.m_MaxSpeed = 277.77777f;
					watercraftData.m_Acceleration = 277.77777f;
					watercraftData.m_Braking = 277.77777f;
				}
				else
				{
					num7 = math.min(num7, watercraftData.m_MaxSpeed);
				}
				Bounds1 speedRange2 = (((componentData6.m_LaneFlags & (WatercraftLaneFlags.ResetSpeed | WatercraftLaneFlags.Connection)) == 0) ? VehicleUtils.CalculateSpeedRange(watercraftData, num7, num) : new Bounds1(0f, watercraftData.m_MaxSpeed));
				float3 position = transform2.m_Position;
				if ((componentData6.m_LaneFlags & (WatercraftLaneFlags.TransformTarget | WatercraftLaneFlags.Area)) == 0 && m_CurveData.TryGetComponent(componentData6.m_Lane, out var componentData7))
				{
					PrefabRef prefabRef5 = m_PrefabRefDataFromEntity[componentData6.m_Lane];
					NetLaneData netLaneData = m_PrefabNetLaneData[prefabRef5.m_Prefab];
					float2 value = MathUtils.Tangent(componentData7.m_Bezier, componentData6.m_CurvePosition.x).xz;
					if (MathUtils.TryNormalize(ref value))
					{
						position.xz -= MathUtils.Right(value) * ((netLaneData.m_Width - prefabObjectGeometry.m_Size.x) * componentData6.m_LanePosition * 0.5f);
					}
				}
				if (dynamicBuffer3.Length != 0)
				{
					WatercraftLaneSelectIterator watercraftLaneSelectIterator = new WatercraftLaneSelectIterator
					{
						m_OwnerData = m_OwnerDataFromEntity,
						m_LaneData = m_LaneData,
						m_SlaveLaneData = m_SlaveLaneData,
						m_LaneReservationData = m_LaneReservationData,
						m_MovingData = m_MovingDataFromEntity,
						m_WatercraftData = m_WatercraftDataFromEntity,
						m_Lanes = m_SubLanes,
						m_LaneObjects = m_LaneObjects,
						m_Entity = m_Selected,
						m_Blocker = blocker2.m_Blocker,
						m_Priority = priority2
					};
					watercraftLaneSelectIterator.SetBuffer(ref buffer2);
					WatercraftNavigationLane watercraftNavigationLane2 = dynamicBuffer3[dynamicBuffer3.Length - 1];
					watercraftLaneSelectIterator.CalculateLaneCosts(watercraftNavigationLane2, dynamicBuffer3.Length - 1);
					for (int num8 = dynamicBuffer3.Length - 2; num8 >= 0; num8--)
					{
						WatercraftNavigationLane watercraftNavigationLane3 = dynamicBuffer3[num8];
						watercraftLaneSelectIterator.CalculateLaneCosts(watercraftNavigationLane3, watercraftNavigationLane2, num8);
						watercraftNavigationLane2 = watercraftNavigationLane3;
					}
					WatercraftCurrentLane currentLaneData2 = componentData6;
					watercraftLaneSelectIterator.DrawLaneCosts(currentLaneData2, dynamicBuffer3[0], m_CurveData, m_GizmoBatcher);
					for (int j = 0; j < dynamicBuffer3.Length; j++)
					{
						WatercraftNavigationLane navLaneData2 = dynamicBuffer3[j];
						watercraftLaneSelectIterator.DrawLaneCosts(navLaneData2, m_CurveData, m_GizmoBatcher);
					}
				}
				watercraftLaneSpeedIterator = new WatercraftLaneSpeedIterator
				{
					m_TransformData = m_TransformDataFromEntity,
					m_MovingData = m_MovingDataFromEntity,
					m_WatercraftData = m_WatercraftDataFromEntity,
					m_LaneReservationData = m_LaneReservationData,
					m_LaneSignalData = m_LaneSignalData,
					m_CurveData = m_CurveData,
					m_CarLaneData = m_CarLaneData,
					m_PrefabRefData = m_PrefabRefDataFromEntity,
					m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
					m_PrefabWatercraftData = m_PrefabWatercraftData,
					m_LaneOverlapData = m_LaneOverlaps,
					m_LaneObjectData = m_LaneObjects,
					m_Entity = m_Selected,
					m_Ignore = (((componentData6.m_LaneFlags & WatercraftLaneFlags.IgnoreBlocker) != 0) ? blocker2.m_Blocker : Entity.Null),
					m_Priority = priority2,
					m_TimeStep = num,
					m_SafeTimeStep = num + 0.5f,
					m_SpeedLimitFactor = 1f,
					m_CurrentSpeed = num7,
					m_PrefabWatercraft = watercraftData,
					m_PrefabObjectGeometry = prefabObjectGeometry,
					m_SpeedRange = speedRange2,
					m_MaxSpeed = speedRange2.max,
					m_CanChangeLane = 1f,
					m_CurrentPosition = position
				};
				if ((componentData6.m_LaneFlags & WatercraftLaneFlags.TransformTarget) != 0)
				{
					watercraftLaneSpeedIterator.IterateTarget(watercraftNavigation.m_TargetPosition);
					DrawBlocker(componentData6.m_Lane, watercraftLaneSpeedIterator.m_MaxSpeed / watercraftData.m_MaxSpeed);
					return;
				}
				if ((componentData6.m_LaneFlags & WatercraftLaneFlags.Area) != 0)
				{
					watercraftLaneSpeedIterator.IterateTarget(watercraftNavigation.m_TargetPosition, 11.111112f);
					if (m_AreaLaneData.HasComponent(componentData6.m_Lane))
					{
						Entity owner2 = m_OwnerDataFromEntity[componentData6.m_Lane].m_Owner;
						AreaLane areaLane2 = m_AreaLaneData[componentData6.m_Lane];
						DynamicBuffer<Game.Areas.Node> dynamicBuffer4 = m_AreaNodes[owner2];
						if (areaLane2.m_Nodes.y == areaLane2.m_Nodes.z)
						{
							Triangle3 triangle4 = new Triangle3(dynamicBuffer4[areaLane2.m_Nodes.x].m_Position, dynamicBuffer4[areaLane2.m_Nodes.y].m_Position, dynamicBuffer4[areaLane2.m_Nodes.w].m_Position);
							m_GizmoBatcher.DrawLine(triangle4.a, triangle4.b, UnityEngine.Color.cyan);
							m_GizmoBatcher.DrawLine(triangle4.b, triangle4.c, UnityEngine.Color.cyan);
							m_GizmoBatcher.DrawLine(triangle4.c, triangle4.a, UnityEngine.Color.cyan);
						}
						else
						{
							bool4 bool2 = new bool4(componentData6.m_CurvePosition.yz < 0.5f, componentData6.m_CurvePosition.yz > 0.5f);
							Triangle3 triangle5;
							Triangle3 triangle6;
							if (bool2.w)
							{
								triangle5 = new Triangle3(dynamicBuffer4[areaLane2.m_Nodes.z].m_Position, dynamicBuffer4[areaLane2.m_Nodes.y].m_Position, dynamicBuffer4[areaLane2.m_Nodes.x].m_Position);
								triangle6 = new Triangle3(dynamicBuffer4[areaLane2.m_Nodes.y].m_Position, dynamicBuffer4[areaLane2.m_Nodes.z].m_Position, dynamicBuffer4[areaLane2.m_Nodes.w].m_Position);
							}
							else
							{
								triangle5 = new Triangle3(dynamicBuffer4[areaLane2.m_Nodes.y].m_Position, dynamicBuffer4[areaLane2.m_Nodes.z].m_Position, dynamicBuffer4[areaLane2.m_Nodes.w].m_Position);
								triangle6 = new Triangle3(dynamicBuffer4[areaLane2.m_Nodes.z].m_Position, dynamicBuffer4[areaLane2.m_Nodes.y].m_Position, dynamicBuffer4[areaLane2.m_Nodes.x].m_Position);
							}
							if (math.any(bool2.xy & bool2.wz))
							{
								m_GizmoBatcher.DrawLine(triangle5.a, triangle5.b, UnityEngine.Color.blue);
								m_GizmoBatcher.DrawLine(triangle5.b, triangle5.c, UnityEngine.Color.cyan);
								m_GizmoBatcher.DrawLine(triangle5.c, triangle5.a, UnityEngine.Color.cyan);
								m_GizmoBatcher.DrawLine(triangle6.b, triangle6.c, UnityEngine.Color.green);
								m_GizmoBatcher.DrawLine(triangle6.c, triangle6.a, UnityEngine.Color.green);
							}
							else
							{
								m_GizmoBatcher.DrawLine(triangle5.b, triangle5.c, UnityEngine.Color.yellow);
								m_GizmoBatcher.DrawLine(triangle5.c, triangle5.a, UnityEngine.Color.yellow);
								m_GizmoBatcher.DrawLine(triangle6.a, triangle6.b, UnityEngine.Color.blue);
								m_GizmoBatcher.DrawLine(triangle6.b, triangle6.c, UnityEngine.Color.cyan);
								m_GizmoBatcher.DrawLine(triangle6.c, triangle6.a, UnityEngine.Color.cyan);
							}
						}
					}
					m_GizmoBatcher.DrawLine(transform2.m_Position, watercraftNavigation.m_TargetPosition, UnityEngine.Color.red);
					DrawBlocker(componentData6.m_Lane, watercraftLaneSpeedIterator.m_MaxSpeed / watercraftData.m_MaxSpeed);
					return;
				}
				if (componentData6.m_Lane == Entity.Null)
				{
					return;
				}
				if (componentData6.m_ChangeLane != Entity.Null)
				{
					if (!watercraftLaneSpeedIterator.IterateFirstLane(componentData6.m_Lane, componentData6.m_ChangeLane, componentData6.m_CurvePosition, componentData6.m_ChangeProgress))
					{
						goto IL_186b;
					}
				}
				else if (!watercraftLaneSpeedIterator.IterateFirstLane(componentData6.m_Lane, componentData6.m_CurvePosition))
				{
					goto IL_186b;
				}
				goto IL_19be;
			}
			goto IL_19f4;
			IL_19f4:
			if (m_AircraftCurrentLaneType.TryGetComponent(m_Selected, out var componentData8) && m_MovingDataFromEntity.TryGetComponent(m_Selected, out componentData2))
			{
				Game.Objects.Transform transform3 = m_TransformDataFromEntity[m_Selected];
				_ = m_TargetType[m_Selected];
				_ = m_AircraftDataFromEntity[m_Selected];
				PrefabRef prefabRef6 = m_PrefabRefDataFromEntity[m_Selected];
				AircraftNavigation aircraftNavigation = m_AircraftNavigationType[m_Selected];
				Blocker blocker3 = m_BlockerType[m_Selected];
				DynamicBuffer<AircraftNavigationLane> dynamicBuffer5 = m_AircraftNavigationLaneType[m_Selected];
				AircraftData aircraftData = m_PrefabAircraftData[prefabRef6.m_Prefab];
				ObjectGeometryData prefabObjectGeometry2 = m_PrefabObjectGeometryData[prefabRef6.m_Prefab];
				float3 float3;
				if (m_CurveData.HasComponent(componentData8.m_Lane))
				{
					Curve curve = m_CurveData[componentData8.m_Lane];
					Bezier4x3 bezier = MathUtils.Cut(curve.m_Bezier, componentData8.m_CurvePosition.xy);
					Bezier4x3 bezier2 = MathUtils.Cut(curve.m_Bezier, componentData8.m_CurvePosition.yz);
					float3 = bezier2.d;
					m_GizmoBatcher.DrawCurve(bezier, curve.m_Length, new UnityEngine.Color(1f, 0.5f, 0f, 1f));
					m_GizmoBatcher.DrawCurve(bezier2, curve.m_Length, UnityEngine.Color.yellow);
					m_GizmoBatcher.DrawLine(transform3.m_Position, bezier.a, UnityEngine.Color.red);
				}
				else
				{
					if (!m_TransformDataFromEntity.HasComponent(componentData8.m_Lane))
					{
						return;
					}
					float3 = m_TransformDataFromEntity[componentData8.m_Lane].m_Position;
					m_GizmoBatcher.DrawLine(transform3.m_Position, float3, UnityEngine.Color.red);
				}
				for (int k = 0; k < dynamicBuffer5.Length; k++)
				{
					AircraftNavigationLane aircraftNavigationLane = dynamicBuffer5[k];
					if (m_CurveData.HasComponent(aircraftNavigationLane.m_Lane))
					{
						Curve curve2 = m_CurveData[aircraftNavigationLane.m_Lane];
						Bezier4x3 bezier3 = MathUtils.Cut(curve2.m_Bezier, aircraftNavigationLane.m_CurvePosition);
						m_GizmoBatcher.DrawCurve(bezier3, curve2.m_Length, UnityEngine.Color.green);
						if (math.lengthsq(bezier3.a - float3) > 1f)
						{
							m_GizmoBatcher.DrawLine(float3, bezier3.a, UnityEngine.Color.magenta);
						}
						float3 = bezier3.d;
						continue;
					}
					if (!m_TransformDataFromEntity.HasComponent(componentData8.m_Lane))
					{
						break;
					}
					float3 position2 = m_TransformDataFromEntity[componentData8.m_Lane].m_Position;
					if (math.lengthsq(position2 - float3) > 1f)
					{
						m_GizmoBatcher.DrawLine(float3, position2, UnityEngine.Color.magenta);
					}
					float3 = position2;
				}
				float currentSpeed = math.length(componentData2.m_Velocity);
				int priority3 = VehicleUtils.GetPriority(aircraftData);
				if ((componentData8.m_LaneFlags & AircraftLaneFlags.Flying) == 0)
				{
					float3 position3 = transform3.m_Position;
					if (m_CurveData.HasComponent(componentData8.m_Lane))
					{
						Curve curve3 = m_CurveData[componentData8.m_Lane];
						PrefabRef prefabRef7 = m_PrefabRefDataFromEntity[componentData8.m_Lane];
						NetLaneData netLaneData2 = m_PrefabNetLaneData[prefabRef7.m_Prefab];
						float2 value2 = MathUtils.Tangent(curve3.m_Bezier, componentData8.m_CurvePosition.x).xz;
						if (MathUtils.TryNormalize(ref value2))
						{
							position3.xz -= MathUtils.Right(value2) * ((netLaneData2.m_Width - prefabObjectGeometry2.m_Size.x) * componentData8.m_LanePosition * 0.5f);
						}
					}
					Bounds1 speedRange3 = (((componentData8.m_LaneFlags & (AircraftLaneFlags.Connection | AircraftLaneFlags.ResetSpeed)) == 0) ? VehicleUtils.CalculateSpeedRange(aircraftData, currentSpeed, num) : new Bounds1(0f, aircraftData.m_GroundMaxSpeed));
					AircraftLaneSpeedIterator aircraftLaneSpeedIterator = new AircraftLaneSpeedIterator
					{
						m_TransformData = m_TransformDataFromEntity,
						m_MovingData = m_MovingDataFromEntity,
						m_AircraftData = m_AircraftDataFromEntity,
						m_LaneReservationData = m_LaneReservationData,
						m_CurveData = m_CurveData,
						m_CarLaneData = m_CarLaneData,
						m_PrefabRefData = m_PrefabRefDataFromEntity,
						m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
						m_PrefabAircraftData = m_PrefabAircraftData,
						m_LaneOverlapData = m_LaneOverlaps,
						m_LaneObjectData = m_LaneObjects,
						m_Entity = m_Selected,
						m_Ignore = (((componentData8.m_LaneFlags & AircraftLaneFlags.IgnoreBlocker) != 0) ? blocker3.m_Blocker : Entity.Null),
						m_Priority = priority3,
						m_TimeStep = num,
						m_SafeTimeStep = num + 0.5f,
						m_PrefabAircraft = aircraftData,
						m_PrefabObjectGeometry = prefabObjectGeometry2,
						m_SpeedRange = speedRange3,
						m_MaxSpeed = speedRange3.max,
						m_CanChangeLane = 1f,
						m_CurrentPosition = position3
					};
					if ((componentData8.m_LaneFlags & AircraftLaneFlags.TransformTarget) != 0)
					{
						aircraftLaneSpeedIterator.IterateTarget(aircraftNavigation.m_TargetPosition);
						DrawBlocker(componentData8.m_Lane, aircraftLaneSpeedIterator.m_MaxSpeed / aircraftData.m_GroundMaxSpeed);
						return;
					}
					if (componentData8.m_Lane == Entity.Null)
					{
						return;
					}
					if (!aircraftLaneSpeedIterator.IterateFirstLane(componentData8.m_Lane, componentData8.m_CurvePosition))
					{
						int num9 = 0;
						while (true)
						{
							if (num9 < dynamicBuffer5.Length)
							{
								AircraftNavigationLane aircraftNavigationLane2 = dynamicBuffer5[num9];
								if ((aircraftNavigationLane2.m_Flags & AircraftLaneFlags.TransformTarget) == 0)
								{
									if ((aircraftNavigationLane2.m_Flags & AircraftLaneFlags.Connection) != 0)
									{
										aircraftLaneSpeedIterator.m_PrefabAircraft.m_GroundMaxSpeed = 277.77777f;
										aircraftLaneSpeedIterator.m_PrefabAircraft.m_GroundAcceleration = 277.77777f;
										aircraftLaneSpeedIterator.m_PrefabAircraft.m_GroundBraking = 277.77777f;
										aircraftLaneSpeedIterator.m_SpeedRange = new Bounds1(0f, 277.77777f);
									}
									else if ((componentData8.m_LaneFlags & AircraftLaneFlags.Connection) != 0)
									{
										goto IL_20b3;
									}
									bool test2 = aircraftNavigationLane2.m_Lane == componentData8.m_Lane;
									float minOffset2 = math.select(-1f, componentData8.m_CurvePosition.y, test2);
									if (aircraftLaneSpeedIterator.IterateNextLane(aircraftNavigationLane2.m_Lane, aircraftNavigationLane2.m_CurvePosition, minOffset2))
									{
										break;
									}
									num9++;
									continue;
								}
								VehicleUtils.CalculateTransformPosition(ref aircraftLaneSpeedIterator.m_CurrentPosition, aircraftNavigationLane2.m_Lane, m_TransformDataFromEntity, m_PositionData, m_PrefabRefDataFromEntity, m_PrefabBuildingData);
							}
							goto IL_20b3;
							IL_20b3:
							aircraftLaneSpeedIterator.IterateTarget(aircraftLaneSpeedIterator.m_CurrentPosition);
							break;
						}
					}
					if (aircraftLaneSpeedIterator.m_Blocker != Entity.Null)
					{
						DrawBlocker(aircraftLaneSpeedIterator.m_Blocker, aircraftLaneSpeedIterator.m_MaxSpeed / aircraftData.m_GroundMaxSpeed);
					}
				}
			}
			TrainData trainData;
			TrainLaneSpeedIterator trainLaneSpeedIterator;
			if (m_TrainCurrentLaneType.TryGetComponent(m_Selected, out var _) && m_MovingDataFromEntity.TryGetComponent(m_Selected, out componentData2))
			{
				Entity entity = m_Selected;
				if (m_LayoutElementType.TryGetBuffer(m_Selected, out var bufferData))
				{
					if (bufferData.Length == 0)
					{
						return;
					}
					entity = bufferData[0].m_Vehicle;
				}
				Game.Objects.Transform transform4 = m_TransformDataFromEntity[entity];
				Train train = m_TrainDataFromEntity[entity];
				TrainCurrentLane trainCurrentLane = m_TrainCurrentLaneData[entity];
				PrefabRef prefabRef8 = m_PrefabRefDataFromEntity[entity];
				trainData = m_PrefabTrainData[prefabRef8.m_Prefab];
				ObjectGeometryData prefabObjectGeometry3 = m_PrefabObjectGeometryData[prefabRef8.m_Prefab];
				VehicleUtils.CalculateTrainNavigationPivots(transform4, trainData, out var pivot, out var pivot2);
				if ((train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0)
				{
					CommonUtils.Swap(ref pivot, ref pivot2);
					trainData.m_BogieOffsets = trainData.m_BogieOffsets.yx;
					trainData.m_AttachOffsets = trainData.m_AttachOffsets.yx;
				}
				if (!m_CurveData.HasComponent(trainCurrentLane.m_Front.m_Lane) || !m_CurveData.HasComponent(trainCurrentLane.m_Rear.m_Lane))
				{
					return;
				}
				Curve curve4 = m_CurveData[trainCurrentLane.m_Front.m_Lane];
				Curve curve5 = m_CurveData[trainCurrentLane.m_Rear.m_Lane];
				Bezier4x3 bezier4 = MathUtils.Cut(curve4.m_Bezier, trainCurrentLane.m_Front.m_CurvePosition.yw);
				Bezier4x3 bezier4x = MathUtils.Cut(curve5.m_Bezier, trainCurrentLane.m_Rear.m_CurvePosition.yw);
				float length = curve4.m_Length * math.abs(trainCurrentLane.m_Front.m_CurvePosition.w - trainCurrentLane.m_Front.m_CurvePosition.y);
				m_GizmoBatcher.DrawCurve(bezier4, length, new UnityEngine.Color(1f, 0.5f, 0f, 1f));
				m_GizmoBatcher.DrawLine(pivot, bezier4.a, UnityEngine.Color.red);
				m_GizmoBatcher.DrawLine(pivot2, bezier4x.a, UnityEngine.Color.red);
				if (m_TrainNavigationLaneType.TryGetBuffer(m_Selected, out var bufferData2))
				{
					for (int l = 1; l < bufferData.Length; l++)
					{
						Entity vehicle = bufferData[l].m_Vehicle;
						PrefabRef prefabRef9 = m_PrefabRefDataFromEntity[vehicle];
						TrainData trainData2 = m_PrefabTrainData[prefabRef9.m_Prefab];
						trainData.m_MaxSpeed = math.min(trainData.m_MaxSpeed, trainData2.m_MaxSpeed);
						trainData.m_Acceleration = math.min(trainData.m_Acceleration, trainData2.m_Acceleration);
						trainData.m_Braking = math.min(trainData.m_Braking, trainData2.m_Braking);
					}
					float currentSpeed2 = math.length(componentData2.m_Velocity);
					Bounds1 speedRange4 = VehicleUtils.CalculateSpeedRange(trainData, currentSpeed2, num);
					int priority4 = VehicleUtils.GetPriority(trainData);
					trainLaneSpeedIterator = new TrainLaneSpeedIterator
					{
						m_TransformData = m_TransformDataFromEntity,
						m_MovingData = m_MovingDataFromEntity,
						m_CarData = m_CarDataFromEntity,
						m_TrainData = m_TrainDataFromEntity,
						m_LaneReservationData = m_LaneReservationData,
						m_LaneSignalData = m_LaneSignalData,
						m_CreatureData = m_CreatureData,
						m_CurveData = m_CurveData,
						m_TrackLaneData = m_TrackLaneData,
						m_ControllerData = m_ControllerDataFromEntity,
						m_PrefabRefData = m_PrefabRefDataFromEntity,
						m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
						m_PrefabCarData = m_PrefabCarData,
						m_PrefabTrainData = m_PrefabTrainData,
						m_LaneOverlapData = m_LaneOverlaps,
						m_LaneObjectData = m_LaneObjects,
						m_Controller = m_Selected,
						m_Priority = priority4,
						m_TimeStep = num,
						m_SafeTimeStep = num + 0.5f,
						m_CurrentSpeed = currentSpeed2,
						m_PrefabTrain = trainData,
						m_SpeedRange = speedRange4,
						m_RearPosition = pivot2,
						m_PushBlockers = ((trainCurrentLane.m_Front.m_LaneFlags & TrainLaneFlags.PushBlockers) != 0),
						m_MaxSpeed = speedRange4.max,
						m_CurrentPosition = pivot
					};
					float3 d = bezier4.d;
					for (int m = 0; m < bufferData2.Length; m++)
					{
						TrainNavigationLane trainNavigationLane = bufferData2[m];
						if (!m_CurveData.HasComponent(trainNavigationLane.m_Lane))
						{
							break;
						}
						Curve curve6 = m_CurveData[trainNavigationLane.m_Lane];
						bezier4 = MathUtils.Cut(curve6.m_Bezier, trainNavigationLane.m_CurvePosition);
						length = curve6.m_Length * math.abs(trainNavigationLane.m_CurvePosition.x - trainNavigationLane.m_CurvePosition.y);
						m_GizmoBatcher.DrawCurve(bezier4, length, UnityEngine.Color.green);
						if (m != 0 && math.lengthsq(bezier4.a - d) > 1f)
						{
							m_GizmoBatcher.DrawLine(d, bezier4.a, UnityEngine.Color.magenta);
						}
						d = bezier4.d;
					}
					for (int num10 = bufferData.Length - 1; num10 >= 1; num10--)
					{
						Entity vehicle2 = bufferData[num10].m_Vehicle;
						TrainCurrentLane trainCurrentLane2 = m_TrainCurrentLaneData[vehicle2];
						PrefabRef prefabRef10 = m_PrefabRefDataFromEntity[vehicle2];
						TrainData prefabTrain = m_PrefabTrainData[prefabRef10.m_Prefab];
						trainLaneSpeedIterator.m_PrefabTrain = prefabTrain;
						trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane2.m_RearCache.m_Lane, out needSignal);
						trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane2.m_Rear.m_Lane, out needSignal);
						trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane2.m_FrontCache.m_Lane, out needSignal);
						trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane2.m_Front.m_Lane, out needSignal);
					}
					bool flag2 = (trainCurrentLane.m_Front.m_LaneFlags & TrainLaneFlags.Exclusive) != 0;
					bool skipCurrent = false;
					if (!flag2 && bufferData2.Length != 0)
					{
						skipCurrent = (bufferData2[0].m_Flags & (TrainLaneFlags.Reserved | TrainLaneFlags.Exclusive)) == (TrainLaneFlags.Reserved | TrainLaneFlags.Exclusive);
					}
					trainLaneSpeedIterator.m_PrefabTrain = trainData;
					trainLaneSpeedIterator.m_PrefabObjectGeometry = prefabObjectGeometry3;
					trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane.m_RearCache.m_Lane, out needSignal);
					trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane.m_Rear.m_Lane, out needSignal);
					trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane.m_FrontCache.m_Lane, out needSignal);
					if (!trainLaneSpeedIterator.IterateFirstLane(trainCurrentLane.m_Front.m_Lane, trainCurrentLane.m_Front.m_CurvePosition, flag2, ignoreObstacles: false, skipCurrent, out needSignal))
					{
						if ((trainCurrentLane.m_Front.m_LaneFlags & (TrainLaneFlags.EndOfPath | TrainLaneFlags.Return)) == 0)
						{
							int num11 = 0;
							while (num11 < bufferData2.Length)
							{
								TrainNavigationLane trainNavigationLane2 = bufferData2[num11];
								bool flag3 = trainNavigationLane2.m_Lane == trainCurrentLane.m_Front.m_Lane;
								if ((trainNavigationLane2.m_Flags & (TrainLaneFlags.Reserved | TrainLaneFlags.Connection)) == 0)
								{
									while ((trainNavigationLane2.m_Flags & (TrainLaneFlags.EndOfPath | TrainLaneFlags.BlockReserve)) == 0 && ++num11 < bufferData2.Length)
									{
										trainNavigationLane2 = bufferData2[num11];
									}
									trainLaneSpeedIterator.IterateTarget(trainNavigationLane2.m_Lane, flag3);
								}
								else
								{
									if ((trainNavigationLane2.m_Flags & TrainLaneFlags.Connection) != 0)
									{
										trainLaneSpeedIterator.m_PrefabTrain.m_MaxSpeed = 277.77777f;
										trainLaneSpeedIterator.m_PrefabTrain.m_Acceleration = 277.77777f;
										trainLaneSpeedIterator.m_PrefabTrain.m_Braking = 277.77777f;
										trainLaneSpeedIterator.m_SpeedRange = new Bounds1(0f, 277.77777f);
									}
									float minOffset3 = math.select(-1f, trainCurrentLane.m_Front.m_CurvePosition.z, flag3);
									if (!trainLaneSpeedIterator.IterateNextLane(trainNavigationLane2.m_Lane, trainNavigationLane2.m_CurvePosition, minOffset3, (trainNavigationLane2.m_Flags & TrainLaneFlags.Exclusive) != 0, flag3, out needSignal))
									{
										if ((trainNavigationLane2.m_Flags & (TrainLaneFlags.EndOfPath | TrainLaneFlags.Return)) != 0)
										{
											break;
										}
										num11++;
										continue;
									}
								}
								goto IL_291b;
							}
						}
						trainLaneSpeedIterator.IterateTarget();
					}
					goto IL_291b;
				}
			}
			goto IL_294a;
			IL_0e4e:
			if (carLaneSpeedIterator.m_Blocker != Entity.Null)
			{
				DrawBlocker(carLaneSpeedIterator.m_Blocker, carLaneSpeedIterator.m_MaxSpeed / carData2.m_MaxSpeed);
			}
			if (carLaneSpeedIterator.m_TempBuffer.IsCreated)
			{
				tempBuffer = carLaneSpeedIterator.m_TempBuffer;
				tempBuffer.Clear();
			}
			buffer.Dispose();
			goto IL_0ea1;
			IL_0cc5:
			int num12 = 0;
			while (true)
			{
				if (num12 < dynamicBuffer.Length)
				{
					CarNavigationLane carNavigationLane4 = dynamicBuffer[num12];
					if ((carNavigationLane4.m_Flags & (Game.Vehicles.CarLaneFlags.TransformTarget | Game.Vehicles.CarLaneFlags.Area)) == 0)
					{
						if ((carNavigationLane4.m_Flags & Game.Vehicles.CarLaneFlags.Connection) != 0)
						{
							carLaneSpeedIterator.m_PrefabCar.m_MaxSpeed = 277.77777f;
							carLaneSpeedIterator.m_PrefabCar.m_Acceleration = 277.77777f;
							carLaneSpeedIterator.m_PrefabCar.m_Braking = 277.77777f;
							carLaneSpeedIterator.m_SpeedRange = new Bounds1(0f, 277.77777f);
						}
						else
						{
							if ((componentData.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0)
							{
								goto IL_0e40;
							}
							if ((carNavigationLane4.m_Flags & Game.Vehicles.CarLaneFlags.Interruption) != 0)
							{
								carLaneSpeedIterator.m_PrefabCar.m_MaxSpeed = 3f;
							}
						}
						bool test3 = (carNavigationLane4.m_Lane == componentData.m_Lane) | (carNavigationLane4.m_Lane == componentData.m_ChangeLane);
						float falseValue = math.select(-1f, 2f, carNavigationLane4.m_CurvePosition.y < carNavigationLane4.m_CurvePosition.x);
						falseValue = math.select(falseValue, componentData.m_CurvePosition.y, test3);
						if (carLaneSpeedIterator.IterateNextLane(carNavigationLane4.m_Lane, carNavigationLane4.m_CurvePosition, num4, falseValue, dynamicBuffer.AsNativeArray().GetSubArray(num12 + 1, dynamicBuffer.Length - 1 - num12), (carNavigationLane4.m_Flags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0, ref laneFlags, out needSignal))
						{
							break;
						}
						num12++;
						continue;
					}
				}
				goto IL_0e40;
				IL_0e40:
				carLaneSpeedIterator.IterateTarget(carLaneSpeedIterator.m_CurrentPosition);
				break;
			}
			goto IL_0e4e;
			IL_291b:
			if (trainLaneSpeedIterator.m_Blocker != Entity.Null)
			{
				DrawBlocker(trainLaneSpeedIterator.m_Blocker, trainLaneSpeedIterator.m_MaxSpeed / trainData.m_MaxSpeed);
			}
			goto IL_294a;
			IL_294a:
			if (m_HumanCurrentLaneType.TryGetComponent(m_Selected, out var componentData10))
			{
				Blocker blocker4 = m_BlockerType[m_Selected];
				if (m_AreaLaneData.HasComponent(componentData10.m_Lane))
				{
					Entity owner3 = m_OwnerDataFromEntity[componentData10.m_Lane].m_Owner;
					AreaLane areaLane3 = m_AreaLaneData[componentData10.m_Lane];
					DynamicBuffer<Game.Areas.Node> dynamicBuffer6 = m_AreaNodes[owner3];
					if (areaLane3.m_Nodes.y == areaLane3.m_Nodes.z)
					{
						Triangle3 triangle7 = new Triangle3(dynamicBuffer6[areaLane3.m_Nodes.x].m_Position, dynamicBuffer6[areaLane3.m_Nodes.y].m_Position, dynamicBuffer6[areaLane3.m_Nodes.w].m_Position);
						m_GizmoBatcher.DrawLine(triangle7.a, triangle7.b, UnityEngine.Color.cyan);
						m_GizmoBatcher.DrawLine(triangle7.b, triangle7.c, UnityEngine.Color.cyan);
						m_GizmoBatcher.DrawLine(triangle7.c, triangle7.a, UnityEngine.Color.cyan);
					}
					else
					{
						bool4 bool3 = new bool4(componentData10.m_CurvePosition < 0.5f, componentData10.m_CurvePosition > 0.5f);
						Triangle3 triangle8;
						Triangle3 triangle9;
						if (bool3.w)
						{
							triangle8 = new Triangle3(dynamicBuffer6[areaLane3.m_Nodes.z].m_Position, dynamicBuffer6[areaLane3.m_Nodes.y].m_Position, dynamicBuffer6[areaLane3.m_Nodes.x].m_Position);
							triangle9 = new Triangle3(dynamicBuffer6[areaLane3.m_Nodes.y].m_Position, dynamicBuffer6[areaLane3.m_Nodes.z].m_Position, dynamicBuffer6[areaLane3.m_Nodes.w].m_Position);
						}
						else
						{
							triangle8 = new Triangle3(dynamicBuffer6[areaLane3.m_Nodes.y].m_Position, dynamicBuffer6[areaLane3.m_Nodes.z].m_Position, dynamicBuffer6[areaLane3.m_Nodes.w].m_Position);
							triangle9 = new Triangle3(dynamicBuffer6[areaLane3.m_Nodes.z].m_Position, dynamicBuffer6[areaLane3.m_Nodes.y].m_Position, dynamicBuffer6[areaLane3.m_Nodes.x].m_Position);
						}
						if (math.any(bool3.xy & bool3.wz))
						{
							m_GizmoBatcher.DrawLine(triangle8.a, triangle8.b, UnityEngine.Color.blue);
							m_GizmoBatcher.DrawLine(triangle8.b, triangle8.c, UnityEngine.Color.cyan);
							m_GizmoBatcher.DrawLine(triangle8.c, triangle8.a, UnityEngine.Color.cyan);
							m_GizmoBatcher.DrawLine(triangle9.b, triangle9.c, UnityEngine.Color.green);
							m_GizmoBatcher.DrawLine(triangle9.c, triangle9.a, UnityEngine.Color.green);
						}
						else
						{
							m_GizmoBatcher.DrawLine(triangle8.b, triangle8.c, UnityEngine.Color.yellow);
							m_GizmoBatcher.DrawLine(triangle8.c, triangle8.a, UnityEngine.Color.yellow);
							m_GizmoBatcher.DrawLine(triangle9.a, triangle9.b, UnityEngine.Color.blue);
							m_GizmoBatcher.DrawLine(triangle9.b, triangle9.c, UnityEngine.Color.cyan);
							m_GizmoBatcher.DrawLine(triangle9.c, triangle9.a, UnityEngine.Color.cyan);
						}
					}
				}
				if (blocker4.m_Blocker != Entity.Null)
				{
					DrawBlocker(blocker4.m_Blocker, (float)(int)blocker4.m_MaxSpeed * 0.003921569f);
				}
			}
			if (m_AnimalCurrentLaneType.TryGetComponent(m_Selected, out var _))
			{
				Blocker blocker5 = m_BlockerType[m_Selected];
				if (blocker5.m_Blocker != Entity.Null)
				{
					DrawBlocker(blocker5.m_Blocker, (float)(int)blocker5.m_MaxSpeed * 0.003921569f);
				}
			}
			if (tempBuffer.IsCreated)
			{
				tempBuffer.Dispose();
			}
			return;
			IL_19be:
			if (watercraftLaneSpeedIterator.m_Blocker != Entity.Null)
			{
				DrawBlocker(watercraftLaneSpeedIterator.m_Blocker, watercraftLaneSpeedIterator.m_MaxSpeed / watercraftData.m_MaxSpeed);
			}
			buffer2.Dispose();
			goto IL_19f4;
		}

		private void DrawBlocker(Entity blocker, float speedFactor)
		{
			if (!m_TransformDataFromEntity.HasComponent(blocker))
			{
				return;
			}
			Game.Objects.Transform transform = m_TransformDataFromEntity[blocker];
			PrefabRef prefabRef = m_PrefabRefDataFromEntity[blocker];
			if (m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
			{
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
				UnityEngine.Color color = UnityEngine.Color.Lerp(UnityEngine.Color.red, UnityEngine.Color.yellow, math.saturate(speedFactor));
				float4x4 trs = new float4x4(transform.m_Rotation, transform.m_Position);
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					m_GizmoBatcher.DrawWireCylinder(trs, new float3(0f, objectGeometryData.m_Size.y * 0.5f, 0f), objectGeometryData.m_Size.x * 0.5f, objectGeometryData.m_Size.y, color);
				}
				else
				{
					m_GizmoBatcher.DrawWireCube(trs, new float3(0f, objectGeometryData.m_Size.y * 0.5f, 0f), objectGeometryData.m_Size, color);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<CarNavigationLane> __Game_Vehicles_CarNavigationLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<WatercraftNavigationLane> __Game_Vehicles_WatercraftNavigationLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<AircraftNavigationLane> __Game_Vehicles_AircraftNavigationLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Train> __Game_Vehicles_Train_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TrainNavigationLane> __Game_Vehicles_TrainNavigationLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CarNavigationLane> __Game_Vehicles_CarNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CarNavigation> __Game_Vehicles_CarNavigation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<WatercraftNavigationLane> __Game_Vehicles_WatercraftNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftNavigation> __Game_Vehicles_WatercraftNavigation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<AircraftNavigationLane> __Game_Vehicles_AircraftNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<AircraftNavigation> __Game_Vehicles_AircraftNavigation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TrainNavigationLane> __Game_Vehicles_TrainNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Blocker> __Game_Vehicles_Blocker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneCondition> __Game_Net_LaneCondition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaLane> __Game_Net_AreaLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeLane> __Game_Net_NodeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Car> __Game_Vehicles_Car_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Watercraft> __Game_Vehicles_Watercraft_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Aircraft> __Game_Vehicles_Aircraft_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Creature> __Game_Creatures_Creature_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftData> __Game_Prefabs_WatercraftData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AircraftData> __Game_Prefabs_AircraftData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_CarNavigationLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<CarNavigationLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftNavigationLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<WatercraftNavigationLane>(isReadOnly: true);
			__Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_AircraftNavigationLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<AircraftNavigationLane>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Train>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainNavigationLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<TrainNavigationLane>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HumanCurrentLane>(isReadOnly: true);
			__Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalCurrentLane>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentLookup = state.GetComponentLookup<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_CarNavigationLane_RO_BufferLookup = state.GetBufferLookup<CarNavigationLane>(isReadOnly: true);
			__Game_Vehicles_CarNavigation_RO_ComponentLookup = state.GetComponentLookup<CarNavigation>(isReadOnly: true);
			__Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup = state.GetComponentLookup<WatercraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftNavigationLane_RO_BufferLookup = state.GetBufferLookup<WatercraftNavigationLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftNavigation_RO_ComponentLookup = state.GetComponentLookup<WatercraftNavigation>(isReadOnly: true);
			__Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup = state.GetComponentLookup<AircraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_AircraftNavigationLane_RO_BufferLookup = state.GetBufferLookup<AircraftNavigationLane>(isReadOnly: true);
			__Game_Vehicles_AircraftNavigation_RO_ComponentLookup = state.GetComponentLookup<AircraftNavigation>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainNavigationLane_RO_BufferLookup = state.GetBufferLookup<TrainNavigationLane>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_Blocker_RO_ComponentLookup = state.GetComponentLookup<Blocker>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>(isReadOnly: true);
			__Game_Creatures_AnimalCurrentLane_RO_ComponentLookup = state.GetComponentLookup<AnimalCurrentLane>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.TrackLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_LaneReservation_RO_ComponentLookup = state.GetComponentLookup<LaneReservation>(isReadOnly: true);
			__Game_Net_LaneCondition_RO_ComponentLookup = state.GetComponentLookup<LaneCondition>(isReadOnly: true);
			__Game_Net_LaneSignal_RO_ComponentLookup = state.GetComponentLookup<LaneSignal>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_AreaLane_RO_ComponentLookup = state.GetComponentLookup<AreaLane>(isReadOnly: true);
			__Game_Net_NodeLane_RO_ComponentLookup = state.GetComponentLookup<NodeLane>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Vehicles_Car_RO_ComponentLookup = state.GetComponentLookup<Car>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentLookup = state.GetComponentLookup<Bicycle>(isReadOnly: true);
			__Game_Vehicles_Watercraft_RO_ComponentLookup = state.GetComponentLookup<Watercraft>(isReadOnly: true);
			__Game_Vehicles_Aircraft_RO_ComponentLookup = state.GetComponentLookup<Aircraft>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentLookup = state.GetComponentLookup<Creature>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_WatercraftData_RO_ComponentLookup = state.GetComponentLookup<WatercraftData>(isReadOnly: true);
			__Game_Prefabs_AircraftData_RO_ComponentLookup = state.GetComponentLookup<AircraftData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
		}
	}

	private EntityQuery m_NavigationQuery;

	private GizmosSystem m_GizmosSystem;

	private SimulationSystem m_SimulationSystem;

	private ToolSystem m_ToolSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private Option m_HumanOption;

	private Option m_AnimalOption;

	private Option m_CarOption;

	private Option m_TrainOption;

	private Option m_WatercraftOption;

	private Option m_AircraftOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_NavigationQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Objects.Object>(),
				ComponentType.ReadOnly<Moving>()
			},
			Any = new ComponentType[6]
			{
				ComponentType.ReadOnly<CarCurrentLane>(),
				ComponentType.ReadOnly<TrainCurrentLane>(),
				ComponentType.ReadOnly<HumanCurrentLane>(),
				ComponentType.ReadOnly<WatercraftCurrentLane>(),
				ComponentType.ReadOnly<AircraftCurrentLane>(),
				ComponentType.ReadOnly<AnimalCurrentLane>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_HumanOption = AddOption("Humans", defaultEnabled: true);
		m_AnimalOption = AddOption("Animals", defaultEnabled: true);
		m_CarOption = AddOption("Cars", defaultEnabled: true);
		m_TrainOption = AddOption("Trains", defaultEnabled: true);
		m_WatercraftOption = AddOption("Ships", defaultEnabled: true);
		m_AircraftOption = AddOption("Aircrafts", defaultEnabled: true);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_NavigationQuery.IsEmptyIgnoreFilter)
		{
			if (m_ToolSystem.selected != Entity.Null)
			{
				base.Dependency = JobHandle.CombineDependencies(DrawNavigationGizmos(m_NavigationQuery, base.Dependency), DrawSelectedGizmos(base.Dependency));
			}
			else
			{
				base.Dependency = DrawNavigationGizmos(m_NavigationQuery, base.Dependency);
			}
		}
	}

	private JobHandle DrawNavigationGizmos(EntityQuery group, JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new NavigationGizmoJob
		{
			m_HumanOption = m_HumanOption.enabled,
			m_AnimalOption = m_AnimalOption.enabled,
			m_CarOption = m_CarOption.enabled,
			m_TrainOption = m_TrainOption.enabled,
			m_WatercraftOption = m_WatercraftOption.enabled,
			m_AircraftOption = m_AircraftOption.enabled,
			m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_TimeOffset = UnityEngine.Time.realtimeSinceStartup,
			m_Selected = m_ToolSystem.selected,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CarCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_CarNavigationLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_WatercraftCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WatercraftNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftNavigationLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_AircraftCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftNavigationLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TrainType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrainCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrainNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainNavigationLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_HumanCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
		}, group, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	private JobHandle DrawSelectedGizmos(JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle jobHandle = IJobExtensions.Schedule(new SelectedNavigationGizmoJob
		{
			m_Selected = m_ToolSystem.selected,
			m_CarCurrentLaneType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarNavigationLaneType = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_CarNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_CarNavigationType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarNavigation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftCurrentLaneType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftNavigationLaneType = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_WatercraftNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_WatercraftNavigationType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WatercraftNavigation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AircraftCurrentLaneType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AircraftNavigationLaneType = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_AircraftNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_AircraftNavigationType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_AircraftNavigation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainCurrentLaneType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainNavigationLaneType = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_TrainNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElementType = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_BlockerType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Blocker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HumanCurrentLaneType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AnimalCurrentLaneType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneReservationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneReservation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneCondition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneSignalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneSignal_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_AreaLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Watercraft_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AircraftDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Aircraft_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWatercraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WatercraftData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAircraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AircraftData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies),
			m_SimulationFrame = m_SimulationSystem.frameIndex
		}, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
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
	public NavigationDebugSystem()
	{
	}
}
