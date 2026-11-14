using CryptoSimulator.Entities;

namespace CryptoSimulator.DTOs
{
    public class UserGetDto
    {
        public int Id { get; set; }
        public Role Role { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}
