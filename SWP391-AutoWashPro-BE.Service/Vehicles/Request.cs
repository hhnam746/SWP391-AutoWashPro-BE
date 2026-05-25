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
        public string LicensePlateImageUrl { get; set; }
    }

    public class UpdateVehicleRequest
    {
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }
    }
}
