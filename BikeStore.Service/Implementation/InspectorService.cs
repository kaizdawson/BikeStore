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
    private readonly IUnitOfWork _uow;

    public InspectorService(
        IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Inspection> inspectionRepo,
        IGenericRepository<Media> mediaRepo,
        IUnitOfWork uow)
    {
        _bikeRepo = bikeRepo;
        _inspectionRepo = inspectionRepo;
        _mediaRepo = mediaRepo;
        _uow = uow;
    }


    public async Task<PagedResult<BikePendingInspectionDto>> GetPendingBikesAsync(int pageNumber, int pageSize)
    {
        var res = await _bikeRepo.GetAllDataByExpression(
            filter: b => b.Status == BikeStatusEnum.PendingInspection
                      && b.InspectionId == null
                      && b.Listing.Status == ListingStatusEnum.PendingApproval,
            pageNumber: pageNumber,
            pageSize: pageSize,
            orderBy: b => b.CreatedAt,
            isAscending: false,
            includes: b => b.Listing
        );

        var bikes = res.Items?.ToList() ?? new List<Bike>();
        var bikeIds = bikes.Select(b => b.Id).ToList();

        // Lấy medias cho các bike trong page
        var mediasRes = await _mediaRepo.GetAllDataByExpression(
            filter: m => bikeIds.Contains(m.BikeId),
            pageNumber: 1,
            pageSize: 5000,
            orderBy: m => m.Id,
            isAscending: true
        );

        var mediaMap = (mediasRes.Items ?? new List<Media>())
            .GroupBy(m => m.BikeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => new SellerMediaDto
                {
                    Id = m.Id,
                    BikeId = m.BikeId,
                    Image = m.Image,
                    VideoUrl = m.VideoUrl
                }).ToList()
            );

        return new PagedResult<BikePendingInspectionDto>
        {
            TotalPages = res.TotalPages,
            Items = bikes.Select(b => new BikePendingInspectionDto
            {
                Id = b.Id,
                ListingId = b.ListingId,

                Category = b.Category,
                Brand = b.Brand,
                FrameSize = b.FrameSize,
                FrameMaterial = b.FrameMaterial,
                Paint = b.Paint,
                Groupset = b.Groupset,
                Operating = b.Operating,
                TireRim = b.TireRim,
                BrakeType = b.BrakeType,
                Overall = b.Overall,
                Price = b.Price,

                BikeStatus = b.Status.ToString(),
                ListingStatus = b.Listing.Status.ToString(),
                CreatedAt = b.CreatedAt,

                Medias = mediaMap.TryGetValue(b.Id, out var list) ? list : new List<SellerMediaDto>()
            }).ToList()
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

        if (bike.Listing.Status != ListingStatusEnum.PendingApproval)
            return (false, "Listing của xe này không ở trạng thái chờ kiểm định (PendingApproval).");

        if (bike.Status != BikeStatusEnum.PendingInspection)
            return (false, "Bike không ở trạng thái chờ kiểm định (PendingInspection).");

        if (bike.InspectionId != null)
            return (false, "Bike này đã được kiểm định trước đó.");

        var now = DateTimeHelper.NowVN();

        
        var inspection = new Inspection
        {
            Id = Guid.NewGuid(),
            UserId = inspectorId,
            Score = dto.Score,
            Comment = dto.Comment,
            InspectionDate = now,
            CreatedAt = now
        };

        await _inspectionRepo.Insert(inspection);

        
        bike.InspectionId = inspection.Id;
        bike.Status = BikeStatusEnum.Available;
        bike.UpdatedAt = now;

        await _bikeRepo.Update(bike);

        await _uow.SaveChangeAsync();

        return (true, "Đã kiểm định thành công. Bike chuyển sang trạng thái Available.");
    }
}