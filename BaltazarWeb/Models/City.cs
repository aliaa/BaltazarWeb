using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class City : MongoEntity
    {
        public ObjectId ProvinceId { get; set; }
        public string Name { get; set; }
    }
}
