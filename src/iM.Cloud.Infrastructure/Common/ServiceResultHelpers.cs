using BaseCrud.Errors;
using BaseCrud.ServiceResults;

namespace iM.Cloud.Infrastructure.Common;

internal static class ServiceResultHelpers
{
    public static ServiceResult<T> NotFound<T>(NotFoundServiceError error) =>
        (ServiceResult<T>)ServiceResult.NotFound(error);

    public static ServiceResult<T> BadRequest<T>(ValidationServiceError error) =>
        (ServiceResult<T>)ServiceResult.BadRequest(error);

    public static ServiceResult<T> Conflict<T>(ValidationServiceError error) =>
        (ServiceResult<T>)ServiceResult.Conflict(error);

    public static ServiceResult<T> InternalServerError<T>(ServiceError error) =>
        (ServiceResult<T>)ServiceResult.InternalServerError(error);
}
