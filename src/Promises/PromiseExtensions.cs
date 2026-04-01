namespace KatzuoOgust.Promises;

/// <summary>
/// Extension methods for <see cref="Promise{T}"/>.
/// </summary>
public static class PromiseExtensions
{
	private static readonly TimeSpan s_minInterval = TimeSpan.FromMilliseconds(50);
	private static readonly TimeSpan s_maxInterval = TimeSpan.FromSeconds(2);

	/// <summary>
	/// Polls the store with increasing intervals (from 50 ms up to 2 s) until the promise
	/// settles, then returns the promise handle.
	/// Throws <see cref="PromiseFailedException"/> when the promise failed,
	/// <see cref="PromiseNotFoundException"/> if the id disappears,
	/// <see cref="TimeoutException"/> after <paramref name="timeout"/> (when provided),
	/// or <see cref="OperationCanceledException"/> when <paramref name="ct"/> fires.
	/// </summary>
	/// <typeparam name="T">The type of the promise's result.</typeparam>
	/// <param name="promise">The promise to wait on.</param>
	/// <param name="timeout">Maximum time to wait; null for no timeout.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The settled promise handle.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="promise"/> is null.</exception>
	/// <exception cref="PromiseFailedException">Thrown when the promise failed.</exception>
	/// <exception cref="PromiseNotFoundException">Thrown if the promise ID disappears.</exception>
	/// <exception cref="TimeoutException">Thrown if the timeout expires before settlement.</exception>
	/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
	public static Task<Promise<T>> WaitAsync<T>(this Promise<T> promise, TimeSpan? timeout = null,
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
	/// <typeparam name="T">The type of the promise's result.</typeparam>
	/// <param name="promise">The promise to wait on.</param>
	/// <param name="timeout">Maximum time to wait; null for no timeout.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The resolved result.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="promise"/> is null.</exception>
	/// <exception cref="PromiseFailedException">Thrown when the promise failed.</exception>
	/// <exception cref="PromiseNotFoundException">Thrown if the promise ID disappears.</exception>
	/// <exception cref="TimeoutException">Thrown if the timeout expires before settlement.</exception>
	/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
	public static async Task<T> WaitForResultAsync<T>(this Promise<T> promise, TimeSpan? timeout = null,
		CancellationToken ct = default)
	{
		Promise<T> settled = await promise.WaitAsync(timeout, ct).ConfigureAwait(false);
		return await settled.GetResultAsync(ct).ConfigureAwait(false);
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
			PromiseRecord<T> record = await promise.CheckAsync(effectiveCt).ConfigureAwait(false)
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

			interval = interval + interval > s_maxInterval ? s_maxInterval : interval + interval;
		}
	}
}
