namespace Security.Domain.Auditing;

public enum AuditActionType
{
    UserRegistered = 1,
    LoginSucceeded = 2,
    LoginFailed = 3,
    RefreshSucceeded = 4,
    RefreshFailed = 5,
    RefreshReuseDetected = 6,
    LogoutCurrentSession = 7,
    LogoutAllSessions = 8,
    SessionRevoked = 9,
    EmailVerificationRequested = 10,
    EmailVerified = 11,
    PasswordResetRequested = 12,
    PasswordResetCompleted = 13,
    RoleAssigned = 14,
    RoleRemoved = 15,
    PermissionAssignedToRole = 16,
    PermissionRemovedFromRole = 17,
    MfaSetupStarted = 18,
    MfaEnabled = 19,
    MfaLoginChallenged = 20,
    MfaLoginCompleted = 21,
    MfaRecoveryCodeUsed = 22,
}