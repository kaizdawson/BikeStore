using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Seller.Bike;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;

namespace BikeStore.Service.Implementation;

public class SellerBikeService : ISellerBikeService
{
    private readonly IGenericRepository<Bike> _bikeRepo;
    private readonly IGenericRepository<Listing> _listingRepo;
    private readonly IUnitOfWork _uow;

    public SellerBikeService(
        IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Listing> listingRepo,
        IUnitOfWork uow)
    {
        _bikeRepo = bikeRepo;
        _listingRepo = listingRepo;
        _uow = uow;
    }

    public async Task<BikeDto> CreateAsync(Guid sellerId, Guid listingId, BikeUpsertDto dto)
    {
       
        var listing = await _listingRepo.GetFirstByExpression(x => x.Id == listingId && x.UserId == sellerId);
        if (listing == null)
            throw new InvalidOperationException("Listing không tồn tại hoặc không thuộc quyền của bạn.");

        var bike = new Bike
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            Category = dto.Category.Trim(),
            Brand = dto.Brand.Trim(),
            FrameSize = dto.FrameSize.Trim(),
            FrameMaterial = dto.FrameMaterial.Trim(),
            Paint = dto.Paint.Trim(),
            Groupset = dto.Groupset.Trim(),
            Operating = dto.Operating.Trim(),
            TireRim = dto.TireRim.Trim(),
            BrakeType = dto.BrakeType.Trim(),
            Overall = dto.Overall.Trim(),
            Price = dto.Price,
            Status = BikeStatusEnum.PendingInspection,   
            CreatedAt = DateTimeHelper.NowVN() 
        };

        await _bikeRepo.Insert(bike);
        await _uow.SaveChangeAsync();

        return ToDto(bike);
    }

    public async Task<PagedResult<BikeDto>> GetByListingAsync(Guid sellerId, Guid listingId, int pageNumber, int pageSize)
    {
        
        var listing = await _listingRepo.GetFirstByExpression(x => x.Id == listingId && x.UserId == sellerId);
        if (listing == null)
        {
            return new PagedResult<BikeDto>
            {
                Items = new List<BikeDto>(),
                TotalPages = 0
            };
        }

        var res = await _bikeRepo.GetAllDataByExpression(
            filter: b => b.ListingId == listingId,
            pageNumber: pageNumber,
            pageSize: pageSize,
            orderBy: b => b.CreatedAt,
            isAscending: false
        );

        return new PagedResult<BikeDto>
        {
            TotalPages = res.TotalPages,
            Items = res.Items?.Select(ToDto).ToList()
        };
    }

    public async Task<BikeDto?> GetByIdAsync(Guid sellerId, Guid bikeId)
    {
        var bike = await _bikeRepo.GetFirstByExpression(b => b.Id == bikeId, b => b.Listing);
        if (bike == null) return null;

        if (bike.Listing.UserId != sellerId) return null;

        return ToDto(bike);
    }

    public async Task<BikeDto?> UpdateAsync(Guid sellerId, Guid bikeId, BikeUpsertDto dto)
    {
        var bike = await _bikeRepo.GetFirstByExpression(b => b.Id == bikeId, b => b.Listing);
        if (bike == null) return null;

        if (bike.Listing.UserId != sellerId) return null;

        bike.Category = dto.Category.Trim();
        bike.Brand = dto.Brand.Trim();
        bike.FrameSize = dto.FrameSize.Trim();
        bike.FrameMaterial = dto.FrameMaterial.Trim();
        bike.Paint = dto.Paint.Trim();
        bike.Groupset = dto.Groupset.Trim();
        bike.Operating = dto.Operating.Trim();
        bike.TireRim = dto.TireRim.Trim();
        bike.BrakeType = dto.BrakeType.Trim();
        bike.Overall = dto.Overall.Trim();
        bike.Price = dto.Price;
        bike.UpdatedAt = DateTimeHelper.NowVN();

        await _bikeRepo.Update(bike);
        await _uow.SaveChangeAsync();

        return ToDto(bike);
    }

    public async Task<bool> DeleteAsync(Guid sellerId, Guid bikeId)
    {
        var bike = await _bikeRepo.GetFirstByExpression(b => b.Id == bikeId, b => b.Listing);
        if (bike == null) return false;

        if (bike.Listing.UserId != sellerId) return false;

        await _bikeRepo.Delete(bike);
        await _uow.SaveChangeAsync();
        return true;
    }

    private static BikeDto ToDto(Bike b) => new()
    {
        Id = b.Id,
        ListingId = b.ListingId,
        Status = b.Status.ToString(),
        Price = b.Price,

        Category = b.Category,
        Brand = b.Brand,
        FrameSize = b.FrameSize,
        FrameMaterial = b.FrameMaterial,
        Paint = b.Paint,
        Groupset = b.Groupset,
        Operating = b.Operating,
        TireRim = b.TireRim,
        BrakeType = b.BrakeType,
        Overall = b.Overall
    };
}