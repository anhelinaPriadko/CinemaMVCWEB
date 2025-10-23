using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CinemaDomain.Model
{
    public class User : IdentityUser
    {
        public int Year { get; set; }

        [JsonIgnore]
        public Viewer Viewer { get; set; }
    }
}
