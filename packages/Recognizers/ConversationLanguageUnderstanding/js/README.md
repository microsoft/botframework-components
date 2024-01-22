# Conversation Language Understanding (CLU) Recognizer

## Summary
This recognizer helps you add a custom recognizer to an empty bot built with Bot Framework Composer in order to use the [Conversation Language Understanding Service](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/conversational-language-understanding/overview) in place of the now deprecated LUIS.

## Installation
In order to enable the CLU recognizer, you must first install the [CLU recognizer package](https://www.npmjs.com/package/clu-recognizer) from NPM in your Composer project. 

1. Create a new Composer bot using the `Empty Bot` template.

2. Open the Package Manager in Composer.

3. Search for `Microsoft.Bot.Components.Recognizers.CLURecognizer` and install the package.

## Configuration
To enable the Conversation Language Understanding recognizer, complete the following steps:

1. Select `Custom` as your root dialog's recognizer type. 

2. Paste the following JSON into the custom recognizer configuration window:

```json
{
  "$kind": "Microsoft.CluRecognizer",
  "projectName": "<your project name>",
  "endpoint": "<your endpoint, including https://>",
  "endpointKey": "<your endpoint key>",
  "deploymentName": "<your deployment name>"
}
```
3. Update the `projectName`, `endpoint`, `endpointKey`, and `deploymentName` fields with the values from your Conversation Language Understanding service.

    - The `deploymentName` value can be found in the `Deploying a model` blade under `Deployments` inside `Language Studio`.
  
    - The `endpoint` value can be found in the `Deploying a model` blade under `Deployments` inside `Language Studio` by clicking on the `Get prediction URL` option. It can also be found in the `Keys & Endpoint` blade of your Language resource in the Azure Portal. The endpoint should take the format `https://<language-resource-name>.cognitiveservices.azure.com`. Ensure that the trailing slash is *omitted*.

    - The `projectName` and `endpointKey` values can be found in your `Project Settings` blade under `Azure Language Resource` inside `Language Studio`.

Ensure that you have selected the correct values for each field. Using the wrong values can lead to errors when running the bot.

## Usage
Once you have configured intents and entities in your Conversation Language Understanding project, custom intent triggers and the CLU intent triggers should function as normal. When creating a new intent trigger in a Composer bot, make sure that the `intent` value of the trigger matches the intent in the deployed Language resource (case-sensitive).

Since the Conversation Language Understanding recognizer is a modified version of the existing LUIS recognizer, the workflow elements work the same. In addition the respective LUIS events and telemetry are written out to the logs.

## Limitations
Please remember that Composer does not integrate natively with Conversation Language Understanding, so managing intents and entities **must** be done in the Language Studio portal instead of Composer.
