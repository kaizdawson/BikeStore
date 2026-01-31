using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Seller.Bike;
using BikeStore.Common.DTOs.Seller.Listing;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;

namespace BikeStore.Service.Implementation;

public class SellerListingService : ISellerListingService
{
    private readonly IGenericRepository<Listing> _listingRepo;
    private readonly IUnitOfWork _uow;

    public SellerListingService(IGenericRepository<Listing> listingRepo, IUnitOfWork uow)
    {
        _listingRepo = listingRepo;
        _uow = uow;
    }

    public async Task<ListingDto> CreateAsync(Guid sellerId, ListingUpsertDto dto)
    {
        var entity = new Listing
        {
            Id = Guid.NewGuid(),
            UserId = sellerId,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Status = ListingStatusEnum.Draft, 
            CreatedAt = DateTimeHelper.NowVN(),
        };

        await _listingRepo.Insert(entity);
        await _uow.SaveChangeAsync();

        return ToDto(entity);
    }

    public async Task<PagedResult<ListingDto>> GetMyListingsAsync(Guid sellerId, int pageNumber, int pageSize)
    {
        var res = await _listingRepo.GetAllDataByExpression(
            filter: x => x.UserId == sellerId,
            pageNumber: pageNumber,
            pageSize: pageSize,
            orderBy: x => x.CreatedAt,
            isAscending: false
        );

        return new PagedResult<ListingDto>
        {
            TotalPages = res.TotalPages,
            Items = res.Items?.Select(ToDto).ToList()
        };
    }

    public async Task<ListingDto?> GetByIdAsync(Guid sellerId, Guid listingId)
    {
        var entity = await _listingRepo.GetFirstByExpression(x => x.Id == listingId && x.UserId == sellerId);
        return entity == null ? null : ToDto(entity);
    }

    public async Task<ListingDto?> UpdateAsync(Guid sellerId, Guid listingId, ListingUpsertDto dto)
    {
        var entity = await _listingRepo.GetFirstByExpression(x => x.Id == listingId && x.UserId == sellerId);
        if (entity == null) return null;

        if (entity.Status != ListingStatusEnum.Draft)
            throw new InvalidOperationException("Listing đã gửi duyệt/đã hoạt động nên không thể chỉnh sửa nữa.");

        entity.Title = dto.Title.Trim();
        entity.Description = dto.Description.Trim();
        entity.UpdatedAt = DateTimeHelper.NowVN();

        await _listingRepo.Update(entity);
        await _uow.SaveChangeAsync();

        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid sellerId, Guid listingId)
    {
        var entity = await _listingRepo.GetFirstByExpression(x => x.Id == listingId && x.UserId == sellerId);
        if (entity == null) return false;


        if (entity.Status != ListingStatusEnum.Draft)
            throw new InvalidOperationException("Listing đã gửi duyệt/đã hoạt động nên không thể xoá.");

        await _listingRepo.Delete(entity);
        await _uow.SaveChangeAsync();
        return true;
    }

    private static ListingDto ToDto(Listing x) => new()
    {
        Id = x.Id,
        Title = x.Title,
        Description = x.Description,
        Status = x.Status.ToString(),
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };

    public async Task<ListingDetailsDto?> GetDetailsAsync(Guid sellerId, Guid listingId)
    {
        var listing = await _listingRepo.GetFirstByExpression(
            x => x.Id == listingId && x.UserId == sellerId,
            x => x.Bikes
        );

        if (listing == null) return null;

        var bike = listing.Bikes?.FirstOrDefault(); 

        return new ListingDetailsDto
        {
            Id = listing.Id,
            Title = listing.Title,
            Description = listing.Description,
            Status = listing.Status.ToString(),
            CreatedAt = listing.CreatedAt,
            UpdatedAt = listing.UpdatedAt,
            Bike = bike == null ? null : ToBikeDto(bike)
        };
    }

    private static BikeDto ToBikeDto(Bike b) => new BikeDto
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