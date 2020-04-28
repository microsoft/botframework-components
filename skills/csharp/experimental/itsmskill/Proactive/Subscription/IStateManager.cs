using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Proactive.Subscription
{
    public interface IStateManager
    {
        /// <summary>
        /// Create a managed state property accessor for named property.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <returns>property accessor for accessing the object of type T.</returns>
        IStateValueAccessor<T> CreateProperty<T>();
    }
}
