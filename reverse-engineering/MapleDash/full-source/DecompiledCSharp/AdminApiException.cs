using System;

public sealed class AdminApiException : Exception
{
	public AdminApiException(string message)
		: base(message)
	{
	}
}
