using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Cosmetics.DTO.KOLVideos;
using Cosmetics.Models;
using Cosmetics.Repositories.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cosmetics.Controllers
{
	[Authorize(Roles = "Affiliates")]
	[Route("api/[controller]")]
	[ApiController]
	public class KOLVideoController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly Cloudinary _cloudinary;

		public KOLVideoController(IUnitOfWork unitOfWork, IMapper mapper, Cloudinary cloudinary)
        {
            _unitOfWork = unitOfWork;
			_mapper = mapper;
			_cloudinary = cloudinary;
        }

		[HttpGet("myVideos")]
		public async Task<IActionResult> GetMyVideos()
		{
			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
			var profile = await _unitOfWork.AffiliateProfiles.GetByUserIdAsync(userId);
			if(profile == null)
			{
				return BadRequest("Affiliate profile not found");
			}

			var videos = await _unitOfWork.KolVideos.GetAllByAffiliateProfileIdAsync(profile.AffiliateProfileId);
			return Ok(_mapper.Map<List<KOLVideoDTO>>(videos));
		}

		[HttpPost("upload")]
		public async Task<IActionResult> UploadVideo([FromForm] KOLVideoCreateDTO dto)
		{
			if(dto.VideoFile == null || dto.VideoFile.Length == 0)
			{
				return BadRequest("No video file provided.");
			}

			var allowExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
			var extension = Path.GetExtension(dto.VideoFile.FileName).ToLowerInvariant();

			if(!allowExtensions.Contains(extension))
			{
				return BadRequest("Invalid video format. Allowed formats: mp4, mov, avi, mkv, webm.");
			}

			if(dto.VideoFile.Length > 100 * 1024 * 1024)
			{
				return BadRequest("File too large. Max size allowed is 100MD");
			}

			var uploadParams = new VideoUploadParams
			{
				File = new CloudinaryDotNet.FileDescription(dto.VideoFile.FileName, dto.VideoFile.OpenReadStream()),
				Folder = "kol-videos"
			};

			var uploadResult = await _cloudinary.UploadAsync(uploadParams);

			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
			var profile = await _unitOfWork.AffiliateProfiles.GetByUserIdAsync(userId);
			if (profile == null)
			{
				return BadRequest("Affiliate profile not found");
			}

			if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
			{
				var video = new Kolvideo
				{
					VideoId = Guid.NewGuid(),
					Title = dto.Title,
					Description = dto.Description,
					VideoUrl = uploadResult.SecureUrl.ToString(),
					ProductId = dto.ProductId,
					AffiliateProfileId = profile.AffiliateProfileId,
					CreatedAt = DateTime.UtcNow,
					IsActive = true,
				};

				await _unitOfWork.KolVideos.AddAsync(video);
				await _unitOfWork.CompleteAsync();

				var videoResponse = _mapper.Map<KOLVideoDTO>(video);

				return Ok(new { Url = uploadResult.SecureUrl.ToString(), PublicId = uploadResult.PublicId, VideoInfo = videoResponse });

			}
			else
			{
				return StatusCode(500, $"Upload error: {uploadResult.Error.Message}");
			}
		}
    }
}
