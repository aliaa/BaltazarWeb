using AliaaCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class UpgradeInfo : MongoEntity
    {
        public int AppVersionMax { get; set; }
        public string Message { get; set; }
        public bool Force { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }
}
