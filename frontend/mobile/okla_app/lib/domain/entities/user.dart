/// User entity representing an authenticated user
class User {
  final String id;
  final String email;
  final String? firstName;
  final String? lastName;
  final String? fullName;
  final String? avatarUrl;
  final String accountType; // buyer, seller, dealer, admin
  final bool isEmailVerified;
  final bool isTwoFactorEnabled;
  final String? dealerId;
  final DateTime? createdAt;

  const User({
    required this.id,
    required this.email,
    this.firstName,
    this.lastName,
    this.fullName,
    this.avatarUrl,
    required this.accountType,
    this.isEmailVerified = false,
    this.isTwoFactorEnabled = false,
    this.dealerId,
    this.createdAt,
  });

  String get displayName => fullName ?? '$firstName $lastName'.trim();
  bool get isDealer => accountType == 'dealer' || dealerId != null;
  bool get isSeller => accountType == 'seller';
  bool get isAdmin => accountType == 'admin';
  bool get isBuyer => accountType == 'buyer' || accountType == 'user';
  String get initials {
    final f = firstName?.isNotEmpty == true ? firstName![0] : '';
    final l = lastName?.isNotEmpty == true ? lastName![0] : '';
    return '$f$l'.toUpperCase();
  }
}
