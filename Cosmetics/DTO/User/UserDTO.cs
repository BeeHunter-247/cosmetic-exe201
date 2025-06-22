namespace Cosmetics.DTO.User
{
    public class UserDTO
    {
        public int UserId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public int RoleType { get; set; }

        public string Address { get; set; }
    }

    public class UpdateAddressModel
    {
        public string Address { get; set; }
    }
}