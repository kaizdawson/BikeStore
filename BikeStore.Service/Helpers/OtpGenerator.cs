using System.Security.Cryptography;

namespace BikeStore.Service.Helpers
{
    public static class OtpGenerator
    {
        public static string GenerateOtp(int length = 6)
        {
            var max = (int)Math.Pow(10, length);
            var value = RandomNumberGenerator.GetInt32(0, max);
            return value.ToString(new string('0', length));
        }
    }
}
