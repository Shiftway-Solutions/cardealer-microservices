import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:okla_app/core/constants/colors.dart';
import 'package:okla_app/core/constants/app_constants.dart';
import 'package:okla_app/domain/entities/vehicle.dart';
import 'package:okla_app/presentation/blocs/vehicles/vehicles_bloc.dart';
import 'package:okla_app/presentation/widgets/common/vehicle_card.dart';

class VehicleDetailPage extends StatefulWidget {
  final String slug;
  const VehicleDetailPage({super.key, required this.slug});

  @override
  State<VehicleDetailPage> createState() => _VehicleDetailPageState();
}

class _VehicleDetailPageState extends State<VehicleDetailPage> {
  @override
  void initState() {
    super.initState();
    context.read<VehiclesBloc>().add(VehicleDetailRequested(slugOrId: widget.slug));
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<VehiclesBloc, VehiclesState>(
      builder: (context, state) {
        if (state is VehicleDetailLoaded) {
          return _buildDetail(context, state.vehicle, state.similarVehicles);
        }
        if (state is VehiclesError) {
          return Scaffold(
            appBar: AppBar(),
            body: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.error_outline, size: 48, color: OklaColors.error),
                  const SizedBox(height: 16),
                  Text(state.message),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: () {
                      context.read<VehiclesBloc>().add(
                        VehicleDetailRequested(slugOrId: widget.slug),
                      );
                    },
                    child: const Text('Reintentar'),
                  ),
                ],
              ),
            ),
          );
        }
        return Scaffold(
          appBar: AppBar(),
          body: const Center(
            child: CircularProgressIndicator(color: OklaColors.primary500),
          ),
        );
      },
    );
  }

  Widget _buildDetail(BuildContext context, Vehicle vehicle, List<Vehicle> similar) {
    final currencyFormat = 'RD\$';

    return Scaffold(
      body: CustomScrollView(
        slivers: [
          // Image carousel
          SliverAppBar(
            expandedHeight: 300,
            pinned: true,
            backgroundColor: OklaColors.primary500,
            leading: IconButton(
              icon: const CircleAvatar(
                backgroundColor: Colors.white,
                radius: 18,
                child: Icon(Icons.arrow_back, color: OklaColors.neutral800, size: 20),
              ),
              onPressed: () => context.pop(),
            ),
            actions: [
              IconButton(
                icon: const CircleAvatar(
                  backgroundColor: Colors.white,
                  radius: 18,
                  child: Icon(Icons.favorite_border, color: OklaColors.error, size: 20),
                ),
                onPressed: () {
                  // Toggle favorite
                },
              ),
              IconButton(
                icon: const CircleAvatar(
                  backgroundColor: Colors.white,
                  radius: 18,
                  child: Icon(Icons.share, color: OklaColors.neutral800, size: 20),
                ),
                onPressed: () {
                  // Share
                },
              ),
            ],
            flexibleSpace: FlexibleSpaceBar(
              background: vehicle.imageUrls.isNotEmpty
                  ? PageView.builder(
                      itemCount: vehicle.imageUrls.length,
                      itemBuilder: (context, index) {
                        return Image.network(
                          vehicle.imageUrls[index],
                          fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) => Container(
                            color: OklaColors.neutral100,
                            child: const Icon(Icons.directions_car, size: 80, color: OklaColors.neutral300),
                          ),
                        );
                      },
                    )
                  : Container(
                      color: OklaColors.neutral100,
                      child: const Icon(Icons.directions_car, size: 80, color: OklaColors.neutral300),
                    ),
            ),
          ),

          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.all(OklaDimens.spacing16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Title & price
                  Text(
                    vehicle.title,
                    style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w700, color: OklaColors.neutral800),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '$currencyFormat ${_formatPrice(vehicle.price)}',
                    style: const TextStyle(fontSize: 26, fontWeight: FontWeight.w800, color: OklaColors.primary500),
                  ),

                  // Condition & Year badges
                  const SizedBox(height: 12),
                  Wrap(
                    spacing: 8,
                    children: [
                      _Badge(
                        label: vehicle.condition,
                        color: vehicle.condition.toLowerCase() == 'nuevo' ? OklaColors.success : OklaColors.info,
                      ),
                      _Badge(label: '${vehicle.year}', color: OklaColors.neutral500),
                      if (vehicle.transmission != null && vehicle.transmission!.isNotEmpty)
                        _Badge(label: vehicle.transmission!, color: OklaColors.neutral500),
                      if (vehicle.fuelType != null && vehicle.fuelType!.isNotEmpty)
                        _Badge(label: vehicle.fuelType!, color: OklaColors.neutral500),
                    ],
                  ),

                  const Divider(height: 32),

                  // Key specs
                  const Text('Especificaciones', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                  const SizedBox(height: 12),
                  _SpecsGrid(vehicle: vehicle),

                  if (vehicle.description != null && vehicle.description!.isNotEmpty) ...[
                    const Divider(height: 32),
                    const Text('Descripción', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                    const SizedBox(height: 8),
                    Text(
                      vehicle.description!,
                      style: const TextStyle(fontSize: 15, color: OklaColors.neutral600, height: 1.5),
                    ),
                  ],

                  // Features
                  if (vehicle.features.isNotEmpty) ...[
                    const Divider(height: 32),
                    const Text('Características', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                    const SizedBox(height: 12),
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: vehicle.features.map((f) => Chip(
                        label: Text(f, style: const TextStyle(fontSize: 13)),
                        backgroundColor: OklaColors.primary50,
                        side: const BorderSide(color: OklaColors.primary100),
                      )).toList(),
                    ),
                  ],

                  // Location
                  if (vehicle.location != null && vehicle.location!.isNotEmpty) ...[
                    const Divider(height: 32),
                    Row(
                      children: [
                        const Icon(Icons.location_on, color: OklaColors.primary500, size: 20),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            vehicle.location!,
                            style: const TextStyle(fontSize: 15, color: OklaColors.neutral600),
                          ),
                        ),
                      ],
                    ),
                  ],

                  // Similar vehicles
                  if (similar.isNotEmpty) ...[
                    const Divider(height: 32),
                    const Text('Vehículos Similares', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                    const SizedBox(height: 12),
                    SizedBox(
                      height: 260,
                      child: ListView.builder(
                        scrollDirection: Axis.horizontal,
                        itemCount: similar.length,
                        itemBuilder: (context, index) {
                          return SizedBox(
                            width: 280,
                            child: Padding(
                              padding: const EdgeInsets.only(right: 12),
                              child: VehicleCard(
                                vehicle: similar[index],
                                onTap: () {
                                  final s = (similar[index].slug ?? '').isNotEmpty
                                      ? similar[index].slug!
                                      : similar[index].id;
                                  context.push('/vehiculos/$s');
                                },
                              ),
                            ),
                          );
                        },
                      ),
                    ),
                  ],

                  const SizedBox(height: 100),
                ],
              ),
            ),
          ),
        ],
      ),

      // Bottom contact bar
      bottomNavigationBar: SafeArea(
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: OklaDimens.spacing16, vertical: OklaDimens.spacing8),
          decoration: BoxDecoration(
            color: Colors.white,
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.1),
                blurRadius: 8,
                offset: const Offset(0, -2),
              ),
            ],
          ),
          child: Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: () {
                    // Open messaging
                  },
                  icon: const Icon(Icons.chat_bubble_outline),
                  label: const Text('Mensaje'),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: OklaColors.primary500,
                    side: const BorderSide(color: OklaColors.primary500),
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: ElevatedButton.icon(
                  onPressed: () {
                    // Call seller
                  },
                  icon: const Icon(Icons.phone),
                  label: const Text('Llamar'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: OklaColors.primary500,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  String _formatPrice(double price) {
    if (price >= 1000000) {
      return '${(price / 1000000).toStringAsFixed(1)}M';
    }
    if (price >= 1000) {
      return '${(price / 1000).toStringAsFixed(0)},000';
    }
    return price.toStringAsFixed(0);
  }
}

class _Badge extends StatelessWidget {
  final String label;
  final Color color;
  const _Badge({required this.label, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(OklaDimens.radiusFull),
        border: Border.all(color: color.withValues(alpha: 0.3)),
      ),
      child: Text(
        label,
        style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: color),
      ),
    );
  }
}

class _SpecsGrid extends StatelessWidget {
  final Vehicle vehicle;
  const _SpecsGrid({required this.vehicle});

  @override
  Widget build(BuildContext context) {
    final specs = <_SpecItem>[
      if (vehicle.make.isNotEmpty) _SpecItem(Icons.branding_watermark, 'Marca', vehicle.make),
      if (vehicle.model.isNotEmpty) _SpecItem(Icons.model_training, 'Modelo', vehicle.model),
      _SpecItem(Icons.calendar_today, 'Año', '${vehicle.year}'),
      if (vehicle.mileage != null && vehicle.mileage! > 0) _SpecItem(Icons.speed, 'Kilometraje', '${vehicle.mileage!.toStringAsFixed(0)} km'),
      if (vehicle.transmission != null && vehicle.transmission!.isNotEmpty) _SpecItem(Icons.settings, 'Transmisión', vehicle.transmission!),
      if (vehicle.fuelType != null && vehicle.fuelType!.isNotEmpty) _SpecItem(Icons.local_gas_station, 'Combustible', vehicle.fuelType!),
      if (vehicle.engineSize != null && vehicle.engineSize!.isNotEmpty) _SpecItem(Icons.engineering, 'Motor', vehicle.engineSize!),
      if (vehicle.color != null && vehicle.color!.isNotEmpty) _SpecItem(Icons.palette, 'Color', vehicle.color!),
      if (vehicle.doors != null && vehicle.doors! > 0) _SpecItem(Icons.sensor_door, 'Puertas', '${vehicle.doors}'),
      if (vehicle.seats != null && vehicle.seats! > 0) _SpecItem(Icons.event_seat, 'Asientos', '${vehicle.seats}'),
    ];

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        childAspectRatio: 3.5,
        crossAxisSpacing: 12,
        mainAxisSpacing: 8,
      ),
      itemCount: specs.length,
      itemBuilder: (context, index) {
        final spec = specs[index];
        return Row(
          children: [
            Icon(spec.icon, size: 20, color: OklaColors.primary500),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(spec.label, style: const TextStyle(fontSize: 11, color: OklaColors.neutral400)),
                  Text(spec.value, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600)),
                ],
              ),
            ),
          ],
        );
      },
    );
  }
}

class _SpecItem {
  final IconData icon;
  final String label;
  final String value;
  const _SpecItem(this.icon, this.label, this.value);
}
