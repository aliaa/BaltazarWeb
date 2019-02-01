using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class CoinTransaction
    {
        public enum TransactionType
        {
            AskQuestion,
            AnswerQuestion,
            AnswerBaltazar,
            Buy,
            InviteFriend,
            ProfileCompletion,
        }

        public int Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public ObjectId SourceId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public TransactionType Type { get; set; }
    }
}
