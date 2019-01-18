using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ApiModels
{
    public class DataResponse<D> : CommonResponse
    {
        public D Data { get; set; }
    }
}
