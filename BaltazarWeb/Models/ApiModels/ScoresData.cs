using AliaaCommon;
using FarsiLibrary;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ApiModels
{
    public class TopStudent
    {
        public ObjectId StudentId { get; set; }
        public string UserName { get; set; }
        public ObjectId CityId { get; set; }
        public string School { get; set; }
        public int Points { get; set; }
    }

    public class ScoresData
    {
        public string FestivalName { get; set; }
        public int MyFestivalPoints { get; set; }
        public int MyFestivalPointsFromLeague { get; set; }
        public int MyFestivalPointsFromOtherQuestions { get; set; }
        public long MyFestivalScore { get; set; }
        public long MyFestivalScoreOnGrade { get; set; }
        public long MyAllTimePoints { get; set; }
        public long MyAllTimeTotalScore { get; set; }

        public List<TopStudent> FestivalTop { get; set; }
        public List<TopStudent> FestivalTopOnGrade { get; set; }
        public List<TopStudent> TotalTop { get; set; }

        public static string CurrentFestivalName
        {
            get
            {
                PersianDate pDate = PersianDateConverter.ToPersianDate(DateTime.Now);
                int currentMonth = pDate.Month;
                int currentSeason1Based = (currentMonth - 1) / 3 + 1;
                return pDate.Year + "S" + currentSeason1Based;
            }
        }

        public static string CurrentFestivalDisplayName
        {
            get
            {
                PersianDate pDate = PersianDateConverter.ToPersianDate(DateTime.Now);
                int currentMonth = pDate.Month;
                int currentSeason0Based = (currentMonth - 1) / 3;
                return Consts.SEASON_NAMES[currentSeason0Based] + " " + (pDate.Year % 100);
            }
        }
    }
}
