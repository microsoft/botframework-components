using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ITSMSkill.Proactive.Subscription
{
    public class StateAccessor : IStateManager
    {
        private readonly string _keyPrefix;
        private readonly IStorage _storage;

        public StateAccessor(IStorage storage, string keyPrefix)
        {
            _keyPrefix = keyPrefix ?? string.Empty;
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public IStateValueAccessor<T> CreateProperty<T>()
        {
            return new StateValueAccessor<T>(this);
        }

        /// <summary>
        /// Reads in  the current state object and caches it in the context object for this turm.
        /// </summary>
        /// <param name="storageKey">The storage key object.</param>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="force">Optional. True to bypass the cache.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task LoadAsync(
            string storageKey,
            ITurnContext turnContext,
            bool force = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (string.IsNullOrEmpty(storageKey))
            {
                throw new ArgumentNullException(nameof(storageKey));
            }

            string fullStorageKey = this.GetFullStorageKey(storageKey);

            var cachedState = turnContext.TurnState.Get<CachedState>(fullStorageKey);
            if (force || cachedState?.State == null)
            {
                IDictionary<string, object> items = await _storage.ReadAsync(new[] { fullStorageKey }, cancellationToken)
                    .ConfigureAwait(false);
                items.TryGetValue(fullStorageKey, out object val);
                turnContext.TurnState[fullStorageKey] = new CachedState((IDictionary<string, object>)val);
            }
        }

        /// <summary>
        /// Delete any state currently stored in this state scope.
        /// </summary>
        /// <param name="storageKey">The storage key.</param>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DeleteAsync(string storageKey, ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(storageKey))
            {
                throw new ArgumentNullException(nameof(storageKey));
            }

            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            string fullStorageKey = this.GetFullStorageKey(storageKey);

            var cachedState = turnContext.TurnState.Get<CachedState>(fullStorageKey);
            if (cachedState != null)
            {
                turnContext.TurnState.Remove(fullStorageKey);
            }

            await _storage.DeleteAsync(new[] { fullStorageKey }, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// If it has changed, writes to storage the state object that is cached in the current context object for this turn.
        /// </summary>
        /// <param name="storageKey">The storage key.</param>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="force">Optional. True to save state to storage whether or not there are changes.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task SaveChangesAsync(string storageKey, ITurnContext turnContext, bool force = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            string fullStorageKey = this.GetFullStorageKey(storageKey);

            var cachedState = turnContext.TurnState.Get<CachedState>(fullStorageKey);
            if (force || (cachedState != null && cachedState.IsChanged()))
            {
                var changes = new Dictionary<string, object>
                {
                    { fullStorageKey, cachedState.State },
                };

                await _storage.WriteAsync(changes, cancellationToken)
                    .ConfigureAwait(false);
                cachedState.Hash = CachedState.ComputeHash(cachedState.State);
            }
        }

        /// <summary>
        /// Gets a property from the state cache in the turn context.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="storageKey">The storage key.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="turnContext">The context object for this turn.</param>
        public T GetPropertyValue<T>(
            string storageKey,
            string propertyName,
            ITurnContext turnContext)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (storageKey == null)
            {
                throw new ArgumentNullException(nameof(storageKey));
            }

            string fullStorageKey = this.GetFullStorageKey(storageKey);

            var cachedState = turnContext.TurnState.Get<CachedState>(fullStorageKey);

            // if there is no value, this will throw, to signal to IPropertyAccesor that a default value should be computed
            // This allows this to work with value types
            return (T)cachedState.State[propertyName];
        }

        /// <summary>
        /// Set the value of a property in the state cache in the turn context.
        /// </summary>
        /// <param name="storageKey">The property value.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="value">The value to set on the property.</param>
        public void SetPropertyValue<T>(
            string storageKey,
            string propertyName,
            ITurnContext turnContext,
            T value)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (storageKey == null)
            {
                throw new ArgumentNullException(nameof(storageKey));
            }

            string fullStorageKey = this.GetFullStorageKey(storageKey);

            var cachedState = turnContext.TurnState.Get<CachedState>(fullStorageKey);
            cachedState.State[propertyName] = value;
        }

        /// <summary>
        /// Deletes a property from the state cache in the turn context.
        /// </summary>
        /// <param name="storageKey">The storage key.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="turnContext">The context object for this turn.</param>
        public void DeletePropertyValue(
            string storageKey,
            string propertyName,
            ITurnContext turnContext)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (storageKey == null)
            {
                throw new ArgumentNullException(nameof(storageKey));
            }

            var cachedState = turnContext.TurnState.Get<CachedState>(this.GetFullStorageKey(storageKey));
            cachedState.State.Remove(propertyName);
        }

        private string GetFullStorageKey(string key) =>
    !string.IsNullOrEmpty(key) ? $"{_keyPrefix}.{key}" : throw new ArgumentNullException(nameof(key));

        /// <summary>
        /// Internal cached bot state.
        /// </summary>
        private class CachedState
        {
            public CachedState(IDictionary<string, object> state = null)
            {
                this.State = state ?? new ConcurrentDictionary<string, object>();
                this.Hash = ComputeHash(this.State);
            }

            public IDictionary<string, object> State { get; }

            internal string Hash { get; set; }

            public bool IsChanged()
            {
                return this.Hash != ComputeHash(this.State);
            }

            internal static string ComputeHash(object obj)
            {
                return JsonConvert.SerializeObject(obj);
            }
        }


        private class StateValueAccessor<T> : IStateValueAccessor<T>
        {
            private readonly StateAccessor _botState;

            public StateValueAccessor(
                StateAccessor botState,
                string name = nameof(T))
            {
                this.Name = name;
                this._botState = botState;
            }

            /// <inheritdoc />
            public string Name { get; }

            /// <inheritdoc />
            public Task<T> GetAsync(
                string key,
                ITurnContext turnContext,
                Func<T> defaultValueFactory,
                CancellationToken cancellationToken)
            {
                return this.GetAsync(key, turnContext, false, defaultValueFactory, cancellationToken);
            }

            public async Task<T> GetAsync(
                string key,
                ITurnContext turnContext,
                bool force,
                Func<T> defaultValueFactory = null,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                T result = await this.TryGetAsync(key, turnContext, force, cancellationToken);

                if (result == null)
                {
                    if (defaultValueFactory == null)
                    {
                        throw new MissingMemberException("Property not set and no default provided.");
                    }

                    result = defaultValueFactory();

                    // save default value for any further calls
                    await this.SetAsync(key, turnContext, result, force, cancellationToken)
                        .ConfigureAwait(false);
                }

                return result;
            }

            public Task<T> TryGetAsync(
                string key,
                ITurnContext turnContext,
                CancellationToken cancellationToken)
            {
                return this.TryGetAsync(key, turnContext, false, cancellationToken);
            }

            public async Task<T> TryGetAsync(
                string key,
                ITurnContext turnContext,
                bool force,
                CancellationToken cancellationToken)
            {
                await _botState.LoadAsync(key, turnContext, force, cancellationToken)
                    .ConfigureAwait(false);

                try
                {
                    return _botState.GetPropertyValue<T>(
                        key,
                        this.Name,
                        turnContext);
                }
                catch (KeyNotFoundException)
                {
                    return default(T);
                }
            }

            /// <inheritdoc />
            public Task SetAsync(
                string storageKey,
                ITurnContext turnContext,
                T value,
                CancellationToken cancellationToken)
            {
                return this.SetAsync(storageKey, turnContext, value, false, cancellationToken);
            }

            /// <inheritdoc />
            public async Task SetAsync(
                string storageKey,
                ITurnContext turnContext,
                T value,
                bool force,
                CancellationToken cancellationToken)
            {
                await _botState.LoadAsync(storageKey, turnContext, force, cancellationToken).ConfigureAwait(false);
                _botState.SetPropertyValue(storageKey, this.Name, turnContext, value);
                await _botState.SaveChangesAsync(storageKey, turnContext, force, cancellationToken).ConfigureAwait(false);
            }

            /// <inheritdoc />
            public async Task DeleteAsync(
                string key,
                ITurnContext turnContext,
                CancellationToken cancellationToken)
            {
                await _botState.LoadAsync(key, turnContext, false, cancellationToken).ConfigureAwait(false);
                _botState.DeletePropertyValue(key, this.Name, turnContext);
                await _botState.SaveChangesAsync(key, turnContext, false, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
