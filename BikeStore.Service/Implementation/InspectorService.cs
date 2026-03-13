using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Inspector;
using BikeStore.Common.DTOs.Seller.Media;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Implementation;

public class InspectorService : IInspectorService
{
    private readonly IGenericRepository<Bike> _bikeRepo;
    private readonly IGenericRepository<Inspection> _inspectionRepo;
    private readonly IGenericRepository<Media> _mediaRepo;
    private readonly IGenericRepository<Listing> _listingRepo;
    private readonly IUnitOfWork _uow;

    public InspectorService(
        IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Inspection> inspectionRepo,
        IGenericRepository<Media> mediaRepo,
        IGenericRepository<Listing> listingRepo,
        IUnitOfWork uow)
    {
        _bikeRepo = bikeRepo;
        _inspectionRepo = inspectionRepo;
        _mediaRepo = mediaRepo;
        _listingRepo = listingRepo;
        _uow = uow;
    }


    public async Task<PagedResult<BikePendingInspectionListDto>> GetPendingBikesAsync(int pageNumber, int pageSize)
    {
        var res = await _bikeRepo.GetAllDataByExpression(
            filter: b => b.Status == BikeStatusEnum.PendingInspection
                      && b.InspectionId == null
                      && b.Listing.Status == ListingStatusEnum.Active,
            pageNumber: pageNumber,
            pageSize: pageSize,
            orderBy: b => b.CreatedAt,
            isAscending: false,
            includes: b => b.Listing
        );

        var bikes = res.Items?.ToList() ?? new List<Bike>();
        var bikeIds = bikes.Select(b => b.Id).ToList();
        var listingIds = bikes.Select(b => b.ListingId).Distinct().ToList();

        var mediasRes = await _mediaRepo.GetAllDataByExpression(
            filter: m => bikeIds.Contains(m.BikeId),
            pageNumber: 1,
            pageSize: 5000,
            orderBy: m => m.Id,
            isAscending: true
        );

        var listingsRes = await _listingRepo.GetAllDataByExpression(
            filter: l => listingIds.Contains(l.Id),
            pageNumber: 1,
            pageSize: 5000,
            orderBy: l => l.CreatedAt,
            isAscending: false,
            includes: l => l.User
        );

        var thumbnailMap = (mediasRes.Items ?? new List<Media>())
            .GroupBy(m => m.BikeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => m.Image).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            );

        var listingMap = (listingsRes.Items ?? new List<Listing>())
            .ToDictionary(l => l.Id, l => l);

        return new PagedResult<BikePendingInspectionListDto>
        {
            TotalPages = res.TotalPages,
            Items = bikes.Select(b =>
            {
                listingMap.TryGetValue(b.ListingId, out var listing);
                thumbnailMap.TryGetValue(b.Id, out var thumb);

                return new BikePendingInspectionListDto
                {
                    Id = b.Id,
                    ListingId = b.ListingId,
                    BikeName = $"{b.Brand} {b.Category}",
                    BikeCode = $"XE-{b.Id.ToString("N")[..4].ToUpper()}",
                    Thumbnail = thumb,
                    SellerName = listing?.User?.FullName ?? "",
                    SellerPhoneNumber = listing?.User?.PhoneNumber ?? "",
                    CreatedAt = b.CreatedAt,
                    BikeStatus = b.Status.ToString()
                };
            }).ToList()
        };
    }

    public async Task<BikePendingInspectionDto?> GetPendingBikeDetailsAsync(Guid pendingBikeId)
    {
        var bike = await _bikeRepo.GetFirstByExpression(
            b => b.Id == pendingBikeId
              && b.Status == BikeStatusEnum.PendingInspection
              && b.InspectionId == null
              && b.Listing.Status == ListingStatusEnum.Active,
            b => b.Listing
        );

        if (bike == null) return null;

        var mediaRes = await _mediaRepo.GetAllDataByExpression(
            filter: m => m.BikeId == bike.Id,
            pageNumber: 1,
            pageSize: 1000,
            orderBy: m => m.Id,
            isAscending: true
        );

        return new BikePendingInspectionDto
        {
            Id = bike.Id,
            ListingId = bike.ListingId,

            Category = bike.Category,
            Brand = bike.Brand,
            FrameSize = bike.FrameSize,
            FrameMaterial = bike.FrameMaterial,
            Paint = bike.Paint,
            Groupset = bike.Groupset,
            Operating = bike.Operating,
            TireRim = bike.TireRim,
            BrakeType = bike.BrakeType,
            Overall = bike.Overall,
            Price = bike.Price,

            BikeStatus = bike.Status.ToString(),
            ListingStatus = bike.Listing.Status.ToString(),
            ListingDescription = bike.Listing.Description,
            CreatedAt = bike.CreatedAt,

            Medias = mediaRes.Items?.Select(m => new SellerMediaDto
            {
                Id = m.Id,
                BikeId = m.BikeId,
                Image = m.Image,
                VideoUrl = m.VideoUrl
            }).ToList() ?? new List<SellerMediaDto>()
        };
    }
    public async Task<(bool Success, string Message)> ApproveBikeAsync(Guid inspectorId, Guid bikeId, ApproveBikeDto dto)
    {
        var bike = await _bikeRepo.GetFirstByExpression(
            b => b.Id == bikeId,
            b => b.Listing
        );

        if (bike == null)
            return (false, "Không tìm thấy xe (Bike).");

        if (bike.Listing == null)
            return (false, "Bike chưa liên kết listing.");

        if (bike.Listing.Status != ListingStatusEnum.Active)
            return (false, "Listing của xe này không ở trạng thái Active.");

        if (bike.Status != BikeStatusEnum.PendingInspection)
            return (false, "Bike không ở trạng thái PendingInspection.");

        if (bike.InspectionId != null)
            return (false, "Bike này đã được kiểm định trước đó.");

        var failedCount = 0;
        if (!dto.Frame) failedCount++;
        if (!dto.PaintCondition) failedCount++;
        if (!dto.Drivetrain) failedCount++;
        if (!dto.Brakes) failedCount++;

        var now = DateTimeHelper.NowVN();

        var inspection = new Inspection
        {
            Id = Guid.NewGuid(),
            UserId = inspectorId,
            Frame = dto.Frame,
            PaintCondition = dto.PaintCondition,
            Drivetrain = dto.Drivetrain,
            Brakes = dto.Brakes,
            Score = dto.Score,
            Comment = dto.Comment,
            InspectionDate = now,
            CreatedAt = now
        };

        await _inspectionRepo.Insert(inspection);

        bike.InspectionId = inspection.Id;
        bike.UpdatedAt = now;

        
        if (failedCount >= 3 || dto.Score < 50)
        {
            bike.Status = BikeStatusEnum.Disabled;
            bike.Listing.Status = ListingStatusEnum.Rejected;
            bike.Listing.UpdatedAt = now;

            await _bikeRepo.Update(bike);
            await _listingRepo.Update(bike.Listing);
            await _uow.SaveChangeAsync();

            return (false, "Bike không đạt kiểm định. Hệ thống đã tự động reject: Bike = Disabled, Listing = Rejected.");
        }

        
        bike.Status = BikeStatusEnum.Available;

        await _bikeRepo.Update(bike);
        await _uow.SaveChangeAsync();

        return (true, "Đã kiểm định thành công. Bike chuyển sang Available.");
    }

    public async Task<(bool Success, string Message)> RejectBikeAsync(Guid inspectorId, Guid bikeId, string? comment)
    {
        var bike = await _bikeRepo.GetFirstByExpression(
            b => b.Id == bikeId,
            b => b.Listing
        );

        if (bike == null)
            return (false, "Không tìm thấy xe (Bike).");

        if (bike.Listing == null)
            return (false, "Bike chưa liên kết listing.");

        if (bike.Listing.Status != ListingStatusEnum.Active)
            return (false, "Listing của xe này không ở trạng thái Active.");

        if (bike.Status != BikeStatusEnum.PendingInspection)
            return (false, "Bike không ở trạng thái PendingInspection.");

        var now = DateTimeHelper.NowVN();

        var inspection = new Inspection
        {
            Id = Guid.NewGuid(),
            UserId = inspectorId,
            Frame = false,
            PaintCondition = false,
            Drivetrain = false,
            Brakes = false,
            Score = 0,
            Comment = comment,
            InspectionDate = now,
            CreatedAt = now
        };

        await _inspectionRepo.Insert(inspection);

        bike.InspectionId = inspection.Id;
        bike.Status = BikeStatusEnum.Disabled; 
        bike.UpdatedAt = now;

        bike.Listing.Status = ListingStatusEnum.Rejected; 
        bike.Listing.UpdatedAt = now;

        await _bikeRepo.Update(bike);
        await _listingRepo.Update(bike.Listing);
        await _uow.SaveChangeAsync();

        return (true, "Đã từ chối kiểm duyệt. Bike chuyển sang Disabled và Listing chuyển sang Rejected.");
    }
}