using Cosmetics.DTO.AIScanner;

namespace Cosmetics.Service.SkinAnalysisService
{
    public interface ISkinAnalysisService
    {
        /// <summary>
        /// Bắt đầu phân tích da từ ảnh đã tải lên.
        /// </summary>
        /// <param name="image">Ảnh khuôn mặt.</param>
        /// <param name="userId">ID người dùng.</param>
        /// <returns>ID của tác vụ phân tích da.</returns>
        Task<string> StartSkinAnalysisAsync(IFormFile image, int userId);

        /// <summary>
        /// Kiểm tra trạng thái tác vụ phân tích da dựa trên taskId.
        /// </summary>
        /// <param name="taskId">ID của tác vụ phân tích.</param>
        /// <returns>Kết quả kiểm tra trạng thái tác vụ.</returns>
        Task<CheckStatusResponse> CheckTaskStatusAsync(string taskId);
    }
}
