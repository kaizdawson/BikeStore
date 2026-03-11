using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using System.Linq.Expressions;

public class PolicyService : IPolicyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGenericRepository<Policy> _policyRepo;

    public PolicyService(IUnitOfWork unitOfWork, IGenericRepository<Policy> policyRepo)
    {
        _unitOfWork = unitOfWork;
        _policyRepo = policyRepo;
    }
    public async Task<bool> CreatePolicyAsync(PolicyDto dto)
    {
        var nowVn = DateTimeHelper.NowVN();

        if (dto.AppliedDate <= nowVn)
            throw new Exception($"Ngày áp dụng phải ở tương lai. Hiện tại là: {nowVn:HH:mm dd/MM/yyyy}");

        if (dto.PercentOfSystem + dto.PercentOfSeller != 100)
            throw new Exception("Tổng phần trăm chia sẻ phải bằng 100%.");

        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            Description = dto.Description,
            PercentOfSystem = dto.PercentOfSystem,
            PercentOfSeller = dto.PercentOfSeller,
            AppliedDate = dto.AppliedDate,
            Status = PolicyStatusEnum.Inactive, 
            IsDeleted = false,
            CreatedAt = nowVn,
            UpdatedAt = nowVn
        };

        await _policyRepo.Insert(policy);
        return await _unitOfWork.SaveChangeAsync() > 0;
    }

    public async Task<Policy?> GetCurrentActivePolicyAsync()
    {
        var nowVn = DateTimeHelper.NowVN();
        var result = await _policyRepo.GetAllDataByExpression(
            filter: p => p.Status == PolicyStatusEnum.Active && p.IsDeleted == false && p.AppliedDate <= nowVn,
            pageNumber: 1,
            pageSize: 1,
            orderBy: p => p.AppliedDate,
            isAscending: false
        );

        return result.Items.FirstOrDefault();
    }

    public async Task<List<object>> GetAllPoliciesAsync()
    {
        var nowVn = DateTimeHelper.NowVN();
        var all = await _policyRepo.GetAllDataByExpression(
            filter: p => p.IsDeleted == false,
            pageNumber: 1,
            pageSize: 100, 
            orderBy: p => p.AppliedDate,
            isAscending: false
        );

        var currentActive = await GetCurrentActivePolicyAsync();

        return all.Items.Select(p => {
            string timelineStatus;

            if (currentActive != null && p.Id == currentActive.Id)
                timelineStatus = "Đang áp dụng";
            else if (p.AppliedDate > nowVn)
                timelineStatus = "Chờ hiệu lực";
            else if (p.Status == PolicyStatusEnum.Expired)
                timelineStatus = "Đã hết hạn";
            else
                timelineStatus = "Bị tạm dừng";

            return new
            {
                p.Id,
                p.Description,
                p.PercentOfSystem,
                p.PercentOfSeller,
                p.AppliedDate,
                DbStatus = (int)p.Status,
                TimelineStatus = timelineStatus
            };
        }).Cast<object>().ToList();
    }

    public async Task<bool> UpdatePolicyAsync(Guid id, PolicyDto dto)
    {
        var policy = await _policyRepo.GetById(id);
        if (policy == null || policy.IsDeleted == true)
            throw new Exception("Không tìm thấy Policy.");

        var nowVn = DateTimeHelper.NowVN();

        if (policy.AppliedDate <= nowVn)
            throw new Exception("Không thể sửa Policy đã hoặc đang có hiệu lực.");

        if (dto.AppliedDate <= nowVn)
            throw new Exception("Ngày áp dụng mới phải ở tương lai.");

        policy.Description = dto.Description;
        policy.PercentOfSystem = dto.PercentOfSystem;
        policy.PercentOfSeller = dto.PercentOfSeller;
        policy.AppliedDate = dto.AppliedDate;
        policy.UpdatedAt = nowVn;

        await _policyRepo.Update(policy);
        return await _unitOfWork.SaveChangeAsync() > 0;
    }

    public async Task<bool> DeletePolicyAsync(Guid id)
    {
        var policy = await _policyRepo.GetById(id);
        if (policy == null || policy.IsDeleted == true) return false;

        var nowVn = DateTimeHelper.NowVN();

        if (policy.Status == PolicyStatusEnum.Active && policy.AppliedDate <= nowVn)
        {
            throw new Exception("Không thể xóa Policy đang trong thời gian áp dụng.");
        }

        policy.IsDeleted = true;
        policy.Status = PolicyStatusEnum.Inactive; 
        policy.UpdatedAt = nowVn;

        await _policyRepo.Update(policy);
        return await _unitOfWork.SaveChangeAsync() > 0;
    }
}