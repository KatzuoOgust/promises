namespace KatzuoOgust.Promises;

public sealed record PromiseRecord<T>
{
	public string Id { get; init; }
	public PromiseStatus Status { get; init; }
	public T? Result { get; init; }
	public string? ErrorMessage { get; init; }
	public DateTimeOffset CreatedAt { get; init; }
	public DateTimeOffset? CompletedAt { get; init; }

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
