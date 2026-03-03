using BikeStore.Common.DTOs.PayOs;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BikeStore.Service.Implementation
{
    public class PayOsCheckoutService : IPayOsCheckoutService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Transaction> _tranRepo;
        private readonly IUnitOfWork _uow;
        private readonly PayOsSettings _cfg;
        private readonly HttpClient _httpClient;

        public PayOsCheckoutService(
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Transaction> tranRepo,
            IUnitOfWork uow,
            IOptions<PayOsSettings> cfg,
            HttpClient httpClient 
        )
        {
            _orderRepo = orderRepo;
            _tranRepo = tranRepo;
            _uow = uow;
            _cfg = cfg.Value;
            _httpClient = httpClient;
        }

        public async Task<CheckoutResultDto> CheckoutAsync(Guid userId, Guid orderId)
        {
            var order = await _orderRepo.GetFirstByExpression(o => o.Id == orderId && !o.IsDeleted);
            if (order == null) return new CheckoutResultDto { Success = false, Message = "Không tìm thấy Order." };

            if (order.UserId != userId) return new CheckoutResultDto { Success = false, Message = "Order không thuộc user này." };

            if (order.TotalAmount <= 0) return new CheckoutResultDto { Success = false, Message = "TotalAmount không hợp lệ." };

            var now = DateTimeHelper.NowVN();

            var pendingTran = await _tranRepo.GetFirstByExpression(t =>
                t.OrderId == orderId && !t.IsDeleted && t.Status == TransactionStatusEnum.Pending);

            if (pendingTran != null)
            {
                var urlOld = await CreatePayOsPaymentLinkAsync(
                    orderCodeString: pendingTran.OrderCode,
                    amount: (int)order.TotalAmount,
                    description: pendingTran.OrderCode
                );

                return new CheckoutResultDto
                {
                    Success = true,
                    Message = "OK",
                    Data = new CheckoutResponseDto
                    {
                        OrderCode = pendingTran.OrderCode,
                        CheckoutUrl = urlOld
                    }
                };
            }

            var orderCode = GenerateOrderCodeNumberString();

            var tran = new Transaction
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                OrderCode = orderCode,
                Status = TransactionStatusEnum.Pending,
                Description = orderCode, 
                Amount = order.TotalAmount,
                PaidAt = null,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                PolicyId = null
            };

            await _tranRepo.Insert(tran);
            await _uow.SaveChangeAsync();

            var checkoutUrl = await CreatePayOsPaymentLinkAsync(
                orderCodeString: orderCode,
                amount: (int)order.TotalAmount,
                description: orderCode
            );

            return new CheckoutResultDto
            {
                Success = true,
                Message = "Tạo link thanh toán thành công.",
                Data = new CheckoutResponseDto
                {
                    OrderCode = orderCode,
                    CheckoutUrl = checkoutUrl
                }
            };
        }

        private static string GenerateOrderCodeNumberString()
            => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        private async Task<string> CreatePayOsPaymentLinkAsync(string orderCodeString, int amount, string description)
        {
            if (!long.TryParse(orderCodeString, out var orderCode))
                throw new Exception("OrderCode phải là chuỗi số để gửi PayOS.");

           
            description = orderCodeString;

            var rawData =
                $"amount={amount}&cancelUrl={_cfg.CancelUrl}&description={description}&orderCode={orderCode}&returnUrl={_cfg.ReturnUrl}";

            var signature = GenerateSignature(rawData, _cfg.ChecksumKey);

            var payload = new
            {
                orderCode = orderCode,
                amount = amount,
                description = description,
                cancelUrl = _cfg.CancelUrl,
                returnUrl = _cfg.ReturnUrl,
                signature = signature
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests");
            request.Headers.Add("x-client-id", _cfg.ClientId);
            request.Headers.Add("x-api-key", _cfg.ApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PayOS HTTP Error: {response.StatusCode} – {responseContent}");

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            
            var code = root.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
            var desc = root.TryGetProperty("desc", out var descEl) ? descEl.GetString() : responseContent;

            if (!string.Equals(code, "00", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"PayOS Business Error: code={code}, desc={desc}, raw={responseContent}");

            if (!root.TryGetProperty("data", out var data))
                throw new Exception($"PayOS response missing data. raw={responseContent}");

            var checkoutUrl = data.GetProperty("checkoutUrl").GetString();
            if (string.IsNullOrWhiteSpace(checkoutUrl))
                throw new Exception($"PayOS checkoutUrl null/empty. raw={responseContent}");

            return checkoutUrl!;
        }



        private static string GenerateSignature(string rawData, string checksumKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}