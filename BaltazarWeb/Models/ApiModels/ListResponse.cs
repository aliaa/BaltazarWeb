using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ApiModels
{
    public class ListResponse<T> : CommonResponse
    {
        public List<T> List { get; set; }
    }
}
