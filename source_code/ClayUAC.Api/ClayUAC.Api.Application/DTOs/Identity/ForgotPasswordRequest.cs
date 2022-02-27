using System.ComponentModel.DataAnnotations;

namespace ClayUAC.Api.Application.DTOs.Identity
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}