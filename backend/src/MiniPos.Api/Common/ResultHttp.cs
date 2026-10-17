using MiniPos.Domain.Common;

namespace MiniPos.Api.Common;

/// <summary>Maps a handler <see cref="Result{T}"/> to an HTTP result. The one error envelope for the API.</summary>
public static class ResultHttp
{
    public static IResult ToHttp<T>(this Result<T> result, Func<T, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        var error = result.Error!;
        var status = error.Kind switch
        {
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return Results.Problem(title: error.Code, detail: error.Message, statusCode: status);
    }
}
