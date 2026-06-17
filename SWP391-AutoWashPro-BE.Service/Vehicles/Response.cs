using SWP391_AutoWashPro_BE.Repository.Entities;

namespace SWP391_AutoWashPro_BE.Service.Vehicles;
using SWP391_AutoWashPro_BE.Repository.Enums;

public class Response
{
    public class GetVehiclesResponse
    {
        public required List<VehicleListItemResponse> Data { get; set; }
        public required PaginationResponse Pagination { get; set; }
    }

    public class VehicleListItemResponse
    {
        public required Guid Id { get; set; }
        public required string LicensePlate { get; set; }
        public required string Brand { get; set; }
        public required VehicleTypes VehicelType { get; set; }
        public required string Model { get; set; }
        public required string Color { get; set; }
        public required bool IsActive { get; set; }
        public required bool HasActiveBooking { get; set; }
        public required List<VehicleImageResponse> VehicleImages { get; set; }
    }

    public class CreateVehicleResponse
    {
        public required Guid Id { get; set; }
        public required string LicensePlate { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public required string Color { get; set; }
        public required bool IsActive { get; set; }
        public required VehicleTypes VehicleType { get; set; }
    }

    public class VehicleImageResponse
    {
        public required Guid Id { get; set; }
        public required string ImageUrl { get; set; }
    }
    
    public class GetVehicleByIdResponse
    {
        public required Guid Id { get; set; }
        public required string LicensePlate { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public required VehicleTypes VehicleType { get; set; }
        public required string Color { get; set; }
        public required bool IsActive { get; set; }
        public required List<VehicleImageResponse> VehicleImages { get; set; }
    }

    public class UpdateVehicleResponse
    {
        public required Guid Id { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public required string Color { get; set; }
        public required VehicleTypes VehicleType { get; set; }
    }

    public class DeleteVehicleResponse
    {
        public required string Message { get; set; }
    }

    public class PaginationResponse
    {
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalCount { get; set; }
        public required int TotalPages { get; set; }
    }
}
