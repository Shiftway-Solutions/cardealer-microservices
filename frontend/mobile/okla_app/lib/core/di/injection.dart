import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';
import 'package:okla_app/core/config/app_config.dart';
import 'package:okla_app/core/network/api_client.dart';
import 'package:okla_app/data/datasources/remote/auth_remote_datasource.dart';
import 'package:okla_app/data/datasources/remote/vehicle_remote_datasource.dart';
import 'package:okla_app/data/repositories/auth_repository_impl.dart';
import 'package:okla_app/data/repositories/vehicle_repository_impl.dart';
import 'package:okla_app/domain/repositories/auth_repository.dart';
import 'package:okla_app/domain/repositories/vehicle_repository.dart';
import 'package:okla_app/presentation/blocs/auth/auth_bloc.dart';
import 'package:okla_app/presentation/blocs/vehicles/vehicles_bloc.dart';

final sl = GetIt.instance;

/// Initialize all dependencies
Future<void> initDependencies({AppConfig config = AppConfig.production}) async {
  // ──── Core ────
  sl.registerSingleton<AppConfig>(config);
  sl.registerSingleton<FlutterSecureStorage>(const FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
    iOptions: IOSOptions(accessibility: KeychainAccessibility.first_unlock),
  ));
  sl.registerSingleton<ApiClient>(ApiClient(
    config: sl<AppConfig>(),
    storage: sl<FlutterSecureStorage>(),
  ));

  // ──── Data Sources ────
  sl.registerLazySingleton(() => AuthRemoteDataSource(client: sl<ApiClient>()));
  sl.registerLazySingleton(() => VehicleRemoteDataSource(client: sl<ApiClient>()));

  // ──── Repositories ────
  sl.registerLazySingleton<AuthRepository>(() => AuthRepositoryImpl(
        remote: sl<AuthRemoteDataSource>(),
        storage: sl<FlutterSecureStorage>(),
      ));
  sl.registerLazySingleton<VehicleRepository>(() => VehicleRepositoryImpl(
        remote: sl<VehicleRemoteDataSource>(),
      ));

  // ──── BLoCs ────
  sl.registerFactory(() => AuthBloc(repository: sl<AuthRepository>()));
  sl.registerFactory(() => VehiclesBloc(repository: sl<VehicleRepository>()));
}
