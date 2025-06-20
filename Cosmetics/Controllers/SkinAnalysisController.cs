using Cosmetics.Service.SkinAnalysisService;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cosmetics.Controllers
{
    public class SkinAnalysisController : ControllerBase
    {
        private readonly ISkinAnalysisService _service;

        public SkinAnalysisController(ISkinAnalysisService service)
        {
            _service = service;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartAnalysis(IFormFile image)
        {
            if (image == null || image.Length == 0) return BadRequest("Không có ảnh được cung cấp.");
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized("User is not authenticated.");
                }

                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    return BadRequest("Invalid user ID.");
                }
                var taskId = await _service.StartSkinAnalysisAsync(image, userId);
                return Ok(new { taskId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("status/{taskId}")]
        public async Task<IActionResult> CheckStatus(string taskId)
        {
            try
            {
                var result = await _service.CheckTaskStatusAsync(taskId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
