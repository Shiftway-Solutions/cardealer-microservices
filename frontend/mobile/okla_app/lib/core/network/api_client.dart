import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:okla_app/core/config/app_config.dart';
import 'package:okla_app/core/constants/app_constants.dart';

/// API Client built on Dio with JWT auth, refresh, and error handling
class ApiClient {
  late final Dio _dio;
  final FlutterSecureStorage _storage;
  final AppConfig _config;

  ApiClient({
    required AppConfig config,
    required FlutterSecureStorage storage,
  })  : _config = config,
        _storage = storage {
    _dio = Dio(BaseOptions(
      baseUrl: config.apiBaseUrl,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 15),
      sendTimeout: const Duration(seconds: 15),
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'X-Client': 'okla-mobile',
        'X-Client-Version': OklaStrings.appVersion,
      },
    ));

    _dio.interceptors.addAll([
      _AuthInterceptor(storage: _storage, dio: _dio),
      if (config.enableLogging) _LoggingInterceptor(),
    ]);
  }

  Dio get dio => _dio;

  // ──── GET ────
  Future<Response<T>> get<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
    Options? options,
    CancelToken? cancelToken,
  }) =>
      _dio.get<T>(
        path,
        queryParameters: queryParameters,
        options: options,
        cancelToken: cancelToken,
      );

  // ──── POST ────
  Future<Response<T>> post<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
    CancelToken? cancelToken,
  }) =>
      _dio.post<T>(
        path,
        data: data,
        queryParameters: queryParameters,
        options: options,
        cancelToken: cancelToken,
      );

  // ──── PUT ────
  Future<Response<T>> put<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
  }) =>
      _dio.put<T>(path, data: data, queryParameters: queryParameters, options: options);

  // ──── PATCH ────
  Future<Response<T>> patch<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
  }) =>
      _dio.patch<T>(path, data: data, queryParameters: queryParameters, options: options);

  // ──── DELETE ────
  Future<Response<T>> delete<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
  }) =>
      _dio.delete<T>(path, data: data, queryParameters: queryParameters, options: options);

  // ──── Multipart Upload ────
  Future<Response<T>> upload<T>(
    String path, {
    required FormData formData,
    void Function(int, int)? onSendProgress,
    CancelToken? cancelToken,
  }) =>
      _dio.post<T>(
        path,
        data: formData,
        onSendProgress: onSendProgress,
        cancelToken: cancelToken,
        options: Options(
          contentType: 'multipart/form-data',
          sendTimeout: const Duration(minutes: 5),
          receiveTimeout: const Duration(minutes: 5),
        ),
      );
}

/// Auth interceptor — adds JWT, handles 401 with refresh
class _AuthInterceptor extends Interceptor {
  final FlutterSecureStorage _storage;
  final Dio _dio;
  bool _isRefreshing = false;

  _AuthInterceptor({required FlutterSecureStorage storage, required Dio dio})
      : _storage = storage,
        _dio = dio;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final token = await _storage.read(key: OklaStrings.accessTokenKey);
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode == 401 && !_isRefreshing) {
      _isRefreshing = true;
      try {
        final refreshToken = await _storage.read(key: OklaStrings.refreshTokenKey);
        if (refreshToken == null) {
          _isRefreshing = false;
          handler.next(err);
          return;
        }

        final response = await _dio.post(
          '/auth/refresh',
          data: {'refreshToken': refreshToken},
          options: Options(headers: {'Authorization': ''}),
        );

        if (response.statusCode == 200) {
          final newAccessToken = response.data['data']?['accessToken'] ?? response.data['accessToken'];
          final newRefreshToken = response.data['data']?['refreshToken'] ?? response.data['refreshToken'];

          await _storage.write(key: OklaStrings.accessTokenKey, value: newAccessToken);
          if (newRefreshToken != null) {
            await _storage.write(key: OklaStrings.refreshTokenKey, value: newRefreshToken);
          }

          // Retry original request
          final opts = err.requestOptions;
          opts.headers['Authorization'] = 'Bearer $newAccessToken';
          final retryResponse = await _dio.fetch(opts);
          _isRefreshing = false;
          handler.resolve(retryResponse);
          return;
        }
      } catch (_) {
        // Refresh failed — clear tokens
        await _storage.delete(key: OklaStrings.accessTokenKey);
        await _storage.delete(key: OklaStrings.refreshTokenKey);
      }
      _isRefreshing = false;
    }
    handler.next(err);
  }
}

/// Logging interceptor for debug
class _LoggingInterceptor extends Interceptor {
  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    if (kDebugMode) {
      debugPrint('→ ${options.method} ${options.uri}');
    }
    handler.next(options);
  }

  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    if (kDebugMode) {
      debugPrint('← ${response.statusCode} ${response.requestOptions.uri}');
    }
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (kDebugMode) {
      debugPrint('✗ ${err.response?.statusCode} ${err.requestOptions.uri}: ${err.message}');
    }
    handler.next(err);
  }
}
