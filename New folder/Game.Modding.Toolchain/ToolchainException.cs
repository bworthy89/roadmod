using System;

namespace Game.Modding.Toolchain;

public class ToolchainException : Exception
{
	public IToolchainDependency source { get; }

	public ToolchainError error { get; }

	public bool isFatal { get; }

	public ToolchainException(ToolchainError error, IToolchainDependency source, string message = null, Exception innerException = null, bool isFatal = true)
		: base(message, innerException)
	{
		this.source = source;
		this.error = error;
		this.isFatal = isFatal;
	}

	public ToolchainException(ToolchainError error, IToolchainDependency source, Exception innerException, bool isFatal = true)
		: this(error, source, string.Empty, innerException, isFatal)
	{
	}
}
