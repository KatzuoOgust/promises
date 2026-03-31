namespace KatzuoOgust.Promises;

/// <summary>
/// Lightweight handle to a promise stored in an <see cref="IPromiseStore{T}"/>.
/// Each call to <see cref="CheckAsync"/> or <see cref="GetResultAsync"/> re-queries
/// the store — no caching, no hidden polling.
/// </summary>
public sealed class Promise<T>
{
	private readonly IPromiseStore<T> _store;

	public string Id { get; }
	internal IPromiseStore<T> Store => _store;

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
	public async Task<PromiseRecord<T>> CheckAsync(CancellationToken ct = default)
	{
		var record = await _store.GetAsync(Id, ct).ConfigureAwait(false);
		return record ?? throw new PromiseNotFoundException(Id);
	}

	/// <summary>
	/// Re-queries the store and returns the resolved result.
	/// Throws <see cref="PromiseNotResolvedException"/> when still pending,
	/// <see cref="PromiseFailedException"/> when failed.
	/// </summary>
	public async Task<T> GetResultAsync(CancellationToken ct = default)
	{
		var record = await CheckAsync(ct).ConfigureAwait(false);

		return record.Status switch
		{
			PromiseStatus.Resolved => record.Result!,
			PromiseStatus.Failed => throw new PromiseFailedException(Id, record.ErrorMessage),
			_ => throw new PromiseNotResolvedException(Id)
		};
	}
}
