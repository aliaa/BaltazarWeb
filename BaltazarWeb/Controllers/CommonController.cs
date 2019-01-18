using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ApiModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        public ActionResult<DataResponse<CommonData>> Index()
        {
            return new DataResponse<CommonData>
            {
                Success = true,
                Data = new CommonData
                {
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