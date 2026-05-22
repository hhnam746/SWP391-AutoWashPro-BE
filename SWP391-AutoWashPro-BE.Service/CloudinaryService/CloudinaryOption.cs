using System.ComponentModel.DataAnnotations;

namespace SWP391_AutoWashPro_BE.Service.CloudinaryService;

public record CloudinaryOptions
{
    [Required]public string CloudName { get; set; }
    [Required]public string ApiKey { get; set; }
    [Required]public string ApiSecret { get; set; }
}