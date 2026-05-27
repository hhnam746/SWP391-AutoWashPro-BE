namespace SWP391_AutoWashPro_BE.Service.Vehicles;

public interface IService
{
    public Task<Response.GetVehiclesResponse> GetVehicles(int page, int pageSize);
    public Task<Response.CreateVehicleResponse> CreateVehicle(Request.CreateVehicleRequest request);
    public Task<Response.GetVehicleByIdResponse> GetVehicleById(Guid id);
    public Task<Response.UpdateVehicleResponse> UpdateVehicle(Guid id, Request.UpdateVehicleRequest request);
    public Task<Response.DeleteVehicleResponse> DeleteVehicle(Guid id);
}
