using System.ComponentModel.DataAnnotations;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Admin;

public class Request
{
    public class UpdateUserByStatusRequest
    {
        [Required]
        [EnumDataType(typeof(AccountStatus))]
        public AccountStatus? Status { get; set; }
    }
}
