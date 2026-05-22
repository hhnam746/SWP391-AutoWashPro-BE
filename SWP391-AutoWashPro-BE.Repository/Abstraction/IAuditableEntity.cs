using System;

namespace SWP391_AutoWashPro_BE.Repository.Abstraction;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
}