using proto_back.Models.Entities;

namespace proto_back.Interfaces.IRepositories;

public interface IErrorLogRepository
{
    Task CreateAsync(ErrorLog errorLog);
    Task<ErrorLog?> GetByErrorIdAsync(string errorId);
}
