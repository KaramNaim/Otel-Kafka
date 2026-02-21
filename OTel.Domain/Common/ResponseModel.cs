using OTel.Domain.Enums;

namespace OTel.Domain.Common;

public class ResponseModel<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public Error? Error { get; set; }
    public string? Message { get; set; }

    public static ResponseModel<T> Success(T data, string? message = null) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message
    };

    public static ResponseModel<T> Failure(Error error, string message) => new()
    {
        IsSuccess = false,
        Error = error,
        Message = message
    };
}
