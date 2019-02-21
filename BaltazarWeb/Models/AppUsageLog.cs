using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    [CollectionOptions(Capped = true, MaxSize = 10000000)]
    public class AppUsageLog : MongoEntity
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public ObjectId StudentId { get; set; }
        public string UUID { get; set; }
        public int AppVersion { get; set; }
        public int AndroidVersion { get; set; }
        public string PusheId { get; set; }
    }
}
