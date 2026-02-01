using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;

namespace BikeStore.Service.Implementation
{
    public class AdminApproveService : IAdminApproveService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Listing> _listingRepo;

        public AdminApproveService(IUnitOfWork unitOfWork, IGenericRepository<Listing> listingRepo)
        {
            _unitOfWork = unitOfWork;
            _listingRepo = listingRepo;
        }

        public async Task<bool> ApproveListingAsync(Guid id, AdminApproveDto dto)
        {
            var listing = await _listingRepo.GetById(id);
            if (listing == null) throw new Exception("Không tìm thấy tin đăng.");

            // Chỉ duyệt khi tin đang ở trạng thái PendingApproval (2)
            if (listing.Status != ListingStatusEnum.PendingApproval)
                throw new Exception("Tin này không ở trạng thái chờ duyệt.");

            // Ánh xạ quyết định: True -> Active (3), False -> Rejected (5)
            listing.Status = dto.IsApproved ? ListingStatusEnum.Active : ListingStatusEnum.Rejected;
            listing.UpdatedAt = DateTimeHelper.NowVN();

            await _listingRepo.Update(listing);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }

        public async Task<List<Listing>> GetPendingListingsAsync()
        {
            var result = await _listingRepo.GetAllDataByExpression(
                filter: l => l.Status == ListingStatusEnum.PendingApproval,
                pageNumber: 1,
                pageSize: 100
            );
            return result.Items.ToList();
        }
    }
}