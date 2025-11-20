namespace Game.Simulation;

public enum TaxiRequestType : byte
{
	Stand = 0,
	Customer = 1,
	Outside = 2,
	None = byte.MaxValue
}
