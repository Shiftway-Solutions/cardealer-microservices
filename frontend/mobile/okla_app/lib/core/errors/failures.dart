/// Base failure class for domain layer errors
abstract class Failure {
  final String message;
  final int? statusCode;
  final Map<String, dynamic>? errors;

  const Failure({
    required this.message,
    this.statusCode,
    this.errors,
  });
}

/// Server-side failures (API errors)
class ServerFailure extends Failure {
  const ServerFailure({
    required super.message,
    super.statusCode,
    super.errors,
  });
}

/// Network connectivity failures
class NetworkFailure extends Failure {
  const NetworkFailure({
    super.message = 'Sin conexión a internet. Verifica tu conexión e intenta de nuevo.',
  });
}

/// Cache/local storage failures
class CacheFailure extends Failure {
  const CacheFailure({
    super.message = 'Error al acceder a datos locales.',
  });
}

/// Authentication failures
class AuthFailure extends Failure {
  const AuthFailure({
    required super.message,
    super.statusCode,
  });
}

/// Validation failures
class ValidationFailure extends Failure {
  const ValidationFailure({
    required super.message,
    super.errors,
  });
}

/// Timeout failures
class TimeoutFailure extends Failure {
  const TimeoutFailure({
    super.message = 'La solicitud tomó demasiado tiempo. Intenta de nuevo.',
  });
}

/// Unknown failures
class UnknownFailure extends Failure {
  const UnknownFailure({
    super.message = 'Ocurrió un error inesperado. Intenta de nuevo.',
  });
}

/// Custom exceptions
class ServerException implements Exception {
  final String message;
  final int? statusCode;
  final Map<String, dynamic>? errors;

  const ServerException({
    required this.message,
    this.statusCode,
    this.errors,
  });
}

class CacheException implements Exception {
  final String message;
  const CacheException({this.message = 'Cache error'});
}

class AuthException implements Exception {
  final String message;
  final int? statusCode;
  const AuthException({required this.message, this.statusCode});
}
