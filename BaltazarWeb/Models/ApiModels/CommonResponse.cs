using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ApiModels
{
    [Serializable]
    public class CommonResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
