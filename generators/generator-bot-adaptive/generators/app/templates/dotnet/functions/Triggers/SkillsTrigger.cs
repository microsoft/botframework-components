using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
namespace <%= botName %>.Triggers
{
    /// <summary>
    /// Functions trigger for Bot Framework Skills messages.
    /// </summary>
    public class SkillsTrigger
    {
        private readonly ChannelServiceHandlerBase _skillHandler;
        private readonly ILogger<SkillsTrigger> _logger;

        public SkillsTrigger(ChannelServiceHandlerBase skillHandler, ILogger<SkillsTrigger> logger)
        {
            this._skillHandler = skillHandler ?? throw new ArgumentNullException(nameof(skillHandler));
            this._logger = logger;
        }

        /// <summary>
        /// Bot Framework ReplyToActivity trigger handling.
        /// </summary>
        /// <param name="req">
        /// The <see cref="HttpRequest"/>.
        /// </param>
        /// <param name="conversationId">
        /// Conversation ID.
        /// </param>
        /// <param name="activityId">
        /// activityId the reply is to.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [FunctionName("ReplyToActivity")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v3/conversations/{conversationId}/activities/{activityId}")] HttpRequest req,
            string conversationId,
            string activityId)
        {
            var body = await req.ReadAsStringAsync().ConfigureAwait(false);
            var activity = JsonConvert.DeserializeObject<Activity>(body, ActivitySerializationSettings.Default);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"RunAsync: conversationId={conversationId}, activityId={activityId}, activity={body}");
            }

            var result = await _skillHandler.HandleReplyToActivityAsync(req.Headers["Authorization"], conversationId, activityId, activity).ConfigureAwait(false);

            return new JsonResult(result, ActivitySerializationSettings.Default);
        }
    }
}
