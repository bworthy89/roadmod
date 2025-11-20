using Game.Economy;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;

namespace Game.Pathfind;

public struct SetupQueueTarget
{
	public SetupTargetType m_Type;

	public PathMethod m_Methods;

	public Entity m_Entity;

	public Entity m_Entity2;

	public Resource m_Resource;

	public ActivityMask m_ActivityMask;

	public MaintenanceType m_MaintenanceType;

	public SetupTargetFlags m_Flags;

	public TrackTypes m_TrackTypes;

	public RoadTypes m_RoadTypes;

	public RoadTypes m_FlyingTypes;

	public int m_Value;

	public float m_Value2;

	public float m_RandomCost;

	public int m_Value3;
}
