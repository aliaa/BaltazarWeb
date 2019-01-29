using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    public class AnswerController : Controller
    {
        private readonly MongoHelper DB;
        private readonly string ImageUploadPath;

        public AnswerController(MongoHelper DB, IHostingEnvironment env)
        {
            this.DB = DB;
            ImageUploadPath = Path.Combine(env.WebRootPath, Consts.UPLOAD_IMAGE_DIR);
            if (!Directory.Exists(ImageUploadPath))
                Directory.CreateDirectory(ImageUploadPath);
        }

        [Authorize]
        public IActionResult ApproveList()
        {
            return View(DB.Find<Answer>(a => a.PublishStatus == BaseUserContent.PublishStatusEnum.WaitForApprove).SortBy(q => q.CreateDate).ToEnumerable());
        }

        [Authorize]
        public IActionResult Accept(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            DB.UpdateOne(q => q.Id == objId, Builders<Answer>.Update.Set(a => a.PublishStatus, BaseUserContent.PublishStatusEnum.Published));
            return RedirectToAction(nameof(ApproveList));
        }

        [Authorize]
        public IActionResult Reject(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            DB.UpdateOne(q => q.Id == objId, Builders<Answer>.Update.Set(a => a.PublishStatus, BaseUserContent.PublishStatusEnum.Rejected));
            return RedirectToAction(nameof(ApproveList));
        }

        [Authorize]
        public IActionResult Details(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            return View(DB.FindById<Answer>(objId));
        }

        [HttpPost]
        public ActionResult<DataResponse<Answer>> Publish([FromHeader] Guid token, [FromBody] Answer answer)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();

            Question question;
            if (answer.ToBaltazarQuestion)
                question = DB.FindById<BaltazarQuestion>(answer.QuestionId);
            else
                question = DB.FindById<Question>(answer.QuestionId);

            if (question == null)
                return new DataResponse<Answer> { Success = true, Message = "سوال یافت نشد!" };
            if(question.PublishStatus != BaseUserContent.PublishStatusEnum.Published)

            answer.Id = ObjectId.Empty;
            answer.PublishStatus = BaseUserContent.PublishStatusEnum.WaitForApprove;
            answer.UserId = student.Id;
            answer.UserName = student.DisplayName;
            answer.HasImage = false;
            answer.Response = Answer.QuestionerResponseEnum.NotSeen;

            DB.Save(answer);
            return new DataResponse<Answer> { Success = true, Data = answer };
        }

        [HttpPost]
        [Route("Answer/UploadImage/{id?}")]
        public ActionResult<CommonResponse> UploadImage([FromHeader] Guid token, [FromRoute] string id, IFormFile image)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            Answer answer = DB.FindById<Answer>(id);
            if (answer.UserId != student.Id)
                return Unauthorized();
            if (image == null)
                return new CommonResponse { Success = false };
            string filePath = Path.Combine(ImageUploadPath, id + ".jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                image.CopyTo(fs);
            }
            DB.UpdateOne(q => q.Id == answer.Id, Builders<Answer>.Update.Set(q => q.HasImage, true));
            return new CommonResponse { Success = true };
        }

        public ActionResult<CommonResponse> SetResponse([FromHeader] Guid token, string questionId, string answerId, Answer.QuestionerResponseEnum response)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            Question question = DB.FindById<Question>(questionId);
            if (question == null)
                return new CommonResponse { Success = false, Message = "سوال یافت نشد!" };
            if (question.UserId != student.Id)
                return new CommonResponse { Success = false, Message = "سوال شما نیست!" };
            Answer answer = DB.FindById<Answer>(answerId);
            if (answer == null)
                return new CommonResponse { Success = false, Message = "جواب یافت نشد!" };
            if (answer.QuestionId != question.Id)
                return new CommonResponse { Success = false, Message = "جواب مربوط به سوال نیست!" };
            if(answer.Response != Answer.QuestionerResponseEnum.NotSeen)
                return new CommonResponse { Success = false, Message = "به جواب قبلا واکنش نشان داده شده است!" };

            if (response == Answer.QuestionerResponseEnum.Accepted)
            {
                if (question.AcceptedAnswerId != ObjectId.Empty || DB.Any<Answer>(a => a.QuestionId == question.Id && a.Response == Answer.QuestionerResponseEnum.Accepted))
                    return new CommonResponse { Success = false, Message = "قبلا جوابی برای این سوال قبول شده است!" };

                question.AcceptedAnswerId = answer.Id;
                DB.Save(question);

                Student answererStudent = DB.FindById<Student>(answer.UserId);
                if (answererStudent != null)
                {
                    answererStudent.CoinTransactions.Add(new CoinTransaction
                    {
                        Type = CoinTransaction.TransactionType.AnswerQuestion,
                        Amount = question.Prize,
                        SourceId = question.Id
                    });
                    answererStudent.Coins += question.Prize;
                    answererStudent.Points += question.Prize;
                    answererStudent.PointsFromOtherQuestions += question.Prize;
                    DB.Save(answererStudent);
                }
            }

            answer.Response = response;
            DB.Save(answer);

            return new CommonResponse { Success = true };
        }

        //public ActionResult<ListResponse<Answer>> List([FromHeader] Guid token, string questionId)
        //{
        //    Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
        //    if (student == null)
        //        return Unauthorized();
        //    Question question = DB.FindById<Question>(questionId);
        //    if (question == null)
        //        return new ListResponse<Answer> { Success = false, Message = "سوال یافت نشد!" };
        //    if (question.UserId != student.Id)
        //        return new ListResponse<Answer> { Success = false, Message = "سوال شما نیست!" };

        //    var list = DB.Find<Answer>(a => a.QuestionId == question.Id && 
        //                                    a.Response == Answer.QuestionerResponseEnum.NotSeen && 
        //                                    a.PublishStatus == BaseUserContent.PublishStatusEnum.Published).ToList();
        //    return new ListResponse<Answer> { Success = true, List = list };
        //}
    }
}