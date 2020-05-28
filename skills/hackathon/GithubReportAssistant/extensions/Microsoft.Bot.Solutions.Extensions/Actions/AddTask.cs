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
            await taskService.AddTaskAsync(ListType, taskContentProperty);
            var alltasks = await taskService.GetTasksAsync(ListType);

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(AddTask), alltasks, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, alltasks);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: alltasks, cancellationToken: cancellationToken);
        }
    }
}
