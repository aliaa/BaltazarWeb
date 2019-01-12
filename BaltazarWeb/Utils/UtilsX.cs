using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
