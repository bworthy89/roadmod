using Colossal;
using Colossal.Mathematics;
using Game.Pathfind;
using Game.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

public class PathfindDebugSystem : BaseDebugSystem
{
	private struct PathfindLine
	{
		public Entity m_Owner;

		public PathFlags m_Flags;

		public float3 m_Time;

		public Line3.Segment m_Line;

		public bool m_TooLong;

		public PathfindLine(Entity owner, PathFlags flags, float2 time, Line3.Segment line)
		{
			m_Owner = owner;
			m_Flags = flags;
			m_Time = time.xxy;
			m_Line = line;
			m_TooLong = false;
		}
	}

	[BurstCompile]
	private struct EdgeCountJob : IJob
	{
		[ReadOnly]
		public NativePathfindData m_PathfindData;

		public NativeReference<int> m_EdgeCount;

		public void Execute()
		{
			UnsafePathfindData readOnlyData = m_PathfindData.GetReadOnlyData();
			m_EdgeCount.Value = readOnlyData.m_Edges.Length;
		}
	}

	[BurstCompile]
	private struct PathfindEdgeGizmoJob : IJobParallelForDefer
	{
		[ReadOnly]
		public bool m_RestrictedOption;

		[ReadOnly]
		public bool4 m_CostOptions;

		[ReadOnly]
		public NativePathfindData m_PathfindData;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(int index)
		{
			UnsafePathfindData readOnlyData = m_PathfindData.GetReadOnlyData();
			ref Edge edge = ref readOnlyData.GetEdge(new EdgeID
			{
				m_Index = index
			});
			if (edge.m_Owner == Entity.Null)
			{
				return;
			}
			Color edgeColor = GetEdgeColor(edge.m_Specification, edge.m_Location);
			if ((edge.m_Specification.m_Flags & EdgeFlags.Secondary) != 0)
			{
				edgeColor *= 0.5f;
			}
			if (math.lengthsq(edge.m_Location.m_Line.b - edge.m_Location.m_Line.a) > 0.0001f)
			{
				switch (edge.m_Specification.m_Flags & (EdgeFlags.Forward | EdgeFlags.Backward))
				{
				case EdgeFlags.Forward:
					m_GizmoBatcher.DrawArrow(edge.m_Location.m_Line.a, edge.m_Location.m_Line.b, edgeColor, 1f);
					break;
				case EdgeFlags.Backward:
					m_GizmoBatcher.DrawArrow(edge.m_Location.m_Line.b, edge.m_Location.m_Line.a, edgeColor, 1f);
					break;
				case EdgeFlags.Forward | EdgeFlags.Backward:
					m_GizmoBatcher.DrawLine(edge.m_Location.m_Line.a, edge.m_Location.m_Line.b, edgeColor);
					break;
				default:
					edgeColor *= 0.5f;
					m_GizmoBatcher.DrawLine(edge.m_Location.m_Line.a, edge.m_Location.m_Line.b, edgeColor);
					break;
				}
			}
			if ((edge.m_Specification.m_Flags & (EdgeFlags.Forward | EdgeFlags.Backward)) != 0)
			{
				bool flag = false;
				bool flag2 = false;
				if ((edge.m_Specification.m_Flags & EdgeFlags.Forward) != 0)
				{
					int reversedConnectionCount = readOnlyData.GetReversedConnectionCount(edge.m_StartID);
					for (int i = 0; i < reversedConnectionCount; i++)
					{
						EdgeID edgeID = new EdgeID
						{
							m_Index = readOnlyData.GetReversedConnection(edge.m_StartID, i)
						};
						if (edgeID.m_Index != index)
						{
							ref Edge edge2 = ref readOnlyData.GetEdge(edgeID);
							if (edge.m_Owner != edge2.m_Owner)
							{
								flag = true;
								break;
							}
						}
					}
					int connectionCount = readOnlyData.GetConnectionCount(edge.m_EndID);
					for (int j = 0; j < connectionCount; j++)
					{
						EdgeID edgeID2 = new EdgeID
						{
							m_Index = readOnlyData.GetConnection(edge.m_EndID, j)
						};
						if (edgeID2.m_Index != index)
						{
							ref Edge edge3 = ref readOnlyData.GetEdge(edgeID2);
							if (edge.m_Owner != edge3.m_Owner)
							{
								flag2 = true;
								break;
							}
						}
					}
				}
				if ((edge.m_Specification.m_Flags & EdgeFlags.Backward) != 0)
				{
					if (!flag)
					{
						int connectionCount2 = readOnlyData.GetConnectionCount(edge.m_StartID);
						for (int k = 0; k < connectionCount2; k++)
						{
							EdgeID edgeID3 = new EdgeID
							{
								m_Index = readOnlyData.GetConnection(edge.m_StartID, k)
							};
							if (edgeID3.m_Index != index)
							{
								ref Edge edge4 = ref readOnlyData.GetEdge(edgeID3);
								if (edge.m_Owner != edge4.m_Owner)
								{
									flag = true;
									break;
								}
							}
						}
					}
					if (!flag2)
					{
						int reversedConnectionCount2 = readOnlyData.GetReversedConnectionCount(edge.m_EndID);
						for (int l = 0; l < reversedConnectionCount2; l++)
						{
							EdgeID edgeID4 = new EdgeID
							{
								m_Index = readOnlyData.GetReversedConnection(edge.m_EndID, l)
							};
							if (edgeID4.m_Index != index)
							{
								ref Edge edge5 = ref readOnlyData.GetEdge(edgeID4);
								if (edge.m_Owner != edge5.m_Owner)
								{
									flag2 = true;
									break;
								}
							}
						}
					}
				}
				if (!flag)
				{
					if ((edge.m_Specification.m_Flags & EdgeFlags.Secondary) != 0)
					{
						m_GizmoBatcher.DrawWireNode(edge.m_Location.m_Line.a, 0.5f, Color.red * 0.5f);
					}
					else
					{
						m_GizmoBatcher.DrawWireNode(edge.m_Location.m_Line.a, 0.5f, Color.red);
					}
				}
				if (!flag2)
				{
					if ((edge.m_Specification.m_Flags & EdgeFlags.Secondary) != 0)
					{
						m_GizmoBatcher.DrawWireNode(edge.m_Location.m_Line.b, 0.5f, Color.red * 0.5f);
					}
					else
					{
						m_GizmoBatcher.DrawWireNode(edge.m_Location.m_Line.b, 0.5f, Color.red);
					}
				}
			}
			if ((edge.m_Specification.m_Flags & EdgeFlags.AllowMiddle) == 0)
			{
				return;
			}
			int connectionCount3 = readOnlyData.GetConnectionCount(edge.m_MiddleID);
			for (int m = 0; m < connectionCount3; m++)
			{
				EdgeID edgeID5 = new EdgeID
				{
					m_Index = readOnlyData.GetConnection(edge.m_MiddleID, m)
				};
				if (edgeID5.m_Index == index)
				{
					continue;
				}
				ref Edge edge6 = ref readOnlyData.GetEdge(edgeID5);
				if ((edge6.m_Specification.m_Flags & EdgeFlags.Forward) != 0 && edge.m_MiddleID.Equals(edge6.m_StartID))
				{
					Color edgeColor2 = GetEdgeColor(edge6.m_Specification, edge6.m_Location);
					if ((edge6.m_Specification.m_Flags & EdgeFlags.Secondary) != 0)
					{
						edgeColor2 *= 0.5f;
					}
					float3 @float = MathUtils.Position(edge.m_Location.m_Line, edge6.m_StartCurvePos);
					m_GizmoBatcher.DrawWireNode(@float, 0.5f, edgeColor2);
					if (math.lengthsq(@float - edge6.m_Location.m_Line.a) > 0.0001f)
					{
						m_GizmoBatcher.DrawLine(edge6.m_Location.m_Line.a, @float, edgeColor2);
					}
				}
				else if ((edge6.m_Specification.m_Flags & EdgeFlags.Backward) != 0 && edge.m_MiddleID.Equals(edge6.m_EndID))
				{
					Color edgeColor3 = GetEdgeColor(edge6.m_Specification, edge6.m_Location);
					if ((edge6.m_Specification.m_Flags & EdgeFlags.Secondary) != 0)
					{
						edgeColor3 *= 0.5f;
					}
					float3 float2 = MathUtils.Position(edge.m_Location.m_Line, edge6.m_EndCurvePos);
					m_GizmoBatcher.DrawWireNode(float2, 0.5f, edgeColor3);
					if (math.lengthsq(float2 - edge6.m_Location.m_Line.b) > 0.0001f)
					{
						m_GizmoBatcher.DrawLine(edge6.m_Location.m_Line.b, float2, edgeColor3);
					}
				}
			}
			int reversedConnectionCount3 = readOnlyData.GetReversedConnectionCount(edge.m_MiddleID);
			for (int n = 0; n < reversedConnectionCount3; n++)
			{
				EdgeID edgeID6 = new EdgeID
				{
					m_Index = readOnlyData.GetReversedConnection(edge.m_MiddleID, n)
				};
				if (edgeID6.m_Index == index)
				{
					continue;
				}
				ref Edge edge7 = ref readOnlyData.GetEdge(edgeID6);
				if ((edge7.m_Specification.m_Flags & (EdgeFlags.Forward | EdgeFlags.Backward)) == EdgeFlags.Backward && edge.m_MiddleID.Equals(edge7.m_StartID))
				{
					Color edgeColor4 = GetEdgeColor(edge7.m_Specification, edge7.m_Location);
					if ((edge7.m_Specification.m_Flags & EdgeFlags.Secondary) != 0)
					{
						edgeColor4 *= 0.5f;
					}
					float3 float3 = MathUtils.Position(edge.m_Location.m_Line, edge7.m_StartCurvePos);
					m_GizmoBatcher.DrawWireNode(float3, 0.5f, edgeColor4);
					if (math.lengthsq(float3 - edge7.m_Location.m_Line.a) > 0.0001f)
					{
						m_GizmoBatcher.DrawArrow(edge7.m_Location.m_Line.a, float3, edgeColor4, 1f);
					}
				}
				else if ((edge7.m_Specification.m_Flags & (EdgeFlags.Forward | EdgeFlags.Backward)) == EdgeFlags.Forward && edge.m_MiddleID.Equals(edge7.m_EndID))
				{
					Color edgeColor5 = GetEdgeColor(edge7.m_Specification, edge7.m_Location);
					if ((edge7.m_Specification.m_Flags & EdgeFlags.Secondary) != 0)
					{
						edgeColor5 *= 0.5f;
					}
					float3 float4 = MathUtils.Position(edge.m_Location.m_Line, edge7.m_EndCurvePos);
					m_GizmoBatcher.DrawWireNode(float4, 0.5f, edgeColor5);
					if (math.lengthsq(float4 - edge7.m_Location.m_Line.b) > 0.0001f)
					{
						m_GizmoBatcher.DrawArrow(edge7.m_Location.m_Line.b, float4, edgeColor5, 1f);
					}
				}
			}
		}

		private Color GetEdgeColor(PathSpecification specification, LocationSpecification location)
		{
			if (m_RestrictedOption)
			{
				if (specification.m_AccessRequirement >= 0)
				{
					if ((specification.m_Flags & EdgeFlags.RequireAuthorization) != 0)
					{
						return Color.yellow;
					}
					if ((specification.m_Flags & EdgeFlags.AllowExit) != 0)
					{
						return Color.cyan;
					}
					if ((specification.m_Flags & EdgeFlags.AllowEnter) != 0)
					{
						return Color.green;
					}
					return Color.magenta;
				}
				if ((specification.m_Flags & EdgeFlags.RequireAuthorization) != 0)
				{
					return Color.red;
				}
				return Color.gray;
			}
			if (math.any(m_CostOptions))
			{
				float4 value = specification.m_Costs.m_Value;
				value.x += specification.m_Length / specification.m_MaxSpeed;
				float num = math.dot(specification.m_Costs.m_Value, math.select(new float4(0f), new float4(1f), m_CostOptions));
				num = ((!(specification.m_Length > 0f)) ? math.log(num + 1f) : (math.log(num / specification.m_Length + 1f) * 5f));
				if (num < 1f)
				{
					return Color.Lerp(Color.cyan, Color.green, math.saturate(num));
				}
				if (num < 2f)
				{
					return Color.Lerp(Color.green, Color.yellow, math.saturate(num - 1f));
				}
				if (num < 3f)
				{
					return Color.Lerp(Color.yellow, Color.red, math.saturate(num - 2f));
				}
				if (num < 4f)
				{
					return Color.Lerp(Color.red, Color.magenta, math.saturate(num - 3f));
				}
				return Color.Lerp(Color.magenta, Color.black, math.saturate(num - 4f));
			}
			return specification.m_Methods switch
			{
				PathMethod.Road => Color.cyan, 
				PathMethod.Road | PathMethod.Bicycle => new Color(1f, 0.75f, 1f, 1f), 
				PathMethod.MediumRoad => new Color(1f, 0.5f, 1f, 1f), 
				PathMethod.Offroad => new Color(1f, 0.5f, 0.5f, 1f), 
				PathMethod.Parking => Color.black, 
				PathMethod.Parking | PathMethod.Boarding => Color.black, 
				PathMethod.SpecialParking => Color.black, 
				PathMethod.Boarding | PathMethod.SpecialParking => Color.black, 
				PathMethod.BicycleParking => Color.black, 
				PathMethod.Boarding => new Color(0.25f, 0.25f, 0.25f, 1f), 
				PathMethod.Pedestrian => Color.green, 
				PathMethod.Bicycle => new Color(1f, 0.75f, 0.5f, 1f), 
				PathMethod.PublicTransportDay => new Color(0.5f, 0.5f, 1f, 1f), 
				PathMethod.PublicTransportNight => new Color(0f, 0f, 1f, 1f), 
				PathMethod.PublicTransportDay | PathMethod.PublicTransportNight => new Color(0.25f, 0.25f, 1f, 1f), 
				PathMethod.Track => Color.white, 
				PathMethod.Taxi => Color.yellow, 
				PathMethod.CargoTransport => new Color(0.5f, 0f, 1f, 1f), 
				PathMethod.PublicTransportDay | PathMethod.CargoTransport => new Color(0.75f, 0.5f, 1f, 1f), 
				PathMethod.CargoTransport | PathMethod.PublicTransportNight => new Color(0.75f, 0f, 1f, 1f), 
				PathMethod.PublicTransportDay | PathMethod.CargoTransport | PathMethod.PublicTransportNight => new Color(0.75f, 0.25f, 1f, 1f), 
				PathMethod.CargoLoading => new Color(0f, 0.5f, 1f, 1f), 
				PathMethod.Pedestrian | PathMethod.CargoLoading => new Color(0f, 1f, 0.5f, 1f), 
				PathMethod.Road | PathMethod.Track => new Color(0.5f, 1f, 1f, 1f), 
				PathMethod.Road | PathMethod.Track | PathMethod.Bicycle => new Color(1f, 1f, 0.75f, 1f), 
				PathMethod.Flying => new Color(1f, 0.5f, 0f, 1f), 
				_ => Color.gray, 
			};
		}
	}

	[BurstCompile]
	private struct FillPathfindGizmoLinesJob : IJob
	{
		[ReadOnly]
		public NativePathfindData m_PathfindData;

		[ReadOnly]
		public Entity m_Owner;

		[ReadOnly]
		public PathFlags m_Flags;

		[ReadOnly]
		public PathfindAction m_Action;

		public NativeList<PathfindLine> m_PathfindLines;

		public void Execute()
		{
			UnsafePathfindData readOnlyData = m_PathfindData.GetReadOnlyData();
			int length = m_Action.readOnlyData.m_StartTargets.Length;
			int length2 = m_Action.readOnlyData.m_EndTargets.Length;
			Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
			Bounds3 bounds2 = new Bounds3(float.MaxValue, float.MinValue);
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < length; i++)
			{
				PathTarget pathTarget = m_Action.readOnlyData.m_StartTargets[i];
				if (readOnlyData.m_PathEdges.TryGetValue(pathTarget.m_Entity, out var item))
				{
					float3 @float = MathUtils.Position(readOnlyData.GetEdge(item).m_Location.m_Line, pathTarget.m_Delta);
					bounds |= @float;
					num++;
				}
			}
			for (int j = 0; j < length2; j++)
			{
				PathTarget pathTarget2 = m_Action.readOnlyData.m_EndTargets[j];
				if (readOnlyData.m_PathEdges.TryGetValue(pathTarget2.m_Entity, out var item2))
				{
					float3 float2 = MathUtils.Position(readOnlyData.GetEdge(item2).m_Location.m_Line, pathTarget2.m_Delta);
					bounds2 |= float2;
					num2++;
				}
			}
			if (num == 0 || num2 == 0)
			{
				float3 float3 = MathUtils.Center(bounds);
				float3 float4 = MathUtils.Center(bounds2);
				for (int k = 0; k < length; k++)
				{
					PathTarget pathTarget3 = m_Action.readOnlyData.m_StartTargets[k];
					if (readOnlyData.m_PathEdges.TryGetValue(pathTarget3.m_Entity, out var item3))
					{
						float3 a = MathUtils.Position(readOnlyData.GetEdge(item3).m_Location.m_Line, pathTarget3.m_Delta);
						m_PathfindLines.Add(new PathfindLine(m_Owner, m_Flags, new float2(-1f, 0f), new Line3.Segment(a, float3)));
					}
				}
				for (int l = 0; l < length2; l++)
				{
					PathTarget pathTarget4 = m_Action.readOnlyData.m_EndTargets[l];
					if (readOnlyData.m_PathEdges.TryGetValue(pathTarget4.m_Entity, out var item4))
					{
						float3 b = MathUtils.Position(readOnlyData.GetEdge(item4).m_Location.m_Line, pathTarget4.m_Delta);
						m_PathfindLines.Add(new PathfindLine(m_Owner, m_Flags, new float2(-1f, 0f), new Line3.Segment(float4, b)));
					}
				}
				if (num > 1)
				{
					m_PathfindLines.Add(new PathfindLine(m_Owner, m_Flags, new float2(-1f, 0f), new Line3.Segment(float3, float3)));
				}
				if (num2 > 1)
				{
					m_PathfindLines.Add(new PathfindLine(m_Owner, m_Flags, new float2(-1f, 0f), new Line3.Segment(float4, float4)));
				}
				return;
			}
			float3 float5 = MathUtils.Center(bounds);
			float3 float6 = MathUtils.Center(bounds2);
			float num3 = math.length(MathUtils.Size(bounds));
			float num4 = math.length(MathUtils.Size(bounds2));
			float num5 = math.distance(float5, float6);
			if (num > 1 && num2 > 1)
			{
				if (num3 >= num5 && num4 >= num5)
				{
					float5 = math.lerp(float5, float6, 0.5f);
					float6 = float5;
				}
				else if (num3 >= num5)
				{
					float5 = float6;
				}
				else if (num4 >= num5)
				{
					float6 = float5;
				}
			}
			else if (num > 1)
			{
				if (num3 >= num5)
				{
					float5 = float6;
				}
			}
			else if (num2 > 1 && num4 >= num5)
			{
				float6 = float5;
			}
			float2 time = new float2(0f, 0f);
			if (num > 1)
			{
				time.x -= 1f;
			}
			if (!float5.Equals(float6) || (num == 1 && num2 == 1))
			{
				time.x -= 1f;
			}
			if (num2 > 1)
			{
				time.x -= 1f;
			}
			if (num > 1)
			{
				for (int m = 0; m < length; m++)
				{
					PathTarget pathTarget5 = m_Action.readOnlyData.m_StartTargets[m];
					if (readOnlyData.m_PathEdges.TryGetValue(pathTarget5.m_Entity, out var item5))
					{
						float3 a2 = MathUtils.Position(readOnlyData.GetEdge(item5).m_Location.m_Line, pathTarget5.m_Delta);
						m_PathfindLines.Add(new PathfindLine(m_Owner, m_Flags, time, new Line3.Segment(a2, float5)));
					}
				}
				time.y -= 1f;
			}
			if (!float5.Equals(float6) || (num == 1 && num2 == 1))
			{
				m_PathfindLines.Add(new PathfindLine(m_Owner, m_Flags, time, new Line3.Segment(float5, float6)));
				time.y -= 1f;
			}
			if (num2 <= 1)
			{
				return;
			}
			for (int n = 0; n < length2; n++)
			{
				PathTarget pathTarget6 = m_Action.readOnlyData.m_EndTargets[n];
				if (readOnlyData.m_PathEdges.TryGetValue(pathTarget6.m_Entity, out var item6))
				{
					float3 b2 = MathUtils.Position(readOnlyData.GetEdge(item6).m_Location.m_Line, pathTarget6.m_Delta);
					m_PathfindLines.Add(new PathfindLine(m_Owner, m_Flags, time, new Line3.Segment(float6, b2)));
				}
			}
		}
	}

	[BurstCompile]
	private struct SetPathfindGizmoLineFlagsJob : IJob
	{
		[ReadOnly]
		public Entity m_Owner;

		[ReadOnly]
		public PathFlags m_Flags;

		[ReadOnly]
		public bool m_TooLong;

		public NativeList<PathfindLine> m_PathfindLines;

		public void Execute()
		{
			for (int i = 0; i < m_PathfindLines.Length; i++)
			{
				PathfindLine value = m_PathfindLines[i];
				if ((value.m_Flags & PathFlags.Pending) != 0 && value.m_Owner == m_Owner)
				{
					value.m_Flags = m_Flags;
					value.m_TooLong = m_TooLong;
					m_PathfindLines[i] = value;
				}
			}
		}
	}

	[BurstCompile]
	private struct PathfindLineGizmoJob : IJob
	{
		[ReadOnly]
		public float m_DeltaTime;

		public NativeList<PathfindLine> m_PathfindLines;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute()
		{
			int num = 0;
			int num2 = 0;
			while (num < m_PathfindLines.Length)
			{
				PathfindLine value = m_PathfindLines[num++];
				value.m_Time.yz += m_DeltaTime;
				if (!(value.m_Time.y >= value.m_Time.x) || !(value.m_Time.y <= 0f))
				{
					continue;
				}
				Color color = (((value.m_Flags & PathFlags.Failed) != 0) ? (value.m_TooLong ? Color.yellow : Color.red) : (((value.m_Flags & PathFlags.Pending) == 0) ? Color.green : Color.gray));
				if (math.distancesq(value.m_Line.a, value.m_Line.b) > 1E-06f)
				{
					m_GizmoBatcher.DrawLine(value.m_Line.a, value.m_Line.b, color);
					if (value.m_Time.z > 0f && value.m_Time.z <= 1f)
					{
						float3 pos = MathUtils.Position(value.m_Line, value.m_Time.z);
						m_GizmoBatcher.DrawArrowHead(pos, value.m_Line.b - value.m_Line.a, color, 10f);
					}
				}
				else
				{
					m_GizmoBatcher.DrawWireNode(value.m_Line.a, 5f, color);
				}
				m_PathfindLines[num2++] = value;
			}
			if (num2 < m_PathfindLines.Length)
			{
				m_PathfindLines.RemoveRange(num2, m_PathfindLines.Length - num2);
			}
		}
	}

	private PathfindQueueSystem m_PathfindQueueSystem;

	private GizmosSystem m_GizmosSystem;

	private RenderingSystem m_RenderingSystem;

	private NativeList<PathfindLine> m_PathfindLines;

	private Option m_GraphOption;

	private Option m_RestrictedOption;

	private Option m_TimeCostOption;

	private Option m_BehaviorCostOption;

	private Option m_MoneyCostOption;

	private Option m_ComfortCostOption;

	private Option m_PathfindOption;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_PathfindLines = new NativeList<PathfindLine>(Allocator.Persistent);
		m_GraphOption = AddOption("Draw Graph", defaultEnabled: true);
		m_RestrictedOption = AddOption("Show Restrictions", defaultEnabled: false);
		m_TimeCostOption = AddOption("Show time cost", defaultEnabled: false);
		m_BehaviorCostOption = AddOption("Show behavior cost", defaultEnabled: false);
		m_MoneyCostOption = AddOption("Show money cost", defaultEnabled: false);
		m_ComfortCostOption = AddOption("Show comfort cost", defaultEnabled: false);
		m_PathfindOption = AddOption("Visualize Queries", defaultEnabled: true);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_PathfindLines.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected unsafe override JobHandle OnUpdate(JobHandle inputDeps)
	{
		if (!m_GraphOption.enabled && !m_PathfindOption.enabled)
		{
			return inputDeps;
		}
		JobHandle jobHandle = inputDeps;
		JobHandle dependencies;
		NativePathfindData dataContainer = m_PathfindQueueSystem.GetDataContainer(out dependencies);
		inputDeps = JobHandle.CombineDependencies(inputDeps, dependencies);
		if (m_GraphOption.enabled)
		{
			NativeReference<int> nativeReference = new NativeReference<int>(Allocator.TempJob);
			EdgeCountJob jobData = new EdgeCountJob
			{
				m_PathfindData = dataContainer,
				m_EdgeCount = nativeReference
			};
			JobHandle dependencies2;
			JobHandle jobHandle2 = new PathfindEdgeGizmoJob
			{
				m_RestrictedOption = m_RestrictedOption.enabled,
				m_CostOptions = new bool4(m_TimeCostOption.enabled, m_BehaviorCostOption.enabled, m_MoneyCostOption.enabled, m_ComfortCostOption.enabled),
				m_PathfindData = dataContainer,
				m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies2)
			}.Schedule(dependsOn: JobHandle.CombineDependencies(IJobExtensions.Schedule(jobData, inputDeps), dependencies2), forEachCount: nativeReference.GetUnsafePtrWithoutChecks(), innerloopBatchCount: 64);
			nativeReference.Dispose(jobHandle2);
			m_GizmosSystem.AddGizmosBatcherWriter(jobHandle2);
			m_PathfindQueueSystem.AddDataReader(jobHandle2);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
		}
		if (m_PathfindOption.enabled)
		{
			m_PathfindQueueSystem.RequireDebug();
			PathfindQueueSystem.ActionList<PathfindAction> pathfindActions = m_PathfindQueueSystem.GetPathfindActions();
			JobHandle jobHandle3 = inputDeps;
			for (int i = 0; i < pathfindActions.m_Items.Count; i++)
			{
				PathfindQueueSystem.ActionListItem<PathfindAction> value = pathfindActions.m_Items[i];
				if ((value.m_Flags & PathFlags.Debug) == 0)
				{
					JobHandle jobHandle4 = IJobExtensions.Schedule(new FillPathfindGizmoLinesJob
					{
						m_Action = value.m_Action,
						m_Owner = value.m_Owner,
						m_Flags = PathFlags.Pending,
						m_PathfindLines = m_PathfindLines,
						m_PathfindData = dataContainer
					}, JobHandle.CombineDependencies(jobHandle3, value.m_Dependencies));
					m_PathfindQueueSystem.AddDataReader(jobHandle4);
					jobHandle3 = jobHandle4;
					value.m_Dependencies = jobHandle4;
					value.m_Flags |= PathFlags.Debug;
					pathfindActions.m_Items[i] = value;
				}
				if ((value.m_Flags & (PathFlags.Pending | PathFlags.Scheduled)) == 0)
				{
					PathFlags pathFlags = (PathFlags)0;
					bool tooLong = false;
					if (value.m_Action.readOnlyData.m_Result[0].m_Distance < 0f)
					{
						pathFlags |= PathFlags.Failed;
						tooLong = value.m_Action.readOnlyData.m_Result[0].m_TotalCost > 0f;
					}
					jobHandle3 = IJobExtensions.Schedule(new SetPathfindGizmoLineFlagsJob
					{
						m_Owner = value.m_Owner,
						m_Flags = pathFlags,
						m_TooLong = tooLong,
						m_PathfindLines = m_PathfindLines
					}, jobHandle3);
				}
			}
			JobHandle dependencies3;
			JobHandle jobHandle5 = IJobExtensions.Schedule(new PathfindLineGizmoJob
			{
				m_DeltaTime = m_RenderingSystem.frameDelta / 60f,
				m_PathfindLines = m_PathfindLines,
				m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies3)
			}, JobHandle.CombineDependencies(jobHandle3, dependencies3));
			m_GizmosSystem.AddGizmosBatcherWriter(jobHandle5);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle5);
		}
		return jobHandle;
	}

	[Preserve]
	public PathfindDebugSystem()
	{
	}
}
