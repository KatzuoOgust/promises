namespace KatzuoOgust.Promises;

/// <summary>
/// Extension methods for <see cref="Promise{T}"/>.
/// </summary>
public static class PromiseExtensions
{
	private static readonly TimeSpan s_minInterval = TimeSpan.FromMilliseconds(50);
	private static readonly TimeSpan s_maxInterval = TimeSpan.FromSeconds(2);

	/// <param name="promise">The promise to wait on.</param>
	/// <typeparam name="T">The type of the promise's result.</typeparam>
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
		/// <param name="timeout">Maximum time to wait; null for no timeout.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>The settled promise handle.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="promise"/> is null.</exception>
		/// <exception cref="PromiseFailedException">Thrown when the promise failed.</exception>
		/// <exception cref="PromiseNotFoundException">Thrown if the promise ID disappears.</exception>
		/// <exception cref="TimeoutException">Thrown if the timeout expires before settlement.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public Task<Promise<T>> WaitAsync(
			TimeSpan? timeout = null,
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
		/// <param name="timeout">Maximum time to wait; null for no timeout.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>The resolved result.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="promise"/> is null.</exception>
		/// <exception cref="PromiseFailedException">Thrown when the promise failed.</exception>
		/// <exception cref="PromiseNotFoundException">Thrown if the promise ID disappears.</exception>
		/// <exception cref="TimeoutException">Thrown if the timeout expires before settlement.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public async Task<T> WaitForResultAsync(
			TimeSpan? timeout = null,
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
		TimeSpan interval = s_minInterval;

		while (true)
		{
			ct.ThrowIfCancellationRequested();
			Error.ThrowIfTimeoutExpired(promise.Id, timeoutCts, timeout);

			PromiseRecord<T> record = await promise.CheckAsync(effectiveCt).ConfigureAwait(false)
				?? throw Error.NotFound(promise.Id);

			switch (record.Status)
			{
				case PromiseStatus.Resolved:
					return promise;
				case PromiseStatus.Failed:
					throw Error.Failed(promise.Id, record.ErrorMessage);
				default:
					try
					{
						await Task.Delay(interval, effectiveCt).ConfigureAwait(false);
					}
					catch (OperationCanceledException)
					{
						// NOTE: Do not throw here: use checks in the next loop iteration to
						// determine whether to throw TimeoutException or OperationCanceledException.
					}

					interval = interval + interval > s_maxInterval ? s_maxInterval : interval + interval;
					break;
			}
		}
	}

	private static class Error
	{
		public static PromiseNotFoundException NotFound(string id) => new(id);

		public static PromiseFailedException Failed(string id, string? errorMessage) => new(id, errorMessage);

		public static void ThrowIfTimeoutExpired(
			string id,
			CancellationTokenSource? timeoutCts,
			TimeSpan? timeout)
		{
			if (timeoutCts?.IsCancellationRequested != true) return;

			throw new TimeoutException($"Promise '{id}' did not settle within {timeout!.Value}.");
		}
	}
}
