using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Inspector;
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
    private readonly IUnitOfWork _uow;

    public InspectorService(
        IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Inspection> inspectionRepo,
        IUnitOfWork uow)
    {
        _bikeRepo = bikeRepo;
        _inspectionRepo = inspectionRepo;
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

        return new PagedResult<BikePendingInspectionDto>
        {
            TotalPages = res.TotalPages,
            Items = res.Items?.Select(b => new BikePendingInspectionDto
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
                CreatedAt = b.CreatedAt
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