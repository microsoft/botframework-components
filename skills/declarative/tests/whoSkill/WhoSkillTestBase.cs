// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Bot.Dialogs.Tests.Common;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Graph;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;

    [TestClass]
    public abstract class WhoSkillTestBase<T> : PbxDialogTestBase<T>, IHaveComponentsToInitialize 
        where T : IHaveComponentsToInitialize, new()
    {
        protected List<User> TestUsers
        {
            get;
            private set;
        }

        /// <summary>
        /// Mock test client for graph
        /// </summary>
        private Mock<IGraphServiceClient> testGraphClient = new Mock<IGraphServiceClient>();

        [TestInitialize]
        public void InitializeTest()
        {
            this.testGraphClient = new Mock<IGraphServiceClient>();
            this.TestUsers = new List<User>();

            this.SetupSearch();

            MSGraphClient.RegisterTestInstance(this.testGraphClient.Object);
        }

        public void InitializeComponents()
        {
            //var services = new ServiceCollection();
            //IConfiguration configuration = new ConfigurationBuilder().Build();

            //new GraphBotComponent().ConfigureServices(services, configuration);
        }

        /// <summary>
        /// Creates a new user profile for the test
        /// </summary>
        /// <param name="name"></param>
        /// <param name="email"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="officeLocation"></param>
        /// <param name="jobTitle"></param>
        protected Profile AddUserProfile(string name, string email, string phoneNumber, string officeLocation, string jobTitle, bool addToSearchResult= true)
        {
            Profile profile = new Profile
            {
                Id = Guid.NewGuid().ToString(),
                Names = new ProfileNamesCollectionPage(),
                Positions = new ProfilePositionsCollectionPage(),
                Emails = new ProfileEmailsCollectionPage(),
                Phones = new ProfilePhonesCollectionPage()
            };

            profile.Names.Add(new PersonName() { DisplayName = name });
            profile.Positions.Add(new WorkPosition() { Detail = new PositionDetail() { JobTitle = jobTitle, Company = new CompanyDetail() { OfficeLocation = officeLocation } } });
            profile.Emails.Add(new ItemEmail() { Address = email });
            profile.Phones.Add(new ItemPhone() { Number = phoneNumber });

            if (addToSearchResult)
            {
                this.TestUsers.Add(new User() { DisplayName = name, JobTitle = jobTitle, Id = profile.Id });
            }

            return profile;
        }

        protected void SetupUserRequest(Profile profile, Profile manager = null, IEnumerable<Profile> directReports = null, IEnumerable<Profile> collaborators = null)
        {
            var profileRequest = new Mock<IProfileRequest>();
            profileRequest.Setup(req => req.GetAsync()).ReturnsAsync(profile);
            var profileRequestBuilder = new Mock<IProfileRequestBuilder>();
            profileRequestBuilder.Setup(req => req.Request()).Returns(profileRequest.Object);
            var userRequestBuilder = new Mock<IUserRequestBuilder>();
            userRequestBuilder.SetupGet(req => req.Profile).Returns(profileRequestBuilder.Object);

            // Attach manager
            var managerDirectoryRequest = new Mock<IDirectoryObjectWithReferenceRequest>();

            if (manager != null)
            {
                managerDirectoryRequest.Setup(req => req.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => new DirectoryObject() { Id = manager.Id } );
            }
            else
            {
                // Simulate manager not found
                managerDirectoryRequest.Setup(req => req.GetAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ServiceException(new Error(), null, System.Net.HttpStatusCode.NotFound));
            }

            var managerDirectoryRequestBuilder = new Mock<IDirectoryObjectWithReferenceRequestBuilder>();
            managerDirectoryRequestBuilder.Setup(req => req.Request()).Returns(managerDirectoryRequest.Object);

            userRequestBuilder.SetupGet(req => req.Manager).Returns(managerDirectoryRequestBuilder.Object);

            // Attach direct reports
            var directReportsDirectoryRequest = new Mock<IUserDirectReportsCollectionWithReferencesRequest>();

            if (directReports != null)
            {
                var page = new Mock<IUserDirectReportsCollectionWithReferencesPage>();
                page.SetupGet(p => p.CurrentPage).Returns(() => directReports.Select(r => new DirectoryObject() { Id = r.Id }).ToList());
                directReportsDirectoryRequest.Setup(req => req.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(page.Object);
            }
            else
            {
                directReportsDirectoryRequest.Setup(req => req.GetAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ServiceException(new Error(), null, System.Net.HttpStatusCode.NotFound));
            }

            directReportsDirectoryRequest.Setup(req => req.Top(It.IsAny<int>())).Returns(directReportsDirectoryRequest.Object);

            var directReportsDirectoryRequestBuilder = new Mock<IUserDirectReportsCollectionWithReferencesRequestBuilder>();
            directReportsDirectoryRequestBuilder.Setup(req => req.Request()).Returns(directReportsDirectoryRequest.Object);
            userRequestBuilder.SetupGet(req => req.DirectReports).Returns(directReportsDirectoryRequestBuilder.Object);

            // Attach collaborators
            var peopleDirectoryRequest = new Mock<IUserPeopleCollectionRequest>();
            peopleDirectoryRequest.Setup(req => req.Top(It.IsAny<int>())).Returns(peopleDirectoryRequest.Object);
            peopleDirectoryRequest.Setup(req => req.Filter(It.IsAny<string>())).Returns(peopleDirectoryRequest.Object);

            if (collaborators != null)
            {
                var page = new Mock<IUserPeopleCollectionPage>();
                page.SetupGet(p => p.CurrentPage).Returns(() => collaborators.Select(r => new Person() { Id = r.Id }).ToList());
                peopleDirectoryRequest.Setup(req => req.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(page.Object);
            }
            else
            {
                peopleDirectoryRequest.Setup(req => req.GetAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ServiceException(new Error(), null, System.Net.HttpStatusCode.NotFound));
            }

            var peopleDirectoryRequestBuilder = new Mock<IUserPeopleCollectionRequestBuilder>();
            peopleDirectoryRequestBuilder.Setup(req => req.Request()).Returns(peopleDirectoryRequest.Object);
            userRequestBuilder.SetupGet(req => req.People).Returns(peopleDirectoryRequestBuilder.Object);

            // Setup photo to say not found
            var photoContentRequest = new Mock<IProfilePhotoContentRequest>();
            // HACKHACK: Force the result to be notfound to simply load the standard icon
            photoContentRequest.Setup(req => req.GetAsync(It.IsAny<CancellationToken>(), It.IsAny<HttpCompletionOption>())).ThrowsAsync(new ServiceException(new Error(), null, System.Net.HttpStatusCode.NotFound));
            var photoContentRequestBuilder = new Mock<IProfilePhotoContentRequestBuilder>();
            photoContentRequestBuilder.Setup(req => req.Request(It.IsAny<IEnumerable<Option>>())).Returns(photoContentRequest.Object);
            var photoRequestBuilder = new Mock<IProfilePhotoRequestBuilder>();
            photoRequestBuilder.SetupGet(req => req.Content).Returns(photoContentRequestBuilder.Object);
            userRequestBuilder.SetupGet(req => req.Photo).Returns(photoRequestBuilder.Object);

            this.testGraphClient.SetupGet(client => client.Users[profile.Id]).Returns(userRequestBuilder.Object);
        }

        /// <summary>
        /// Sets up the search scenario
        /// </summary>
        private void SetupSearch()
        {
            var usersSearchResultPage = new Mock<IGraphServiceUsersCollectionPage>();
            usersSearchResultPage.SetupGet(page => page.CurrentPage).Returns(() => this.TestUsers); 
            
            var usersSearchRequest = new Mock<IGraphServiceUsersCollectionRequest>();
            usersSearchRequest.Setup(req => req.Top(It.IsAny<int>())).Returns(usersSearchRequest.Object);
            usersSearchRequest.Setup(req => req.Filter(It.IsAny<string>())).Returns(usersSearchRequest.Object);
            usersSearchRequest.Setup(req => req.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(usersSearchResultPage.Object);

            var usersSearchRequestBuilder = new Mock<IGraphServiceUsersCollectionRequestBuilder>();
            usersSearchRequestBuilder.Setup(req => req.Request()).Returns(usersSearchRequest.Object);

            this.testGraphClient.SetupGet(client => client.Users).Returns(usersSearchRequestBuilder.Object);
        }
    }
}
