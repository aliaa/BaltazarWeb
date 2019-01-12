using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class CourseSection
    {
        public ObjectId CourseId { get; set; }
        public string Name { get; set; }
    }
}
