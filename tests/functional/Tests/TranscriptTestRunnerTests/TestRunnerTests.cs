// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Moq;
using TranscriptTestRunner;
using Xunit;

namespace TranscriptTestRunnerTests
{
    public class TestRunnerTests
    {
        [Fact]
        public void CreateTestRunnerTest()
        {
            var testClientMock = new Mock<TestClientBase>();
            var testRunner = new TestRunner(testClientMock.Object);

            Assert.NotNull(testRunner);
        }
    }
}
