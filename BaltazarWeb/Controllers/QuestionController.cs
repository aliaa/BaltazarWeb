﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ApiModels;
using BaltazarWeb.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    public class QuestionController : Controller
    {
        private readonly MongoHelper DB;
        private readonly IPushNotificationProvider pushProvider;
        private readonly string ImageUploadPath;
        private const int PAGE_SIZE = 200;

        public QuestionController(MongoHelper DB, IHostingEnvironment env, IPushNotificationProvider pushProvider)
        {
            this.DB = DB;
            this.pushProvider = pushProvider;
            ImageUploadPath = Path.Combine(env.WebRootPath, Consts.UPLOAD_IMAGE_DIR);
            if (!Directory.Exists(ImageUploadPath))
                Directory.CreateDirectory(ImageUploadPath);
        }

        [Authorize(policy: nameof(Permission.ViewContent))]
        public IActionResult Index(string status, string grade = "", string course = "")
        {
            BaseUserContent.PublishStatusEnum statusEnum = (BaseUserContent.PublishStatusEnum)Enum.Parse(typeof(BaseUserContent.PublishStatusEnum), status);
            List<FilterDefinition<Question>> filters = new List<FilterDefinition<Question>>();
            var fb = Builders<Question>.Filter;
            filters.Add(fb.Eq(q => q.PublishStatus, statusEnum));

            int gradeInt = 0;
            int.TryParse(grade, out gradeInt);
            if (gradeInt > 0)
                filters.Add(fb.Eq(q => q.Grade, gradeInt));

            ObjectId courseId = ObjectId.Empty;
            ObjectId.TryParse(course, out courseId);
            if (courseId != ObjectId.Empty)
                filters.Add(fb.Eq(q => q.CourseId, courseId));

            var totalFilter = fb.And(filters);
            if (filters.Count == 1)
                totalFilter = filters[0];

            var query = DB.Find(totalFilter);
            if (statusEnum == BaseUserContent.PublishStatusEnum.WaitForApprove)
                query = query.SortBy(q => q.CreateDate);
            else
                query = query.SortByDescending(q => q.CreateDate);

            List<SelectListItem> grades = new List<SelectListItem>();
            grades.Add(new SelectListItem("همه", ""));
            for (int i = 1; i <= 12; i++)
                grades.Add(new SelectListItem(i.ToString(), i.ToString()));
            ViewBag.Grades = grades;
            ViewBag.SelectedGrade = grade;
            ViewBag.Status = status;

            List<SelectListItem> courses = new List<SelectListItem>();
            courses.Add(new SelectListItem("همه", ""));
            if(gradeInt > 0)
                courses.AddRange(DB.Find<Course>(c => c.Grade == gradeInt).ToEnumerable().Select(c => new SelectListItem(c.Name, c.Id.ToString())));
            ViewBag.Courses = courses;
            ViewBag.SelectedCourseId = course;

            var list = query.Limit(1000).ToList();
            foreach (var item in list)
                item.UserName = DB.FindById<Student>(item.UserId).DisplayName;
            return View(list);
        }

        [Authorize(policy: nameof(Permission.ApproveContent))]
        public IActionResult Accept(string id)
        {
            Question question = DB.FindById<Question>(id);
            question.PublishStatus = BaseUserContent.PublishStatusEnum.Published;
            DB.Save(question);
            Student student = DB.FindById<Student>(question.UserId);
            DB.Save(student);
            if(student.PusheId != null)
                pushProvider.SendMessageToUser("تائید سوال", "سوال شما تائید و منتشر شد!", student.PusheId, out _);
            return NextNeedToApprove();
        }

        [Authorize(policy: nameof(Permission.ApproveContent))]
        public IActionResult Reject(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            Question question = DB.FindById<Question>(objId);
            if (question != null)
            {
                DB.UpdateOne(q => q.Id == objId, Builders<Question>.Update.Set(q => q.PublishStatus, BaseUserContent.PublishStatusEnum.Rejected));

                Student student = DB.FindById<Student>(question.UserId);
                var transaction = student.CoinTransactions.Where(t => t.Type == CoinTransaction.TransactionType.AskQuestion && t.SourceId == question.Id).FirstOrDefault();
                if(transaction != null)
                {
                    student.CoinTransactions.Remove(transaction);
                    student.Coins += Math.Abs(transaction.Amount);
                    DB.Save(student);
                }
                if (student.PusheId != null)
                    pushProvider.SendMessageToUser("رد سوال شما", "متاسفانه سوال شما برای انتشار رد شد!", student.PusheId, out _);
            }
            return NextNeedToApprove();
        }

        private IActionResult NextNeedToApprove()
        {
            var next = DB.Find<Question>(q => q.PublishStatus == BaseUserContent.PublishStatusEnum.WaitForApprove).SortBy(q => q.CreateDate).FirstOrDefault();
            if (next == null)
                return RedirectToAction(nameof(Index), new { status = BaseUserContent.PublishStatusEnum.WaitForApprove });
            return View(nameof(Details), next);
        }

        [Authorize(policy: nameof(Permission.ViewContent))]
        public IActionResult Details(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            return View(DB.FindById<Question>(objId));
        }

        [HttpPost]
        public ActionResult<DataResponse<Question>> Publish([FromHeader] Guid token, [FromBody] Question question)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null || student.IsTeacher)
                return Unauthorized();

            if (student.Coins < Consts.QUESTION_COIN_COST)
                return new DataResponse<Question> { Success = false, Message = "تعداد سکه باقی مانده شما کم است!" };

            question.Id = ObjectId.Empty;
            question.PublishStatus = BaseUserContent.PublishStatusEnum.WaitForApprove;
            question.UserId = student.Id;
            question.UserName = student.NickName;
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
            student.CoinTransactions.Add(new CoinTransaction
            {
                Type = CoinTransaction.TransactionType.AskQuestion,
                Amount = -Consts.QUESTION_COIN_COST,
                SourceId = question.Id
            });
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

            if (grade == null && !student.IsTeacher)
                filters.Add(fb.Lte(q => q.Grade, student.Grade));
            else if(grade != null)
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
            
            if (!student.IsTeacher)
                list.AddRange(DB.Find<BaltazarQuestion>(q => q.ExpireDate > DateTime.Now && student.Grade >= q.Grade && student.Grade <= q.MaxGrade).ToEnumerable());

            foreach (var item in list)
            {
                if (!(item is BaltazarQuestion))
                {
                    item.Hot = !DB.Any<Answer>(a => a.QuestionId == item.Id);
                    item.UserName = DB.FindById<Student>(item.UserId).NickName;
                }
                item.IAlreadyAnswered = DB.Any<Answer>(a => a.UserId == student.Id && a.QuestionId == item.Id);
            }

            list = list.OrderByDescending(q => q.CreateDate).ToList();
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
                foreach (var ans in question.Answers)
                    ans.UserName = DB.FindById<Student>(ans.UserId)?.NickName;

                if (question.AcceptedAnswerId != ObjectId.Empty)
                {
                    question.AcceptedAnswer = DB.FindById<Answer>(question.AcceptedAnswerId);
                    if (question.AcceptedAnswer != null)
                        question.AcceptedAnswer.UserName = DB.FindById<Student>(question.AcceptedAnswer.UserId)?.NickName;
                }
            }

            return new DataResponse<List<Question>> { Success = true, Data = list };
        }

        [HttpDelete]
        [Route("Question/{id}")]
        public ActionResult<CommonResponse> Delete([FromHeader] Guid token, [FromRoute] string id)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            Question question = DB.FindById<Question>(id);
            if (question.UserId != student.Id)
                return new CommonResponse { Message = "این سوال شما نیست!" };
            if (question.PublishStatus != BaseUserContent.PublishStatusEnum.WaitForApprove)
                return new CommonResponse { Message = "این سوال قبلا منتشر شده یا رد شده است!" };
            var coinTansaction = student.CoinTransactions.LastOrDefault(t => t.Type == CoinTransaction.TransactionType.AskQuestion && t.SourceId == question.Id);
            if (coinTansaction != null)
            {
                student.Coins += Math.Abs(coinTansaction.Amount);
                student.CoinTransactions.Remove(coinTansaction);
                DB.Save(student);
            }
            DB.DeleteOne(question);
            return new CommonResponse { Success = true, Message = "سوال حذف شد!" };
        }
    }
}