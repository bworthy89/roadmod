namespace Game.UI;

internal class ErrorEntry
{
	public readonly Fingerprint key;

	public ErrorDialog error;

	public ErrorEntry(Fingerprint key, ErrorDialog error)
	{
		this.key = key;
		this.error = error;
	}
}
