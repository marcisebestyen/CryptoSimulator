namespace CryptoSimulator.Entities
{
    public enum Role
    {
        Admin,
        User
    }

    public class User
    {
        public int Id { get; set; }
        public Role Role { get; set; } = Role.User;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
