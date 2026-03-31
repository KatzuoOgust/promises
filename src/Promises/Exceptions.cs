namespace KatzuoOgust.Promises;

/// <summary>Base class for all promise-related exceptions.</summary>
public class PromiseException(string message) : Exception(message);

/// <summary>Thrown when <see cref="Promise{T}.GetResultAsync"/> is called but the promise is still pending.</summary>
public sealed class PromiseNotResolvedException(string id)
	: PromiseException($"Promise '{id}' is still pending.");

/// <summary>Thrown when <see cref="Promise{T}.GetResultAsync"/> is called but the promise has failed.</summary>
public sealed class PromiseFailedException(string id, string? errorMessage)
	: PromiseException($"Promise '{id}' failed: {errorMessage}");

/// <summary>Thrown when a promise id is not found in the store.</summary>
public sealed class PromiseNotFoundException(string id)
	: PromiseException($"Promise '{id}' was not found in the store.");
