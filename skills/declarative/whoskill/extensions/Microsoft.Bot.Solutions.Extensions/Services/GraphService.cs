using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Services
{
    public static class GraphService
    {
        public static async Task<User> GetCurrentUser(string token)
        {
            var graphClient = GraphClientManager.GetAuthenticatedClient(token);

            try
            {
                var result = await graphClient.Me
                       .Request()
                       .Select(x => new
                       {
                           x.BusinessPhones,
                           x.Department,
                           x.DisplayName,
                           x.Id,
                           x.JobTitle,
                           x.Mail,
                           x.MobilePhone,
                           x.OfficeLocation,
                           x.UserPrincipalName
                       })
                       .GetAsync();
                return result;
            }
            catch (ServiceException ex)
            {
                throw GraphClientManager.HandleGraphAPIException(ex);
            }
        }

        public static async Task<IGraphServiceUsersCollectionPage> GetUser(string token, string keyword, int top = 15)
        {
            var graphClient = GraphClientManager.GetAuthenticatedClient(token);

            var filterClause = string.Format(
                "(startswith(displayName,'{0}') or startswith(givenName,'{0}') or startswith(surname,'{0}') or startswith(mail,'{0}') or startswith(userPrincipalName,'{0}'))",
                keyword);
            try
            {
                var result = await graphClient.Users
                       .Request()
                       .Select(x => new
                       {
                           x.BusinessPhones,
                           x.Department,
                           x.DisplayName,
                           x.Id,
                           x.JobTitle,
                           x.Mail,
                           x.MobilePhone,
                           x.OfficeLocation,
                           x.UserPrincipalName
                       })
                       .Filter(filterClause)
                       .Top(top)
                       .GetAsync();
                return result;
            }
            catch (ServiceException ex)
            {
                throw GraphClientManager.HandleGraphAPIException(ex);
            }
        }

        public static async Task<User> GetUser(string token, string id)
        {
            var graphClient = GraphClientManager.GetAuthenticatedClient(token);

            try
            {
                var result = await graphClient.Users[id]
                       .Request()
                       .Select(x => new
                       {
                           x.BusinessPhones,
                           x.Department,
                           x.DisplayName,
                           x.Id,
                           x.JobTitle,
                           x.Mail,
                           x.MobilePhone,
                           x.OfficeLocation,
                           x.UserPrincipalName
                       })
                       .GetAsync();
                return result;
            }
            catch (ServiceException ex)
            {
                throw GraphClientManager.HandleGraphAPIException(ex);
            }
        }

        public static async Task<DirectoryObject> GetManager(string token, string id)
        {
            var graphClient = GraphClientManager.GetAuthenticatedClient(token);

            try
            {
                var result = await graphClient.Users[id]
                       .Manager
                       .Request()
                       .Select("businessPhones,department,displayName,id,jobTitle,mail,mobilePhone,officeLocation,userPrincipalName")
                       .GetAsync();
                return result;
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw GraphClientManager.HandleGraphAPIException(ex);
                }
            }
        }

        public static async Task<IUserDirectReportsCollectionWithReferencesPage> GetDirectReports(string token, string id)
        {
            var graphClient = GraphClientManager.GetAuthenticatedClient(token);

            try
            {
                var result = await graphClient.Users[id]
                    .DirectReports
                    .Request()
                    .Select("businessPhones,department,displayName,id,jobTitle,mail,mobilePhone,officeLocation,userPrincipalName")
                    .GetAsync();
                return result;
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw GraphClientManager.HandleGraphAPIException(ex);
                }
            }
        }

        public static async Task<IUserEventsCollectionPage> GetEvent(string token, int top = 15)
        {
            var graphClient = GraphClientManager.GetAuthenticatedClient(token);

            var orderClause = "createdDateTime desc";
            try
            {
                var result = await graphClient.Me
                     .Events
                     .Request()
                     .Select(x => new
                     {
                         x.Attendees,
                         x.Organizer
                     })
                     .Top(top)
                     .OrderBy(orderClause)
                     .GetAsync();
                return result;
            }
            catch (ServiceException ex)
            {
                throw GraphClientManager.HandleGraphAPIException(ex);
            }
        }

        public static async Task<IUserEventsCollectionPage> GetEvent(string token, string keyword, int top = 15)
        {
            var graphClient = GraphClientManager.GetAuthenticatedClient(token);

            var filterClause = string.Format("contains(subject,'{0}')", keyword);
            var orderClause = "createdDateTime desc";
            try
            {
                var result = await graphClient.Me
                     .Events
                     .Request()
                     .Select(x => new
                     {
                         x.Attendees,
                         x.Organizer
                     })
                     .Filter(filterClause)
                     .OrderBy(orderClause)
                     .Top(top)
                     .GetAsync();
                return result;
            }
            catch (ServiceException ex)
            {
                throw GraphClientManager.HandleGraphAPIException(ex);
            }
        }

        public static async Task<IUserMessagesCollectionPage> GetMessages(string token, int top = 15)
        {
            var graphClient = GraphClientManager.GetAuthenticatedClient(token);

            try
            {
                var result = await graphClient.Me
                     .Messages
                     .Request()
                     .Select(x => new
                     {
                         x.CcRecipients,
                         x.Sender,
                         x.ToRecipients
                     })
                     .Top(top)
                     .GetAsync();
                return result;
            }
            catch (ServiceException ex)
            {
                throw GraphClientManager.HandleGraphAPIException(ex);
            }
        }

        public static async Task<IUserMessagesCollectionPage> GetMessages(string token, string keyword, int top = 15)
        {
            var httpClient = HttpClientManager.GetAuthenticatedClient(token);

            var baseUrl = "https://graph.microsoft.com/v1.0/me/messages";
            var selectClause = "$select=sender,toRecipients,ccRecipients";
            var searchClause = string.Format("$search=\"(body: '{0}' OR subject: '{0}')\"", keyword);
            var topClause = string.Format("$top={0}", top.ToString());
            var requestUrl = baseUrl
                + "?" + "&" + selectClause
                + "&" + searchClause
                + "&" + topClause;

            try
            {
                var request = await httpClient.GetAsync(requestUrl);
                var responseString = await request.Content.ReadAsStringAsync();
                dynamic responseObj = JObject.Parse(responseString);
                var responseValueString = JsonConvert.SerializeObject(responseObj.value);
                var result = JsonConvert.DeserializeObject<IUserMessagesCollectionPage>(responseValueString);
                return result;
            }
            catch (Exception ex)
            {
                throw HttpClientManager.HandleGraphAPIException(ex);
            }
        }

        public static async Task<string> GetPhoto(string token, string id)
        {
            var graphClient = GraphClientManager.GetAuthenticatedClient(token);

            try
            {
                var originalPhoto = await graphClient.Users[id]
                    .Photos["64x64"]
                    .Content
                    .Request()
                    .GetAsync();

                using (MemoryStream ms = new MemoryStream())
                {
                    originalPhoto.CopyTo(ms);
                    var result = string.Format("data:image/jpeg;base64,{0}", Convert.ToBase64String(ms.ToArray()));
                    return result;
                }
            }
            catch (ServiceException ex)
            {
                return null;
            }
        }
    }
}
