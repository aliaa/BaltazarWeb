using AliaaCommon;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace BaltazarWeb.Models
{
    [MongoIndex(new string[] { nameof(QuestionId) })]
    [MongoIndex(new string[] { nameof(UserId), nameof(QuestionId) })]
    public class Answer : BaseUserContent
    {
        public enum QuestionerResponseEnum
        {
            [Display(Name = "دیده نشده")]
            NotSeen,
            [Display(Name = "تائید شده")]
            Accepted,
            [Display(Name = "رد شده")]
            Rejected,
            [Display(Name = "گزارش شده")]
            Reported,
        }

        public ObjectId QuestionId { get; set; }

        [Display(Name = "پاسخ سوال کننده")]
        public QuestionerResponseEnum Response { get; set; }

        [Display(Name = "برای سوال بالتازار")]
        public bool ToBaltazarQuestion { get; set; }
    }
}
