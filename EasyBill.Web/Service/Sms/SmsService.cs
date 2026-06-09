using Microsoft.Extensions.Options;

namespace EasyBill.UI.Service.Sms
{
    public class SmsService
    {
        private readonly HttpClient _httpClient;

        // SMS API Configuration
        private const string BaseUrl = "http://bulk.powerstext.in/";
        private const string SenderId = "RMCLAG";
        private const string Token = "363644454d4f393431383130301745050774";
        private const string Route = "1";
        private const string TemplateId = "1607100000000379766";

        public SmsService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<SmsResponse> SendSmsAsync(string number, string message)
        {
            try
            {
                var url = $"{BaseUrl}" +
                          $"?authentic-key={Token}" +
                          $"&senderid={SenderId}" +
                          $"&route={Route}" +
                          $"&number={number}" +
                          $"&message={Uri.EscapeDataString(message)}" +
                          $"&templateid={TemplateId}";

                var response = await _httpClient.GetAsync(url);

                var result = await response.Content.ReadAsStringAsync();

                return new SmsResponse
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode
                        ? "SMS sent successfully."
                        : "SMS sending failed.",
                    ResponseData = result
                };
            }
            catch (Exception ex)
            {
                return new SmsResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
    }
    public class SmsResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ResponseData { get; set; } = string.Empty;
    }

}
