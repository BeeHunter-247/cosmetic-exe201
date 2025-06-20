using Cosmetics.DTO.AIScanner;
using Cosmetics.Models;
using Cosmetics.Repositories.UnitOfWork;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Cosmetics.Service.SkinAnalysisService
{
    public class SkinAnalysisService : ISkinAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl;
        private static int _requestIdCounter = 0;

        public SkinAnalysisService(HttpClient httpClient, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _unitOfWork = unitOfWork;
            _clientId = configuration["YouCamApi:ClientId"];
            _clientSecret = configuration["YouCamApi:ClientSecret"];
            _baseUrl = configuration["YouCamApi:BaseUrl"];
        }

        public async Task<string> StartSkinAnalysisAsync(IFormFile image, int userId)
        {
            // Kiểm tra kích thước và định dạng ảnh
            if (image.Length > 10 * 1024 * 1024) throw new Exception("Kích thước ảnh vượt quá 10MB.");
            if (!new[] { "image/jpeg", "image/png" }.Contains(image.ContentType)) throw new Exception("Định dạng ảnh không hợp lệ.");

            var fileName = $"{Guid.NewGuid()}_{image.FileName}";

            // Bước 1: Gọi API để lấy file_id và chi tiết tải lên
            var fileUploadResponse = await UploadFileAsync(image, fileName);
            var fileId = fileUploadResponse.Result.Files[0].FileId;
            var uploadUrl = fileUploadResponse.Result.Files[0].Requests[0].Url;
            var uploadHeaders = fileUploadResponse.Result.Files[0].Requests[0].Headers;
            var uploadMethod = fileUploadResponse.Result.Files[0].Requests[0].Method;

            // Bước 2: Tải nội dung ảnh lên API mà không lưu trữ cục bộ
            using var fileStream = image.OpenReadStream();
            await UploadFileContentAsync(uploadUrl, uploadMethod, uploadHeaders, fileStream);

            // Bước 3: Lưu image entity với FilePath là file_id
            var imageEntity = new Image
            {
                UserId = userId,
                FileName = fileName,
                FilePath = fileId,  // Lưu file_id vào FilePath
                UploadedAt = DateTime.UtcNow
            };
            await _unitOfWork.Image.AddAsync(imageEntity);
            await _unitOfWork.CompleteAsync();

            // Bước 4: Chạy tác vụ phân tích da
            var taskId = await RunSkinAnalysisTaskAsync(fileId);

            // Bước 5: Lưu kết quả phân tích ban đầu
            var analysisResult = new SkinAnalysisResult
            {
                ImageId = imageEntity.ImageId,
                AnalysisData = JsonConvert.SerializeObject(new { taskId, status = "running" }),
                AnalyzedAt = DateTime.UtcNow
            };
            await _unitOfWork.SkinAnalysisResult.AddAsync(analysisResult);
            await _unitOfWork.CompleteAsync();

            return taskId;
        }

        public async Task<CheckStatusResponse> CheckTaskStatusAsync(string taskId)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());
            var response = await _httpClient.GetAsync($"{_baseUrl}/s2s/v1.0/task/skin-analysis?task_id={taskId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gọi API kiểm tra trạng thái thất bại: {response.ReasonPhrase}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var statusResponse = JsonConvert.DeserializeObject<CheckStatusResponse>(json);

            if (statusResponse.Result.Status == "success" || statusResponse.Result.Status == "error")
            {
                var searchString = $"\"taskId\":\"{taskId}\"";
                var analysisResult = await _unitOfWork.SkinAnalysisResult.FirstOrDefaultAsync(r => r.AnalysisData.Contains(searchString));
                if (analysisResult != null)
                {
                    analysisResult.AnalysisData = json;
                    analysisResult.AnalyzedAt = DateTime.UtcNow;
                    await _unitOfWork.CompleteAsync();
                }
            }

            return statusResponse;
        }

        private async Task<FileUploadResponse> UploadFileAsync(IFormFile image, string fileName)
        {
            var payload = new FileUploadRequest
            {
                Files = new List<DTO.AIScanner.FileInfo>
            {
                new DTO.AIScanner.FileInfo
                {
                    ContentType = image.ContentType,
                    FileName = fileName,
                    FileSize = image.Length
                }
            }
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/s2s/v1.1/file/skin-analysis", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gọi API tải file thất bại: {response.ReasonPhrase}");
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<FileUploadResponse>(json);
        }

        private async Task UploadFileContentAsync(string url, string method, Dictionary<string, string> headers, Stream fileStream)
        {
            using var content = new StreamContent(fileStream);
            foreach (var header in headers)
            {
                content.Headers.Add(header.Key, header.Value);
            }
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            request.Content = content;
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Tải nội dung file thất bại: {response.ReasonPhrase}");
            }
        }

        private async Task<string> RunSkinAnalysisTaskAsync(string fileId)
        {
            var requestId = Interlocked.Increment(ref _requestIdCounter);
            var payload = new RunTaskRequest
            {
                RequestId = requestId,
                Payload = new Payload
                {
                    FileSets = new FileSets { SrcIds = new List<string> { fileId } },
                    Actions = new List<DTO.AIScanner.Action>
                {
                    new DTO.AIScanner.Action
                    {
                        Id = 0,
                        Params = new Params
                        {
                            DstActions = new List<string>
                            {
                                "hd_redness", "hd_oiliness", "hd_age_spot", "hd_radiance", "hd_moisture",
                                "hd_dark_circle", "hd_eye_bag", "hd_droopy_upper_eyelid", "hd_droopy_lower_eyelid",
                                "hd_firmness", "hd_texture", "hd_acne", "hd_pore", "hd_wrinkle"
                            }
                        }
                    }
                }
                }
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/s2s/v1.0/task/skin-analysis", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gọi API chạy tác vụ thất bại: {response.ReasonPhrase}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var taskResponse = JsonConvert.DeserializeObject<RunTaskResponse>(json);
            return taskResponse.Result.TaskId;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var content = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("client_id", _clientId),
        new KeyValuePair<string, string>("client_secret", _clientSecret),
        new KeyValuePair<string, string>("grant_type", "client_credentials")
    });

            var response = await _httpClient.PostAsync("/s2s/v1.0/token", content);

            if (!response.IsSuccessStatusCode)
            {
                var raw = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lấy token thất bại: {(int)response.StatusCode} - {response.ReasonPhrase}\n{raw}");
            }

            var json = await response.Content.ReadAsStringAsync();
            dynamic tokenData = JsonConvert.DeserializeObject(json);
            return tokenData.access_token;
        }



    }
}
