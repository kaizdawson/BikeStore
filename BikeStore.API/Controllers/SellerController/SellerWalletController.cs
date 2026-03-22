using BikeStore.Common.DTOs.Transaction;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SellerWalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public SellerWalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetMyWalletBalance()
        {
            try
            {
                var result = await _walletService.GetMyWalletBalanceAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("withdrawal")]
        public async Task<IActionResult> CreateWithdrawal([FromBody] CreateWithdrawalDto dto)
        {
            try
            {
                var result = await _walletService.CreateWithdrawalAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("withdrawals")]
        public async Task<IActionResult> GetMyWithdrawals()
        {
            try
            {
                var result = await _walletService.GetMyWithdrawalsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("finance")]
        public async Task<IActionResult> GetMyFinance()
        {
            try
            {
                var result = await _walletService.GetMyFinanceAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}