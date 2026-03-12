namespace KYCService.Application.Exceptions;

/// <summary>
/// Base exception for KYC service
/// </summary>
public class KYCException : Exception
{
    public KYCException(string message) : base(message) { }
    public KYCException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when attempting to create a duplicate KYC profile for a user
/// </summary>
public class DuplicateProfileException : KYCException
{
    public DuplicateProfileException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a document number is already registered to another user
/// </summary>
public class DuplicateDocumentException : KYCException
{
    public DuplicateDocumentException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a KYC profile is not found
/// </summary>
public class ProfileNotFoundException : KYCException
{
    public ProfileNotFoundException(string message) : base(message) { }
    public ProfileNotFoundException(Guid profileId)
        : base($"KYC Profile with ID {profileId} was not found.") { }
}

/// <summary>
/// Thrown when a KYC operation is not allowed due to profile status
/// </summary>
public class InvalidProfileStatusException : KYCException
{
    public InvalidProfileStatusException(string message) : base(message) { }
}

/// <summary>
/// Thrown when attempting an unauthorized KYC operation
/// </summary>
public class KYCAuthorizationException : KYCException
{
    public KYCAuthorizationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when document verification fails
/// </summary>
public class DocumentVerificationException : KYCException
{
    public DocumentVerificationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when there's a concurrent modification conflict
/// </summary>
public class ConcurrencyException : KYCException
{
    public ConcurrencyException(string message) : base(message) { }
}
