# Microsoft.Bot.Components.HelpAndCancel

Dialogs and supporting declarative assets to handle "help" and "cancel" utterances.

## Getting started

Once you've installed the package using [Bot Framework Composer](https://docs.microsoft.com/composer), you'll want to perform the steps outlined below.

### Connect your new dialogs to your root dialog

To add your "CancelDialog" dialog:

1. From the Design tab in Composer, add a new "Intent Recognized" trigger to your root dialog.
1. Name your trigger "Cancel"
1. Add some example utterances for your language model (for example "Cancel", "Quit", "Go back" etc.)
1. Click "Submit" to add the trigger.
1. Click the "+" button, and then "Dialog Management" > "Begin a new dialog".
1. In the "Dialog Name" dropdown select the "CancelDialog" option.

Repeat the above steps for the Help dialog. You may choose to use an "Unknown intent" trigger if you wish to respond to any unknown user utterances with the help dialog.

### Customizing your dialogs

The dialogs contained in this package are examples for handling help and cancel utterances from your users. You'll want to customize their messages and trigger intents to meet your needs.

## Feedback and issues

If you encounter any issues with this package, or would like to share any feedback please open an Issue in our [GitHub repository](https://github.com/microsoft/botframework-components/issues/new/choose).
