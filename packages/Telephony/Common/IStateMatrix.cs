using System;
using System.Threading.Tasks;

namespace Microsoft.Bot.Components.Telephony.Common
{
    internal enum StateStatus
    {
        /// <summary>
        /// Represents an item that is currently running.
        /// </summary>
        Running,

        /// <summary>
        /// Represents an item that is currently completed.
        /// </summary>
        Completed
    }

    /// <summary>
    /// Represents methods for accessing a state matrix tracking the current status of unique id'd states.
    /// </summary>
    internal interface IStateMatrix
    {
        Task<StateStatus> GetStatusForIdAsync(string id);

        Task CompleteAsync(string id);

        Task StartAsync(string id);

        Task RunForStatusAsync(string id, StateStatus status, Func<Task> action);
    }
}
