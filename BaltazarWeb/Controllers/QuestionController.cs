using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ApiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    public class QuestionController : Controller
    {
        public const string UPLOAD_DIR = "Uploads";
        private readonly MongoHelper DB;
        private readonly string UploadPath;

        public QuestionController(MongoHelper DB, IHostingEnvironment env)
        {
            this.DB = DB;
            UploadPath = Path.Combine(env.WebRootPath, UPLOAD_DIR);
            if (!Directory.Exists(UploadPath))
                Directory.CreateDirectory(UploadPath);
        }
        
        [Authorize]
        public IActionResult ApproveList()
        {
            return View(DB.Find<Question>(q => q.PublishStatus == BaseUserContent.PublishStatusEnum.WaitForApprove).SortBy(q => q.CreateDate).ToEnumerable());
        }
        
        [Authorize]
        public IActionResult Accept(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            DB.UpdateOne(q => q.Id == objId, Builders<Question>.Update.Set(q => q.PublishStatus, BaseUserContent.PublishStatusEnum.Published));
            return RedirectToAction(nameof(ApproveList));
        }
        
        [Authorize]
        public IActionResult Reject(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            DB.UpdateOne(q => q.Id == objId, Builders<Question>.Update.Set(q => q.PublishStatus, BaseUserContent.PublishStatusEnum.Rejected));
            return RedirectToAction(nameof(ApproveList));
        }

        [Authorize]
        public IActionResult Details(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            return View(DB.FindById<Question>(objId));
        }

        [HttpPost]
        public ActionResult<CommonResponse> Publish([FromHeader] Guid token, [FromBody] Question question)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();

            question.Id = ObjectId.Empty;
            question.PublishStatus = BaseUserContent.PublishStatusEnum.WaitForApprove;
            question.UserId = student.Id;
            question.HasImage = false;
            question.AcceptedAnswerId = ObjectId.Empty;

            Course course = DB.FindById<Course>(question.CourseId);
            if (course == null)
                return new CommonResponse { Success = false, Message = "نام درس صحیح نیست!" };
            question.Grade = course.Grade;

            DB.Save(question);
            return new CommonResponse { Success = true };
        }

        [HttpPost]
        [Route("Question/UploadImage/{id}")]
        public ActionResult<CommonResponse> UploadImage([FromHeader] Guid token, [FromRoute] string id, IFormFile image)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            Question question = DB.FindById<Question>(id);
            if (question.UserId != student.Id)
                return Unauthorized();
            if (image == null)
                return new CommonResponse { Success = false };
            string filePath = Path.Combine(UploadPath, id + ".jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                image.CopyTo(fs);
            }
            DB.UpdateOne(q => q.Id == question.Id, Builders<Question>.Update.Set(q => q.HasImage, true));
            return new CommonResponse { Success = true };
        }

        public ActionResult<DataResponse<List<Question>>> List([FromHeader] Guid token)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            var list = DB.Find<Question>(q => q.Grade <= student.Grade && q.UserId != student.Id && q.AcceptedAnswerId == ObjectId.Empty)
                .SortByDescending(q => q.Grade).ThenByDescending(q => q.CreateDate)
                .Limit(1000).ToList();
            return new DataResponse<List<Question>> { Success = true, Data = list };
        }

        public ActionResult<DataResponse<List<Question>>> Mine([FromHeader] Guid token)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            var list = DB.Find<Question>(q => q.UserId == student.Id).SortByDescending(q => q.CreateDate).ToList();
            foreach (var question in list)
            {
                question.Answers = DB.Find<Answer>(a => 
                        a.QuestionId == question.Id &&
                        a.Response == Answer.QuestionerResponseEnum.NotSeen &&
                        a.PublishStatus == BaseUserContent.PublishStatusEnum.Published).ToList();
            }
            return new DataResponse<List<Question>> { Success = true, Data = list };
        }        
    }
}