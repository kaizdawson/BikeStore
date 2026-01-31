using BikeStore.Common.DTOs.Seller.Media;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BikeStore.Service.Implementation;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly IGenericRepository<Bike> _bikeRepo;
    private readonly IGenericRepository<Media> _mediaRepo;
    private readonly IUnitOfWork _uow;

    public CloudinaryService(
        IOptions<CloudinarySettings> config,
        IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Media> mediaRepo,
        IUnitOfWork uow)
    {
        var acc = new Account(config.Value.CloudName, config.Value.ApiKey, config.Value.ApiSecret);
        _cloudinary = new Cloudinary(acc) { Api = { Secure = true } };

        _bikeRepo = bikeRepo;
        _mediaRepo = mediaRepo;
        _uow = uow;
    }

    public async Task<SellerMediaDto> UploadBikeImageAndSaveAsync(Guid sellerId, Guid bikeId, IFormFile file)
    {
        var bike = await GetOwnedBikeAsync(sellerId, bikeId);

        ValidateFile(file, maxMb: 5);
        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            throw new InvalidOperationException("Chỉ cho phép ảnh JPG/PNG/WEBP.");

        var folder = $"bikestore/bikes/{bikeId}/images";

        await using var stream = file.OpenReadStream();
        var url = await UploadImageToCloudinary(stream, file.FileName, folder);

        var media = new Media
        {
            Id = Guid.NewGuid(),
            BikeId = bike.Id,
            Image = url,
            VideoUrl = null,
        };

        await _mediaRepo.Insert(media);
        await _uow.SaveChangeAsync();

        return new SellerMediaDto
        {
            Id = media.Id,
            BikeId = media.BikeId,
            Image = media.Image,
            VideoUrl = media.VideoUrl,

        };
    }

    public async Task<SellerMediaDto> UploadBikeVideoAndSaveAsync(Guid sellerId, Guid bikeId, IFormFile file)
    {
        var bike = await GetOwnedBikeAsync(sellerId, bikeId);

        ValidateFile(file, maxMb: 50);
        var allowed = new[] { "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska" };
        if (!allowed.Contains(file.ContentType))
            throw new InvalidOperationException("Chỉ cho phép video MP4/MOV/AVI/MKV.");

        var folder = $"bikestore/bikes/{bikeId}/videos";

        await using var stream = file.OpenReadStream();
        var url = await UploadVideoToCloudinary(stream, file.FileName, folder);

        var now = DateTimeHelper.NowVN();
        var media = new Media
        {
            Id = Guid.NewGuid(),
            BikeId = bike.Id,
            Image = null,
            VideoUrl = url,
        };

        await _mediaRepo.Insert(media);
        await _uow.SaveChangeAsync();

        return new SellerMediaDto
        {
            Id = media.Id,
            BikeId = media.BikeId,
            Image = media.Image,
            VideoUrl = media.VideoUrl,
        };
    }

    private async Task<Bike> GetOwnedBikeAsync(Guid sellerId, Guid bikeId)
    {
        if (bikeId == Guid.Empty)
            throw new InvalidOperationException("BikeId không hợp lệ.");

        var bike = await _bikeRepo.GetFirstByExpression(b => b.Id == bikeId, b => b.Listing);
        if (bike == null)
            throw new InvalidOperationException("Không tìm thấy xe (Bike).");

        if (bike.Listing.UserId != sellerId)
            throw new InvalidOperationException("Bạn không có quyền upload media cho xe này.");

        return bike;
    }

    private static void ValidateFile(IFormFile file, int maxMb)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File không hợp lệ.");

        if (file.Length > maxMb * 1024 * 1024)
            throw new InvalidOperationException($"File quá lớn (tối đa {maxMb}MB).");
    }

    private async Task<string> UploadImageToCloudinary(Stream stream, string fileName, string folder)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var res = await _cloudinary.UploadAsync(uploadParams);
        if (res.Error != null)
            throw new InvalidOperationException("Upload ảnh thất bại: " + res.Error.Message);

        return res.SecureUrl.ToString();
    }

    private async Task<string> UploadVideoToCloudinary(Stream stream, string fileName, string folder)
    {
        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var res = await _cloudinary.UploadAsync(uploadParams);
        if (res.Error != null)
            throw new InvalidOperationException("Upload video thất bại: " + res.Error.Message);

        return res.SecureUrl.ToString();
    }
}