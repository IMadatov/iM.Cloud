namespace iM.Cloud.Application.Common;

/// <summary>
/// Translation keys for BaseCrud ServiceError.
/// <c>ErrorKey</c> = notification title/summary; <c>ErrorMessage</c> = notification detail/body.
/// BaseCrud ctor order: <c>(errorMessage, errorKey)</c>.
/// </summary>
public static class ErrorKeys
{
    public static class Auth
    {
        public const string InvalidCredentials = "auth.invalid_credentials";
        public const string InvalidCredentialsMessage = "auth.invalid_credentials.message";

        public const string Unauthorized = "auth.unauthorized";
        public const string UnauthorizedMessage = "auth.unauthorized.message";

        public const string InvalidRefreshToken = "auth.invalid_refresh_token";
        public const string InvalidRefreshTokenMessage = "auth.invalid_refresh_token.message";

        public const string UserNotFound = "auth.user_not_found";
        public const string UserNotFoundMessage = "auth.user_not_found.message";
    }

    public static class Db
    {
        public const string NotFoundById = "db.not-found-by-id";
        public const string NotFoundByIdMessage = "db.not-found-by-id.message";
    }

    public static class Validation
    {
        public const string NameRequired = "validation.name_required";
        public const string NameRequiredMessage = "validation.name_required.message";

        public const string EmailRequired = "validation.email_required";
        public const string EmailRequiredMessage = "validation.email_required.message";

        public const string PasswordRequired = "validation.password_required";
        public const string PasswordRequiredMessage = "validation.password_required.message";

        public const string EmailExists = "validation.email_exists";
        public const string EmailExistsMessage = "validation.email_exists.message";

        public const string NameExists = "validation.name_exists";
        public const string NameExistsMessage = "validation.name_exists.message";

        public const string RoleExists = "validation.role_exists";
        public const string RoleExistsMessage = "validation.role_exists.message";

        public const string UserCreateFailed = "validation.user_create_failed";
        public const string UserCreateFailedMessage = "validation.user_create_failed.message";

        public const string UserUpdateFailed = "validation.user_update_failed";
        public const string UserUpdateFailedMessage = "validation.user_update_failed.message";

        public const string RoleCreateFailed = "validation.role_create_failed";
        public const string RoleCreateFailedMessage = "validation.role_create_failed.message";

        public const string RoleUpdateFailed = "validation.role_update_failed";
        public const string RoleUpdateFailedMessage = "validation.role_update_failed.message";

        public const string RoleAssignFailed = "validation.role_assign_failed";
        public const string RoleAssignFailedMessage = "validation.role_assign_failed.message";

        public const string RoleRemoveFailed = "validation.role_remove_failed";
        public const string RoleRemoveFailedMessage = "validation.role_remove_failed.message";
    }

    public static class Users
    {
        public const string NotFound = "users.not_found";
        public const string NotFoundMessage = "users.not_found.message";
    }

    public static class Roles
    {
        public const string NotFound = "roles.not_found";
        public const string NotFoundMessage = "roles.not_found.message";

        public const string PermissionNotFound = "roles.permission_not_found";
        public const string PermissionNotFoundMessage = "roles.permission_not_found.message";

        public const string PermissionLinkNotFound = "roles.permission_link_not_found";
        public const string PermissionLinkNotFoundMessage = "roles.permission_link_not_found.message";
    }

    public static class Permissions
    {
        public const string NotFound = "permissions.not_found";
        public const string NotFoundMessage = "permissions.not_found.message";

        public const string ReadOnly = "permissions.read_only";
        public const string ReadOnlyMessage = "permissions.read_only.message";

        public const string UserLinkNotFound = "permissions.user_link_not_found";
        public const string UserLinkNotFoundMessage = "permissions.user_link_not_found.message";
    }

    public static class Groups
    {
        public const string NotFound = "groups.not_found";
        public const string NotFoundMessage = "groups.not_found.message";

        public const string UserNotFound = "groups.user_not_found";
        public const string UserNotFoundMessage = "groups.user_not_found.message";

        public const string MembershipNotFound = "groups.membership_not_found";
        public const string MembershipNotFoundMessage = "groups.membership_not_found.message";
    }

    public static class Server
    {
        public const string Unhandled = "errors.unhandled";
        public const string UnhandledMessage = "errors.unhandled.message";
    }
}
