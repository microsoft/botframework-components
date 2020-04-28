using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models
{
    /// <summary>
    /// Defines a conversation's subscriptions.
    /// Key is subscription id.
    /// Value is subscription object.
    /// </summary>
    public class ConversationSubscriptions<T> : Dictionary<string, T>
    {
    }
}
