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
        public const string DeclarativeType = "Microsoft.Graph.Calendar.RetrievePhoto";

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

        [JsonProperty("email")]
        public StringExpression Email { get; set; }

        [JsonProperty("retrieveMode")]
        public ObjectExpression<RetrieveModeType> RetrieveMode { get; set; }

        public static readonly string DefaultAvatarIconPathFormat = "https://ui-avatars.com/api/?name={0}";

        private IGraphServiceClient _graphClient;

        public enum RetrieveModeType
        {
            Me,
            Other
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            _graphClient = GraphClient.GetAuthenticatedClient(token);

            var retrieveMode = RetrieveMode == null ? RetrieveModeType.Me : RetrieveMode.GetValue(dcState);
            var user = await GetUserByEmailAsync(Email.GetValue(dcState));

            Stream originalPhoto = null;
            string photoUrl = string.Empty;
            try
            {
                if (retrieveMode == RetrieveModeType.Me)
                {
                    originalPhoto = await _graphClient.Me.Photos["64x64"].Content.Request().GetAsync();
                }
                else
                {
                    originalPhoto = await _graphClient.Users[user.Id].Photos["64x64"].Content.Request().GetAsync();
                }
                photoUrl = Convert.ToBase64String(ReadFully(originalPhoto));
            }
            catch (ServiceException)
            {
                return null;
            }

            var results = string.Empty;
            if (string.IsNullOrEmpty(photoUrl))
            {
                results = string.Format(DefaultAvatarIconPathFormat, retrieveMode == RetrieveModeType.Me ? "Me" : user.DisplayName);
            }
            else
            {
                results = string.Format("data:image/jpeg;base64,{0}", photoUrl);
            }
            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(RetrievePhoto), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, results);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
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
