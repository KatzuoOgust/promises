namespace KatzuoOgust.Promises;

/// <summary>
/// Extension methods for <see cref="Promise{T}"/>.
/// </summary>
public static class PromiseExtensions
{
	private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(50);
	private static readonly TimeSpan MaxInterval = TimeSpan.FromSeconds(2);

	extension<T>(Promise<T> promise)
	{
		/// <summary>
		/// Polls the store with increasing intervals (from 50 ms up to 2 s) until the promise
		/// settles, then returns the promise handle.
		/// Throws <see cref="PromiseFailedException"/> when the promise failed,
		/// <see cref="PromiseNotFoundException"/> if the id disappears,
		/// <see cref="TimeoutException"/> after <paramref name="timeout"/> (when provided),
		/// or <see cref="OperationCanceledException"/> when <paramref name="ct"/> fires.
		/// </summary>
		public Task<Promise<T>> WaitAsync(TimeSpan? timeout = null,
			CancellationToken ct = default)
		{
			ArgumentNullException.ThrowIfNull(promise);
			return WaitCoreAsync(promise, timeout, ct);
		}

		/// <summary>
		/// Polls the store with increasing intervals (from 50 ms up to 2 s) until the promise
		/// settles, then returns the resolved result directly.
		/// Throws <see cref="PromiseFailedException"/> when the promise failed,
		/// <see cref="PromiseNotFoundException"/> if the id disappears,
		/// <see cref="TimeoutException"/> after <paramref name="timeout"/> (when provided),
		/// or <see cref="OperationCanceledException"/> when <paramref name="ct"/> fires.
		/// </summary>
		public async Task<T> WaitForResultAsync(TimeSpan? timeout = null,
			CancellationToken ct = default)
		{
			Promise<T> settled = await promise.WaitAsync(timeout, ct).ConfigureAwait(false);
			return await settled.GetResultAsync(ct).ConfigureAwait(false);
		}
	}

	private static async Task<Promise<T>> WaitCoreAsync<T>(
		Promise<T> promise,
		TimeSpan? timeout,
		CancellationToken ct)
	{
		using CancellationTokenSource? timeoutCts = timeout.HasValue
			? new CancellationTokenSource(timeout.Value)
			: null;

		using CancellationTokenSource? linkedCts = timeoutCts is not null
			? CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token)
			: null;

		CancellationToken effectiveCt = linkedCts?.Token ?? ct;
		TimeSpan interval = MinInterval;

		while (true)
		{
			PromiseRecord<T> record = await promise.Store.GetAsync(promise.Id, effectiveCt).ConfigureAwait(false)
			                          ?? throw new PromiseNotFoundException(promise.Id);

			switch (record.Status)
			{
				case PromiseStatus.Resolved:
					return promise;
				case PromiseStatus.Failed:
					throw new PromiseFailedException(promise.Id, record.ErrorMessage);
			}

			try
			{
				await Task.Delay(interval, effectiveCt).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true && !ct.IsCancellationRequested)
			{
				throw new TimeoutException($"Promise '{promise.Id}' did not settle within {timeout}.");
			}

			interval = interval + interval > MaxInterval ? MaxInterval : interval + interval;
		}
	}
}
