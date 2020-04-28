namespace ITSMSkill.Proactive.Subscription
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;

    public interface IStateValueAccessor<T> : IStatePropertyInfo
    {
        /// <summary>
        /// Get the property value from the source.
        /// If the property is not set, and no default value was defined, a <see cref="T:System.MissingMemberException" /> is thrown.
        /// </summary>
        /// <param name="key">The document key.</param>
        /// <param name="turnContext">Turn Context.</param>
        /// <param name="defaultValueFactory">Function which defines the property value to be returned if no value has been set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> representing the result of the asynchronous operation.</returns>
        Task<T> GetAsync(
            string key,
            ITurnContext turnContext,
            Func<T> defaultValueFactory = null,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the property value from the source.
        /// If the property is not set, and no default value was defined, a <see cref="T:System.MissingMemberException" /> is thrown.
        /// </summary>
        /// <param name="key">The document key.</param>
        /// <param name="turnContext">Turn Context.</param>
        /// <param name="force">Get value from storage and skip cache.</param>
        /// <param name="defaultValueFactory">Function which defines the property value to be returned if no value has been set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> representing the result of the asynchronous operation.</returns>
        Task<T> GetAsync(
            string key,
            ITurnContext turnContext,
            bool force,
            Func<T> defaultValueFactory = null,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Tries to get the property value from the source.
        /// If the property is not set, returns <value>null</value>.
        /// </summary>
        /// <param name="key">The document key.</param>
        /// <param name="turnContext">Turn Context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> representing the result of the asynchronous operation.</returns>
        Task<T> TryGetAsync(
            string key,
            ITurnContext turnContext,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tries to get the property value from the source.
        /// If the property is not set, returns <value>null</value>.
        /// </summary>
        /// <param name="key">The document key.</param>
        /// <param name="turnContext">Turn Context.</param>
        /// <param name="force">Get value from storage and skip cache.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> representing the result of the asynchronous operation.</returns>
        Task<T> TryGetAsync(
            string key,
            ITurnContext turnContext,
            bool force,
            CancellationToken cancellationToken);

        /// <summary>Delete the property from the source.</summary>
        /// <param name="key">The document key.</param>
        /// <param name="turnContext">Turn Context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> representing the asynchronous operation.</returns>
        Task DeleteAsync(
            string key,
            ITurnContext turnContext,
            CancellationToken cancellationToken);

        /// <summary>Set the property value on the source.</summary>
        /// <param name="key">The document key.</param>
        /// <param name="turnContext">Turn Context.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> representing the asynchronous operation.</returns>
        Task SetAsync(
            string key,
            ITurnContext turnContext,
            T value,
            CancellationToken cancellationToken);

        /// <summary>Set the property value on the source.</summary>
        /// <param name="key">The document key.</param>
        /// <param name="turnContext">Turn Context.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="forceSave">Force immediate save to db.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> representing the asynchronous operation.</returns>
        Task SetAsync(
            string key,
            ITurnContext turnContext,
            T value,
            bool forceSave,
            CancellationToken cancellationToken);
    }
}
