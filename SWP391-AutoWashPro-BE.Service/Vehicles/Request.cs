using Microsoft.AspNetCore.Http;
using SWP391_AutoWashPro_BE.Repository.Enums;
using System.ComponentModel.DataAnnotations;

namespace SWP391_AutoWashPro_BE.Service.Vehicles;

public class Request
{
    public class GetVehiclesRequest
    {
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CreateVehicleRequest
    {
        public string LicensePlate { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }
        public VehicleTypes VehicleType { get; set; }
        [MinLength(3, ErrorMessage = "At least 3 vehicle images are required.")]
        public required List<IFormFile> VehicleImages { get; set; }
    }

    public class UpdateVehicleRequest
    {
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }
        public VehicleTypes VehicleType { get; set; }
    }
}
