using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class CoinTransaction
    {
        public int Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public ObjectId QuestionId { get; set; }
    }
}
