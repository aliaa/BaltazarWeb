using System;
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

        public ActionResult<DataResponse<CommonData>> Index([FromHeader] Guid token, [FromQuery] int appVersion, [FromQuery] int androidVersion, [FromQuery] string uuid)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            
            var log = new AppUsageLog
            {
                StudentId = student != null ? student.Id : ObjectId.Empty,
                UUID = uuid,
                AppVersion = appVersion,
                AndroidVersion = androidVersion
            };
            DB.Save(log);

            if (student != null)
            {
                student.Password = null;
            }
            return new DataResponse<CommonData>
            {
                Success = true,
                Data = new CommonData
                {
                    Me = student,
                    Provinces = DB.All<Province>().ToList(),
                    Cities = DB.All<City>().ToList(),
                    Courses = DB.All<Course>().ToList(),
                    StudyFields = DB.All<StudyField>().ToList(),
                    Sections = DB.All<CourseSection>().ToList()
                }
            };
        }
    }
}