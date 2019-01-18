using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        [Display(Name = "نام")]
        public string FirstName { get; set; }
        [Display(Name = "نام خانوادگی")]
        public string LastName { get; set; }
        [Display(Name = "نام و نام خانوادگی")]
        public string DisplayName => FirstName + " " + LastName;
        [Display(Name = "شماره همراه")]
        public string Phone { get; set; }
        public string Password { get; set; }
        [Display(Name = "مقطع تحصیلی")]
        public int Grade { get; set; }
        [Display(Name = "رشته")]
        public ObjectId StudyFieldId { get; set; }
        [Display(Name = "آدرس")]
        public string Address { get; set; }
        [Display(Name = "جنسیت")]
        public GenderEnum Gender { get; set; }
        [Display(Name = "شهر")]
        public ObjectId CityId { get; set; }
        [Display(Name = "مدرسه")]
        public string SchoolName { get; set; }
        public Guid Token { get; set; }
        [Display(Name = "تعداد سکه ها")]
        public int Coins { get; set; }
        public List<CoinTransaction> CoinTransactions { get; set; } = new List<CoinTransaction>();
    }
}
