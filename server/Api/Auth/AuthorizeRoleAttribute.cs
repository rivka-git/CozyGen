using Microsoft.AspNetCore.Authorization;

namespace Api.Auth
{
    /// <summary>
    /// Restricts an endpoint to users whose JWT contains the given role claim.
    /// Usage: [AuthorizeRole("Admin")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        public AuthorizeRoleAttribute(string role)
        {
            Roles = role;
        }
    }
}
