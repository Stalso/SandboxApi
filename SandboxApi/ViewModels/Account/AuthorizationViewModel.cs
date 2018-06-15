using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SandboxApi.ViewModels.Account
{
    public class AuthorizationViewModel
    {
        //
        // Summary:
        //     Gets or sets the "password" parameter.
        public string Password { get; set; }
       
        //
        // Summary:
        //     Gets or sets the "grant_type" parameter.
        public string GrantType { get; set; }
        
        //
        // Summary:
        //     Gets or sets the "username" parameter.
        public string Username { get; set; }
    }
}
