using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class DeletedItems : MongoEntity
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public List<MongoEntity> Items { get; set; } = new List<MongoEntity>();
        public ObjectId User { get; set; }
        public string Type { get; set; }
    }
}
