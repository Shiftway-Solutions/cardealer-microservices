import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:okla_app/core/constants/app_constants.dart';
import 'package:okla_app/core/errors/failures.dart';
import 'package:okla_app/core/network/api_response_handler.dart';
import 'package:okla_app/data/datasources/remote/auth_remote_datasource.dart';
import 'package:okla_app/domain/entities/user.dart';
import 'package:okla_app/domain/repositories/auth_repository.dart';

class AuthRepositoryImpl implements AuthRepository {
  final AuthRemoteDataSource _remote;
  final FlutterSecureStorage _storage;

  AuthRepositoryImpl({
    required AuthRemoteDataSource remote,
    required FlutterSecureStorage storage,
  })  : _remote = remote,
        _storage = storage;

  @override
  Future<(User?, Failure?)> login({required String email, required String password}) async {
    try {
      final tokens = await _remote.login(email: email, password: password);
      await _storage.write(key: OklaStrings.accessTokenKey, value: tokens.accessToken);
      if (tokens.refreshToken != null) {
        await _storage.write(key: OklaStrings.refreshTokenKey, value: tokens.refreshToken);
      }
      return (tokens.user as User, null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(User?, Failure?)> loginWithProvider(String provider) async {
    // TODO: Implement OAuth flow with Google/Apple sign-in packages
    return (null, const ServerFailure(message: 'Login social no implementado aún'));
  }

  @override
  Future<(User?, Failure?)> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
  }) async {
    try {
      final tokens = await _remote.register(
        email: email,
        password: password,
        firstName: firstName,
        lastName: lastName,
      );
      await _storage.write(key: OklaStrings.accessTokenKey, value: tokens.accessToken);
      if (tokens.refreshToken != null) {
        await _storage.write(key: OklaStrings.refreshTokenKey, value: tokens.refreshToken);
      }
      return (tokens.user as User, null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(bool, Failure?)> verifyEmail(String token) async {
    try {
      await _remote.verifyEmail(token);
      return (true, null);
    } catch (e) {
      return (false, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(bool, Failure?)> forgotPassword(String email) async {
    try {
      await _remote.forgotPassword(email);
      return (true, null);
    } catch (e) {
      return (false, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(bool, Failure?)> resetPassword({required String token, required String newPassword}) async {
    try {
      await _remote.resetPassword(token: token, newPassword: newPassword);
      return (true, null);
    } catch (e) {
      return (false, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(User?, Failure?)> getCurrentUser() async {
    try {
      final user = await _remote.getCurrentUser();
      return (user as User, null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(bool, Failure?)> logout() async {
    try {
      await _remote.logout();
      await clearTokens();
      return (true, null);
    } catch (e) {
      await clearTokens();
      return (true, null); // Always succeed locally
    }
  }

  @override
  Future<(bool, Failure?)> verify2FA(String code) async {
    try {
      final tokens = await _remote.verify2FA(code);
      await _storage.write(key: OklaStrings.accessTokenKey, value: tokens.accessToken);
      if (tokens.refreshToken != null) {
        await _storage.write(key: OklaStrings.refreshTokenKey, value: tokens.refreshToken);
      }
      return (true, null);
    } catch (e) {
      return (false, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<bool> isLoggedIn() async {
    final token = await _storage.read(key: OklaStrings.accessTokenKey);
    return token != null && token.isNotEmpty;
  }

  @override
  Future<String?> getAccessToken() async {
    return await _storage.read(key: OklaStrings.accessTokenKey);
  }

  @override
  Future<void> clearTokens() async {
    await _storage.delete(key: OklaStrings.accessTokenKey);
    await _storage.delete(key: OklaStrings.refreshTokenKey);
    await _storage.delete(key: OklaStrings.userKey);
  }
}
