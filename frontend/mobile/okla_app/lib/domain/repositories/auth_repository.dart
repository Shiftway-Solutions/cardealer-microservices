import 'package:okla_app/core/errors/failures.dart';
import 'package:okla_app/domain/entities/user.dart';

/// Auth repository interface
abstract class AuthRepository {
  Future<(User?, Failure?)> login({required String email, required String password});
  Future<(User?, Failure?)> loginWithProvider(String provider);
  Future<(User?, Failure?)> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
  });
  Future<(bool, Failure?)> verifyEmail(String token);
  Future<(bool, Failure?)> forgotPassword(String email);
  Future<(bool, Failure?)> resetPassword({required String token, required String newPassword});
  Future<(User?, Failure?)> getCurrentUser();
  Future<(bool, Failure?)> logout();
  Future<(bool, Failure?)> verify2FA(String code);
  Future<bool> isLoggedIn();
  Future<String?> getAccessToken();
  Future<void> clearTokens();
}
