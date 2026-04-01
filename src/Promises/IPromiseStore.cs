namespace KatzuoOgust.Promises;

/// <summary>
/// Read/write store for promise records. Implementations decide persistence strategy.
/// </summary>
/// <typeparam name="T">The type of results stored in promises.</typeparam>
public interface IPromiseStore<T>
{
	/// <summary>
	/// Creates a new pending promise and returns its handle.
	/// </summary>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A new promise handle.</returns>
	Task<Promise<T>> CreateAsync(CancellationToken ct = default);

	/// <summary>
	/// Marks the promise as resolved with the given result.
	/// </summary>
	/// <param name="id">The promise ID.</param>
	/// <param name="result">The result value.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <exception cref="PromiseNotFoundException">Thrown if the promise ID is not found.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the promise is already settled.</exception>
	Task ResolveAsync(string id, T result, CancellationToken ct = default);

	/// <summary>
	/// Marks the promise as failed with an error message.
	/// </summary>
	/// <param name="id">The promise ID.</param>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <exception cref="PromiseNotFoundException">Thrown if the promise ID is not found.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the promise is already settled.</exception>
	Task FailAsync(string id, string errorMessage, CancellationToken ct = default);

	/// <summary>
	/// Returns the current record, or <c>null</c> if the id is unknown.
	/// </summary>
	/// <param name="id">The promise ID.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The promise record, or <c>null</c> if not found.</returns>
	Task<PromiseRecord<T>?> GetAsync(string id, CancellationToken ct = default);
}
