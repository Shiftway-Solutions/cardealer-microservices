import 'package:okla_app/core/network/api_client.dart';
import 'package:okla_app/data/models/user_model.dart';

/// Remote data source for auth operations
class AuthRemoteDataSource {
  final ApiClient _client;

  AuthRemoteDataSource({required ApiClient client}) : _client = client;

  Future<AuthTokens> login({required String email, required String password}) async {
    final response = await _client.post('/auth/login', data: {
      'email': email,
      'password': password,
    });
    return AuthTokens.fromJson(response.data as Map<String, dynamic>);
  }

  Future<AuthTokens> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
  }) async {
    final response = await _client.post('/auth/register', data: {
      'email': email,
      'password': password,
      'firstName': firstName,
      'lastName': lastName,
    });
    return AuthTokens.fromJson(response.data as Map<String, dynamic>);
  }

  Future<UserModel> getCurrentUser() async {
    final response = await _client.get('/users/me');
    final data = response.data as Map<String, dynamic>;
    final userData = data['data'] ?? data;
    return UserModel.fromJson(userData as Map<String, dynamic>);
  }

  Future<void> verifyEmail(String token) async {
    await _client.post('/auth/verify-email', data: {'token': token});
  }

  Future<void> forgotPassword(String email) async {
    await _client.post('/auth/forgot-password', data: {'email': email});
  }

  Future<void> resetPassword({required String token, required String newPassword}) async {
    await _client.post('/auth/reset-password', data: {
      'token': token,
      'password': newPassword,
    });
  }

  Future<AuthTokens> verify2FA(String code) async {
    final response = await _client.post('/auth/2fa/verify', data: {'code': code});
    return AuthTokens.fromJson(response.data as Map<String, dynamic>);
  }

  Future<void> logout() async {
    try {
      await _client.post('/auth/logout');
    } catch (_) {
      // Ignore logout errors — clear local state regardless
    }
  }
}
