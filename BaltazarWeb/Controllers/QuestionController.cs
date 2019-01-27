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
        private readonly MongoHelper DB;
        private readonly string ImageUploadPath;
        private const int PAGE_SIZE = 200;

        public QuestionController(MongoHelper DB, IHostingEnvironment env)
        {
            this.DB = DB;
            ImageUploadPath = Path.Combine(env.WebRootPath, Consts.UPLOAD_IMAGE_DIR);
            if (!Directory.Exists(ImageUploadPath))
                Directory.CreateDirectory(ImageUploadPath);
        }

        [Authorize]
        public IActionResult Index(BaseUserContent.PublishStatusEnum status)
        {
            var query = DB.Find<Question>(q => q.PublishStatus == status);
            if (status == BaseUserContent.PublishStatusEnum.WaitForApprove)
                query = query.SortBy(q => q.CreateDate);
            else
                query = query.SortByDescending(q => q.CreateDate);
            return View(query.ToEnumerable());
        }
        
        [Authorize]
        public IActionResult Accept(string id)
        {
            Question question = DB.FindById<Question>(id);
            question.PublishStatus = BaseUserContent.PublishStatusEnum.Published;
            DB.Save(question);
            Student student = DB.FindById<Student>(question.UserId);
            DB.Save(student);
            return RedirectToAction(nameof(Index), new { status = BaseUserContent.PublishStatusEnum.WaitForApprove });
        }
        
        [Authorize]
        public IActionResult Reject(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            Question question = DB.FindById<Question>(objId);
            if (question != null)
            {
                DB.UpdateOne(q => q.Id == objId, Builders<Question>.Update.Set(q => q.PublishStatus, BaseUserContent.PublishStatusEnum.Rejected));

                Student student = DB.FindById<Student>(question.UserId);
                var transaction = student.CoinTransactions.Where(t => t.QuestionId == question.Id).FirstOrDefault();
                if(transaction != null)
                {
                    student.CoinTransactions.Remove(transaction);
                    student.Coins += transaction.Amount;
                    DB.Save(student);
                }
            }
            return RedirectToAction(nameof(Index), new { status = BaseUserContent.PublishStatusEnum.WaitForApprove });
        }

        [Authorize]
        public IActionResult Details(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            return View(DB.FindById<Question>(objId));
        }

        [HttpPost]
        public ActionResult<DataResponse<Question>> Publish([FromHeader] Guid token, [FromBody] Question question)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();

            if (student.Coins < Consts.QUESTION_COIN_COST)
                return new DataResponse<Question> { Success = false, Message = "تعداد سکه باقی مانده شما کم است!" };

            question.Id = ObjectId.Empty;
            question.PublishStatus = BaseUserContent.PublishStatusEnum.WaitForApprove;
            question.UserId = student.Id;
            question.UserName = student.DisplayName;
            question.HasImage = false;
            question.AcceptedAnswerId = ObjectId.Empty;
            question.Prize = Consts.ANSWER_DEFAULT_PRIZE;

            Course course = DB.FindById<Course>(question.CourseId);
            if (course == null)
                return new DataResponse<Question> { Success = false, Message = "نام درس صحیح نیست!" };
            question.Grade = course.Grade;

            if (question.SectionId != ObjectId.Empty && !DB.Any<Question>(q => q.SectionId == question.SectionId))
                return new DataResponse<Question> { Success = false, Message = "سرفصل موجود نمیباشد!" };

            DB.Save(question);

            student.Coins -= Consts.QUESTION_COIN_COST;
            student.CoinTransactions.Add(new CoinTransaction { Amount = Consts.QUESTION_COIN_COST, QuestionId = question.Id });
            DB.Save(student);

            return new DataResponse<Question> { Success = true, Data = question };
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
                return new CommonResponse { Success = false, Message = "تصویر دریافت نشد!" };
            string filePath = Path.Combine(ImageUploadPath, id + ".jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                image.CopyTo(fs);
            }
            DB.UpdateOne(q => q.Id == question.Id, Builders<Question>.Update.Set(q => q.HasImage, true));
            return new CommonResponse { Success = true };
        }

        public ActionResult<DataResponse<List<Question>>> List([FromHeader] Guid token, 
            int? grade = null, string courseId = null, string sectionId = null, int page = 0)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();

            var fb = Builders<Question>.Filter;
            List<FilterDefinition<Question>> filters = new List<FilterDefinition<Question>>();
            filters.Add(fb.Ne(q => q.UserId, student.Id));
            filters.Add(fb.Eq(q => q.AcceptedAnswerId, ObjectId.Empty));
            filters.Add(fb.Eq(q => q.PublishStatus, BaseUserContent.PublishStatusEnum.Published));

            if (grade == null)
                filters.Add(fb.Lte(q => q.Grade, student.Grade));
            else
                filters.Add(fb.Eq(q => q.Grade, grade.Value));

            ObjectId id;
            if (courseId != null && ObjectId.TryParse(courseId, out id))
                filters.Add(fb.Eq(q => q.CourseId, id));
            if (sectionId != null && ObjectId.TryParse(sectionId, out id))
                filters.Add(fb.Eq(q => q.SectionId, id));

            var list = DB.Find(fb.And(filters))
                .SortByDescending(q => q.Grade).ThenByDescending(q => q.CreateDate)
                .Limit(PAGE_SIZE)
                .Skip(page * PAGE_SIZE)
                .ToList();
            foreach (var item in list)
            {
                item.Hot = !DB.Any<Answer>(a => a.QuestionId == item.Id);
                item.UserName = DB.FindById<Student>(item.UserId).DisplayName;
            }
            var baltazarQuestions = DB.Find<BaltazarQuestion>(q => q.ExpireDate > DateTime.Now && student.Grade >= q.Grade && student.Grade <= q.MaxGrade).ToList();
            Random random = new Random();
            foreach (var bq in baltazarQuestions)
            {
                int index = random.Next(list.Count);
                list.Insert(index, bq);
            }

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