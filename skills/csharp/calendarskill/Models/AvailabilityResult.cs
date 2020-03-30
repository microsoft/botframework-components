// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace CalendarSkill.Models
{
    public class AvailabilityResult
    {
        public List<string> AvailabilityViewList { get; set; } = new List<string>();

        public List<EventModel> MySchedule { get; set; } = new List<EventModel>();
    }
}
