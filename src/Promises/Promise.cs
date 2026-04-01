namespace KatzuoOgust.Promises;

/// <summary>
/// Lightweight handle to a promise stored in an <see cref="IPromiseStore{T}"/>.
/// Each call to <see cref="CheckAsync"/> or <see cref="GetResultAsync"/> re-queries
/// the store — no caching, no hidden polling.
/// </summary>
/// <typeparam name="T">The type of the promise's result.</typeparam>
public sealed class Promise<T>
{
	private readonly IPromiseStore<T> _store;

	/// <summary>The unique identifier for this promise.</summary>
	public string Id { get; }

	/// <summary>
	/// Initializes a new promise handle.
	/// </summary>
	/// <param name="id">The unique identifier for the promise.</param>
	/// <param name="store">The store that backs this promise.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or whitespace.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
	public Promise(string id, IPromiseStore<T> store)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(id);
		ArgumentNullException.ThrowIfNull(store);
		Id = id;
		_store = store;
	}

	/// <summary>
	/// Re-queries the store and returns the current <see cref="PromiseRecord{T}"/>.
	/// Throws <see cref="PromiseNotFoundException"/> if the id no longer exists.
	/// </summary>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The current promise record.</returns>
	/// <exception cref="PromiseNotFoundException">Thrown if the promise is not found in the store.</exception>
	public async Task<PromiseRecord<T>> CheckAsync(CancellationToken ct = default)
	{
		PromiseRecord<T>? record = await _store.GetAsync(Id, ct).ConfigureAwait(false);
		return record ?? throw Error.NotFound(Id);
	}

	/// <summary>
	/// Re-queries the store and returns the resolved result.
	/// Throws <see cref="PromiseNotResolvedException"/> when still pending,
	/// <see cref="PromiseFailedException"/> when failed.
	/// </summary>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The resolved result.</returns>
	/// <exception cref="PromiseNotResolvedException">Thrown when the promise is still pending.</exception>
	/// <exception cref="PromiseFailedException">Thrown when the promise has failed.</exception>
	/// <exception cref="PromiseNotFoundException">Thrown if the promise is not found in the store.</exception>
	public async Task<T> GetResultAsync(CancellationToken ct = default)
	{
		PromiseRecord<T> record = await CheckAsync(ct).ConfigureAwait(false);

		return record.Status switch
		{
			PromiseStatus.Resolved => record.Result!,
			PromiseStatus.Failed => throw Error.Failed(Id, record.ErrorMessage),
			_ => throw Error.NotResolved(Id)
		};
	}

	private static class Error
	{
		public static PromiseNotFoundException NotFound(string id) => new(id);

		public static PromiseFailedException Failed(string id, string? errorMessage) => new(id, errorMessage);

		public static PromiseNotResolvedException NotResolved(string id) => new(id);
	}
}
