import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:okla_app/presentation/pages/auth/login_page.dart';
import 'package:okla_app/presentation/pages/auth/register_page.dart';
import 'package:okla_app/presentation/pages/auth/forgot_password_page.dart';
import 'package:okla_app/presentation/pages/home/home_page.dart';
import 'package:okla_app/presentation/pages/search/search_page.dart';
import 'package:okla_app/presentation/pages/vehicles/vehicle_detail_page.dart';
import 'package:okla_app/presentation/pages/favorites/favorites_page.dart';
import 'package:okla_app/presentation/pages/messages/messages_page.dart';
import 'package:okla_app/presentation/pages/notifications/notifications_page.dart';
import 'package:okla_app/presentation/pages/profile/profile_page.dart';
import 'package:okla_app/presentation/pages/settings/settings_page.dart';
import 'package:okla_app/presentation/pages/seller/publish_vehicle_page.dart';
import 'package:okla_app/presentation/pages/seller/my_vehicles_page.dart';
import 'package:okla_app/presentation/pages/dealer/dealer_dashboard_page.dart';
import 'package:okla_app/presentation/pages/dealer/dealer_inventory_page.dart';
import 'package:okla_app/presentation/pages/checkout/checkout_page.dart';
import 'package:okla_app/presentation/pages/compare/compare_page.dart';
import 'package:okla_app/presentation/pages/reviews/reviews_page.dart';
import 'package:okla_app/presentation/pages/alerts/alerts_page.dart';
import 'package:okla_app/presentation/pages/chatbot/chatbot_page.dart';
import 'package:okla_app/presentation/widgets/common/main_scaffold.dart';

/// App router with GoRouter — declarative navigation
class AppRouter {
  static final _rootNavigatorKey = GlobalKey<NavigatorState>();
  static final _shellNavigatorKey = GlobalKey<NavigatorState>();

  static GoRouter get router => _router;

  static final _router = GoRouter(
    navigatorKey: _rootNavigatorKey,
    initialLocation: '/',
    debugLogDiagnostics: false,
    routes: [
      // ──── Auth Routes (no bottom nav) ────
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginPage(),
      ),
      GoRoute(
        path: '/registro',
        builder: (context, state) => const RegisterPage(),
      ),
      GoRoute(
        path: '/recuperar-contrasena',
        builder: (context, state) => const ForgotPasswordPage(),
      ),

      // ──── Main Shell (with bottom nav) ────
      ShellRoute(
        navigatorKey: _shellNavigatorKey,
        builder: (context, state, child) => MainScaffold(child: child),
        routes: [
          // Home Tab
          GoRoute(
            path: '/',
            pageBuilder: (context, state) => const NoTransitionPage(child: HomePage()),
          ),
          // Search Tab
          GoRoute(
            path: '/buscar',
            pageBuilder: (context, state) => const NoTransitionPage(child: SearchPage()),
          ),
          // Favorites Tab
          GoRoute(
            path: '/favoritos',
            pageBuilder: (context, state) => const NoTransitionPage(child: FavoritesPage()),
          ),
          // Messages Tab
          GoRoute(
            path: '/mensajes',
            pageBuilder: (context, state) => const NoTransitionPage(child: MessagesPage()),
          ),
          // Profile Tab
          GoRoute(
            path: '/perfil',
            pageBuilder: (context, state) => const NoTransitionPage(child: ProfilePage()),
          ),
        ],
      ),

      // ──── Detail Routes (fullscreen, no bottom nav) ────
      GoRoute(
        path: '/vehiculos/:slug',
        builder: (context, state) => VehicleDetailPage(
          slug: state.pathParameters['slug']!,
        ),
      ),
      GoRoute(
        path: '/comparar',
        builder: (context, state) => const ComparePage(),
      ),
      GoRoute(
        path: '/notificaciones',
        builder: (context, state) => const NotificationsPage(),
      ),
      GoRoute(
        path: '/configuracion',
        builder: (context, state) => const SettingsPage(),
      ),

      // ──── Seller Routes ────
      GoRoute(
        path: '/publicar',
        builder: (context, state) => const PublishVehiclePage(),
      ),
      GoRoute(
        path: '/mis-vehiculos',
        builder: (context, state) => const MyVehiclesPage(),
      ),

      // ──── Dealer Routes ────
      GoRoute(
        path: '/dealer',
        builder: (context, state) => const DealerDashboardPage(),
        routes: [
          GoRoute(
            path: 'inventario',
            builder: (context, state) => const DealerInventoryPage(),
          ),
        ],
      ),

      // ──── Checkout ────
      GoRoute(
        path: '/checkout',
        builder: (context, state) => const CheckoutPage(),
      ),

      // ──── Reviews ────
      GoRoute(
        path: '/resenas/:targetId',
        builder: (context, state) => ReviewsPage(
          targetId: state.pathParameters['targetId']!,
        ),
      ),

      // ──── Alerts ────
      GoRoute(
        path: '/alertas',
        builder: (context, state) => const AlertsPage(),
      ),

      // ──── Chatbot ────
      GoRoute(
        path: '/chatbot',
        builder: (context, state) => const ChatbotPage(),
      ),
    ],
  );
}
