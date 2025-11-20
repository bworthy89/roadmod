using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Rendering;
using Game.Tools;
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
public class LaneDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct LaneGizmoJob : IJobChunk
	{
		[ReadOnly]
		public bool m_StandaloneOption;

		[ReadOnly]
		public bool m_SlaveOption;

		[ReadOnly]
		public bool m_MasterOption;

		[ReadOnly]
		public bool m_ConnectionOption;

		[ReadOnly]
		public bool m_OverlapOption;

		[ReadOnly]
		public bool m_ReservedOption;

		[ReadOnly]
		public bool m_BlockageOption;

		[ReadOnly]
		public bool m_ConditionOption;

		[ReadOnly]
		public bool m_SignalsOption;

		[ReadOnly]
		public bool m_PriorityOption;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<CarLane> m_CarLaneType;

		[ReadOnly]
		public ComponentTypeHandle<TrackLane> m_TrackLaneType;

		[ReadOnly]
		public ComponentTypeHandle<ParkingLane> m_ParkingLaneType;

		[ReadOnly]
		public ComponentTypeHandle<PedestrianLane> m_PedestrianLaneType;

		[ReadOnly]
		public ComponentTypeHandle<ConnectionLane> m_ConnectionLaneType;

		[ReadOnly]
		public ComponentTypeHandle<MasterLane> m_MasterLaneType;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> m_SlaveLaneType;

		[ReadOnly]
		public ComponentTypeHandle<LaneReservation> m_LaneReservationType;

		[ReadOnly]
		public ComponentTypeHandle<LaneCondition> m_LaneConditionType;

		[ReadOnly]
		public ComponentTypeHandle<LaneSignal> m_LaneSignalType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<LaneObject> m_LaneObjectType;

		[ReadOnly]
		public BufferTypeHandle<LaneOverlap> m_LaneOverlapType;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Curve> nativeArray = chunk.GetNativeArray(ref m_CurveType);
			BufferAccessor<LaneObject> bufferAccessor = chunk.GetBufferAccessor(ref m_LaneObjectType);
			BufferAccessor<LaneOverlap> bufferAccessor2 = chunk.GetBufferAccessor(ref m_LaneOverlapType);
			if (chunk.Has(ref m_TempType))
			{
				if (chunk.Has(ref m_CarLaneType))
				{
					bool flag = chunk.Has(ref m_MasterLaneType);
					bool flag2 = chunk.Has(ref m_SlaveLaneType);
					if ((flag && m_MasterOption) || (flag2 && m_SlaveOption) || (!flag && !flag2 && m_StandaloneOption))
					{
						NativeArray<CarLane> nativeArray2 = chunk.GetNativeArray(ref m_CarLaneType);
						for (int i = 0; i < nativeArray.Length; i++)
						{
							CarLane carLane = nativeArray2[i];
							Curve curve = nativeArray[i];
							if ((carLane.m_Flags & CarLaneFlags.Twoway) != 0 || curve.m_Length <= 0.1f)
							{
								m_GizmoBatcher.DrawCurve(curve, Color.blue);
							}
							else
							{
								m_GizmoBatcher.DrawFlowCurve(curve, Color.blue, curve.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
						}
					}
				}
				else if (chunk.Has(ref m_TrackLaneType))
				{
					if (m_StandaloneOption)
					{
						NativeArray<TrackLane> nativeArray3 = chunk.GetNativeArray(ref m_TrackLaneType);
						for (int j = 0; j < nativeArray3.Length; j++)
						{
							TrackLane trackLane = nativeArray3[j];
							Curve curve2 = nativeArray[j];
							if ((trackLane.m_Flags & TrackLaneFlags.Twoway) != 0 || curve2.m_Length <= 0.1f)
							{
								m_GizmoBatcher.DrawCurve(curve2, Color.blue);
							}
							else
							{
								m_GizmoBatcher.DrawFlowCurve(curve2, Color.blue, curve2.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
						}
					}
				}
				else if (chunk.Has(ref m_ParkingLaneType))
				{
					if (m_StandaloneOption)
					{
						NativeArray<ParkingLane> nativeArray4 = chunk.GetNativeArray(ref m_ParkingLaneType);
						for (int k = 0; k < nativeArray4.Length; k++)
						{
							ParkingLane parkingLane = nativeArray4[k];
							Curve curve3 = nativeArray[k];
							if ((parkingLane.m_Flags & ParkingLaneFlags.AdditionalStart) != 0 || curve3.m_Length <= 0.1f)
							{
								m_GizmoBatcher.DrawCurve(curve3, Color.blue);
							}
							else
							{
								m_GizmoBatcher.DrawFlowCurve(curve3, Color.blue, curve3.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
						}
					}
				}
				else if (m_StandaloneOption)
				{
					for (int l = 0; l < nativeArray.Length; l++)
					{
						m_GizmoBatcher.DrawCurve(nativeArray[l], Color.blue);
					}
				}
			}
			else if (chunk.Has(ref m_CarLaneType))
			{
				NativeArray<CarLane> nativeArray5 = chunk.GetNativeArray(ref m_CarLaneType);
				NativeArray<LaneSignal> nativeArray6 = chunk.GetNativeArray(ref m_LaneSignalType);
				Color red = Color.red;
				Color yellow = Color.yellow;
				Color color = Color.cyan;
				if (chunk.Has(ref m_TrackLaneType))
				{
					color = new Color(0.5f, 1f, 1f, 1f);
				}
				if (chunk.Has(ref m_MasterLaneType))
				{
					if (m_MasterOption)
					{
						red *= 0.5f;
						yellow *= 0.5f;
						color *= 0.5f;
						for (int m = 0; m < nativeArray5.Length; m++)
						{
							Curve curve4 = nativeArray[m];
							CarLane carLane2 = nativeArray5[m];
							if ((carLane2.m_Flags & CarLaneFlags.Twoway) != 0 || curve4.m_Length <= 0.1f)
							{
								if (m_SignalsOption && nativeArray6.Length != 0)
								{
									m_GizmoBatcher.DrawCurve(curve4, GetSignalColor(nativeArray6[m]) * 0.5f);
								}
								else if (m_PriorityOption)
								{
									m_GizmoBatcher.DrawCurve(curve4, GetPriorityColor(carLane2) * 0.5f);
								}
								else if ((carLane2.m_Flags & CarLaneFlags.Forbidden) != 0)
								{
									m_GizmoBatcher.DrawCurve(curve4, red);
								}
								else if ((carLane2.m_Flags & CarLaneFlags.Unsafe) != 0)
								{
									m_GizmoBatcher.DrawCurve(curve4, yellow);
								}
								else
								{
									m_GizmoBatcher.DrawCurve(curve4, color);
								}
							}
							else if (m_SignalsOption && nativeArray6.Length != 0)
							{
								m_GizmoBatcher.DrawFlowCurve(curve4, GetSignalColor(nativeArray6[m]) * 0.5f, curve4.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else if (m_PriorityOption)
							{
								m_GizmoBatcher.DrawFlowCurve(curve4, GetPriorityColor(carLane2) * 0.5f, curve4.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else if ((carLane2.m_Flags & CarLaneFlags.Forbidden) != 0)
							{
								m_GizmoBatcher.DrawFlowCurve(curve4, red, curve4.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else if ((carLane2.m_Flags & CarLaneFlags.Unsafe) != 0)
							{
								m_GizmoBatcher.DrawFlowCurve(curve4, yellow, curve4.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else
							{
								m_GizmoBatcher.DrawFlowCurve(curve4, color, curve4.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							if (m_BlockageOption && carLane2.m_BlockageEnd >= carLane2.m_BlockageStart)
							{
								Bounds1 blockageBounds = carLane2.blockageBounds;
								Bezier4x3 bezier4x = MathUtils.Cut(curve4.m_Bezier, blockageBounds);
								float length = curve4.m_Length * MathUtils.Size(blockageBounds);
								float3 @float = new float3(0f, 1f, 0f);
								Color color2 = Color.red * 0.5f;
								m_GizmoBatcher.DrawLine(bezier4x.a, bezier4x.a + @float, color2);
								m_GizmoBatcher.DrawLine(bezier4x.d, bezier4x.d + @float, color2);
								m_GizmoBatcher.DrawCurve(bezier4x + @float, length, color2);
							}
						}
					}
				}
				else
				{
					bool flag3 = chunk.Has(ref m_SlaveLaneType);
					if ((flag3 && m_SlaveOption) || (!flag3 && m_StandaloneOption))
					{
						NativeArray<LaneReservation> nativeArray7 = chunk.GetNativeArray(ref m_LaneReservationType);
						NativeArray<LaneCondition> nativeArray8 = chunk.GetNativeArray(ref m_LaneConditionType);
						for (int n = 0; n < nativeArray5.Length; n++)
						{
							Curve curve5 = nativeArray[n];
							CarLane carLane3 = nativeArray5[n];
							DynamicBuffer<LaneObject> dynamicBuffer = bufferAccessor[n];
							LaneReservation laneReservation = nativeArray7[n];
							float offset = laneReservation.GetOffset();
							int priority = laneReservation.GetPriority();
							if ((carLane3.m_Flags & CarLaneFlags.Twoway) != 0 || curve5.m_Length <= 0.1f)
							{
								if (m_SignalsOption && nativeArray6.Length != 0)
								{
									m_GizmoBatcher.DrawCurve(curve5, GetSignalColor(nativeArray6[n]));
								}
								else if (m_PriorityOption)
								{
									m_GizmoBatcher.DrawCurve(curve5, GetPriorityColor(carLane3));
								}
								else if (m_ConditionOption && nativeArray8.Length != 0)
								{
									LaneCondition laneCondition = nativeArray8[n];
									float4 vector = RenderingUtils.Lerp(new float4(0f, 1f, 0f, 1f), new float4(1f, 1f, 0f, 1f), new float4(1f, 0f, 0f, 1f), math.saturate(laneCondition.m_Wear / 10f));
									m_GizmoBatcher.DrawCurve(curve5, RenderingUtils.ToColor(vector));
								}
								else if (dynamicBuffer.Length != 0 && m_ReservedOption)
								{
									m_GizmoBatcher.DrawCurve(curve5, Color.magenta);
								}
								else if (offset > 0f && m_ReservedOption)
								{
									m_GizmoBatcher.DrawCurve(curve5, new Color(0.5f, 0f, 1f, 1f));
								}
								else if (priority != 0 && m_ReservedOption)
								{
									m_GizmoBatcher.DrawCurve(curve5, new Color(0f, 0.5f, 1f, 1f));
								}
								else if ((carLane3.m_Flags & CarLaneFlags.Forbidden) != 0)
								{
									m_GizmoBatcher.DrawCurve(curve5, red);
								}
								else if ((carLane3.m_Flags & CarLaneFlags.Unsafe) != 0)
								{
									m_GizmoBatcher.DrawCurve(curve5, yellow);
								}
								else
								{
									m_GizmoBatcher.DrawCurve(curve5, color);
								}
							}
							else if (m_SignalsOption && nativeArray6.Length != 0)
							{
								m_GizmoBatcher.DrawFlowCurve(curve5, GetSignalColor(nativeArray6[n]), curve5.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else if (m_PriorityOption)
							{
								m_GizmoBatcher.DrawFlowCurve(curve5, GetPriorityColor(carLane3), curve5.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else if (m_ConditionOption && nativeArray8.Length != 0)
							{
								LaneCondition laneCondition2 = nativeArray8[n];
								float4 vector2 = RenderingUtils.Lerp(new float4(0f, 1f, 0f, 1f), new float4(1f, 1f, 0f, 1f), new float4(1f, 0f, 0f, 1f), math.saturate(laneCondition2.m_Wear / 10f));
								m_GizmoBatcher.DrawFlowCurve(curve5, RenderingUtils.ToColor(vector2), curve5.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else if (dynamicBuffer.Length != 0 && m_ReservedOption)
							{
								m_GizmoBatcher.DrawFlowCurve(curve5, Color.magenta, curve5.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else if (offset > 0f && m_ReservedOption)
							{
								if (offset == 1f)
								{
									m_GizmoBatcher.DrawFlowCurve(curve5, new Color(0.5f, 0f, 1f, 1f), curve5.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
								}
								else
								{
									MathUtils.Divide(curve5.m_Bezier, out var output, out var output2, offset);
									float2 float2 = curve5.m_Length * new float2(offset, 1f - offset);
									float3 pos = MathUtils.Position(curve5.m_Bezier, 0.5f);
									float3 dir = math.normalize(MathUtils.Tangent(curve5.m_Bezier, 0.5f));
									m_GizmoBatcher.DrawCurve(output, float2.x, new Color(0.5f, 0f, 1f, 1f));
									if ((carLane3.m_Flags & CarLaneFlags.Forbidden) != 0)
									{
										m_GizmoBatcher.DrawCurve(output2, float2.y, red);
										m_GizmoBatcher.DrawArrowHead(pos, dir, red, 1f);
									}
									else if ((carLane3.m_Flags & CarLaneFlags.Unsafe) != 0)
									{
										m_GizmoBatcher.DrawCurve(output2, float2.y, yellow);
										m_GizmoBatcher.DrawArrowHead(pos, dir, yellow, 1f);
									}
									else
									{
										m_GizmoBatcher.DrawCurve(output2, float2.y, color);
										m_GizmoBatcher.DrawArrowHead(pos, dir, color, 1f);
									}
								}
							}
							else if (priority != 0 && m_ReservedOption)
							{
								m_GizmoBatcher.DrawFlowCurve(curve5, new Color(0f, 0.5f, 1f, 1f), curve5.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else if ((carLane3.m_Flags & CarLaneFlags.Forbidden) != 0)
							{
								m_GizmoBatcher.DrawFlowCurve(curve5, red, curve5.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else if ((carLane3.m_Flags & CarLaneFlags.Unsafe) != 0)
							{
								m_GizmoBatcher.DrawFlowCurve(curve5, yellow, curve5.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							else
							{
								m_GizmoBatcher.DrawFlowCurve(curve5, color, curve5.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
							}
							if (m_BlockageOption && carLane3.m_BlockageEnd >= carLane3.m_BlockageStart)
							{
								Bounds1 blockageBounds2 = carLane3.blockageBounds;
								Bezier4x3 bezier4x2 = MathUtils.Cut(curve5.m_Bezier, blockageBounds2);
								float length2 = curve5.m_Length * MathUtils.Size(blockageBounds2);
								float3 float3 = new float3(0f, 1f, 0f);
								m_GizmoBatcher.DrawLine(bezier4x2.a, bezier4x2.a + float3, Color.red);
								m_GizmoBatcher.DrawLine(bezier4x2.d, bezier4x2.d + float3, Color.red);
								m_GizmoBatcher.DrawCurve(bezier4x2 + float3, length2, Color.red);
							}
						}
					}
				}
			}
			else if (chunk.Has(ref m_TrackLaneType))
			{
				if (m_StandaloneOption)
				{
					NativeArray<TrackLane> nativeArray9 = chunk.GetNativeArray(ref m_TrackLaneType);
					NativeArray<LaneReservation> nativeArray10 = chunk.GetNativeArray(ref m_LaneReservationType);
					NativeArray<LaneSignal> nativeArray11 = chunk.GetNativeArray(ref m_LaneSignalType);
					for (int num = 0; num < nativeArray9.Length; num++)
					{
						TrackLane trackLane2 = nativeArray9[num];
						Curve curve6 = nativeArray[num];
						DynamicBuffer<LaneObject> dynamicBuffer2 = bufferAccessor[num];
						int priority2 = nativeArray10[num].GetPriority();
						if ((trackLane2.m_Flags & TrackLaneFlags.Twoway) != 0 || curve6.m_Length <= 0.1f)
						{
							if (m_SignalsOption && nativeArray11.Length != 0)
							{
								m_GizmoBatcher.DrawCurve(curve6, GetSignalColor(nativeArray11[num]));
							}
							else if (dynamicBuffer2.Length != 0 && m_ReservedOption)
							{
								m_GizmoBatcher.DrawCurve(curve6, Color.magenta);
							}
							else if (priority2 != 0 && m_ReservedOption)
							{
								m_GizmoBatcher.DrawCurve(curve6, new Color(0.5f, 0f, 1f, 1f));
							}
							else
							{
								m_GizmoBatcher.DrawCurve(curve6, Color.white);
							}
						}
						else if (m_SignalsOption && nativeArray11.Length != 0)
						{
							m_GizmoBatcher.DrawFlowCurve(curve6, GetSignalColor(nativeArray11[num]), curve6.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
						}
						else if (dynamicBuffer2.Length != 0 && m_ReservedOption)
						{
							m_GizmoBatcher.DrawFlowCurve(curve6, Color.magenta, curve6.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
						}
						else if (priority2 != 0 && m_ReservedOption)
						{
							m_GizmoBatcher.DrawFlowCurve(curve6, new Color(0.5f, 0f, 1f, 1f), curve6.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
						}
						else
						{
							m_GizmoBatcher.DrawFlowCurve(curve6, Color.white, curve6.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
						}
					}
				}
			}
			else if (chunk.Has(ref m_PedestrianLaneType))
			{
				if (m_StandaloneOption)
				{
					NativeArray<PedestrianLane> nativeArray12 = chunk.GetNativeArray(ref m_PedestrianLaneType);
					NativeArray<LaneSignal> nativeArray13 = chunk.GetNativeArray(ref m_LaneSignalType);
					for (int num2 = 0; num2 < nativeArray12.Length; num2++)
					{
						PedestrianLane pedestrianLane = nativeArray12[num2];
						DynamicBuffer<LaneObject> dynamicBuffer3 = bufferAccessor[num2];
						if (m_SignalsOption && nativeArray13.Length != 0)
						{
							m_GizmoBatcher.DrawCurve(nativeArray[num2], GetSignalColor(nativeArray13[num2]));
						}
						else if (dynamicBuffer3.Length != 0 && m_ReservedOption)
						{
							m_GizmoBatcher.DrawCurve(nativeArray[num2], Color.magenta);
						}
						else if ((pedestrianLane.m_Flags & PedestrianLaneFlags.Unsafe) != 0)
						{
							m_GizmoBatcher.DrawCurve(nativeArray[num2], Color.yellow);
						}
						else
						{
							m_GizmoBatcher.DrawCurve(nativeArray[num2], Color.green);
						}
					}
				}
			}
			else if (chunk.Has(ref m_ParkingLaneType))
			{
				if (m_StandaloneOption)
				{
					NativeArray<ParkingLane> nativeArray14 = chunk.GetNativeArray(ref m_ParkingLaneType);
					for (int num3 = 0; num3 < nativeArray14.Length; num3++)
					{
						ParkingLane parkingLane2 = nativeArray14[num3];
						Curve curve7 = nativeArray[num3];
						DynamicBuffer<LaneObject> dynamicBuffer4 = bufferAccessor[num3];
						if ((parkingLane2.m_Flags & ParkingLaneFlags.AdditionalStart) != 0 || curve7.m_Length <= 0.1f)
						{
							if ((parkingLane2.m_Flags & ParkingLaneFlags.VirtualLane) != 0)
							{
								m_GizmoBatcher.DrawCurve(curve7, Color.black * 0.5f);
							}
							else if (dynamicBuffer4.Length != 0 && m_ReservedOption)
							{
								m_GizmoBatcher.DrawCurve(curve7, Color.magenta);
							}
							else
							{
								m_GizmoBatcher.DrawCurve(curve7, Color.black);
							}
						}
						else if ((parkingLane2.m_Flags & ParkingLaneFlags.VirtualLane) != 0)
						{
							m_GizmoBatcher.DrawFlowCurve(curve7, Color.black * 0.5f, curve7.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
						}
						else if (dynamicBuffer4.Length != 0 && m_ReservedOption)
						{
							m_GizmoBatcher.DrawFlowCurve(curve7, Color.magenta, curve7.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
						}
						else
						{
							m_GizmoBatcher.DrawFlowCurve(curve7, Color.black, curve7.m_Length * 0.5f + 0.5f, reverse: false, 1, -1, 1f);
						}
					}
				}
			}
			else if (chunk.Has(ref m_ConnectionLaneType))
			{
				if (m_ConnectionOption)
				{
					for (int num4 = 0; num4 < nativeArray.Length; num4++)
					{
						m_GizmoBatcher.DrawCurve(nativeArray[num4], new Color(1f, 0f, 0.5f, 0.5f));
					}
				}
			}
			else if (m_StandaloneOption)
			{
				for (int num5 = 0; num5 < nativeArray.Length; num5++)
				{
					m_GizmoBatcher.DrawCurve(nativeArray[num5], Color.gray);
				}
			}
			if (!m_OverlapOption || !chunk.Has(ref m_LaneOverlapType))
			{
				return;
			}
			for (int num6 = 0; num6 < bufferAccessor2.Length; num6++)
			{
				DynamicBuffer<LaneOverlap> dynamicBuffer5 = bufferAccessor2[num6];
				if (dynamicBuffer5.Length == 0)
				{
					continue;
				}
				Curve curve8 = nativeArray[num6];
				for (int num7 = 0; num7 < dynamicBuffer5.Length; num7++)
				{
					LaneOverlap laneOverlap = dynamicBuffer5[num7];
					float3 float4 = new float3(0f, 0.2f + (float)num7 * 0.2f, 0f);
					float2 float5 = new float2((int)laneOverlap.m_ThisStart, (int)laneOverlap.m_ThisEnd) * 0.003921569f;
					float num8 = (float)(int)laneOverlap.m_Parallelism * (1f / 128f);
					Color color3 = ((!(num8 < 1f)) ? Color.Lerp(Color.yellow, Color.green, num8 - 1f) : Color.Lerp(Color.red, Color.yellow, num8));
					if (float5.y != float5.x)
					{
						Bezier4x3 bezier4x3 = MathUtils.Cut(curve8.m_Bezier, float5.xy);
						Line3.Segment segment = MathUtils.Line(bezier4x3);
						bezier4x3 += float4;
						m_GizmoBatcher.DrawLine(segment.a, bezier4x3.a, color3);
						m_GizmoBatcher.DrawLine(segment.b, bezier4x3.d, color3);
						m_GizmoBatcher.DrawCurve(bezier4x3, curve8.m_Length * math.abs(float5.y - float5.x), color3);
					}
					else
					{
						float3 float6 = MathUtils.Position(curve8.m_Bezier, float5.x);
						m_GizmoBatcher.DrawLine(float6, float6 + float4, color3);
					}
				}
			}
		}

		private Color GetSignalColor(LaneSignal laneSignal)
		{
			return laneSignal.m_Signal switch
			{
				LaneSignalType.Stop => Color.red, 
				LaneSignalType.SafeStop => Color.yellow, 
				LaneSignalType.Yield => Color.magenta, 
				LaneSignalType.Go => Color.green, 
				_ => Color.black, 
			};
		}

		private Color GetPriorityColor(CarLane carLane)
		{
			if ((carLane.m_Flags & CarLaneFlags.Stop) != 0)
			{
				return Color.red;
			}
			if ((carLane.m_Flags & CarLaneFlags.Yield) != 0)
			{
				return Color.magenta;
			}
			if ((carLane.m_Flags & CarLaneFlags.RightOfWay) != 0)
			{
				return Color.green;
			}
			if ((carLane.m_Flags & CarLaneFlags.Unsafe) != 0)
			{
				return Color.yellow;
			}
			return Color.cyan;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarLane> __Game_Net_CarLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrackLane> __Game_Net_TrackLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkingLane> __Game_Net_ParkingLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MasterLane> __Game_Net_MasterLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> __Game_Net_SlaveLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LaneReservation> __Game_Net_LaneReservation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LaneCondition> __Game_Net_LaneCondition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LaneSignal> __Game_Net_LaneSignal_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LaneObject> __Game_Net_LaneObject_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrackLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkingLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PedestrianLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ConnectionLane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MasterLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SlaveLane>(isReadOnly: true);
			__Game_Net_LaneReservation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LaneReservation>(isReadOnly: true);
			__Game_Net_LaneCondition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LaneCondition>(isReadOnly: true);
			__Game_Net_LaneSignal_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LaneSignal>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferTypeHandle = state.GetBufferTypeHandle<LaneOverlap>(isReadOnly: true);
		}
	}

	private EntityQuery m_LaneQuery;

	private GizmosSystem m_GizmosSystem;

	private Option m_StandaloneOption;

	private Option m_SlaveOption;

	private Option m_MasterOption;

	private Option m_ConnectionOption;

	private Option m_OverlapOption;

	private Option m_ReservedOption;

	private Option m_BlockageOption;

	private Option m_ConditionOption;

	private Option m_SignalsOption;

	private Option m_PriorityOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_LaneQuery = GetEntityQuery(ComponentType.ReadOnly<Lane>(), ComponentType.ReadOnly<Curve>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Hidden>());
		RequireForUpdate(m_LaneQuery);
		m_StandaloneOption = AddOption("Standalone Lanes", defaultEnabled: true);
		m_SlaveOption = AddOption("Slave Lanes", defaultEnabled: true);
		m_MasterOption = AddOption("Master Lanes", defaultEnabled: false);
		m_ConnectionOption = AddOption("Connection Lanes", defaultEnabled: false);
		m_OverlapOption = AddOption("Draw Overlaps", defaultEnabled: false);
		m_ReservedOption = AddOption("Draw Reserved", defaultEnabled: true);
		m_BlockageOption = AddOption("Draw Blocked", defaultEnabled: true);
		m_ConditionOption = AddOption("Draw Condition", defaultEnabled: false);
		m_SignalsOption = AddOption("Draw Signals", defaultEnabled: false);
		m_PriorityOption = AddOption("Draw Priorities", defaultEnabled: false);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new LaneGizmoJob
		{
			m_StandaloneOption = m_StandaloneOption.enabled,
			m_SlaveOption = m_SlaveOption.enabled,
			m_MasterOption = m_MasterOption.enabled,
			m_ConnectionOption = m_ConnectionOption.enabled,
			m_OverlapOption = m_OverlapOption.enabled,
			m_ReservedOption = m_ReservedOption.enabled,
			m_BlockageOption = m_BlockageOption.enabled,
			m_ConditionOption = m_ConditionOption.enabled,
			m_SignalsOption = m_SignalsOption.enabled,
			m_PriorityOption = m_PriorityOption.enabled,
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrackLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkingLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PedestrianLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectionLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MasterLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SlaveLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LaneReservationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneReservation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LaneConditionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneCondition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LaneSignalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneSignal_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LaneObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LaneOverlapType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
		}, m_LaneQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		base.Dependency = jobHandle;
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
	public LaneDebugSystem()
	{
	}
}
