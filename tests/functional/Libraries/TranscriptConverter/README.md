# Transcript Converter

## User Step-by-step Guide
This step-by-step guide shows how to run the TranscriptConverter project to convert a transcript to a test script to be used in TranscriptTestRunner.

## Generate a test script
1- Create a transcript file, follow [these steps](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-debug-transcript?view=azure-bot-service-4.0#creatingstoring-a-bot-transcript-file).

2- Build the TranscriptConverter project and navigate to its executable.

3- The command to convert a transcript to a new test script can be executed like this:
```PS
btc convert "path-to-source-transcript"
```
You can convert a transcript to an existing test script like this:
```PS
btc convert "path-to-source-transcript" "path-to-target-test-script"
```
