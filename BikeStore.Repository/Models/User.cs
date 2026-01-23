using BikeStore.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Repository.Models
{
    public class User : BaseEntity
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? AvtUrl { get; set; }
        public string? FirebaseUID { get; set; }

        public RoleEnum Role { get; set; }
        public decimal WalletBalance { get; set; }

        public UserStatusEnum Status { get; set; } = UserStatusEnum.InActive;

        public Cart? Cart { get; set; }
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    }
}
