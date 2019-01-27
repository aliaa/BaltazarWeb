using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ApiModels
{
    public class TopStudent
    {
        public string UserName { get; set; }
        public ObjectId CityId { get; set; }
        public string School { get; set; }
        public int Points { get; set; }
    }

    public class ScoresData
    {
        public int MyPoints { get; set; }
        public int MyPointsFromLeague { get; set; }
        public int MyPointsFromOtherQuestions { get; set; }
        public long MyTotalScore { get; set; }
        public long MyScoreOnBase { get; set; }
        public List<TopStudent> TotalTop { get; set; } = new List<TopStudent>();
        public List<TopStudent> TopOnBase { get; set; } = new List<TopStudent>();
    }
}
