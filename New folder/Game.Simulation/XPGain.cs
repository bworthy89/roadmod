using Unity.Entities;

namespace Game.Simulation;

public struct XPGain
{
	public Entity entity;

	public int amount;

	public XPReason reason;
}
