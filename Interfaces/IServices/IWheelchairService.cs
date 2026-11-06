using proto_back.DTOs.Responses;

namespace proto_back.Interfaces.IServices;

public interface IWheelchairService
{
    WheelchairCatalogResponse GetCatalog();
}
