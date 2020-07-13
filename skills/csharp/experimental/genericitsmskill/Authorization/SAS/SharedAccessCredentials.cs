using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericITSMSkill.Authorization.SAS
{
    public class SharedAccessCredentials
    {
        /// <summary>
        /// The SAS start time query parameter.
        /// </summary>
        public const string SasStartTimeQueryParameter = "st";

        /// <summary>
        /// The SAS expire time query parameter.
        /// </summary>
        public const string SasExpireTimeQueryParameter = "se";

        /// <summary>
        /// The SAS permissions query parameter.
        /// </summary>
        public const string SasPermissionsQueryParameter = "sp";

        /// <summary>
        /// The SAS version query parameter.
        /// </summary>
        public const string SasVersionQueryParamater = "sv";

        /// <summary>
        /// The SAS signature query parameter.
        /// </summary>
        public const string SasSignatureQueryParameter = "sig";

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessCredentials" /> class.
        /// </summary>
        /// <param name="policy">The policy.</param>
        /// <param name="signature">The signature.</param>
        public SharedAccessCredentials(SharedAccessPolicy policy, string signature)
        {
            this.Policy = policy;
            this.Signature = signature;
        }

        /// <summary>
        /// Gets the shared access policy.
        /// </summary>
        public SharedAccessPolicy Policy { get; private set; }

        /// <summary>
        /// Gets the shared access signature.
        /// </summary>
        public string Signature { get; private set; }
    }
}
