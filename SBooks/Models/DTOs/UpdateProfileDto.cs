using System.ComponentModel.DataAnnotations;

namespace SBooks.Models.DTOs
{
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Tên đầy đủ là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tên đầy đủ không được vượt quá 255 ký tự")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
        public string Email { get; set; } = null!;
    }
}