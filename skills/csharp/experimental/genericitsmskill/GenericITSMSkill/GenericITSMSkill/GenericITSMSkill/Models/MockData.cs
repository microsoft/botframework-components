using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericITSMSkill.Models
{
    public class MockData
    {
        public string Id;
        public string Title;
        public int Severity;
        public List<string> Mentions;
        public string Status;

        // construct a mock data object with default values.
        // As this is for testing, I create a set of testing mentions here.
        public MockData(string id, string title, int severity, string status)
        {
            Id = id;
            Title = title;
            Severity = severity;
            Mentions = new List<string> { "test1@microsoft.com", "test2@microsoft.com" };
            Status = status;
        }
    }
}
