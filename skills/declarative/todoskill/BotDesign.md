# LU

**note interruptions will conflict with content of task**

## Main Dialog
- Add
- Del
- Show
- Mark
- Cancel
- Help

## Show Dialog
- Input List
- Next
- Previous
- Show ToDo

## Add Dialog
## Del Dialog
## Mark Dialog

# Variable
conversation.listType
- for interruption from show to del
- remember to clear
- pay attention to order

conversation.page
- for interruption from show to del

conversation.taskContent
- for interruption in change task content
- move to dialog? it is not persist between dialogs
- for local interruption, should always repeat to verify logic

containsAll
ordinal

settings.displaySize
settings.interruptScore
settings.intentScore

url

turn.token
turn.listToId
- list name to id in outlook
- assume no deletion

{
    // Index
    Id,
    IsCompleted,
    Topic,
    id,
}

tasks
- all
todos
- for display
items
- for operation
- local cache from tasks

# Flow

When repeat, previous dialog state are cleared

## All

all is handled wrongly when repeat like 'do nothing at all'

support all in first show in mark

# Tests

**if prompt in show, interrupt will not go back**

help, cancel always included

## Add
### Ask content
- add
- show
    - V
- mark
    - V

### Ask list type
- add
- show
    - V
- mark
    - V

### Duplicate
- add
- show
    - V
- mark
    - V

## Show
### Ask list type
- add
    - V
- show
    - not needed here
- mark
    - V

### Ask next/prev
- add
    - V
- show
    - V
- mark
    - V

## Mark
### Ask list type
- add
    - V
- show
    - V
- mark
    - subtle..

### Ask content
- add
    - V
- show
    - V
- mark in another list?
