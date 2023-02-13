# Custom Question Answering Recognizer

## Summary
This recognizer helps you add a custom recognizer to an empty bot built with Bot Framework Composer in order to use [Azure Custom Question Answering](https://azure.microsoft.com/en-us/products/cognitive-services/question-answering/) in place of the now deprecated QnA Maker.

## Installation
In order to enable the Custom Question Answering recognizer, you must first install the [recognizer package](https://TBD) from npm in your Composer project.

1. Create a new Composer bot using the `Empty Bot` template.

2. Open the Package Manager in Composer.

3. Search for `custom-question-answering-recognizer` and install the package.

## Configuration
To enable the Custom Question Answering recognizer, complete the following steps:

1. Select `Custom` as your root dialog's recognizer type.

2. Paste the following JSON into the custom recognizer configuration window:

```json
{
  "$kind": "Microsoft.CustomQuestionAnsweringRecognizer",
  "hostname": "<your endpoint, including https://>",
  "projectName": "<your project name>",
  "endpointKey": "<your endpoint key>"
}
```
3. Update the `hostname`, `projectName`, and `endpointKey` fields with the values from your Custom Question Answering service.

    - The hostname and endpoint key can be found in the `Keys & Endpoint` blade in the menus for your Azure Language Service in the Azure Portal
    - The project name can be found in your `Language Studio`

Ensure that you have selected the correct values for each field. Using the wrong values can lead to errors when running the bot.

## Usage
Once you have configured question and answer pairs in your Custom Question Answering project, custom intent triggers and the QnA intent triggers should function as normal.

Since the Custom Question Answering recognizer is a modified version of the existing QnAMaker recognizer, the workflow elements (such as multi turn) work the same. In addition the same QnAMaker events and telemetry are written out to the logs.

## Limitations
Please remember that Composer does not integrate natively with Question Answering, so managing question and answer pairs **must** be done in the Language Studio portal instead of Composer.