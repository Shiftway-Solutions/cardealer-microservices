import 'package:okla_app/domain/entities/user.dart';

/// User JSON model
class UserModel extends User {
  const UserModel({
    required super.id,
    required super.email,
    super.firstName,
    super.lastName,
    super.fullName,
    super.avatarUrl,
    required super.accountType,
    super.isEmailVerified,
    super.isTwoFactorEnabled,
    super.dealerId,
    super.createdAt,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      firstName: json['firstName']?.toString(),
      lastName: json['lastName']?.toString(),
      fullName: json['fullName']?.toString(),
      avatarUrl: json['avatarUrl']?.toString() ?? json['profileImageUrl']?.toString(),
      accountType: json['accountType']?.toString() ?? json['role']?.toString() ?? 'buyer',
      isEmailVerified: json['isEmailVerified'] == true || json['emailVerified'] == true,
      isTwoFactorEnabled: json['isTwoFactorEnabled'] == true || json['twoFactorEnabled'] == true,
      dealerId: json['dealerId']?.toString(),
      createdAt: json['createdAt'] != null ? DateTime.tryParse(json['createdAt'].toString()) : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'email': email,
      'firstName': firstName,
      'lastName': lastName,
      'fullName': fullName,
      'avatarUrl': avatarUrl,
      'accountType': accountType,
      'isEmailVerified': isEmailVerified,
      'isTwoFactorEnabled': isTwoFactorEnabled,
      'dealerId': dealerId,
      'createdAt': createdAt?.toIso8601String(),
    };
  }
}

/// Auth tokens response
class AuthTokens {
  final String accessToken;
  final String? refreshToken;
  final UserModel user;

  const AuthTokens({
    required this.accessToken,
    this.refreshToken,
    required this.user,
  });

  factory AuthTokens.fromJson(Map<String, dynamic> json) {
    final data = json['data'] ?? json;
    return AuthTokens(
      accessToken: data['accessToken']?.toString() ?? data['token']?.toString() ?? '',
      refreshToken: data['refreshToken']?.toString(),
      user: UserModel.fromJson(data['user'] as Map<String, dynamic>? ?? {}),
    );
  }
}
