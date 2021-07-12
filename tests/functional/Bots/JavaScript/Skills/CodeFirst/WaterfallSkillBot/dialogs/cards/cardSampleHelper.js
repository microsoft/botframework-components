// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { CardFactory, ActionTypes } = require('botbuilder');

class CardSampleHelper {
  static createAdaptiveCardBotAction () {
    return CardFactory.adaptiveCard({
      type: 'AdaptiveCard',
      version: '1.2',
      body: [
        {
          text: 'Bot Builder actions',
          type: 'TextBlock'
        }
      ],
      actions: [
        {
          type: 'Action.Submit',
          title: 'imBack',
          data: {
            msteams: {
              type: 'imBack',
              value: 'text'
            }
          }
        },
        {
          type: 'Action.Submit',
          title: 'message back',
          data: {
            msteams: {
              type: ActionTypes.MessageBack,
              value: {
                key: 'value'
              }
            }
          }
        },
        {
          type: 'Action.Submit',
          title: 'message back local echo',
          data: {
            msteams: {
              type: ActionTypes.MessageBack,
              text: 'text received by bots',
              displayText: 'display text message back',
              value: {
                key: 'value'
              }
            }
          }
        },
        {
          type: 'Action.Submit',
          title: 'invoke',
          data: {
            msteams: {
              type: 'invoke',
              value: {
                key: 'value'
              }
            }
          }
        }
      ]
    });
  }

  static createAdaptiveCardTaskModule () {
    return CardFactory.adaptiveCard({
      type: 'AdaptiveCard',
      version: '1.2',
      body: [
        {
          type: 'TextBlock',
          text: 'Task Module Adaptive Card'
        }
      ],
      actions: [
        {
          type: 'Action.Submit',
          title: 'Launch Task Module',
          data: {
            msteams: {
              type: 'invoke',
              value: '{\r\n  "hiddenKey": "hidden value from task module launcher",\r\n  "type": "task/fetch"\r\n}'
            }
          }
        }
      ]
    });
  }

  static createAdaptiveCardSubmit () {
    return CardFactory.adaptiveCard({
      type: 'AdaptiveCard',
      version: '1.2',
      body: [
        {
          type: 'TextBlock',
          text: 'Bot Builder actions'
        },
        {
          type: 'Input.Text',
          id: 'x'
        }
      ],
      actions: [
        {
          type: 'Action.Submit',
          title: 'Action.Submit',
          data: {
            key: 'value'
          }
        }
      ]
    });
  }

  static createHeroCard () {
    const { title, subtitle, text, images, buttons } = {
      title: 'BotFramework Hero Card',
      subtitle: 'Microsoft Bot Framework',
      text: 'Build and connect intelligent bots to interact with your users naturally wherever they are, from text/sms to Skype, Slack, Office 365 mail and other popular services.',
      images: [
        {
          url: 'https://sec.ch9.ms/ch9/7ff5/e07cfef0-aa3b-40bb-9baa-7c9ef8ff7ff5/buildreactionbotframework_960.jpg'
        }
      ],
      buttons: [
        {
          type: ActionTypes.OpenUrl,
          title: 'Get Started',
          value: 'https://docs.microsoft.com/bot-framework'
        }
      ]
    };

    return CardFactory.heroCard(title, images, buttons, { subtitle, text });
  }

  static createThumbnailCard () {
    const { title, subtitle, text, images, buttons } = {
      title: 'BotFramework Thumbnail Card',
      subtitle: 'Microsoft Bot Framework',
      text: 'Build and connect intelligent bots to interact with your users naturally wherever they are, from text/sms to Skype, Slack, Office 365 mail and other popular services.',
      images: [
        {
          url: 'https://sec.ch9.ms/ch9/7ff5/e07cfef0-aa3b-40bb-9baa-7c9ef8ff7ff5/buildreactionbotframework_960.jpg'
        }
      ],
      buttons: [
        {
          type: ActionTypes.OpenUrl,
          title: 'Get Started',
          value: 'https://docs.microsoft.com/bot-framework'
        }
      ]
    };

    return CardFactory.thumbnailCard(title, images, buttons, { subtitle, text });
  }

  static createReceiptCard () {
    return CardFactory.receiptCard({
      title: 'John Doe',
      facts: [
        {
          key: 'Order Number',
          value: '1234'
        },
        {
          key: 'Payment Method',
          value: 'VISA 5555-****'
        }
      ],
      items: [
        {
          title: 'Data Transfer',
          image: {
            url: 'https://github.com/amido/azure-vector-icons/raw/master/renders/traffic-manager.png'
          },
          price: '$ 38.45',
          quantity: '368'
        },
        {
          title: 'App Service',
          image: {
            url: 'https://github.com/amido/azure-vector-icons/raw/master/renders/cloud-service.png'
          },
          price: '$ 45.00',
          quantity: '720'
        }
      ],
      total: '$ 90.95',
      tax: '$ 7.50',
      buttons: [
        {
          type: ActionTypes.OpenUrl,
          title: 'More information',
          image: 'https://account.windowsazure.com/content/6.10.1.38-.8225.160809-1618/aux-pre/images/offer-icon-freetrial.png',
          value: 'https://azure.microsoft.com/en-us/pricing/'
        }
      ]
    });
  }

  static createSigninCard () {
    return CardFactory.signinCard('Sign-in', 'https://login.microsoftonline.com/', 'BotFramework Sign-in Card');
  }

  static createO365ConnectorCard () {
    return CardFactory.o365ConnectorCard({
      title: 'card title',
      text: 'card text',
      summary: 'O365 card summary',
      themeColor: '#E67A9E',
      sections: [
        {
          title: '**section title**',
          text: 'section text',
          activityTitle: 'activity title',
          activitySubtitle: 'activity subtitle',
          activityText: 'activity text',
          activityImage: 'http://connectorsdemo.azurewebsites.net/images/MSC12_Oscar_002.jpg',
          activityImageType: 'avatar',
          markdown: true,
          facts: [
            {
              name: 'Fact name 1',
              value: 'Fact value 1'
            },
            {
              name: 'Fact name 2',
              value: 'Fact value 2'
            }
          ],
          images: [
            {
              image: 'http://connectorsdemo.azurewebsites.net/images/MicrosoftSurface_024_Cafe_OH-06315_VS_R1c.jpg',
              title: 'image 1'
            },
            {
              image: 'http://connectorsdemo.azurewebsites.net/images/WIN12_Scene_01.jpg',
              title: 'image 2'
            },
            {
              image: 'http://connectorsdemo.azurewebsites.net/images/WIN12_Anthony_02.jpg',
              title: 'image 3'
            }
          ]
        }
      ],
      potentialAction:
                [
                  {
                    '@type': 'ActionCard',
                    inputs: [
                      {
                        '@type': 'MultichoiceInput',
                        choices: [
                          {
                            display: 'Choice 1',
                            value: '1'
                          },
                          {
                            display: 'Choice 2',
                            value: '2'
                          },
                          {
                            display: 'Choice 3',
                            value: '3'
                          }
                        ],
                        style: 'expanded',
                        isMultiSelect: true,
                        id: 'list-1',
                        isRequired: true,
                        title: 'Pick multiple options'
                      },
                      {
                        '@type': 'MultichoiceInput',
                        choices: [
                          {
                            display: 'Choice 4',
                            value: '4'
                          },
                          {
                            display: 'Choice 5',
                            value: '5'
                          },
                          {
                            display: 'Choice 6',
                            value: '6'
                          }
                        ],
                        style: 'compact',
                        isMultiSelect: true,
                        id: 'list-2',
                        isRequired: true,
                        title: 'Pick multiple options'
                      },
                      {
                        '@type': 'MultichoiceInput',
                        choices: [
                          {
                            display: 'Choice a',
                            value: 'a'
                          },
                          {
                            display: 'Choice b',
                            value: 'b'
                          },
                          {
                            display: 'Choice c',
                            value: 'c'
                          }
                        ],
                        style: 'expanded',
                        isMultiSelect: false,
                        id: 'list-3',
                        isRequired: false,
                        title: 'Pick an option'
                      },
                      {
                        '@type': 'MultichoiceInput',
                        choices: [
                          {
                            display: 'Choice x',
                            value: 'x'
                          },
                          {
                            display: 'Choice y',
                            value: 'y'
                          },
                          {
                            display: 'Choice z',
                            value: 'z'
                          }
                        ],
                        style: 'compact',
                        isMultiSelect: false,
                        id: 'list-4',
                        isRequired: false,
                        title: 'Pick an option'
                      }
                    ],
                    actions: [
                      {
                        '@type': 'HttpPOST',
                        body: '{"list1":"{{list-1.value}}", "list2":"{{list-2.value}}", "list3":"{{list-3.value}}", "list4":"{{list-4.value}}"}',
                        name: 'Send',
                        '@id': 'card-1-btn-1'
                      }
                    ],
                    name: 'Multiple Choice',
                    '@id': 'card-1'
                  },
                  {
                    '@type': 'ActionCard',
                    inputs: [
                      {
                        '@type': 'TextInput',
                        isMultiline: true,
                        id: 'text-1',
                        isRequired: false,
                        title: 'multiline, no maxLength'
                      },
                      {
                        '@type': 'TextInput',
                        isMultiline: false,
                        id: 'text-2',
                        isRequired: false,
                        title: 'single line, no maxLength'
                      },
                      {
                        '@type': 'TextInput',
                        isMultiline: true,
                        maxLength: 10,
                        id: 'text-3',
                        isRequired: true,
                        title: 'multiline, max len = 10, isRequired'
                      },
                      {
                        '@type': 'TextInput',
                        isMultiline: false,
                        maxLength: 10,
                        id: 'text-4',
                        isRequired: true,
                        title: 'single line, max len = 10, isRequired'
                      }
                    ],
                    actions: [
                      {
                        '@type': 'HttpPOST',
                        body: '{"text1":"{{text-1.value}}", "text2":"{{text-2.value}}", "text3":"{{text-3.value}}", "text4":"{{text-4.value}}"}',
                        name: 'Send',
                        '@id': 'card-2-btn-1'
                      }
                    ],
                    name: 'Text Input',
                    '@id': 'card-2'
                  },
                  {
                    '@type': 'ActionCard',
                    inputs: [
                      {
                        '@type': 'DateInput',
                        includeTime: true,
                        id: 'date-1',
                        isRequired: true,
                        title: 'date with time'
                      },
                      {
                        '@type': 'DateInput',
                        includeTime: false,
                        id: 'date-2',
                        isRequired: false,
                        title: 'date only'
                      }
                    ],
                    actions: [
                      {
                        '@type': 'HttpPOST',
                        body: '{"date1":"{{date-1.value}}", "date2":"{{date-2.value}}"}',
                        name: 'Send',
                        '@id': 'card-3-btn-1'
                      }
                    ],
                    name: 'Date Input',
                    '@id': 'card-3'
                  },
                  {
                    '@type': 'ViewAction',
                    target: [
                      'http://microsoft.com'
                    ],
                    name: 'View Action'
                  },
                  {
                    '@type': 'OpenUri',
                    targets: [
                      {
                        os: 'default',
                        uri: 'http://microsoft.com'
                      },
                      {
                        os: 'iOS',
                        uri: 'http://microsoft.com'
                      },
                      {
                        os: 'android',
                        uri: 'http://microsoft.com'
                      },
                      {
                        os: 'windows',
                        uri: 'http://microsoft.com'
                      }
                    ],
                    name: 'Open Uri',
                    '@id': 'open-uri'
                  }
                ]

    });
  }

  static createTeamsFileConsentCard (filename, filesize) {
    const consentContext = { filename };

    return {
      contentType: 'application/vnd.microsoft.teams.card.file.consent',
      name: filename,
      content: {
        description: 'This is the file I want to send you',
        sizeInbytes: filesize,
        acceptContext: consentContext,
        declineContext: consentContext
      }
    };
  }

  static createAnimationCard (url) {
    return CardFactory.animationCard('Animation Card', [{ url }], null, { autostart: true });
  }

  static createAudioCard (url) {
    return CardFactory.audioCard('Audio Card', [{ url }], null, { autoloop: true });
  }

  static createVideoCard (url) {
    return CardFactory.videoCard('Video Card', [{ url }]);
  }

  static createUpdateAdaptiveCard () {
    const { title, text, buttons } = {
      title: 'Update card',
      text: 'Update Card Action',
      buttons: [
        {
          type: ActionTypes.MessageBack,
          title: 'Update card title',
          text: 'Update card text',
          value: { count: 0 }
        }
      ]
    };

    return CardFactory.heroCard(title, text, null, buttons);
  }
}

module.exports.CardSampleHelper = CardSampleHelper;
