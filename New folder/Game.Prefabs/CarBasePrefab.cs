using Game.Vehicles;
using Unity.Mathematics;

namespace Game.Prefabs;

public abstract class CarBasePrefab : VehiclePrefab
{
	public SizeClass m_SizeClass;

	public EnergyTypes m_EnergyType = EnergyTypes.Fuel;

	public float m_MaxSpeed = 200f;

	public float m_Acceleration = 5f;

	public float m_Braking = 10f;

	public float2 m_Turning = new float2(90f, 15f);

	public float m_Stiffness = 100f;
}
