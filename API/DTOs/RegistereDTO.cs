using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class RegistereDTO
    {
        [Required]
        public string Username { get; set; }
        
        [Required]
        [StringLength(20,MinimumLength = 8)]
        public string Password { get; set; }

    }
}
