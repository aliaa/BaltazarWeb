using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ApiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    public class ContactUsController : Controller
    {
        private readonly MongoHelper DB;

        public ContactUsController(MongoHelper DB)
        {
            this.DB = DB;
        }

        [Authorize(Policy = nameof(Permission.ViewContactUsMessages))]
        public IActionResult List()
        {
            var messages = DB.Find<ContactUsMessage>(_ => true).SortByDescending(m => m.Date).Limit(1000).ToList();
            return View(messages);
        }

        [HttpPost]
        public ActionResult<CommonResponse> Submit([FromHeader] Guid token, [FromBody] ContactUsMessage message)
        {
            if (token != Guid.Empty)
                message.StudentId = DB.Find<Student>(s => s.Token == token).Project(s => s.Id).FirstOrDefault();
            message.Date = DateTime.Now;
            message.FromAndroid = true;
            DB.Save(message);
            return new CommonResponse { Success = true, Message = "نظر شما با موفقیت ثبت شد! از شما متشکریم!" };
        }
    }
}