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
    - ask for list in add -> delete in an empty list -> add in that list without prompt
- pay attention to order

conversation.page
- for interruption from show to del

dialog.taskContent
- for local interruption, should always repeat to verify logic

containsAll
ordinal

currentInput
- indicate current input for GetInput

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

all is tricky for show after delete

find better api for deleting all

## Ordinal

Also tricky

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

# ToDo

all is handled wrongly when repeat like 'do nothing at all'

support all in GetInput in mark/delete
