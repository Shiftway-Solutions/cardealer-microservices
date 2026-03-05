import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:okla_app/core/constants/colors.dart';

/// Main scaffold with bottom navigation bar
class MainScaffold extends StatelessWidget {
  final Widget child;

  const MainScaffold({super.key, required this.child});

  int _currentIndex(BuildContext context) {
    final location = GoRouterState.of(context).uri.toString();
    if (location.startsWith('/buscar')) return 1;
    if (location.startsWith('/favoritos')) return 2;
    if (location.startsWith('/mensajes')) return 3;
    if (location.startsWith('/perfil')) return 4;
    return 0;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: child,
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex(context),
        onDestinationSelected: (index) {
          switch (index) {
            case 0:
              context.go('/');
              break;
            case 1:
              context.go('/buscar');
              break;
            case 2:
              context.go('/favoritos');
              break;
            case 3:
              context.go('/mensajes');
              break;
            case 4:
              context.go('/perfil');
              break;
          }
        },
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.home_outlined),
            selectedIcon: Icon(Icons.home, color: OklaColors.primary500),
            label: 'Inicio',
          ),
          NavigationDestination(
            icon: Icon(Icons.search_outlined),
            selectedIcon: Icon(Icons.search, color: OklaColors.primary500),
            label: 'Buscar',
          ),
          NavigationDestination(
            icon: Icon(Icons.favorite_outline),
            selectedIcon: Icon(Icons.favorite, color: OklaColors.primary500),
            label: 'Favoritos',
          ),
          NavigationDestination(
            icon: Icon(Icons.chat_bubble_outline),
            selectedIcon: Icon(Icons.chat_bubble, color: OklaColors.primary500),
            label: 'Mensajes',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person, color: OklaColors.primary500),
            label: 'Perfil',
          ),
        ],
      ),
    );
  }
}
