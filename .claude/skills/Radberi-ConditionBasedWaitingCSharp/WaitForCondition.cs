// <copyright file="WaitForCondition.cs" company="Radberi">
// Copyright (c) Radberi. All rights reserved.
// </copyright>

namespace SpaceHub.TestInfrastructure.Helpers;

using System.Diagnostics;

/// <summary>
/// Provides condition-based waiting utilities for integration and async tests.
/// Replaces arbitrary Thread.Sleep/Task.Delay with polling for actual conditions.
/// </summary>
/// <remarks>
/// <para>
/// <b>Core principle:</b> Wait for the actual condition you care about, not a guess about how long it takes.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// // Instead of: await Task.Delay(500);
/// var user = await WaitFor.ConditionAsync(
///     () =&gt; dbContext.Users.Find(userId),
///     "user to exist in database");
/// </code>
/// </para>
/// </remarks>
public static class WaitFor
{
    /// <summary>
    /// Default timeout for condition waiting (5 seconds).
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Default poll interval between condition checks (10 milliseconds).
    /// </summary>
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// Polls a synchronous condition until it returns a non-null value or times out.
    /// </summary>
    /// <typeparam name="T">The type of result to wait for (must be a reference type).</typeparam>
    /// <param name="condition">Function that returns the value when ready, null otherwise.</param>
    /// <param name="description">Human-readable description for timeout error messages.</param>
    /// <param name="timeout">Maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="pollInterval">Time between condition checks. Defaults to 10ms.</param>
    /// <param name="cancellationToken">Token to cancel the wait operation.</param>
    /// <returns>The non-null result from the condition function.</returns>
    /// <exception cref="TimeoutException">Thrown when timeout expires before condition is met.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <example>
    /// <code>
    /// var room = await WaitFor.ConditionAsync(
    ///     () =&gt; dbContext.Rooms.FirstOrDefault(r =&gt; r.Name == "Test"),
    ///     "room to be created");
    /// </code>
    /// </example>
    public static async Task<T> ConditionAsync<T>(
        Func<T?> condition,
        string description,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var timeoutValue = timeout ?? DefaultTimeout;
        var pollIntervalValue = pollInterval ?? DefaultPollInterval;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = condition();
            if (result is not null)
            {
                return result;
            }

            if (stopwatch.Elapsed > timeoutValue)
            {
                throw new TimeoutException(
                    $"Timeout waiting for {description} after {timeoutValue.TotalMilliseconds:F0}ms");
            }

            await Task.Delay(pollIntervalValue, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Polls a synchronous boolean condition until it returns true or times out.
    /// </summary>
    /// <param name="condition">Function that returns true when the condition is met.</param>
    /// <param name="description">Human-readable description for timeout error messages.</param>
    /// <param name="timeout">Maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="pollInterval">Time between condition checks. Defaults to 10ms.</param>
    /// <param name="cancellationToken">Token to cancel the wait operation.</param>
    /// <returns>A task that completes when the condition is met.</returns>
    /// <exception cref="TimeoutException">Thrown when timeout expires before condition is met.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <example>
    /// <code>
    /// await WaitFor.ConditionAsync(
    ///     () =&gt; service.IsReady,
    ///     "service to become ready");
    /// </code>
    /// </example>
    public static async Task ConditionAsync(
        Func<bool> condition,
        string description,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var timeoutValue = timeout ?? DefaultTimeout;
        var pollIntervalValue = pollInterval ?? DefaultPollInterval;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (condition())
            {
                return;
            }

            if (stopwatch.Elapsed > timeoutValue)
            {
                throw new TimeoutException(
                    $"Timeout waiting for {description} after {timeoutValue.TotalMilliseconds:F0}ms");
            }

            await Task.Delay(pollIntervalValue, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Polls an asynchronous condition until it returns a non-null value or times out.
    /// </summary>
    /// <typeparam name="T">The type of result to wait for (must be a reference type).</typeparam>
    /// <param name="asyncCondition">Async function that returns the value when ready, null otherwise.</param>
    /// <param name="description">Human-readable description for timeout error messages.</param>
    /// <param name="timeout">Maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="pollInterval">Time between condition checks. Defaults to 10ms.</param>
    /// <param name="cancellationToken">Token to cancel the wait operation.</param>
    /// <returns>The non-null result from the condition function.</returns>
    /// <exception cref="TimeoutException">Thrown when timeout expires before condition is met.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <example>
    /// <code>
    /// var user = await WaitFor.ConditionAsync(
    ///     async () =&gt; await dbContext.Users.FirstOrDefaultAsync(u =&gt; u.Id == userId),
    ///     "user to exist in database");
    /// </code>
    /// </example>
    public static async Task<T> ConditionAsync<T>(
        Func<Task<T?>> asyncCondition,
        string description,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(asyncCondition);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var timeoutValue = timeout ?? DefaultTimeout;
        var pollIntervalValue = pollInterval ?? DefaultPollInterval;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await asyncCondition().ConfigureAwait(false);
            if (result is not null)
            {
                return result;
            }

            if (stopwatch.Elapsed > timeoutValue)
            {
                throw new TimeoutException(
                    $"Timeout waiting for {description} after {timeoutValue.TotalMilliseconds:F0}ms");
            }

            await Task.Delay(pollIntervalValue, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Polls an asynchronous boolean condition until it returns true or times out.
    /// </summary>
    /// <param name="asyncCondition">Async function that returns true when the condition is met.</param>
    /// <param name="description">Human-readable description for timeout error messages.</param>
    /// <param name="timeout">Maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="pollInterval">Time between condition checks. Defaults to 10ms.</param>
    /// <param name="cancellationToken">Token to cancel the wait operation.</param>
    /// <returns>A task that completes when the condition is met.</returns>
    /// <exception cref="TimeoutException">Thrown when timeout expires before condition is met.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <example>
    /// <code>
    /// await WaitFor.ConditionAsync(
    ///     async () =&gt; await dbContext.Database.CanConnectAsync(),
    ///     "database connection available");
    /// </code>
    /// </example>
    public static async Task ConditionAsync(
        Func<Task<bool>> asyncCondition,
        string description,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asyncCondition);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var timeoutValue = timeout ?? DefaultTimeout;
        var pollIntervalValue = pollInterval ?? DefaultPollInterval;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await asyncCondition().ConfigureAwait(false))
            {
                return;
            }

            if (stopwatch.Elapsed > timeoutValue)
            {
                throw new TimeoutException(
                    $"Timeout waiting for {description} after {timeoutValue.TotalMilliseconds:F0}ms");
            }

            await Task.Delay(pollIntervalValue, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Polls a condition until it returns a non-null value for nullable value types, or times out.
    /// </summary>
    /// <typeparam name="T">The value type to wait for.</typeparam>
    /// <param name="condition">Function that returns the value when ready, null otherwise.</param>
    /// <param name="description">Human-readable description for timeout error messages.</param>
    /// <param name="timeout">Maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="pollInterval">Time between condition checks. Defaults to 10ms.</param>
    /// <param name="cancellationToken">Token to cancel the wait operation.</param>
    /// <returns>The non-null result from the condition function.</returns>
    /// <exception cref="TimeoutException">Thrown when timeout expires before condition is met.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <example>
    /// <code>
    /// var count = await WaitFor.ValueAsync(
    ///     () =&gt; items.Count &gt;= 5 ? items.Count : null,
    ///     "at least 5 items");
    /// </code>
    /// </example>
    public static async Task<T> ValueAsync<T>(
        Func<T?> condition,
        string description,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var timeoutValue = timeout ?? DefaultTimeout;
        var pollIntervalValue = pollInterval ?? DefaultPollInterval;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = condition();
            if (result.HasValue)
            {
                return result.Value;
            }

            if (stopwatch.Elapsed > timeoutValue)
            {
                throw new TimeoutException(
                    $"Timeout waiting for {description} after {timeoutValue.TotalMilliseconds:F0}ms");
            }

            await Task.Delay(pollIntervalValue, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits for a collection to reach a minimum count.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="getCollection">Function that returns the current collection.</param>
    /// <param name="expectedCount">Minimum number of items to wait for.</param>
    /// <param name="description">Human-readable description for timeout error messages.</param>
    /// <param name="timeout">Maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="pollInterval">Time between condition checks. Defaults to 10ms.</param>
    /// <param name="cancellationToken">Token to cancel the wait operation.</param>
    /// <returns>The collection once it has the expected count.</returns>
    /// <exception cref="TimeoutException">Thrown when timeout expires before count is reached.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <example>
    /// <code>
    /// var messages = await WaitFor.CountAsync(
    ///     () =&gt; hubMessages,
    ///     expectedCount: 3,
    ///     "3 SignalR messages");
    /// </code>
    /// </example>
    public static async Task<IReadOnlyList<T>> CountAsync<T>(
        Func<IEnumerable<T>> getCollection,
        int expectedCount,
        string description,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(getCollection);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedCount);

        var timeoutValue = timeout ?? DefaultTimeout;
        var pollIntervalValue = pollInterval ?? DefaultPollInterval;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var collection = getCollection().ToList();
            if (collection.Count >= expectedCount)
            {
                return collection;
            }

            if (stopwatch.Elapsed > timeoutValue)
            {
                throw new TimeoutException(
                    $"Timeout waiting for {description} after {timeoutValue.TotalMilliseconds:F0}ms " +
                    $"(expected {expectedCount}, got {collection.Count})");
            }

            await Task.Delay(pollIntervalValue, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits for an item matching a predicate to appear in a collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="getCollection">Function that returns the current collection.</param>
    /// <param name="predicate">Predicate to match the desired item.</param>
    /// <param name="description">Human-readable description for timeout error messages.</param>
    /// <param name="timeout">Maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="pollInterval">Time between condition checks. Defaults to 10ms.</param>
    /// <param name="cancellationToken">Token to cancel the wait operation.</param>
    /// <returns>The first matching item.</returns>
    /// <exception cref="TimeoutException">Thrown when timeout expires before match is found.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <example>
    /// <code>
    /// var notification = await WaitFor.MatchAsync(
    ///     () =&gt; notifications,
    ///     n =&gt; n.Type == "RoomCreated",
    ///     "RoomCreated notification");
    /// </code>
    /// </example>
    public static async Task<T> MatchAsync<T>(
        Func<IEnumerable<T>> getCollection,
        Func<T, bool> predicate,
        string description,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(getCollection);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var timeoutValue = timeout ?? DefaultTimeout;
        var pollIntervalValue = pollInterval ?? DefaultPollInterval;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var match = getCollection().FirstOrDefault(predicate);
            if (match is not null)
            {
                return match;
            }

            if (stopwatch.Elapsed > timeoutValue)
            {
                throw new TimeoutException(
                    $"Timeout waiting for {description} after {timeoutValue.TotalMilliseconds:F0}ms");
            }

            await Task.Delay(pollIntervalValue, cancellationToken).ConfigureAwait(false);
        }
    }
}
