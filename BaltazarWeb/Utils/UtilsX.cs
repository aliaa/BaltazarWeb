using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaltazarWeb.Utils
{
    public static class UtilsX
    {
        public static IEnumerable<SelectListItem> EnumAsListItems(Type type)
        {
            foreach (var item in Enum.GetValues(type))
                yield return new SelectListItem(AliaaCommon.Utils.GetDisplayNameOfMember(type, item.ToString()), item.ToString());
        }

        public static string GetImageUrl(ObjectId id, int backCount = 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < backCount; i++)
                sb.Append("../");
            sb.Append(Consts.UPLOAD_IMAGE_DIR.Replace('\\', '/'))
                .Append("/").Append(id).Append(".jpg");
            return sb.ToString();
        }
    }
}
