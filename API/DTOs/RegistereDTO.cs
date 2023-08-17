using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace API.DTOs
{
    public class RegistereDTO
    {
        [Required]
        public string Username { get; set; }

        [Required] public string KnownAs { get; set; }
        [Required] public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [Required] public string City { get; set; }
        [Required] public string Country { get; set; }

        [Required]
        [StringLength(20,MinimumLength = 8)]
        public string Password { get; set; }

    }
}
