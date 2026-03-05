import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:okla_app/core/constants/colors.dart';
import 'package:okla_app/core/constants/app_constants.dart';
import 'package:okla_app/presentation/blocs/vehicles/vehicles_bloc.dart';
import 'package:okla_app/presentation/widgets/common/vehicle_card.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  @override
  void initState() {
    super.initState();
    context.read<VehiclesBloc>().add(VehiclesFeaturedRequested());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: RefreshIndicator(
        color: OklaColors.primary500,
        onRefresh: () async {
          context.read<VehiclesBloc>().add(VehiclesFeaturedRequested());
        },
        child: CustomScrollView(
          slivers: [
            // Hero App Bar
            SliverAppBar(
              expandedHeight: 200,
              floating: false,
              pinned: true,
              backgroundColor: OklaColors.primary500,
              flexibleSpace: FlexibleSpaceBar(
                background: Container(
                  decoration: const BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topCenter,
                      end: Alignment.bottomCenter,
                      colors: [OklaColors.primary600, OklaColors.primary500],
                    ),
                  ),
                  child: SafeArea(
                    child: Padding(
                      padding: const EdgeInsets.all(OklaDimens.spacing16),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const Text(
                            'OKLA',
                            style: TextStyle(
                              fontSize: 32,
                              fontWeight: FontWeight.w800,
                              color: Colors.white,
                              letterSpacing: 2,
                            ),
                          ),
                          const SizedBox(height: 8),
                          Text(
                            'Tu vehículo ideal te espera',
                            style: TextStyle(
                              fontSize: 16,
                              color: Colors.white.withValues(alpha: 0.9),
                            ),
                          ),
                          const SizedBox(height: 16),
                          // Search bar
                          GestureDetector(
                            onTap: () => context.go('/buscar'),
                            child: Container(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 16,
                                vertical: 12,
                              ),
                              decoration: BoxDecoration(
                                color: Colors.white,
                                borderRadius: BorderRadius.circular(
                                  OklaDimens.radiusFull,
                                ),
                                boxShadow: [
                                  BoxShadow(
                                    color: Colors.black.withValues(alpha: 0.1),
                                    blurRadius: 8,
                                    offset: const Offset(0, 2),
                                  ),
                                ],
                              ),
                              child: const Row(
                                children: [
                                  Icon(
                                    Icons.search,
                                    color: OklaColors.neutral400,
                                  ),
                                  SizedBox(width: 12),
                                  Text(
                                    'Buscar marca, modelo o año...',
                                    style: TextStyle(
                                      color: OklaColors.neutral400,
                                      fontSize: 15,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ),

            // Quick categories
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.all(OklaDimens.spacing16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Categorías',
                      style: TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                        color: OklaColors.neutral800,
                      ),
                    ),
                    const SizedBox(height: 12),
                    SizedBox(
                      height: 100,
                      child: ListView(
                        scrollDirection: Axis.horizontal,
                        children: [
                          _CategoryCard(
                            icon: Icons.directions_car,
                            label: 'Sedán',
                            onTap: () => context.go('/buscar?bodyType=Sedan'),
                          ),
                          _CategoryCard(
                            icon: Icons.local_shipping,
                            label: 'SUV',
                            onTap: () => context.go('/buscar?bodyType=SUV'),
                          ),
                          _CategoryCard(
                            icon: Icons.local_shipping_outlined,
                            label: 'Camioneta',
                            onTap: () => context.go('/buscar?bodyType=Pickup'),
                          ),
                          _CategoryCard(
                            icon: Icons.two_wheeler,
                            label: 'Deportivo',
                            onTap: () => context.go('/buscar?bodyType=Sports'),
                          ),
                          _CategoryCard(
                            icon: Icons.electric_car,
                            label: 'Eléctrico',
                            onTap:
                                () => context.go('/buscar?fuelType=Electric'),
                          ),
                          _CategoryCard(
                            icon: Icons.star,
                            label: 'Híbrido',
                            onTap: () => context.go('/buscar?fuelType=Hybrid'),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),

            // Featured vehicles
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: OklaDimens.spacing16,
                  vertical: OklaDimens.spacing4,
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Vehículos Destacados',
                      style: TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                        color: OklaColors.neutral800,
                      ),
                    ),
                    TextButton(
                      onPressed: () => context.go('/buscar'),
                      child: const Text('Ver todos'),
                    ),
                  ],
                ),
              ),
            ),

            // Featured vehicles grid
            BlocBuilder<VehiclesBloc, VehiclesState>(
              builder: (context, state) {
                if (state is VehicleFeaturedLoaded) {
                  return SliverPadding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: OklaDimens.spacing16,
                    ),
                    sliver: SliverList(
                      delegate: SliverChildBuilderDelegate(
                        (context, index) {
                          final vehicle = state.vehicles[index];
                          return Padding(
                            padding: const EdgeInsets.only(
                              bottom: OklaDimens.spacing8,
                            ),
                            child: VehicleCard(
                              vehicle: vehicle,
                              onTap: () {
                                final slug =
                                    (vehicle.slug ?? '').isNotEmpty
                                        ? vehicle.slug!
                                        : vehicle.id;
                                context.push('/vehiculos/$slug');
                              },
                            ),
                          );
                        },
                        childCount: state.vehicles.length,
                      ),
                    ),
                  );
                }

                if (state is VehiclesError) {
                  return SliverFillRemaining(
                    child: Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const Icon(
                            Icons.error_outline,
                            size: 48,
                            color: OklaColors.error,
                          ),
                          const SizedBox(height: 16),
                          Text(
                            state.message,
                            textAlign: TextAlign.center,
                          ),
                          const SizedBox(height: 16),
                          ElevatedButton(
                            onPressed: () {
                              context.read<VehiclesBloc>().add(
                                VehiclesFeaturedRequested(),
                              );
                            },
                            child: const Text('Reintentar'),
                          ),
                        ],
                      ),
                    ),
                  );
                }

                // Loading state
                return const SliverFillRemaining(
                  child: Center(
                    child: CircularProgressIndicator(
                      color: OklaColors.primary500,
                    ),
                  ),
                );
              },
            ),

            // Bottom padding
            const SliverToBoxAdapter(child: SizedBox(height: 80)),
          ],
        ),
      ),
    );
  }
}

class _CategoryCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _CategoryCard({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right: 12),
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          width: 85,
          decoration: BoxDecoration(
            color: OklaColors.primary50,
            borderRadius: BorderRadius.circular(OklaDimens.radiusMd),
            border: Border.all(color: OklaColors.primary100),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, size: 32, color: OklaColors.primary500),
              const SizedBox(height: 8),
              Text(
                label,
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: OklaColors.primary700,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
