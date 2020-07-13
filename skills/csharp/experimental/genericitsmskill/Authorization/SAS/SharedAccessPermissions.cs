using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericITSMSkill.Authorization.SAS
{
    /// <summary>
    /// Shared access permissions.
    /// </summary>
    public class SharedAccessPermissions
    {
        /// <summary>
        /// The SAS permission wild card action.
        /// </summary>
        public const string SasPermissionWildcardAction = "*";

        /// <summary>
        /// The SAS permission read action.
        /// </summary>
        public const string SasPermissionReadAction = "read";

        /// <summary>
        /// The SAS permission write action.
        /// </summary>
        public const string SasPermissionWriteAction = "write";

        /// <summary>
        /// The SAS permission delete action.
        /// </summary>
        public const string SasPermissionDeleteAction = "delete";

        /// <summary>
        /// The SAS permission run action.
        /// </summary>
        public const string SasPermissionRunAction = "run";

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessPermissions" /> class.
        /// </summary>
        /// <param name="permissions">The permissions.</param>
        private SharedAccessPermissions(params string[] permissions)
        {
            this.Permissions = permissions;
        }

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        private string[] Permissions { get; set; }

        /// <summary>
        /// Validates whether scope is permitted.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="action">The action.</param>
        public bool IsScopePermitted(string scope, string action)
        {
            return this.Permissions.Any(permission =>
            {
                var permissionSegments = permission.Split('/', StringSplitOptions.RemoveEmptyEntries);

                var permissionScope = string.Concat(
                    "/",
                    string.Join("/", permissionSegments.Take(permissionSegments.Length - 1)),
                    permissionSegments.Length > 1 ? "/" : string.Empty);

                var permissionAction = permissionSegments.Last();

                return
                    scope.StartsWith(permissionScope, StringComparison.InvariantCultureIgnoreCase) &&
                    (permissionAction.Equals(SasPermissionWildcardAction) || action.Equals(permissionAction, StringComparison.OrdinalIgnoreCase));
            });
        }

        /// <summary>
        /// Converts to string representation.
        /// </summary>
        public override string ToString()
        {
            return SerializePermissions(this.Permissions);
        }

        /// <summary>
        /// Converts from string representation.
        /// </summary>
        /// <param name="input">The input.</param>
        public static SharedAccessPermissions FromString(string input)
        {
            return new SharedAccessPermissions(permissions: DeserializePermissions(input)
                .Where(permission => !string.IsNullOrEmpty(permission))
                .ToArray());
        }

        /// <summary>
        /// Converts from scope and action.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="action">The action.</param>
        public static SharedAccessPermissions FromScopeAndAction(string scope, string action)
        {
            return FromScopeAndActions(scope: scope, actions: action);
        }

        /// <summary>
        /// Converts from scope and actions.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="actions">The actions.</param>
        public static SharedAccessPermissions FromScopeAndActions(string scope, params string[] actions)
        {
            return new SharedAccessPermissions(permissions: actions.Select(action => string.Format(
                format: "/{0}/{1}",
                arg0: scope.Trim('/'),
                arg1: action.Trim('/')))
                .ToArray());
        }

        /// <summary>
        /// Escape the permission.
        /// </summary>
        /// <param name="value">The value to be escaped.</param>
        private static string EscapePermission(string value)
        {
            return value.Replace(",", ",,");
        }

        /// <summary>
        /// Unescapes the permission.
        /// </summary>
        /// <param name="value">The value to be escaped.</param>
        private static string UnescapePermission(string value)
        {
            return value.Replace(",,", ",");
        }

        /// <summary>
        /// Serializes the permissions.
        /// </summary>
        /// <param name="permissions">The value to be serialized.</param>
        private static string SerializePermissions(string[] permissions)
        {
            return string.Join(",", permissions.Select(permission => EscapePermission(permission)));
        }

        /// <summary>
        /// Deserializes the permissions.
        /// </summary>
        /// <param name="value">The value to be escaped.</param>
        private static IEnumerable<string> DeserializePermissions(string value)
        {
            var segmentStart = 0;
            for (var cursor = 0; cursor < value.Length; cursor++)
            {
                if (value[cursor] == ',')
                {
                    cursor++;
                    if (cursor < value.Length && value[cursor] != ',')
                    {
                        yield return UnescapePermission(value.Substring(segmentStart, cursor - segmentStart - 1));
                        segmentStart = cursor;
                    }
                }
            }

            yield return SharedAccessPermissions.UnescapePermission(value.Substring(segmentStart));
        }
    }
}
