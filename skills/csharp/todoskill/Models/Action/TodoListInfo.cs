// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace ToDoSkill.Models.Action
{
    public class TodoListInfo
    {
        [JsonProperty("todoList")]
        public List<string> ToDoList { get; set; }

        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
