using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Orleans.Runtime;
using System.Linq;

namespace Snakes;

public class ObserverManager<TObserver> : ObserverManager<IAddressable, TObserver>
{
    public ObserverManager(TimeSpan expiration, ILoggerFactory loggerFactory, string logCategory) : base(expiration, loggerFactory, logCategory)
    {
    }
}

/// <summary>
/// Maintains a collection of observers.
/// </summary>
/// <typeparam name="TAddress">
/// The address type.
/// </typeparam>
/// <typeparam name="TObserver">
/// The observer type.
/// </typeparam>
public class ObserverManager<TAddress, TObserver> : IEnumerable<TObserver>
    where TAddress : notnull
{
    /// <summary>
    /// The observers.
    /// </summary>
    private readonly ConcurrentDictionary<TAddress, ObserverEntry> _observers = new ();

    /// <summary>
    /// The log.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverManager{TAddress,TObserver}"/> class. 
    /// </summary>
    /// <param name="expiration">
    /// The expiration.
    /// </param>
    /// <param name="loggerFactory">The log.</param>
    /// <param name="logCategory">The prefix to use when logging.</param>
    public ObserverManager(TimeSpan expiration, ILoggerFactory loggerFactory, string logCategory)
    {
        this.ExpirationDuration = expiration;
        this._logger = loggerFactory.CreateLogger(logCategory);
        this.GetDateTime = () => DateTime.UtcNow;
    }

    /// <summary>
    /// Gets or sets the delegate used to get the date and time, for expiry.
    /// </summary>
    public Func<DateTime> GetDateTime { get; set; }

    /// <summary>
    /// Gets or sets the expiration time span, after which observers are lazily removed.
    /// </summary>
    public TimeSpan ExpirationDuration { get; set; }

    /// <summary>
    /// Gets the number of observers.
    /// </summary>
    public int Count => this._observers.Count;

    /// <summary>
    /// Gets a copy of the observers.
    /// </summary>
    public IDictionary<TAddress, TObserver> Observers
    {
        get
        {
            return this._observers.ToDictionary(_ => _.Key, _ => _.Value.Observer);
        }
    }

    /// <summary>
    /// Removes all observers.
    /// </summary>
    public void Clear()
    {
        this._observers.Clear();
    }

    /// <summary>
    /// Ensures that the provided <paramref name="observer"/> is subscribed, renewing its subscription.
    /// </summary>
    /// <param name="address">
    /// The subscriber's address
    /// </param>
    /// <param name="observer">
    /// The observer.
    /// </param>
    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    public void Subscribe(TAddress address, TObserver observer)
    {
        // Add or update the subscription.
        var now = this.GetDateTime();
        if (this._observers.TryGetValue(address, out var _))
        {
            this._observers[address] = new ObserverEntry(observer, now);
            if (this._logger.IsEnabled(LogLevel.Debug))
            {
                this._logger.LogDebug(": Updating entry for {address}/{observer}. {count} total subscribers.", address, observer, this._observers.Count);
            }
        }
        else
        {
            this._observers[address] = new ObserverEntry(observer, now);
            if (this._logger.IsEnabled(LogLevel.Debug))
            {
                this._logger.LogDebug(": Adding entry for {address}/{observer}. {count} total subscribers after add.", address, observer, this._observers.Count);
            }
        }
    }

    /// <summary>
    /// Ensures that the provided <paramref name="subscriber"/> is unsubscribed.
    /// </summary>
    /// <param name="subscriber">
    /// The observer.
    /// </param>
    public void Unsubscribe(TAddress subscriber)
    {
        this._logger.LogDebug(": Removed entry for {address}. {count} total subscribers after remove.", subscriber, this._observers.Count);
        this._observers.TryRemove(subscriber, out _);
    }

    /// <summary>
    /// Notifies all observers.
    /// </summary>
    /// <param name="notification">
    /// The notification delegate to call on each observer.
    /// </param>
    /// <param name="predicate">
    /// The predicate used to select observers to notify.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the work performed.
    /// </returns>
    public async Task Notify(Func<TObserver, Task> notification, Func<TObserver, bool>? predicate = null)
    {
        var now = this.GetDateTime();
        var defunct = default(List<TAddress>);
        foreach (var observer in this._observers)
        {
            if (observer.Value.LastSeen + this.ExpirationDuration < now)
            {
                // Expired observers will be removed.
                defunct ??= new List<TAddress>();
                defunct.Add(observer.Key);
                continue;
            }

            // Skip observers which don't match the provided predicate.
            if (predicate != null && !predicate(observer.Value.Observer))
            {
                continue;
            }

            try
            {
                await notification(observer.Value.Observer);
            }
            catch (Exception)
            {
                // Failing observers are considered defunct and will be removed..
                defunct ??= new List<TAddress>();
                defunct.Add(observer.Key);
            }
        }

        // Remove defunct observers.
        if (defunct != default(List<TAddress>))
        {
            foreach (var observer in defunct)
            {
                this._observers.TryRemove(observer, out _);
                if (this._logger.IsEnabled(LogLevel.Debug))
                {
                    this._logger.LogDebug(": Removing defunct entry for {address}. {count} total subscribers after remove.", observer, this._observers.Count);
                }
            }
        }
    }

    /// <summary>
    /// Notifies all observers which match the provided <paramref name="predicate"/>.
    /// </summary>
    /// <param name="notification">
    /// The notification delegate to call on each observer.
    /// </param>
    /// <param name="predicate">
    /// The predicate used to select observers to notify.
    /// </param>
    public void Notify(Action<TObserver> notification, Func<TObserver, bool>? predicate = null)
    {
        var now = this.GetDateTime();
        var defunct = default(List<TAddress>);
        foreach (var observer in this._observers)
        {
            if (observer.Value.LastSeen + this.ExpirationDuration < now)
            {
                // Expired observers will be removed.
                defunct ??= new List<TAddress>();
                defunct.Add(observer.Key);
                continue;
            }

            // Skip observers which don't match the provided predicate.
            if (predicate != null && !predicate(observer.Value.Observer))
            {
                continue;
            }

            try
            {
                notification(observer.Value.Observer);
            }
            catch (Exception)
            {
                // Failing observers are considered defunct and will be removed..
                defunct ??= new List<TAddress>();
                defunct.Add(observer.Key);
            }
        }

        // Remove defunct observers.
        if (defunct != default(List<TAddress>))
        {
            foreach (var observer in defunct)
            {
                this._observers.TryRemove(observer, out _);
                if (this._logger.IsEnabled(LogLevel.Debug))
                {
                    this._logger.LogDebug(": Removing defunct entry for {address}. {count} total subscribers after remove.", observer, this._observers.Count);
                }
            }
        }
    }

    /// <summary>
    /// Removed all expired observers.
    /// </summary>
    public void ClearExpired()
    {
        var now = this.GetDateTime();
        var defunct = default(List<TAddress>);
        foreach (var observer in this._observers)
        {
            if (observer.Value.LastSeen + this.ExpirationDuration < now)
            {
                // Expired observers will be removed.
                defunct ??= new List<TAddress>();
                defunct.Add(observer.Key);
            }
        }

        // Remove defunct observers.
        if (defunct != default(List<TAddress>) && defunct.Count > 0)
        {
            this._logger.LogInformation(": Removing {count} defunct observers entries.", defunct.Count);
            foreach (var observer in defunct)
            {
                this._observers.TryRemove(observer, out _);
            }
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<TObserver> GetEnumerator()
    {
        return this._observers.Select(observer => observer.Value.Observer).GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    /// <summary>
    /// An observer entry.
    /// </summary>
    private record class ObserverEntry (
    TObserver Observer,
        DateTime LastSeen);
    
}
