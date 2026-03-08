using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Seller.Bike;
using BikeStore.Common.DTOs.Seller.Listing;
using BikeStore.Common.DTOs.Seller.Media;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;

namespace BikeStore.Service.Implementation;

public class SellerListingService : ISellerListingService
{
    private readonly IGenericRepository<Listing> _listingRepo;
    private readonly IGenericRepository<Bike> _bikeRepo;
    private readonly IGenericRepository<Media> _mediaRepo;
    private readonly IUnitOfWork _uow;

    public SellerListingService(IGenericRepository<Listing> listingRepo, IUnitOfWork uow, IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Media> mediaRepo)
    {
        _listingRepo = listingRepo;
        _bikeRepo = bikeRepo;
        _mediaRepo = mediaRepo;
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

        var listings = res.Items?.ToList() ?? new List<Listing>();
        var listingIds = listings.Select(x => x.Id).ToList();

        var bikesRes = await _bikeRepo.GetAllDataByExpression(
            filter: b => listingIds.Contains(b.ListingId),
            pageNumber: 1,
            pageSize: 5000,
            orderBy: b => b.CreatedAt,
            isAscending: false
        );

        var bikes = bikesRes.Items?.ToList() ?? new List<Bike>();
        var bikeIds = bikes.Select(b => b.Id).ToList();

        var mediasRes = await _mediaRepo.GetAllDataByExpression(
            filter: m => bikeIds.Contains(m.BikeId),
            pageNumber: 1,
            pageSize: 5000,
            orderBy: m => m.Id,
            isAscending: true
        );

        var medias = mediasRes.Items?.ToList() ?? new List<Media>();

        
        var bikeMap = bikes.ToDictionary(b => b.ListingId, b => b);

       
        var imageMap = medias
            .GroupBy(m => m.BikeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Image).FirstOrDefault(img => !string.IsNullOrWhiteSpace(img))
            );

        return new PagedResult<ListingDto>
        {
            TotalPages = res.TotalPages,
            Items = listings.Select(x =>
            {
                bikeMap.TryGetValue(x.Id, out var bike);

                string? image = null;
                if (bike != null)
                {
                    imageMap.TryGetValue(bike.Id, out image);
                }

                return new ListingDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    Status = x.Status.ToString(),
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    Price = bike?.Price,
                    Image = image
                };
            }).ToList()
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
        BikeDto? bikeDto = null;

        if (bike != null)
        {
            var mediaRes = await _mediaRepo.GetAllDataByExpression(
                filter: m => m.BikeId == bike.Id,
                pageNumber: 1,
                pageSize: 1000,
                orderBy: m => m.Id,
                isAscending: true
            );

            bikeDto = ToBikeDto(bike);

            bikeDto.Medias = mediaRes.Items?.Select(m => new SellerMediaDto
            {
                Id = m.Id,
                BikeId = m.BikeId,
                Image = m.Image,
                VideoUrl = m.VideoUrl
            }).ToList() ?? new List<SellerMediaDto>();
        }

        return new ListingDetailsDto
        {
            Id = listing.Id,
            Title = listing.Title,
            Description = listing.Description,
            Status = listing.Status.ToString(),
            CreatedAt = listing.CreatedAt,
            UpdatedAt = listing.UpdatedAt,
            Bike = bikeDto
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