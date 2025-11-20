using System;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Pathfind;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public static class AirwayHelpers
{
	public struct AirwayMap : IDisposable
	{
		private int2 m_GridSize;

		private float m_CellSize;

		private float m_PathHeight;

		private NativeArray<Entity> m_Entities;

		public NativeArray<Entity> entities => m_Entities;

		public AirwayMap(int2 gridSize, float cellSize, float pathHeight, Allocator allocator)
		{
			m_GridSize = gridSize;
			m_CellSize = cellSize;
			m_PathHeight = pathHeight;
			int length = (m_GridSize.x * 3 + 1) * m_GridSize.y + m_GridSize.x;
			m_Entities = new NativeArray<Entity>(length, allocator);
		}

		public void Dispose()
		{
			if (m_Entities.IsCreated)
			{
				m_Entities.Dispose();
			}
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			int2 gridSize = m_GridSize;
			writer.Write(gridSize);
			float cellSize = m_CellSize;
			writer.Write(cellSize);
			float pathHeight = m_PathHeight;
			writer.Write(pathHeight);
			int length = m_Entities.Length;
			writer.Write(length);
			NativeArray<Entity> value = m_Entities;
			writer.Write(value);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			ref int2 gridSize = ref m_GridSize;
			reader.Read(out gridSize);
			ref float cellSize = ref m_CellSize;
			reader.Read(out cellSize);
			if (reader.context.version >= Version.airplaneAirways)
			{
				ref float pathHeight = ref m_PathHeight;
				reader.Read(out pathHeight);
			}
			reader.Read(out int _);
			NativeArray<Entity> value2 = m_Entities;
			reader.Read(value2);
		}

		public void SetDefaults(Context context)
		{
			for (int i = 0; i < m_Entities.Length; i++)
			{
				m_Entities[i] = Entity.Null;
			}
		}

		public int2 GetCellIndex(int entityIndex, out LaneDirection direction)
		{
			int num = m_GridSize.x * 3 + 1;
			int2 result = default(int2);
			result.y = entityIndex / num;
			result.x = entityIndex - result.y * num;
			direction = LaneDirection.HorizontalX;
			if (result.y < m_GridSize.y)
			{
				result.x /= 3;
				direction = (LaneDirection)(entityIndex - result.x * 3 - result.y * num);
				direction += ((int)direction >> 1) & (result.x + result.y);
			}
			return result;
		}

		public int GetEntityIndex(int2 nodeIndex, LaneDirection direction)
		{
			int num = m_GridSize.x * 3 + 1;
			int num2 = nodeIndex.y * num;
			if (nodeIndex.y < m_GridSize.y)
			{
				return num2 + (nodeIndex.x * 3 + math.min(2, (int)direction));
			}
			return num2 + nodeIndex.x;
		}

		public PathNode GetPathNode(int2 index)
		{
			int num = m_GridSize.x * 3 + 1;
			if (index.y < m_GridSize.y)
			{
				return new PathNode(m_Entities[index.y * num + index.x * 3], 0);
			}
			if (index.x < m_GridSize.x)
			{
				return new PathNode(m_Entities[m_GridSize.y * num + index.x], 0);
			}
			return new PathNode(m_Entities[m_GridSize.y * num + m_GridSize.x - 1], 2);
		}

		public float3 GetNodePosition(int2 nodeIndex)
		{
			float2 @float = (nodeIndex - (float2)m_GridSize * 0.5f) * m_CellSize;
			return new float3(@float.x, m_PathHeight, @float.y);
		}

		public int2 GetCellIndex(float3 position)
		{
			return math.clamp((int2)(position.xz / m_CellSize + (float2)m_GridSize * 0.5f), 0, m_GridSize - 1);
		}

		public void FindClosestLane(float3 position, ComponentLookup<Curve> curveData, ref Entity lane, ref float curvePos, ref float distance)
		{
			int2 cellIndex = GetCellIndex(position);
			FindClosestLaneImpl(position, curveData, ref lane, ref curvePos, ref distance, GetEntityIndex(cellIndex, LaneDirection.HorizontalZ));
			FindClosestLaneImpl(position, curveData, ref lane, ref curvePos, ref distance, GetEntityIndex(cellIndex, LaneDirection.HorizontalX));
			FindClosestLaneImpl(position, curveData, ref lane, ref curvePos, ref distance, GetEntityIndex(cellIndex, LaneDirection.Diagonal));
			FindClosestLaneImpl(position, curveData, ref lane, ref curvePos, ref distance, GetEntityIndex(new int2(cellIndex.x + 1, cellIndex.y), LaneDirection.HorizontalZ));
			FindClosestLaneImpl(position, curveData, ref lane, ref curvePos, ref distance, GetEntityIndex(new int2(cellIndex.x, cellIndex.y + 1), LaneDirection.HorizontalX));
		}

		private void FindClosestLaneImpl(float3 position, ComponentLookup<Curve> curveData, ref Entity bestLane, ref float bestCurvePos, ref float bestDistance, int entityIndex)
		{
			Entity entity = m_Entities[entityIndex];
			if (curveData.HasComponent(entity))
			{
				float t;
				float num = MathUtils.Distance(curveData[entity].m_Bezier, position, out t);
				if (num < bestDistance)
				{
					bestLane = entity;
					bestCurvePos = t;
					bestDistance = num;
				}
			}
		}
	}

	public struct AirwayData : IDisposable
	{
		public AirwayMap helicopterMap { get; private set; }

		public AirwayMap airplaneMap { get; private set; }

		public AirwayData(AirwayMap _helicopterMap, AirwayMap _airplaneMap)
		{
			helicopterMap = _helicopterMap;
			airplaneMap = _airplaneMap;
		}

		public void Dispose()
		{
			helicopterMap.Dispose();
			airplaneMap.Dispose();
		}
	}

	public enum LaneDirection
	{
		HorizontalZ,
		HorizontalX,
		Diagonal,
		DiagonalCross
	}
}
