using AliaaCommon;
using BaltazarWeb.Utils;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;

namespace BaltazarWeb.Models
{
    [Serializable]
    [MongoIndex(new string[] { nameof(Token) })]
    [MongoIndex(new string[] { nameof(CityId) })]
    public class Student : MongoEntity
    {
        public enum GenderEnum
        {
            Male,
            Female
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
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
