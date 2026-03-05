/// Environment configuration for the OKLA app
enum Environment { development, staging, production }

class AppConfig {
  final Environment environment;
  final String apiBaseUrl;
  final String appName;
  final bool enableLogging;
  final bool enableCrashlytics;

  const AppConfig({
    required this.environment,
    required this.apiBaseUrl,
    required this.appName,
    required this.enableLogging,
    required this.enableCrashlytics,
  });

  static const AppConfig development = AppConfig(
    environment: Environment.development,
    apiBaseUrl: 'http://localhost:3000/api',
    appName: 'OKLA Dev',
    enableLogging: true,
    enableCrashlytics: false,
  );

  static const AppConfig staging = AppConfig(
    environment: Environment.staging,
    apiBaseUrl: 'https://staging.okla.com.do/api',
    appName: 'OKLA Staging',
    enableLogging: true,
    enableCrashlytics: true,
  );

  static const AppConfig production = AppConfig(
    environment: Environment.production,
    apiBaseUrl: 'https://okla.com.do/api',
    appName: 'OKLA',
    enableLogging: false,
    enableCrashlytics: true,
  );

  bool get isDevelopment => environment == Environment.development;
  bool get isStaging => environment == Environment.staging;
  bool get isProduction => environment == Environment.production;
}
