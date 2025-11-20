using Colossal.Collections;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace Game.Net;

public struct EdgeIterator
{
	private BufferLookup<ConnectedEdge> m_Edges;

	private ComponentLookup<Edge> m_EdgeData;

	private ComponentLookup<Temp> m_TempData;

	private ComponentLookup<Hidden> m_HiddenData;

	private DynamicBuffer<ConnectedEdge> m_Buffer;

	private int m_Iterator;

	private Entity m_Node;

	private Entity m_Edge;

	private Entity m_OriginalEdge;

	private bool m_Permanent;

	private bool m_Delete;

	private bool m_Middles;

	public EdgeIterator(Entity edge, Entity node, BufferLookup<ConnectedEdge> edges, ComponentLookup<Edge> edgeData, ComponentLookup<Temp> tempData, ComponentLookup<Hidden> hiddenData, bool includeMiddleConnections = false)
	{
		m_Node = node;
		m_Edge = edge;
		m_OriginalEdge = Entity.Null;
		m_Edges = edges;
		m_EdgeData = edgeData;
		m_TempData = tempData;
		m_HiddenData = hiddenData;
		m_Buffer = m_Edges[node];
		m_Iterator = 0;
		m_Permanent = !m_TempData.HasComponent(node);
		m_Delete = false;
		m_Middles = includeMiddleConnections;
		if (edge != Entity.Null)
		{
			m_Delete = GetDelete(edge, out m_OriginalEdge);
		}
		else if (!m_Permanent)
		{
			m_Delete = GetDelete(node);
		}
	}

	public int GetMaxCount()
	{
		int num = m_Buffer.Length;
		if (!m_Permanent && m_Edges.TryGetBuffer(m_TempData[m_Node].m_Original, out var bufferData))
		{
			num += bufferData.Length;
		}
		return num;
	}

	public void AddSorted(ref ComponentLookup<BuildOrder> buildOrderData, ref StackList<EdgeIteratorValueSorted> list)
	{
		EdgeIteratorValue value;
		while (GetNext(out value))
		{
			buildOrderData.TryGetComponent(value.m_Edge, out var componentData);
			list.AddNoResize(new EdgeIteratorValueSorted
			{
				m_Edge = value.m_Edge,
				m_SortIndex = (uint)((ulong)((long)componentData.m_Start + (long)componentData.m_End) >> 1),
				m_End = value.m_End,
				m_Middle = value.m_Middle
			});
		}
		list.AsArray().Sort();
	}

	private bool GetDelete(Entity entity)
	{
		if (m_TempData.TryGetComponent(entity, out var componentData))
		{
			return (componentData.m_Flags & TempFlags.Delete) != 0;
		}
		return false;
	}

	private bool GetDelete(Entity entity, out Entity original)
	{
		if (m_TempData.TryGetComponent(entity, out var componentData))
		{
			original = componentData.m_Original;
			return (componentData.m_Flags & TempFlags.Delete) != 0;
		}
		original = Entity.Null;
		return false;
	}

	public bool GetNext(out EdgeIteratorValue value)
	{
		while (true)
		{
			bool flag = m_Buffer.Length > m_Iterator;
			if (flag)
			{
				value.m_Edge = m_Buffer[m_Iterator++].m_Edge;
			}
			else
			{
				value.m_Edge = Entity.Null;
			}
			while (flag)
			{
				if (m_Permanent)
				{
					Edge edge = m_EdgeData[value.m_Edge];
					value.m_End = edge.m_End == m_Node;
					if (value.m_End || edge.m_Start == m_Node)
					{
						value.m_Middle = false;
						return true;
					}
					if (m_Middles)
					{
						value.m_Middle = true;
						return true;
					}
				}
				else if (m_Delete)
				{
					if (value.m_Edge == m_Edge || (m_HiddenData.HasComponent(value.m_Edge) && value.m_Edge != m_OriginalEdge))
					{
						Edge edge2 = m_EdgeData[value.m_Edge];
						value.m_End = edge2.m_End == m_Node;
						if (value.m_End || edge2.m_Start == m_Node)
						{
							value.m_Middle = false;
							return true;
						}
						if (m_Middles)
						{
							value.m_Middle = true;
							return true;
						}
					}
				}
				else if (!m_HiddenData.HasComponent(value.m_Edge) && !GetDelete(value.m_Edge))
				{
					Edge edge3 = m_EdgeData[value.m_Edge];
					value.m_End = edge3.m_End == m_Node;
					if (value.m_End || edge3.m_Start == m_Node)
					{
						value.m_Middle = false;
						return true;
					}
					if (m_Middles)
					{
						value.m_Middle = true;
						return true;
					}
				}
				flag = m_Buffer.Length > m_Iterator;
				if (flag)
				{
					value.m_Edge = m_Buffer[m_Iterator++].m_Edge;
				}
				else
				{
					value.m_Edge = Entity.Null;
				}
			}
			if (!m_TempData.TryGetComponent(m_Node, out var componentData))
			{
				break;
			}
			m_Node = componentData.m_Original;
			if (!m_Edges.TryGetBuffer(m_Node, out m_Buffer))
			{
				break;
			}
			m_Iterator = 0;
		}
		value.m_Edge = Entity.Null;
		value.m_End = false;
		value.m_Middle = false;
		return false;
	}
}
