import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:okla_app/core/di/injection.dart';
import 'package:okla_app/core/router/app_router.dart';
import 'package:okla_app/core/theme/app_theme.dart';
import 'package:okla_app/presentation/blocs/auth/auth_bloc.dart';
import 'package:okla_app/presentation/blocs/vehicles/vehicles_bloc.dart';

// Re-export sl for convenience
export 'package:okla_app/core/di/injection.dart' show sl;

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Lock orientation to portrait
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
  ]);

  // Set system UI overlay style
  SystemChrome.setSystemUIOverlayStyle(
    const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.dark,
      statusBarBrightness: Brightness.light,
    ),
  );

  // Initialize dependency injection
  await initDependencies();

  runApp(const OklaApp());
}

class OklaApp extends StatelessWidget {
  const OklaApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider<AuthBloc>(
          create: (_) => sl<AuthBloc>()..add(AuthCheckRequested()),
        ),
        BlocProvider<VehiclesBloc>(
          create: (_) => sl<VehiclesBloc>(),
        ),
      ],
      child: MaterialApp.router(
        title: 'OKLA - Vehículos RD',
        debugShowCheckedModeBanner: false,
        theme: OklaTheme.light,
        darkTheme: OklaTheme.dark,
        themeMode: ThemeMode.light,
        routerConfig: AppRouter.router,
        locale: const Locale('es', 'DO'),
      ),
    );
  }
}
