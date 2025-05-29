using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Security.Principal;

namespace Project_site
{
    public static class Extensions
    {
        public static void AddUpdateClaim(this IPrincipal currentPrincipal, string key, string value)
        {
            var identity = currentPrincipal.Identity as ClaimsIdentity;
            if (identity == null)
                return;
            // check for existing claim and remove it
            var existingClaim = identity.FindFirst(key);
            if (existingClaim != null)
                identity.RemoveClaim(existingClaim);
            // add new claim
            identity.AddClaim(new Claim(key, value));
        }

        public static void AddUpdateClaim(this IPrincipal currentPrincipal, string key, DateOnly value)
        {
            var identity = currentPrincipal.Identity as ClaimsIdentity;
            if (identity == null)
                return;
            // check for existing claim and remove it
            var existingClaim = identity.FindFirst(key);
            if (existingClaim != null)
                identity.RemoveClaim(existingClaim);
            // add new claim
            identity.AddClaim(new Claim(key, value.ToString()));
        }
    }
}
