using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Cosmetics.DTO.KOLVideos;
using Cosmetics.DTO.User;
using Cosmetics.Models;
using Cosmetics.Repositories.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cosmetics.Controllers
{
	[Authorize]
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

        [Authorize(Roles = "Affiliates")]
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
			if(videos == null || videos.Count() == 0)
			{
                return Ok(new { Message = "You currently don’t have any videos." });
            }

			return Ok(_mapper.Map<List<KOLVideoDTO>>(videos));
		}

        [Authorize(Roles = "Affiliates")]
        [HttpPost("upload")]
		[RequestSizeLimit(300 * 1024 * 1024)]
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

			if(dto.VideoFile.Length > 300 * 1024 * 1024)
			{
				return BadRequest("File too large. Max size allowed is 300MB");
			}

			var uploadParams = new VideoUploadParams
			{
				File = new CloudinaryDotNet.FileDescription(dto.VideoFile.FileName, dto.VideoFile.OpenReadStream()),
				Folder = "kol-videos",
			};

			var uploadResult = await _cloudinary.UploadLargeAsync(uploadParams);

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

        [Authorize(Roles = "Affiliates")]
        [HttpGet("{id:guid}")]
		public async Task<IActionResult> GetVideoById(Guid id)
		{
			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
			var profile = await _unitOfWork.AffiliateProfiles.GetByUserIdAsync(userId);
			if(profile == null)
			{
				return BadRequest("Affiliate profile not found");
			}

			var video = await _unitOfWork.KolVideos.GetByIdAsync(id);
			if(video == null || video.AffiliateProfileId != profile.AffiliateProfileId)
			{
				return NotFound($"You currently don't have any video with ID: {id}");
			}

			var videoDTO = _mapper.Map<KOLVideoDTO>(video);

			return Ok(new ApiResponse
			{
				Success = true,
				Message = $"Successfully retrieved the video by ID: {id}",
				Data = videoDTO
			});
		}

        [Authorize(Roles = "Affiliates")]
        [HttpDelete("{id:guid}")]
		public async Task<IActionResult> DeleteVideo(Guid id)
		{
			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
			var profile = await _unitOfWork.AffiliateProfiles.GetByUserIdAsync(userId);
			if(profile == null)
			{
                return BadRequest("Affiliate profile not found");
            }

			var video = await _unitOfWork.KolVideos.GetByIdAsync(id);
            if (video == null || video.AffiliateProfileId != profile.AffiliateProfileId)
            {
                return NotFound($"You currently don't have any video with ID: {id}");
            }

			_unitOfWork.KolVideos.Delete(video);
			await _unitOfWork.CompleteAsync();

			return Ok(new ApiResponse
			{
				Success = true,
				Message = $"Delete video with the ID {id} successfully."
			});
        }

        [Authorize(Roles = "Affiliates")]
        [HttpPut("{id:guid}")]
		public async Task<IActionResult> UpdateVideo(Guid id, [FromBody] KOLVideoUpdateDTO dto)
		{
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var profile = await _unitOfWork.AffiliateProfiles.GetByUserIdAsync(userId);
            if (profile == null)
            {
                return BadRequest("Affiliate profile not found");
            }

			var video = await _unitOfWork.KolVideos.GetByIdAsync(id);
            if (video == null || video.AffiliateProfileId != profile.AffiliateProfileId)
            {
                return NotFound($"You currently don't have any video with ID: {id}");
            }

			video.Title = dto.Title;
			video.Description = dto.Description;
			video.ProductId = dto.ProductId.GetValueOrDefault();
			video.IsActive = dto.IsActive;

			await _unitOfWork.KolVideos.UpdateAsync(video);
			await _unitOfWork.CompleteAsync();

			return Ok(new ApiResponse
			{
				Success = true,
				Message = "Update successfully.",
				Data = _mapper.Map<KOLVideoDTO>(video)
			});
        }

		[HttpGet("getAllVideos")]
		public async Task<IActionResult> GetAllVideos()
		{
			var videos = await _unitOfWork.KolVideos.GetAllAsync();

			if(videos == null || !videos.Any())
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = "There are currently no videos in the system.",
				});
			}

			var videoDTOs = _mapper.Map<List<KOLVideoDTO>>(videos);

			return Ok(new ApiResponse
			{
				Success = true,
				Message = "Successfully retrieved all videos",
				Data = videoDTOs
			});
		}

		[HttpGet("getAllVideosBy/{id:guid}")]
		public async Task<IActionResult> GetAllVideosById(Guid id)
		{
			var video = await _unitOfWork.KolVideos.GetByIdAsync(id);

			if(video == null)
			{
				return NotFound($"No video found with ID: {id}");
			}

			var videoDTO = _mapper.Map<KOLVideoDTO>(video);

			return Ok(new ApiResponse
			{
				Success = true,
				Message = $"Successfully retrieved the video by ID: {id}",
				Data = videoDTO
			});
		}
    }
}
