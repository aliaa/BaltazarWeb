using Microsoft.AspNetCore.Mvc.ModelBinding;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Utils
{
    public class ObjectIdBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            ObjectId id = ObjectId.Empty;
            ObjectId.TryParse(result.FirstValue, out id);
            bindingContext.Result = ModelBindingResult.Success(id);
            return Task.CompletedTask;
        }
    }
}
