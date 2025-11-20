namespace Game.Simulation;

public struct XPMessage
{
	public uint createdSimFrame { get; private set; }

	public XPReason reason { get; private set; }

	public int amount { get; private set; }

	public XPMessage(uint createdSimFrame, int amount, XPReason reason)
	{
		this.createdSimFrame = createdSimFrame;
		this.amount = amount;
		this.reason = reason;
	}
}
