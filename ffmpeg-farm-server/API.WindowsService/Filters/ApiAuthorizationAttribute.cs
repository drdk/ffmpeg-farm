using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace API.WindowsService.Filters
{
    /// <summary>
    /// This class can be used in ApiController by adding attribute [ApiAuthorization].
    /// </summary>
    public class ApiAuthorizationAttribute : AuthorizeAttribute
    {
        private static readonly string claimIdForGroup = "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid";
        private static readonly string claimIdForUsername = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

        /// <summary>
        /// Checks if the current user is member of AD group specified in config "AuthAdGroup", if the request is secure (https).
        /// NB: if this is used on an insecure (http) request, it will always be authorized!
        /// </summary>
        /// <param name="actionContext"></param>
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            if (actionContext.Request.GetOwinContext().Request.IsSecure)
            {
                // Only challenge client for credentials, if the request is secure.
                // We don't want to force sending user/pwd without https.

                var owinUser = TryGetOwinUser(actionContext);
                if (owinUser == null)
                    throw new AuthenticationException("Unable to get user info. Check if service has enabled AuthenticationSchemes.Ntlm.");

                var grpName = ConfigurationManager.AppSettings["AuthAdGroup"];
                if (string.IsNullOrEmpty(grpName))
                    throw new ConfigurationErrorsException("AuthAdGroup missing from Configuration.");

                var adGroup = GetADGroup(grpName);
                if (!CheckClaim(owinUser, claimIdForGroup, adGroup.Sid.ToString()))
                    return false;
            }

            return true;
        }

        /// <summary>Processes requests that fail authorization.</summary>
        /// <param name="actionContext">The context.</param>
        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            var grp = ConfigurationManager.AppSettings["AuthAdGroup"];
            var owinUser = TryGetOwinUser(actionContext);
            if (owinUser == null)
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Unable to get user info. Check if service has enabled AuthenticationSchemes.Ntlm.");
            else
            {
                var userName = ReadClaim(owinUser, claimIdForUsername);
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, $"User {userName?.Value} is not a a member of AD Group: {grp}");
            }
        }

        private static GroupPrincipal GetADGroup(string groupName)
        {
            var ctx = new PrincipalContext(ContextType.Domain);
            var group = GroupPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, groupName);
            if (group == null)
                throw new ConfigurationErrorsException($"AD group not found: {groupName}");
            return group;
        }

        private static ClaimsPrincipal TryGetOwinUser(HttpActionContext actionContext)
        {
            var context = actionContext?.Request.GetOwinContext();
            return context?.Authentication?.User;
        }

        private static bool CheckClaim(ClaimsPrincipal owinUser, string key, string value)
        {
            return owinUser?.Claims != null && owinUser.Claims.Any(o => o.Type.Equals(key) && o.Value.Equals(value));
        }

        private static Claim ReadClaim(ClaimsPrincipal owinUser, string key)
        {
            return owinUser?.Claims?.FirstOrDefault(o => o.Type.Equals(key));
        }
    }
}
