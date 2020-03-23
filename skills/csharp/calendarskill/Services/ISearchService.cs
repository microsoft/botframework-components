using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarSkill.Models;

namespace CalendarSkill.Services
{
    public interface ISearchService
    {
        Task<List<RoomModel>> GetMeetingRoomAsync(string meetingRoom, string building, int floorNumber = 0);

        Task<List<RoomModel>> GetMeetingRoomAsync(string query, int floorNumber = 0);
    }
}
