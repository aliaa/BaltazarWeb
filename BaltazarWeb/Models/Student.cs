using AliaaCommon;
using AliaaCommon.MongoDB;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BaltazarWeb.Models
{
    [Serializable]
    [MongoIndex(new string[] { nameof(Token) })]
    [MongoIndex(new string[] { nameof(CityId) })]
    [MongoIndex(new string[] { nameof(InvitationCode)})]
    public class Student : MongoEntity
    {
        public enum GenderEnum
        {
            [Display(Name = "نامشخص")]
            Unspecified,
            [Display(Name = "پسر")]
            Male,
            [Display(Name = "دختر")]
            Female
        }

        public class FestivalPoint
        {
            public string DisplayName { get; set; }
            public string Name { get; set; }
            public int PointsFromLeague { get; set; }
            public int PointsFromOtherQuestions { get; set; }
            public int Points { get; set; }
            public int TotalPoints { get; set; }
        }

        [Display(Name = "نام")]
        [Required]
        public string FirstName { get; set; }

        [Display(Name = "نام خانوادگی")]
        [Required]
        public string LastName { get; set; }

        [Display(Name = "نام و نام خانوادگی")]
        public string DisplayName => FirstName + " " + LastName;

        [Display(Name = "نام مستعار")]
        [Required]
        public string NickName { get; set; }

        [Display(Name = "تاریخ ثبت نام")]
        public DateTime RegistrationDate { get; set; }

        [Display(Name = "مدت عضویت")]
        public int MembershipDurationDays => (int)(DateTime.Now - RegistrationDate).TotalDays;

        [Display(Name = "شماره همراه")]
        [Required]
        [RegularExpression("^09\\d{9}$")]
        public string Phone { get; set; }

        [Required]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; }

        [Display(Name = "مقطع تحصیلی")]
        [Range(minimum:1, maximum:12)]
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

        [Display(Name = "تلفن مدرسه")]
        public string SchoolPhone { get; set; }

        public Guid Token { get; set; }

        [Display(Name = "تعداد سکه ها")]
        public int Coins { get; set; }

        [Display(Name = "امتیاز")]
        public int TotalPoints { get; set; }

        [Display(Name = "امتیاز از لیگ")]
        public int TotalPointsFromLeague { get; set; }

        [Display(Name = "امتیاز از سایر سوالات")]
        public int TotalPointsFromOtherQuestions { get; set; }

        [Display(Name = "کد دعوت")]
        public string InvitationCode { get; set; }

        [Display(Name = "کد دعوت وارد شده")]
        public string InvitedFromCode { get; set; }

        [Display(Name = "تاریخ تولد")]
        public DateTime BirthDate { get; set; }

        [Display(Name = "آیدی پوشه")]
        public string PusheId { get; set; }

        public bool IsTeacher { get; set; }

        [JsonIgnore]
        public List<CoinTransaction> CoinTransactions { get; set; } = new List<CoinTransaction>();
        
        public List<FestivalPoint> FestivalPoints { get; set; } = new List<FestivalPoint>();
        
        [JsonIgnore]
        public DateTime LastBlogVisit { get; set; }

        [JsonIgnore]
        public DateTime LastShopVisit { get; set; }


        private static readonly Random random = new Random();

        public static string GenerateNewInvitationCode(MongoHelper DB)
        {
            byte[] buffer = new byte[6];
            string code;
            do
            {
                random.NextBytes(buffer);
                code = Convert.ToBase64String(buffer).Replace('+','A').Replace('/','B').Substring(0, 6).ToUpper();
            } while (DB.Any<Student>(s => s.InvitationCode == code));
            return code;
        }
    }
}
