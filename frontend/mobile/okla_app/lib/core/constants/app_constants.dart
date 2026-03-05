/// App-wide dimension constants
class OklaDimens {
  OklaDimens._();

  // Spacing
  static const double spacing2 = 2;
  static const double spacing4 = 4;
  static const double spacing6 = 6;
  static const double spacing8 = 8;
  static const double spacing10 = 10;
  static const double spacing12 = 12;
  static const double spacing16 = 16;
  static const double spacing20 = 20;
  static const double spacing24 = 24;
  static const double spacing32 = 32;
  static const double spacing40 = 40;
  static const double spacing48 = 48;
  static const double spacing56 = 56;
  static const double spacing64 = 64;

  // Border Radius
  static const double radiusXs = 4;
  static const double radiusSm = 6;
  static const double radiusMd = 8;
  static const double radiusLg = 12;
  static const double radiusXl = 16;
  static const double radius2xl = 24;
  static const double radiusFull = 999;

  // Icon Sizes
  static const double iconXs = 12;
  static const double iconSm = 16;
  static const double iconMd = 20;
  static const double iconLg = 24;
  static const double iconXl = 32;
  static const double icon2xl = 48;

  // Font Sizes
  static const double fontXs = 12;
  static const double fontSm = 14;
  static const double fontMd = 16;
  static const double fontLg = 18;
  static const double fontXl = 20;
  static const double font2xl = 24;
  static const double font3xl = 30;
  static const double font4xl = 36;
  static const double font5xl = 48;

  // Card
  static const double cardElevation = 2;
  static const double cardBorderRadius = 12;

  // Bottom Nav
  static const double bottomNavHeight = 64;

  // App Bar
  static const double appBarHeight = 56;
}

/// App-wide string constants
class OklaStrings {
  OklaStrings._();

  static const String appName = 'OKLA';
  static const String appTagline = 'Vehículos RD';
  static const String appFullName = 'OKLA - Vehículos RD';
  static const String appVersion = '2.0.0';

  // API
  static const String apiBaseUrlProd = 'https://okla.com.do/api';
  static const String apiBaseUrlStaging = 'https://staging.okla.com.do/api';
  static const String apiBaseUrlDev = 'http://localhost:3000/api';

  // Storage Keys
  static const String accessTokenKey = 'okla_access_token';
  static const String refreshTokenKey = 'okla_refresh_token';
  static const String userKey = 'okla_user';
  static const String themeKey = 'okla_theme';
  static const String onboardingKey = 'okla_onboarding_done';
  static const String biometricKey = 'okla_biometric_enabled';
  static const String fcmTokenKey = 'okla_fcm_token';

  // Deep Link
  static const String deepLinkScheme = 'okla';
  static const String deepLinkHost = 'okla.com.do';

  // Currencies
  static const String currencyDOP = 'DOP';
  static const String currencyUSD = 'USD';
  static const String currencySymbolDOP = 'RD\$';
  static const String currencySymbolUSD = 'US\$';
}
