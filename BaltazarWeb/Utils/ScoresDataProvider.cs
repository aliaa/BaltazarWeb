﻿using AliaaCommon.Models;
using AliaaCommon.MongoDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using BaltazarWeb.Models.ApiModels;
using BaltazarWeb.Models;
using AliaaCommon;

namespace BaltazarWeb.Utils
{
    public class ScoresDataProvider
    {
        private readonly MongoHelper DB;
        private const int REFRESH_DURATION_SECS = 1800;

        public ScoresDataProvider(MongoHelper DB)
        {
            this.DB = DB;
        }

        private static CachedData<List<TopStudent>> totalTopCache;
        private static CachedData<List<TopStudent>> festivalTopCache;
        //private static CachedData<List<TopStudent>>[] totalGradesTopCache = new CachedData<List<TopStudent>>[12];
        private static CachedData<List<TopStudent>>[] festivalGradesTopCache = new CachedData<List<TopStudent>>[12];

        public List<TopStudent> TotalTopStudents
        {
            get
            {
                if (totalTopCache == null)
                {
                    totalTopCache = new CachedData<List<TopStudent>>(GetTotalTopFromDB(), REFRESH_DURATION_SECS);
                    totalTopCache.AutoRefreshFunc = _ => GetTotalTopFromDB();
                }
                return totalTopCache.Data;
            }
        }

        private List<TopStudent> GetTotalTopFromDB()
        {
            return DB.Find<Student>(s => s.IsTeacher != true).SortByDescending(s => s.TotalPoints).Limit(10).ToEnumerable()
                .Select(s => new TopStudent { StudentId = s.Id, UserName = s.DisplayName, CityId = s.CityId, Points = s.TotalPoints, School = s.SchoolName }).ToList();
        }

        public List<TopStudent> FestivalTopStudents
        {
            get
            {
                if(festivalTopCache == null)
                {
                    festivalTopCache = new CachedData<List<TopStudent>>(GetFestivalTopFromDB(null), REFRESH_DURATION_SECS);
                    festivalTopCache.AutoRefreshFunc = _ => GetFestivalTopFromDB(null);
                }
                return festivalTopCache.Data;
            }
        }

        private class UnWindedFestivalStudent : MongoEntity
        {
            public Student.FestivalPoint FestivalPoints { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public ObjectId CityId { get; set; }
            public string SchoolName { get; set; }
            public int Grade { get; set; }
        }

        private class TopStudentWithFestivalName : TopStudent
        {
            public string FestivalName { get; set; }
            public int Grade { get; set; }
        }

        private List<TopStudent> GetFestivalTopFromDB(int? grade)
        {
            var currentFestival = ScoresData.CurrentFestivalName;
            var agg = DB.Aggregate<Student>()
                .Match(s => s.IsTeacher != true && s.FestivalPoints.Any(f => f.Name == currentFestival))
                .Project<Student>(Builders<Student>.Projection.Include(s => s.Id).Include(s => s.FirstName).Include(s => s.LastName)
                    .Include(s => s.SchoolName).Include(s => s.Grade).Include(s => s.FestivalPoints).Include(s => s.CityId))
                .Unwind<Student, UnWindedFestivalStudent>(s => s.FestivalPoints)
                .Project(s => new TopStudentWithFestivalName
                {
                    StudentId = s.Id,
                    UserName = s.FirstName + " " + s.LastName,
                    CityId = s.CityId,
                    School = s.SchoolName,
                    Points = s.FestivalPoints.Points,
                    FestivalName = s.FestivalPoints.Name,
                    Grade = s.Grade
                });

            if (grade == null)
                agg = agg.Match(s => s.FestivalName == currentFestival);
            else
                agg = agg.Match(s => s.FestivalName == currentFestival && s.Grade == grade.Value);

            agg = agg
                .Project<TopStudentWithFestivalName>(Builders<TopStudentWithFestivalName>.Projection.Exclude(s => s.FestivalName).Exclude(s => s.Grade))
                .Sort(Builders<TopStudentWithFestivalName>.Sort.Descending(s => s.Points));
            return agg.As<TopStudent>().ToList();
        }

        //public List<TopStudent> GetTotalTopStudentsInGrade(int grade)
        //{
        //    if(totalGradesTopCache[grade-1] == null)
        //    {
        //        totalGradesTopCache[grade-1] = new CachedData<List<TopStudent>>(GetTotalTopStudentsInGradeFromDB(grade), REFRESH_DURATION_SECS);
        //        totalGradesTopCache[grade-1].AutoRefreshFunc = _ => GetTotalTopStudentsInGradeFromDB(grade);
        //    }
        //    return totalGradesTopCache[grade-1].Data;
        //}

        //private List<TopStudent> GetTotalTopStudentsInGradeFromDB(int grade)
        //{
        //    return DB.Find<Student>(s => s.Grade == grade).SortByDescending(s => s.Points).Limit(10).ToEnumerable()
        //        .Select(s => new TopStudent { UserName = s.DisplayName, CityId = s.CityId, Points = s.Points, School = s.SchoolName }).ToList();
        //}

        public List<TopStudent> GetFestivalTopStudentsInGrade(int grade)
        {
            if(festivalGradesTopCache[grade-1] == null)
            {
                festivalGradesTopCache[grade-1] = new CachedData<List<TopStudent>>(GetFestivalTopFromDB(grade), REFRESH_DURATION_SECS);
                festivalGradesTopCache[grade-1].AutoRefreshFunc = _ => GetFestivalTopFromDB(grade);
            }
            return festivalGradesTopCache[grade-1].Data;
        }

        public ScoresData GetScoresData(Student student)
        {
            var currentFestival = ScoresData.CurrentFestivalName;
            ScoresData data = new ScoresData();

            data.MyAllTimePoints = student.TotalPoints;
            data.MyAllTimeTotalScore = DB.Count<Student>(s => s.TotalPoints > student.TotalPoints) + 1;

            data.FestivalName = ScoresData.CurrentFestivalDisplayName;
            var myFestival = student.FestivalPoints.FirstOrDefault(f => f.Name == currentFestival);
            data.MyFestivalPoints = myFestival != null ? myFestival.Points : 0;
            data.MyFestivalPointsFromLeague = myFestival != null ? myFestival.PointsFromLeague : 0;
            data.MyFestivalPointsFromOtherQuestions = myFestival != null ? myFestival.PointsFromOtherQuestions : 0;

            data.MyFestivalScore = DB.Count(Builders<Student>.Filter.ElemMatch(s => s.FestivalPoints, f => f.Points > data.MyFestivalPoints)) + 1;
            data.MyFestivalScoreOnGrade = DB.Count(
                Builders<Student>.Filter.And(
                    Builders<Student>.Filter.Eq(s => s.Grade, student.Grade),
                    Builders<Student>.Filter.ElemMatch(s => s.FestivalPoints, f => f.Points > data.MyFestivalPoints)
                )) + 1;

            data.FestivalTop = FestivalTopStudents;
            data.TotalTop = TotalTopStudents;
            data.FestivalTopOnGrade = GetFestivalTopStudentsInGrade(student.Grade);
            return data;
        }
    }
}
