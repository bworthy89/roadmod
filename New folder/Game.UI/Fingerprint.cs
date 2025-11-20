using System;
using Colossal;

namespace Game.UI;

internal readonly struct Fingerprint : IEquatable<Fingerprint>
{
	private readonly Hash64 exceptionTypeHash;

	private readonly Hash64 messageHash;

	private readonly Hash64 detailsHash;

	private readonly Hash64 identifierHash;

	public Fingerprint(Type exceptionType, string message, string details, string identifier)
	{
		exceptionTypeHash = ((exceptionType != null) ? CreateHash64(exceptionType.FullName) : default(Hash64));
		messageHash = CreateHash64(message);
		detailsHash = CreateHash64(details);
		identifierHash = CreateHash64(identifier);
	}

	public bool Equals(Fingerprint other)
	{
		if (exceptionTypeHash.Equals(other.exceptionTypeHash) && messageHash.Equals(other.messageHash) && detailsHash.Equals(other.detailsHash))
		{
			return identifierHash.Equals(other.identifierHash);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Fingerprint other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((exceptionTypeHash.GetHashCode() * 397) ^ messageHash.GetHashCode()) * 397) ^ detailsHash.GetHashCode()) * 397) ^ identifierHash.GetHashCode();
	}

	private static Hash64 CreateHash64(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return Hash64.CreateGuid(text);
		}
		return default(Hash64);
	}

	public override string ToString()
	{
		return $"{exceptionTypeHash}-{messageHash}-{detailsHash}-{identifierHash}";
	}
}
