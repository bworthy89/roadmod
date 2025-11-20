using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Game.Pathfind;

public static class ModificationJobs
{
	public interface IPathfindModificationJob
	{
		void SetPathfindData(NativePathfindData pathfindData);
	}

	[BurstCompile]
	public struct CreateEdgesJob : IJob, IPathfindModificationJob
	{
		[ReadOnly]
		public CreateAction m_Action;

		public NativePathfindData m_PathfindData;

		public void SetPathfindData(NativePathfindData pathfindData)
		{
			m_PathfindData = pathfindData;
		}

		public void Execute()
		{
			for (int i = 0; i < m_Action.m_CreateData.Length; i++)
			{
				CreateActionData createActionData = m_Action.m_CreateData[i];
				if (createActionData.m_Specification.m_Methods != 0)
				{
					EdgeID edgeID = m_PathfindData.CreateEdge(createActionData.m_StartNode, createActionData.m_MiddleNode, createActionData.m_EndNode, createActionData.m_Specification, createActionData.m_Location);
					m_PathfindData.AddEdge(createActionData.m_Owner, edgeID);
				}
				if (createActionData.m_SecondarySpecification.m_Methods != 0)
				{
					PathNode startNode = new PathNode(createActionData.m_SecondaryStartNode, (createActionData.m_SecondarySpecification.m_Flags & EdgeFlags.SecondaryStart) != 0);
					PathNode middleNode = new PathNode(createActionData.m_MiddleNode, secondaryNode: true);
					PathNode endNode = new PathNode(createActionData.m_SecondaryEndNode, (createActionData.m_SecondarySpecification.m_Flags & EdgeFlags.SecondaryEnd) != 0);
					PathSpecification secondarySpecification = createActionData.m_SecondarySpecification;
					secondarySpecification.m_Flags |= EdgeFlags.Secondary;
					LocationSpecification location = createActionData.m_Location;
					if ((createActionData.m_SecondarySpecification.m_Flags & EdgeFlags.SecondaryStart) != 0)
					{
						location.m_Line.a.y += 1f;
					}
					if ((createActionData.m_SecondarySpecification.m_Flags & EdgeFlags.SecondaryEnd) != 0)
					{
						location.m_Line.b.y += 1f;
					}
					EdgeID edgeID2 = m_PathfindData.CreateEdge(startNode, middleNode, endNode, secondarySpecification, location);
					m_PathfindData.AddSecondaryEdge(createActionData.m_Owner, edgeID2);
				}
			}
		}
	}

	[BurstCompile]
	public struct UpdateEdgesJob : IJob, IPathfindModificationJob
	{
		[ReadOnly]
		public UpdateAction m_Action;

		public NativePathfindData m_PathfindData;

		public void SetPathfindData(NativePathfindData pathfindData)
		{
			m_PathfindData = pathfindData;
		}

		public void Execute()
		{
			for (int i = 0; i < m_Action.m_UpdateData.Length; i++)
			{
				UpdateActionData updateActionData = m_Action.m_UpdateData[i];
				if (m_PathfindData.GetEdge(updateActionData.m_Owner, out var edgeID))
				{
					m_PathfindData.UpdateEdge(edgeID, updateActionData.m_StartNode, updateActionData.m_MiddleNode, updateActionData.m_EndNode, updateActionData.m_Specification, updateActionData.m_Location);
				}
				if (m_PathfindData.GetSecondaryEdge(updateActionData.m_Owner, out edgeID))
				{
					PathNode startNode = new PathNode(updateActionData.m_SecondaryStartNode, (updateActionData.m_SecondarySpecification.m_Flags & EdgeFlags.SecondaryStart) != 0);
					PathNode middleNode = new PathNode(updateActionData.m_MiddleNode, secondaryNode: true);
					PathNode endNode = new PathNode(updateActionData.m_SecondaryEndNode, (updateActionData.m_SecondarySpecification.m_Flags & EdgeFlags.SecondaryEnd) != 0);
					PathSpecification secondarySpecification = updateActionData.m_SecondarySpecification;
					secondarySpecification.m_Flags |= EdgeFlags.Secondary;
					LocationSpecification location = updateActionData.m_Location;
					if ((updateActionData.m_SecondarySpecification.m_Flags & EdgeFlags.SecondaryStart) != 0)
					{
						location.m_Line.a.y += 1f;
					}
					if ((updateActionData.m_SecondarySpecification.m_Flags & EdgeFlags.SecondaryEnd) != 0)
					{
						location.m_Line.b.y += 1f;
					}
					m_PathfindData.UpdateEdge(edgeID, startNode, middleNode, endNode, secondarySpecification, location);
				}
			}
		}
	}

	[BurstCompile]
	public struct DeleteEdgesJob : IJob, IPathfindModificationJob
	{
		[ReadOnly]
		public DeleteAction m_Action;

		public NativePathfindData m_PathfindData;

		public void SetPathfindData(NativePathfindData pathfindData)
		{
			m_PathfindData = pathfindData;
		}

		public void Execute()
		{
			for (int i = 0; i < m_Action.m_DeleteData.Length; i++)
			{
				DeleteActionData deleteActionData = m_Action.m_DeleteData[i];
				if (m_PathfindData.RemoveEdge(deleteActionData.m_Owner, out var edgeID))
				{
					m_PathfindData.DestroyEdge(edgeID);
				}
				if (m_PathfindData.RemoveSecondaryEdge(deleteActionData.m_Owner, out edgeID))
				{
					m_PathfindData.DestroyEdge(edgeID);
				}
			}
		}
	}

	[BurstCompile]
	public struct SetDensityJob : IJob, IPathfindModificationJob
	{
		[ReadOnly]
		public DensityAction m_Action;

		public NativePathfindData m_PathfindData;

		public void SetPathfindData(NativePathfindData pathfindData)
		{
			m_PathfindData = pathfindData;
		}

		public void Execute()
		{
			NativeQueue<DensityActionData>.Enumerator enumerator = m_Action.m_DensityData.AsReadOnly().GetEnumerator();
			while (enumerator.MoveNext())
			{
				DensityActionData current = enumerator.Current;
				if (m_PathfindData.GetEdge(current.m_Owner, out var edgeID))
				{
					m_PathfindData.SetDensity(edgeID) = current.m_Density;
				}
				if (m_PathfindData.GetSecondaryEdge(current.m_Owner, out edgeID))
				{
					m_PathfindData.SetDensity(edgeID) = current.m_Density;
				}
			}
			enumerator.Dispose();
		}
	}

	[BurstCompile]
	public struct SetTimeJob : IJob, IPathfindModificationJob
	{
		[ReadOnly]
		public TimeAction m_Action;

		public NativePathfindData m_PathfindData;

		public void SetPathfindData(NativePathfindData pathfindData)
		{
			m_PathfindData = pathfindData;
		}

		public void Execute()
		{
			NativeQueue<TimeActionData>.Enumerator enumerator = m_Action.m_TimeData.AsReadOnly().GetEnumerator();
			while (enumerator.MoveNext())
			{
				TimeActionData current = enumerator.Current;
				if ((current.m_Flags & TimeActionFlags.SetPrimary) != 0 && m_PathfindData.GetEdge(current.m_Owner, out var edgeID))
				{
					m_PathfindData.SetCosts(edgeID).m_Value.x = current.m_Time;
					m_PathfindData.SetEdgeDirections(edgeID, current.m_StartNode, current.m_EndNode, (current.m_Flags & TimeActionFlags.EnableForward) != 0, (current.m_Flags & TimeActionFlags.EnableBackward) != 0);
				}
				if ((current.m_Flags & TimeActionFlags.SetSecondary) != 0 && m_PathfindData.GetSecondaryEdge(current.m_Owner, out var edgeID2))
				{
					m_PathfindData.SetCosts(edgeID2).m_Value.x = current.m_Time;
					EdgeFlags flags = m_PathfindData.GetFlags(edgeID2);
					PathNode startNode = new PathNode(current.m_SecondaryStartNode, (flags & EdgeFlags.SecondaryStart) != 0);
					PathNode endNode = new PathNode(current.m_SecondaryEndNode, (flags & EdgeFlags.SecondaryEnd) != 0);
					m_PathfindData.SetEdgeDirections(edgeID2, startNode, endNode, (current.m_Flags & TimeActionFlags.EnableForward) != 0, (current.m_Flags & TimeActionFlags.EnableBackward) != 0);
				}
			}
			enumerator.Dispose();
		}
	}

	[BurstCompile]
	public struct SetFlowJob : IJob, IPathfindModificationJob
	{
		[ReadOnly]
		public FlowAction m_Action;

		public NativePathfindData m_PathfindData;

		public void SetPathfindData(NativePathfindData pathfindData)
		{
			m_PathfindData = pathfindData;
		}

		public void Execute()
		{
			NativeQueue<FlowActionData>.Enumerator enumerator = m_Action.m_FlowData.AsReadOnly().GetEnumerator();
			while (enumerator.MoveNext())
			{
				FlowActionData current = enumerator.Current;
				if (m_PathfindData.GetEdge(current.m_Owner, out var edgeID))
				{
					m_PathfindData.SetFlowOffset(edgeID) = current.m_FlowOffset;
				}
				if (m_PathfindData.GetSecondaryEdge(current.m_Owner, out edgeID))
				{
					m_PathfindData.SetFlowOffset(edgeID) = current.m_FlowOffset;
				}
			}
			enumerator.Dispose();
		}
	}
}
