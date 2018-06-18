using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SandboxApi.Models.Settings
{
    public class JwtOptions
    {
        public string Audience { get; set; }
        public double AccessTokenLifetime { get; set; } = 3600;
        public string Authority { get; set; }
    }
}
