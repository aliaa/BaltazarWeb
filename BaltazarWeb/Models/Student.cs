using MongoDB.Bson;
using Newtonsoft.Json;
using System;

namespace BaltazarWeb.Models
{
    public class Student
    {
        public enum GenderEnum
        {
            Male,
            Female
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        [JsonIgnore]
        public string Password { get; set; }
        public int Grade { get; set; }
        public ObjectId StudyFieldId { get; set; }
        public string Address { get; set; }
        public string Avatar { get; set; }
        public GenderEnum Gender { get; set; }
        public ObjectId CityId { get; set; }
        public string SchoolName { get; set; }
        public Guid Token { get; set; }
    }
}
