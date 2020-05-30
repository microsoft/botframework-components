using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Newtonsoft.Json;
using ToDoSkill.Services;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class AddTask : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.ToDo.AddTask";

        [JsonConstructor]
        public AddTask([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("taskContentProperty")]
        public StringExpression TaskContentProperty { get; set; }

        public const string ListType = "To Do";

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = this.Token.GetValue(dcState);
            var taskContentProperty = this.TaskContentProperty.GetValue(dcState);

            var oneNoteService = new OneNoteService();
            var taskService = await oneNoteService.InitAsync(token, new Dictionary<string, string>());
            var alltasks = await taskService.GetTasksAsync(ListType);
            var newIssues = new List<string>();
            var issueList = JsonConvert.DeserializeObject<List<dynamic>>(taskContentProperty);
            foreach (var issue in issueList)
            {
                var taskContent = string.Format("{0}    {1}", ((string)issue.Id).PadRight(12, ' '), (string)issue.Title);
                if (!alltasks.Exists(x => x.Topic == taskContent))
                {
                    newIssues.Add(taskContent);
                }
            }

            foreach (var newIssue in newIssues)
            {
                await taskService.AddTaskAsync(ListType, newIssue);
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(AddTask), null, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, null);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: null, cancellationToken: cancellationToken);
        }
    }
}
