using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace EasyBill.UI.ModelBinders
{
    public class ExpiryDateModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // ✅ Only apply to ExpiryDate property
            if (context.Metadata.PropertyName == "ExpiryDate" &&
                context.Metadata.ModelType == typeof(DateTime?))
            {
                return new ExpiryDateModelBinder();
            }

            return null;
        }
    }
}