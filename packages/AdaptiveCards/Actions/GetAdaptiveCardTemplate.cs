// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Components.AdaptiveCards
{
    public class GetAdaptiveCardTemplate : Dialog
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Bot.Components.GetAdaptiveCardTemplate";

        private const int DefaultCachePeriod = 900;

        // TODO: Migrate to using MemoryCache class.
        private static ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>();
        private static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAdaptiveCardTemplate"/> class.
        /// </summary>
        /// <param name="callerPath">Optional, source file full path.</param>
        /// <param name="callerLine">Optional, line number in source file.</param>
        public GetAdaptiveCardTemplate([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Gets or sets an optional expression which if is true will disable this action.
        /// </summary>
        /// <example>
        /// "user.age > 18".
        /// </example>
        /// <value>
        /// A boolean expression. 
        /// </value>
        [JsonProperty("disabled")]
        public BoolExpression Disabled { get; set; }

        [JsonProperty("location")]
        public StringExpression Location { get; set; }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        [JsonProperty("cacheTemplate")]
        public BoolExpression CacheTemplate { get; set; } = true;

        [JsonProperty("cacheExpiration")]
        public IntExpression CacheExpiration { get; set; } = DefaultCachePeriod;

        /// <summary>
        /// Called when the dialog is started and pushed onto the dialog stack.
        /// </summary>
        /// <param name="dc">The <see cref="DialogContext"/> for the current turn of conversation.</param>
        /// <param name="options">Optional, initial information to pass to the dialog.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            if (this.Disabled != null && this.Disabled.GetValue(dc.State) == true)
            {
                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            var url = this.Location?.GetValue(dc.State);
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception($"{this.Id}: a template url wasn't provided.");
            }

            JObject template = null;
            var cache = this.CacheTemplate?.GetValue(dc.State);
            if (cache == true)
            {
                if (TryGetTemplateFromCache(url, out template))
                {
                    var expiration = this.CacheExpiration?.GetValue(dc.State) ?? DefaultCachePeriod;
                    template = await FetchTemplateAsync(dc, url, cancellationToken).ConfigureAwait(false);
                    AddTemplateToCache(url, template, expiration);
                }
            }
            else
            {
                template = await FetchTemplateAsync(dc, url, cancellationToken).ConfigureAwait(false);
            }

            var resultProp = this.ResultProperty?.GetValue(dc.State);
            if (!string.IsNullOrEmpty(resultProp))
            {
                dc.State.SetValue(resultProp, template);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: template, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Builds the compute Id for the dialog.
        /// </summary>
        /// <returns>A string representing the compute Id.</returns>
        protected override string OnComputeId()
        {
            return $"{this.GetType().Name}({Location?.ToString()})";
        }

#pragma warning disable CA1801 // Review unused parameters. Protected method already shipped, we're leaving dc there.
        protected async Task<JObject> FetchTemplateAsync(DialogContext dc, string url, CancellationToken cancellationToken = default(CancellationToken))
#pragma warning restore CA1801 // Review unused parameters
        {
            // Fetch template
            JObject template;
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{this.Id}: '{(int)response.StatusCode}' error fetching template: {response.ReasonPhrase}");
            }

            // Parse template
            try
            {
                template = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            catch (Exception err)
            {
                throw new Exception($"{this.Id}: error parsing template: {err.Message}");
            }

            return template;
        }

        private static bool TryGetTemplateFromCache(string url, out JObject template)
        {
            PurgeCache();
            CacheEntry entry;
            if (cache.TryGetValue(url, out entry))
            {
                template = entry.Template;
            }
            else
            {
                template = null;
            }

            return template != null;
        }

        private static void AddTemplateToCache(string url, JObject template, int expiration)
        {
            var entry = new CacheEntry()
            {
                Url = url,
                Template = template,
                Expiration = DateTimeOffset.UtcNow.AddSeconds(expiration).UtcDateTime
            };
            cache.TryAdd(url, entry);
        }

        private static void PurgeCache()
        {
            var entries = cache.Values;
            foreach (var entry in entries)
            {
                if (entry.Expiration < DateTime.UtcNow)
                {
                    CacheEntry removed;
                    cache.TryRemove(entry.Url, out removed);
                }
            }
        }

        private class CacheEntry
        {
            public string Url { get; set; }

            public JObject Template { get; set; }

            public DateTime Expiration { get; set; }
        }
    }
}
