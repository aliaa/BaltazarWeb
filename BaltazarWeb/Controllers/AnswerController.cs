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
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    public class AnswerController : Controller
    {
        public const string UPLOAD_DIR = "Uploads";
        private readonly MongoHelper DB;
        private readonly string UploadPath;

        public AnswerController(MongoHelper DB, IHostingEnvironment env)
        {
            this.DB = DB;
            UploadPath = Path.Combine(env.WebRootPath, UPLOAD_DIR);
            if (!Directory.Exists(UploadPath))
                Directory.CreateDirectory(UploadPath);
        }

        [Authorize]
        public IActionResult ApproveList()
        {
            return View(DB.Find<Answer>(a => a.PublishStatus == BaseUserContent.PublishStatusEnum.WaitForApprove).SortBy(q => q.CreateDate));
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
        public ActionResult<CommonResponse> Publish([FromHeader] Guid token, [FromBody] Answer answer)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();

            Question question = DB.FindById<Question>(answer.QuestionId);
            if (question == null)
                return new CommonResponse { Success = true, Message = "سوال یافت نشد!" };
            if(question.PublishStatus != BaseUserContent.PublishStatusEnum.Published)

            answer.Id = ObjectId.Empty;
            answer.PublishStatus = BaseUserContent.PublishStatusEnum.WaitForApprove;
            answer.UserId = student.Id;
            answer.HasImage = answer.ImageFile != null;
            answer.Response = Answer.QuestionerResponseEnum.NotSeen;

            DB.Save(answer);

            if (answer.HasImage)
            {
                string filePath = Path.Combine(UploadPath, answer.Id.ToString() + ".jpg");
                using (FileStream fs = new FileStream(filePath, FileMode.CreateNew))
                {
                    answer.ImageFile.CopyTo(fs);
                }
            }

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
                question.AcceptedAnswerId = answer.Id;
                DB.Save(question);
            }

            answer.Response = response;
            DB.Save(answer);

            return new CommonResponse { Success = true };
        }

        public ActionResult<ListResponse<Answer>> List([FromHeader] Guid token, string questionId)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            Question question = DB.FindById<Question>(questionId);
            if (question == null)
                return new ListResponse<Answer> { Success = false, Message = "سوال یافت نشد!" };
            if (question.UserId != student.Id)
                return new ListResponse<Answer> { Success = false, Message = "سوال شما نیست!" };

            var list = DB.Find<Answer>(a => a.QuestionId == question.Id && 
                                            a.Response == Answer.QuestionerResponseEnum.NotSeen && 
                                            a.PublishStatus == BaseUserContent.PublishStatusEnum.Published).ToList();
            return new ListResponse<Answer> { Success = true, List = list };
        }
    }
}