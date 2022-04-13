using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Components.Telephony.Common
{
    /// <summary>
    /// Represents methods for accessing a state matrix tracking the current status of unique id'd states.
    /// </summary>
    internal class LatchingStateMatrix : IStateMatrix
    {
        //I don't love the tuple version because we have to reretrieve values sometimes. The other option is to use two concurent dictionaries and keep their keys in sync
        private static ConcurrentDictionary<string, (SemaphoreSlim, StateStatus)> perStateIndexLatching = new ConcurrentDictionary<string, (SemaphoreSlim, StateStatus)>();

        public async Task CompleteAsync(string id)
        {
            (SemaphoreSlim semaphore, _) = perStateIndexLatching[id];
            try
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                perStateIndexLatching[id] = (semaphore, StateStatus.Completed);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void ForceComplete(string id)
        {
            (SemaphoreSlim semaphore, _) = perStateIndexLatching[id];
            try
            {
                perStateIndexLatching[id] = (semaphore, StateStatus.Completed);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<StateStatus> GetStatusForIdAsync(string id)
        {
            (SemaphoreSlim semaphore, _) = perStateIndexLatching[id];
            try
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                //retrieve status again, since we got it wrong the first time XD
                (_, StateStatus status) = perStateIndexLatching[id];
                return status;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<T> RunForStatusAsync<T>(string id, StateStatus status, Func<Task<T>> matchedAction, Func<Task<T>> notMatchedAction)
        {
            (SemaphoreSlim semaphore, _) = perStateIndexLatching[id];
            try
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                //retrieve status again, since we got it wrong the first time XD
                (_, StateStatus updatedStatus) = perStateIndexLatching[id];
                if (updatedStatus == status)
                    return await matchedAction().ConfigureAwait(false);
                else return await notMatchedAction().ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public Task StartAsync(string id)
        {
            if (perStateIndexLatching.ContainsKey(id))
            {
                throw new Exception("We have already started that id. States must not be duplicated or something.");
            }

            perStateIndexLatching[id] = (new SemaphoreSlim(1), StateStatus.Running);
            return Task.CompletedTask;
        }
    }
}
