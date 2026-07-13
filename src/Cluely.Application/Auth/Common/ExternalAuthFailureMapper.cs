using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;

namespace Cluely.Application.Auth.Common;

internal static class ExternalAuthFailureMapper
{
    public static Result<T> MapValidationFailure<T>(ExternalTokenValidationFailureReason reason)
        => Result.Failure<T>(reason switch
        {
            ExternalTokenValidationFailureReason.InvalidToken => new BusinessError(
                "InvalidExternalToken",
                "The external provider token is invalid."),
            ExternalTokenValidationFailureReason.ExpiredToken => new BusinessError(
                "ExpiredExternalToken",
                "The external provider token has expired."),
            ExternalTokenValidationFailureReason.WrongAudience => new BusinessError(
                "WrongExternalAudience",
                "The external provider token audience is invalid."),
            ExternalTokenValidationFailureReason.WrongIssuer => new BusinessError(
                "WrongExternalIssuer",
                "The external provider token issuer is invalid."),
            ExternalTokenValidationFailureReason.ProviderUnavailable => new BusinessError(
                "ProviderUnavailable",
                "The external identity provider is temporarily unavailable."),
            _ => new BusinessError(
                "InvalidExternalToken",
                "The external provider token is invalid.")
        });
}
