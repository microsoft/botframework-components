# Bot Framework Functional Tests

This repository contains functional tests for the different [Bot Framework SDK](https://github.com/microsoft/botframework-sdk) tools.

- [Skill functional tests](Tests/SkillFunctionalTests) aims to automate the testing of Host and Skill interactions through several scenarios in all available programming languages.
- [Transcript Test Runner](Libraries/TranscriptTestRunner) aims to simplify complex conversation scenarios against bots, starting from a transcript file with a user-bot recorded conversation.
- [Transcript Converter](Libraries/TranscriptConverter) takes a conversation transcript and converts it into JSON to be consumed by other projects.

Head to [Docs](./Docs/) directory for more information.

## Build Status

 | Branch | Description | Deploy Bot Resources Status | Run Test Scenarios Status |
 |--------|-------------|-----------------------------|---------------------------|
 | Main | 4.14.* Nightly Run | [![Build Status](https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/FunctionalTests/02.A.%20Deploy%20skill%20bots%20(daily)?branchName=main)](https://dev.azure.com/FuseLabs/SDK_v4/_build/latest?definitionId=1229&branchName=main) | [![Build Status](https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/FunctionalTests/02.B.%20Run%20skill%20test%20scenarios%20(daily)?branchName=main)](https://dev.azure.com/FuseLabs/SDK_v4/_build/latest?definitionId=1237&branchName=main)|

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
