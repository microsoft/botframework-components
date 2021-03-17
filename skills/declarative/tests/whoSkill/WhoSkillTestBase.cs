// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Bot.Dialogs.Tests.Common;
    using Microsoft.Graph;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;

    [TestClass]
    public abstract class WhoSkillTestBase : PbxDialogTestBase
    {
        /// <inheritdoc />
        protected override string RelativeRootFolder => Path.Combine(GetProjectPath(), @"..\..\whoSkill");

        protected List<User> TestUsers
        {
            get;
            private set;
        }

        protected Dictionary<string, Profile> TestProfiles
        {
            get;
            private set;
        }

        private Mock<IGraphServiceClient> testGraphClient;

        /// <inheritdoc />
        protected override void InitializeTest()
        {
            ComponentRegistration.Add(new GraphComponentRegistration());

            this.testGraphClient = new Mock<IGraphServiceClient>();
            this.TestUsers = new List<User>();
            this.TestProfiles = new Dictionary<string, Profile>(StringComparer.OrdinalIgnoreCase);

            this.SetupSearch();

            MSGraphClient.RegisterTestInstance(this.testGraphClient.Object);
        }

        /// <summary>
        /// Creates a new user profile for the test
        /// </summary>
        /// <param name="name"></param>
        /// <param name="email"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="officeLocation"></param>
        /// <param name="jobTitle"></param>
        protected void AddUserProfile(string name, string email, string phoneNumber, string officeLocation, string jobTitle)
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

            this.TestUsers.Add(new User() { DisplayName = name, JobTitle = jobTitle, Id = profile.Id });
            this.TestProfiles.Add(profile.Id, profile);

            this.SetupUserRequest(profile);
        }

        private void SetupUserRequest(Profile profile)
        {
            var profileRequest = new Mock<IProfileRequest>();
            profileRequest.Setup(req => req.GetAsync()).ReturnsAsync(profile);
            var profileRequestBuilder = new Mock<IProfileRequestBuilder>();
            profileRequestBuilder.Setup(req => req.Request()).Returns(profileRequest.Object);
            var userRequestBuilder = new Mock<IUserRequestBuilder>();
            userRequestBuilder.SetupGet(req => req.Profile).Returns(profileRequestBuilder.Object);

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
