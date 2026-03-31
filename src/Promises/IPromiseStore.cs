namespace KatzuoOgust.Promises;

/// <summary>
/// Read/write store for promise records. Implementations decide persistence strategy.
/// </summary>
public interface IPromiseStore<T>
{
	/// <summary>Creates a new pending promise and returns its handle.</summary>
	Task<Promise<T>> CreateAsync(CancellationToken ct = default);

	/// <summary>Marks the promise as resolved with <paramref name="result"/>.</summary>
	Task ResolveAsync(string id, T result, CancellationToken ct = default);

	/// <summary>Marks the promise as failed with an error message.</summary>
	Task FailAsync(string id, string errorMessage, CancellationToken ct = default);

	/// <summary>Returns the current record, or <c>null</c> if the id is unknown.</summary>
	Task<PromiseRecord<T>?> GetAsync(string id, CancellationToken ct = default);
}
