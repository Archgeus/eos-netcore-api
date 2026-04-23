namespace EOS.Helpers;

public static class ApiResponseHelper
{
    public static object SuccessResponse(object data, string? requestId = null)
    {
        return new
        {
            data = data,
            header = new { request_id = requestId ?? Guid.NewGuid().ToString() }
        };
    }

    public static object ErrorResponse(object error, string? requestId = null)
    {
        return new
        {
            error = error,
            header = new { request_id = requestId ?? Guid.NewGuid().ToString() }
        };
    }

    public static object TokenResponse(string accessToken, int expiresIn = 1200)
    {
        return new
        {
            access_token = accessToken,
            token_type = "bearer",
            expires_in = expiresIn,
            refresh_token = accessToken
        };
    }
}
