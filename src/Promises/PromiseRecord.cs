namespace KatzuoOgust.Promises;

/// <summary>Immutable record of a promise's state at a point in time.</summary>
/// <typeparam name="T">The type of the promise's result.</typeparam>
public sealed record PromiseRecord<T>
{
	/// <summary>Unique identifier for the promise.</summary>
	public string Id { get; init; }
	/// <summary>Current status of the promise (pending, resolved, or failed).</summary>
	public PromiseStatus Status { get; init; }
	/// <summary>The resolved result, if the promise has resolved.</summary>
	public T? Result { get; init; }
	/// <summary>The error message, if the promise has failed.</summary>
	public string? ErrorMessage { get; init; }
	/// <summary>When the promise was created (UTC).</summary>
	public DateTimeOffset CreatedAt { get; init; }
	/// <summary>When the promise settled (resolved or failed), if applicable.</summary>
	public DateTimeOffset? CompletedAt { get; init; }

	/// <summary>
	/// Initializes a new promise record.
	/// </summary>
	/// <param name="id">Unique identifier for the promise.</param>
	/// <param name="status">Current status of the promise.</param>
	/// <param name="result">The resolved result, if applicable.</param>
	/// <param name="errorMessage">The error message, if the promise has failed.</param>
	/// <param name="createdAt">When the promise was created.</param>
	/// <param name="completedAt">When the promise settled, if applicable.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or whitespace.</exception>
	public PromiseRecord(
		string id,
		PromiseStatus status,
		T? result,
		string? errorMessage,
		DateTimeOffset createdAt,
		DateTimeOffset? completedAt)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(id);

		Id = id;
		Status = status;
		Result = result;
		ErrorMessage = errorMessage;
		CreatedAt = createdAt;
		CompletedAt = completedAt;
	}
}
