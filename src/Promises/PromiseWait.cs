namespace KatzuoOgust.Promises;

/// <summary>
/// Static methods for polling a <see cref="Promise{T}"/> until it settles.
/// </summary>
public static class PromiseWait
{
	private static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMilliseconds(500);

	/// <summary>
	/// Polls the store at <paramref name="pollingInterval"/> until the promise settles,
	/// then returns the resolved result.
	/// Throws <see cref="TimeoutException"/> after <paramref name="timeout"/>,
	/// <see cref="PromiseFailedException"/> when failed,
	/// <see cref="PromiseNotFoundException"/> if the id disappears,
	/// or <see cref="OperationCanceledException"/> when <paramref name="ct"/> fires.
	/// </summary>
	public static Task<T> WaitAsync<T>(
		Promise<T> promise,
		TimeSpan pollingInterval,
		TimeSpan timeout,
		CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(promise);
		return WaitCoreAsync(promise, pollingInterval, timeout, ct);
	}

	/// <summary>
	/// Polls the store at <paramref name="pollingInterval"/> until the promise settles
	/// or <paramref name="ct"/> is cancelled. No timeout.
	/// </summary>
	public static Task<T> WaitAsync<T>(
		Promise<T> promise,
		TimeSpan pollingInterval,
		CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(promise);
		return WaitCoreAsync(promise, pollingInterval, timeout: null, ct);
	}

	/// <summary>
	/// Polls the store at the default interval (500 ms) until the promise settles
	/// or <paramref name="ct"/> is cancelled. No timeout.
	/// </summary>
	public static Task<T> WaitAsync<T>(
		Promise<T> promise,
		CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(promise);
		return WaitCoreAsync(promise, DefaultPollingInterval, timeout: null, ct);
	}

	private static async Task<T> WaitCoreAsync<T>(
		Promise<T> promise,
		TimeSpan pollingInterval,
		TimeSpan? timeout,
		CancellationToken ct)
	{
		using var timeoutCts = timeout.HasValue
			? new CancellationTokenSource(timeout.Value)
			: null;

		using var linkedCts = timeoutCts is not null
			? CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token)
			: null;

		var effectiveCt = linkedCts?.Token ?? ct;

		while (true)
		{
			var record = await promise.Store.GetAsync(promise.Id, effectiveCt).ConfigureAwait(false)
				?? throw new PromiseNotFoundException(promise.Id);

			switch (record.Status)
			{
				case PromiseStatus.Resolved:
					return record.Result!;
				case PromiseStatus.Failed:
					throw new PromiseFailedException(promise.Id, record.ErrorMessage);
			}

			try
			{
				await Task.Delay(pollingInterval, effectiveCt).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true && !ct.IsCancellationRequested)
			{
				throw new TimeoutException($"Promise '{promise.Id}' did not settle within {timeout}.");
			}
		}
	}
}
