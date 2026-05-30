using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace EasyBill.UI.ModelBinders
{
    public class ExpiryDateModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            // If empty, return null
            if (string.IsNullOrWhiteSpace(value))
            {
                return Task.CompletedTask;
            }

            // ✅ Parse MM/YYYY or MM/YY format
            DateTime? result = ParseExpiryDate(value);

            if (result.HasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(result.Value);
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }

            return Task.CompletedTask;
        }

        private DateTime? ParseExpiryDate(string expiryStr)
        {
            if (string.IsNullOrWhiteSpace(expiryStr))
                return null;

            try
            {
                expiryStr = expiryStr.Trim();

                var parts = expiryStr.Split('/');

                if (parts.Length != 2)
                    return null;

                if (!int.TryParse(parts[0], out int month))
                    return null;

                if (!int.TryParse(parts[1], out int yearInput))
                    return null;

                // Validate month
                if (month < 1 || month > 12)
                    return null;

                // ✅ Handle both 2-digit and 4-digit year
                int year;

                if (yearInput >= 0 && yearInput <= 99)
                {
                    // 2-digit year: 00-99 → 2000-2099
                    year = 2000 + yearInput;
                }
                else if (yearInput >= 1900 && yearInput <= 9999)
                {
                    // 4-digit year: use as-is
                    year = yearInput;
                }
                else
                {
                    return null;
                }

                // ✅ Day is always 1
                return new DateTime(year, month, 1);
            }
            catch
            {
                return null;
            }
        }
    }
}