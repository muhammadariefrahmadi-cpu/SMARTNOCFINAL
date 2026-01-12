using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SMART_NOC.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;

        public GeminiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> ExtractTextFromImageAsync(StorageFile file, string apiKey)
        {
            try
            {
                // 1. Convert Image to Base64
                string base64Image = await ConvertImageToBase64(file);
                string mimeType = file.FileType.ToLower() == ".png" ? "image/png" : "image/jpeg";

                // 2. Prepare API URL (Pakai Model Gemini 1.5 Flash yang cepat & support vision)
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

                // 3. Prepare Request Body (Sesuai Dokumentasi Google Terbaru)
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = "Extract all text from this image. If there is a map or location info, estimate the coordinates (latitude, longitude). Return ONLY a JSON format like this: { \"coordinates\": \"-6.xxxx, 106.xxxx\", \"full_text\": \"...\" }. Do not use markdown code blocks." },
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = mimeType,
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    }
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 4. Send Request
                var response = await _httpClient.PostAsync(url, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error: {response.StatusCode} - {responseString}";
                }

                // 5. PARSING JSON RESPONSE (BAGIAN KRUSIAL)
                // Kita pakai JObject biar aman navigasinya
                JObject jsonResponse = JObject.Parse(responseString);

                // Navigasi: root -> candidates[0] -> content -> parts[0] -> text
                // Pakai tanda tanya '?' (Null Conditional) biar gak crash kalau ada yang null
                var textResult = jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                if (string.IsNullOrEmpty(textResult))
                {
                    return "Error: Gemini returned empty response.";
                }

                return textResult;
            }
            catch (Exception ex)
            {
                return $"Error Exception: {ex.Message}";
            }
        }

        private async Task<string> ConvertImageToBase64(StorageFile file)
        {
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                var reader = new DataReader(stream.GetInputStreamAt(0));
                await reader.LoadAsync((uint)stream.Size);
                byte[] bytes = new byte[stream.Size];
                reader.ReadBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }
    }
}