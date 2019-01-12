using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class Course : MongoEntity
    {
        public int Grade { get; set; }
        public ObjectId StudyFieldId { get; set; }
        public string Name { get; set; }
    }
}
