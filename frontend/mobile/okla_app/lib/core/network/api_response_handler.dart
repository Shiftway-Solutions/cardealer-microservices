import 'package:dio/dio.dart';
import 'package:okla_app/core/errors/failures.dart';

/// Handles API responses and converts errors to domain Failures
class ApiResponseHandler {
  /// Extract data from standard ApiResponse<T> or return raw
  static T handleResponse<T>(Response response, T Function(dynamic json) fromJson) {
    final data = response.data;

    // Handle ApiResponse wrapper: { success: true, data: {...} }
    if (data is Map<String, dynamic>) {
      if (data.containsKey('success') && data['success'] == true) {
        return fromJson(data['data'] ?? data);
      }
      if (data.containsKey('success') && data['success'] == false) {
        throw ServerException(
          message: data['error']?.toString() ?? 'Error desconocido',
          statusCode: response.statusCode,
        );
      }
      // Direct JSON (no wrapper)
      return fromJson(data);
    }

    return fromJson(data);
  }

  /// Handle list responses
  static List<T> handleListResponse<T>(
    Response response,
    T Function(Map<String, dynamic>) fromJson,
  ) {
    final data = response.data;

    if (data is Map<String, dynamic>) {
      // Wrapped: { success: true, data: [...] }
      if (data.containsKey('data') && data['data'] is List) {
        return (data['data'] as List).map((e) => fromJson(e as Map<String, dynamic>)).toList();
      }
      // Wrapped: { items: [...] }
      if (data.containsKey('items') && data['items'] is List) {
        return (data['items'] as List).map((e) => fromJson(e as Map<String, dynamic>)).toList();
      }
    }

    if (data is List) {
      return data.map((e) => fromJson(e as Map<String, dynamic>)).toList();
    }

    return [];
  }

  /// Handle paginated responses
  static PaginatedResponse<T> handlePaginatedResponse<T>(
    Response response,
    T Function(Map<String, dynamic>) fromJson,
  ) {
    final data = response.data as Map<String, dynamic>;
    final innerData = data['data'] ?? data;

    final items = innerData is Map
        ? (innerData['items'] as List? ?? []).map((e) => fromJson(e as Map<String, dynamic>)).toList()
        : <T>[];

    return PaginatedResponse<T>(
      items: items,
      totalCount: innerData is Map ? (innerData['totalCount'] as int? ?? items.length) : items.length,
      page: innerData is Map ? (innerData['page'] as int? ?? 1) : 1,
      pageSize: innerData is Map ? (innerData['pageSize'] as int? ?? 20) : 20,
      hasMore: innerData is Map ? (innerData['hasMore'] as bool? ?? false) : false,
    );
  }

  /// Convert DioException to domain Failure
  static Failure handleError(dynamic error) {
    if (error is DioException) {
      switch (error.type) {
        case DioExceptionType.connectionTimeout:
        case DioExceptionType.sendTimeout:
        case DioExceptionType.receiveTimeout:
          return const TimeoutFailure();
        case DioExceptionType.connectionError:
          return const NetworkFailure();
        case DioExceptionType.badResponse:
          return _handleBadResponse(error.response);
        default:
          return UnknownFailure(message: error.message ?? 'Error desconocido');
      }
    }
    if (error is ServerException) {
      return ServerFailure(
        message: error.message,
        statusCode: error.statusCode,
        errors: error.errors,
      );
    }
    return UnknownFailure(message: error.toString());
  }

  static Failure _handleBadResponse(Response? response) {
    if (response == null) {
      return const ServerFailure(message: 'Error del servidor');
    }

    final data = response.data;
    String message = 'Error del servidor';
    Map<String, dynamic>? errors;

    if (data is Map<String, dynamic>) {
      // ProblemDetails format
      if (data.containsKey('title')) {
        message = data['title'] ?? message;
        errors = data['errors'] as Map<String, dynamic>?;
      }
      // ApiResponse error format
      else if (data.containsKey('error')) {
        message = data['error']?.toString() ?? message;
      }
      // Generic message
      else if (data.containsKey('message')) {
        message = data['message']?.toString() ?? message;
      }
    }

    if (response.statusCode == 401) {
      return AuthFailure(message: message, statusCode: 401);
    }
    if (response.statusCode == 403) {
      return AuthFailure(message: 'No tienes permiso para esta acción.', statusCode: 403);
    }
    if (response.statusCode == 422) {
      return ValidationFailure(message: message, errors: errors);
    }

    return ServerFailure(
      message: message,
      statusCode: response.statusCode,
      errors: errors,
    );
  }
}

/// Paginated response wrapper
class PaginatedResponse<T> {
  final List<T> items;
  final int totalCount;
  final int page;
  final int pageSize;
  final bool hasMore;

  const PaginatedResponse({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.hasMore,
  });
}
