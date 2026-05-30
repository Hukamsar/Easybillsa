namespace EasyBill.UI.Service.Whatsapp
{
    public class WhatsAppService
    {
        private readonly HttpClient _httpClient;
        //private const string AuthToken = "373044454d4f574850393431383130301745060406";
        //private const string BaseUrl = "http://wapp.powerstext.in/http-tokenkeyapi.php";
        private const string Username = "Hukamsar";
        private const string Token = "QXoyWTJ5Q28wUGFpVHJrUTNQZGFFQT09";
        private const string BaseUrl = "https://int.chatway.in/api/send-file";
        public WhatsAppService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SendWhatsAppMessageAsync(string phoneNumber, string message, string mediaUrl)
        {
            var sendResult = await SendWhatsAppMessageWithStatusAsync(phoneNumber, message, mediaUrl);
            return string.IsNullOrWhiteSpace(sendResult.RawResponse)
                ? sendResult.Message
                : sendResult.RawResponse;
        }

        public async Task<WhatsAppSendResult> SendWhatsAppMessageWithStatusAsync(string phoneNumber, string message, string mediaUrl)
        {
            var normalizedPhone = NormalizeIndianMobileNumber(phoneNumber);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                return new WhatsAppSendResult
                {
                    IsSuccess = false,
                    Message = "Invalid mobile number for WhatsApp.",
                    RawResponse = string.Empty,
                    NormalizedPhoneNumber = string.Empty
                };
            }

            var safeMessage = message ?? string.Empty;
            var safeMediaUrl = mediaUrl ?? string.Empty;
            var fileName = Path.GetFileName(safeMediaUrl);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "invoice.pdf";
            }

            //var url =
            //    $"{BaseUrl}?authentic-key={Uri.EscapeDataString(AuthToken)}" +
            //    $"&route=1" +
            //    $"&number={Uri.EscapeDataString(normalizedPhone)}" +
            //    $"&message={Uri.EscapeDataString(safeMessage)}" +
            //    $"&fileurl={Uri.EscapeDataString(safeMediaUrl)}" +
            //    $"&filename={Uri.EscapeDataString(fileName)}";

            var url =
         $"{BaseUrl}?" +
         $"username={Uri.EscapeDataString(Username)}" +
         $"&number={Uri.EscapeDataString(normalizedPhone)}" +
         $"&message={Uri.EscapeDataString(safeMessage)}" +
         $"&token={Uri.EscapeDataString(Token)}" +
         $"&file_url={Uri.EscapeDataString(safeMediaUrl)}" +
         $"&file_name={Uri.EscapeDataString(fileName)}";
            try
            {
                var response = await _httpClient.GetAsync(url);
                var rawResponse = await response.Content.ReadAsStringAsync();
                var isSuccess = response.IsSuccessStatusCode && !LooksLikeFailureResponse(rawResponse);

                return new WhatsAppSendResult
                {
                    IsSuccess = isSuccess,
                    Message = isSuccess
                        ? "WhatsApp message sent successfully."
                        : BuildFailureMessage(response.StatusCode.ToString(), rawResponse),
                    RawResponse = rawResponse,
                    NormalizedPhoneNumber = normalizedPhone
                };
            }
            catch (Exception ex)
            {
                return new WhatsAppSendResult
                {
                    IsSuccess = false,
                    Message = $"WhatsApp send failed: {ex.Message}",
                    RawResponse = string.Empty,
                    NormalizedPhoneNumber = normalizedPhone
                };
            }
        }

        private static string? NormalizeIndianMobileNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digits))
            {
                return null;
            }

            if (digits.Length < 10)
            {
                return null;
            }

            if (digits.Length > 10)
            {
                digits = digits[^10..];
            }

            return "91" + digits;
        }

        private static bool LooksLikeFailureResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return true;
            }

            var normalized = responseText.Trim().ToLowerInvariant();
            return normalized.Contains("error") ||
                   normalized.Contains("fail") ||
                   normalized.Contains("invalid") ||
                   normalized.Contains("insufficient") ||
                   normalized.Contains("unauthor");
        }

        private static string BuildFailureMessage(string statusCode, string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return $"WhatsApp send failed with status {statusCode}.";
            }

            var trimmed = responseText.Trim();
            if (trimmed.Length > 180)
            {
                trimmed = trimmed[..180];
            }

            return $"WhatsApp send failed: {trimmed}";
        }
    }

    public sealed class WhatsAppSendResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RawResponse { get; set; } = string.Empty;
        public string NormalizedPhoneNumber { get; set; } = string.Empty;
    }
}
