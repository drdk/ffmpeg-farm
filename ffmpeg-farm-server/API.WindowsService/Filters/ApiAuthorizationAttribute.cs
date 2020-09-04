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
        /// <summary>
        /// ctor, configures Roles with app settings AuthAdGroup
        /// </summary>
        public ApiAuthorizationAttribute()
        {
            var grpName = ConfigurationManager.AppSettings["AuthAdGroup"];
            if (string.IsNullOrEmpty(grpName))
                throw new ConfigurationErrorsException("AuthAdGroup missing from Configuration.");
            Roles = grpName;
        }

        /// <summary>
        /// Checks if the current user is member of AD group specified in config "AuthAdGroup", if the request is secure (https).
        /// NB: if this is used on an insecure (http) request, it will always be authorized!
        /// </summary>
        /// <param name="actionContext"></param>
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            return
                !actionContext.Request.GetOwinContext().Request.IsSecure || // TODO: remove this line then non-https have been deprecated.
                base.IsAuthorized(actionContext);
        }
    }

}
