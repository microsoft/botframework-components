---
name: "Feature spec"
about: Detailed feature definition.
title: ""
labels: ""
assignees: ""
---

## User Story
**As an**  
**I want** 
**so that** 

## Description

## Conversation Design
Links to conversation design diagrams and documents

## Language Understanding
| Intent | Sample Utterances | Prebuilt Entities | Custom Entities | Patterns |
| ------ | ----------------------| ----------------- | ----------------- | --------- |

## Dialogs
Describe the different dialogs to be created as part of the feature and what each will do

## Custom Actions
Describe any custom actions that will need to be created to support the feature
| Name | Input | Output | Notes |
| ------ | ------ | -------- | ------- |

## Channels
Describe any channel specific functionality or limitations that will need to be taken into consideration

## Acceptance Criteria
- [ ] Flow should work in both Direct mode (connecting to the skill bot directly
- [ ] Flow should work in Skill mode (connecting through a VA).
- [ ] Should be deployed in a testing environment for review.
- [ ] Flow should work and render correctly in Teams on Desktop
- [ ] Flow should be tested in Teams for Mac and Teams for Mobile and any compatibility issues should be documented in a comment on this issue
- [ ] Flow should work and render correctly in Web Chat.
- [ ] Flow should work in Direct Line Speech and should follow the design~~ for speech scenarios.
- [ ] Each response (excluding card responses) should have at least 3 variant LG responses.
- [ ] Each response and card should include speak property provided by design team.
- [ ] Help and Cancel interruptions should be implemented and customized for this dialog where relevant
- [ ] Should include supporting unit tests for both custom actions and declarative dialogs.
- [ ] Language model should be tested against other intents to ensure that there is adequate training data to prevent conflicts
- [ ] Supported languages: English 
