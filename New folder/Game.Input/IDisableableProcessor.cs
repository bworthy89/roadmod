namespace Game.Input;

public interface IDisableableProcessor
{
	const bool kDefaultCanBeDisabled = true;

	bool canBeDisabled { get; }

	bool disabled { get; set; }
}
