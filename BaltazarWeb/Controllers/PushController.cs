using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ViewModels;
using BaltazarWeb.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaltazarWeb.Controllers
{
    [Authorize(Policy = nameof(Permission.SendPush))]
    public class PushController : Controller
    {
        private readonly MongoHelper DB;
        private readonly PusheAPI push;
        public PushController(MongoHelper DB, PusheAPI push)
        {
            this.DB = DB;
            this.push = push;
        }
        
        public IActionResult SendNew(string pusheId = null)
        {
            SendPushViewModel data = new SendPushViewModel
            {
                SpecificUserPusheId = pusheId
            };
            return View(data);
        }

        [HttpPost]
        public IActionResult SendNew(SendPushViewModel data)
        {
            bool result;
            if (string.IsNullOrEmpty(data.SpecificUserPusheId))
                result = push.SendMessageToAll(data.Title, data.Content);
            else
            {
                var users = new List<string>();
                users.AddRange(data.SpecificUserPusheId.Split(";"));
                result = push.SendMessageToUsers(data.Title, data.Content, users);
            }
            ViewData["result"] = result;
            return View();
        }
    }
}