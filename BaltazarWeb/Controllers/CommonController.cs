﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ApiModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    [Route("/Common")]
    [ApiController]
    public class CommonController : ControllerBase
    {
        private readonly MongoHelper DB;

        public CommonController(MongoHelper DB)
        {
            this.DB = DB;
        }

        public ActionResult<DataResponse<CommonData>> Index([FromHeader] Guid token, [FromQuery] int appVersion, [FromQuery] int androidVersion, [FromQuery] string uuid, [FromQuery] string pusheId)
        {
            Student student = null;
            if(token != null && token != Guid.Empty)
                student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            
            var log = new AppUsageLog
            {
                StudentId = student != null ? student.Id : ObjectId.Empty,
                UUID = uuid,
                AppVersion = appVersion,
                AndroidVersion = androidVersion,
                PusheId = pusheId
            };
            DB.Save(log);

            if (student != null)
            {
                if (student.PusheId != pusheId)
                {
                    student.PusheId = pusheId;
                    DB.UpdateOne(s => s.Id == student.Id, Builders<Student>.Update.Set(s => s.PusheId, pusheId));
                }
                student.Password = null;
            }

            CommonData.Notifications notifications = null;
            if (student != null)
            {
                notifications = new CommonData.Notifications();
                if (!student.IsTeacher)
                {
                    notifications.NewAnswers = DB.Find<Question>(q => q.UserId == student.Id && q.UnseenAnswersCount > 0).Project(q => q.UnseenAnswersCount).ToEnumerable().Sum();
                    notifications.NewShops = (int)DB.Count<ShopItem>(s => s.DateAdded > student.LastShopVisit);
                }
                notifications.NewBlogs = (int)DB.Count<Blog>(b => b.DateAdded > student.LastBlogVisit);
            }

            var upgradeInfo = DB.Find<UpgradeInfo>(u => appVersion <= u.AppVersionMax).SortByDescending(u => u.Date).FirstOrDefault();
            CommonData.UpgradeData upgrade = null;
            if (upgradeInfo != null)
                upgrade = new CommonData.UpgradeData { Message = upgradeInfo.Message, ForceUpgrade = upgradeInfo.Force };

            return new DataResponse<CommonData>
            {
                Success = true,
                Data = new CommonData
                {
                    Me = student,
                    Notification = notifications,
                    Provinces = DB.All<Province>().ToList(),
                    Cities = DB.All<City>().ToList(),
                    Courses = DB.All<Course>().ToList(),
                    StudyFields = DB.All<StudyField>().ToList(),
                    Sections = DB.All<CourseSection>().ToList(),
                    Upgrade = upgrade
                }
            };
        }
    }
}