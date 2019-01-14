using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ApiModels
{
    public class TokenResponse : CommonResponse
    {
        public Guid Token { get; set; }
    }
}
