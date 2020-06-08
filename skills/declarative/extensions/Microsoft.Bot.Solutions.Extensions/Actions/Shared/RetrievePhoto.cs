using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class RetrievePhoto : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.RetrievePhoto";

        [JsonConstructor]
        public RetrievePhoto([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("userId")]
        public StringExpression UserId { get; set; }

        private IGraphServiceClient _graphClient;

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            _graphClient = GraphClient.GetAuthenticatedClient(token);

            Stream originalPhoto = null;
            string photoUrl = string.Empty;
            try
            {
                if (UserId != null)
                {
                    originalPhoto = await _graphClient.Users[UserId.GetValue(dcState)].Photos["64x64"].Content.Request().GetAsync();
                    photoUrl = Convert.ToBase64String(ReadFully(originalPhoto));
                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        photoUrl = string.Format("data:image/jpeg;base64,{0}", photoUrl);
                    }
                }
            }
            catch (ServiceException)
            {
                photoUrl = null;
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(RetrievePhoto), photoUrl, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, photoUrl);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: photoUrl , cancellationToken: cancellationToken);
        }

        private byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            var optionList = new List<QueryOption>();
            var filterString = $"startswith(mail,'{email}')";
            optionList.Add(new QueryOption("$filter", filterString));

            IGraphServiceUsersCollectionPage users = null;
            try
            {
                users = await _graphClient.Users.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            if (users.Count > 0)
            {
                return users[0];
            }

            return null;
        }
    }
}
