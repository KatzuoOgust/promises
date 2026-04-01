namespace KatzuoOgust.Promises;

/// <summary>Enumeration of possible promise states.</summary>
public enum PromiseStatus
{
	/// <summary>The promise is waiting to be resolved or failed.</summary>
	Pending,
	/// <summary>The promise has been resolved with a result.</summary>
	Resolved,
	/// <summary>The promise has failed with an error.</summary>
	Failed
}
