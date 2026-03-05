import 'package:flutter/material.dart';
import 'package:okla_app/core/constants/colors.dart';
import 'package:okla_app/core/constants/app_constants.dart';
import 'package:okla_app/domain/entities/vehicle.dart';

class VehicleCard extends StatelessWidget {
  final Vehicle vehicle;
  final VoidCallback? onTap;
  final VoidCallback? onFavorite;
  final bool isFavorite;
  final bool compact;

  const VehicleCard({
    super.key,
    required this.vehicle,
    this.onTap,
    this.onFavorite,
    this.isFavorite = false,
    this.compact = false,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(OklaDimens.radiusMd),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.06),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        clipBehavior: Clip.antiAlias,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Image
            Stack(
              children: [
                AspectRatio(
                  aspectRatio: compact ? 16 / 10 : 16 / 9,
                  child: vehicle.imageUrls.isNotEmpty
                      ? Image.network(
                          vehicle.imageUrls.first,
                          fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) => _placeholder(),
                          loadingBuilder: (_, child, loading) {
                            if (loading == null) return child;
                            return _placeholder(
                              child: const CircularProgressIndicator(
                                strokeWidth: 2,
                                color: OklaColors.primary500,
                              ),
                            );
                          },
                        )
                      : _placeholder(),
                ),
                // Condition badge
                Positioned(
                  top: 8,
                  left: 8,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: vehicle.condition.toLowerCase() == 'nuevo'
                          ? OklaColors.success
                          : OklaColors.info,
                      borderRadius: BorderRadius.circular(OklaDimens.radiusSm),
                    ),
                    child: Text(
                      vehicle.condition,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ),
                // Favorite button
                Positioned(
                  top: 8,
                  right: 8,
                  child: GestureDetector(
                    onTap: onFavorite,
                    child: Container(
                      padding: const EdgeInsets.all(6),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.9),
                        shape: BoxShape.circle,
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withValues(alpha: 0.1),
                            blurRadius: 4,
                          ),
                        ],
                      ),
                      child: Icon(
                        isFavorite ? Icons.favorite : Icons.favorite_border,
                        size: 20,
                        color: isFavorite ? OklaColors.error : OklaColors.neutral400,
                      ),
                    ),
                  ),
                ),
                // Image count
                if (vehicle.imageUrls.length > 1)
                  Positioned(
                    bottom: 8,
                    right: 8,
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      decoration: BoxDecoration(
                        color: Colors.black.withValues(alpha: 0.6),
                        borderRadius: BorderRadius.circular(OklaDimens.radiusSm),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.photo_library, size: 14, color: Colors.white),
                          const SizedBox(width: 4),
                          Text(
                            '${vehicle.imageUrls.length}',
                            style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.w600),
                          ),
                        ],
                      ),
                    ),
                  ),
              ],
            ),

            // Info section
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Title
                  Text(
                    vehicle.title,
                    maxLines: compact ? 1 : 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w600,
                      color: OklaColors.neutral800,
                    ),
                  ),
                  const SizedBox(height: 4),

                  // Year / Mileage / Transmission
                  Row(
                    children: [
                      Text(
                        '${vehicle.year}',
                        style: const TextStyle(fontSize: 13, color: OklaColors.neutral500),
                      ),
                      if (vehicle.mileage != null && vehicle.mileage! > 0) ...[
                        const Text(' · ', style: TextStyle(color: OklaColors.neutral300)),
                        Text(
                          '${_formatMileage(vehicle.mileage!)} km',
                          style: const TextStyle(fontSize: 13, color: OklaColors.neutral500),
                        ),
                      ],
                      if (vehicle.transmission != null && vehicle.transmission!.isNotEmpty) ...[
                        const Text(' · ', style: TextStyle(color: OklaColors.neutral300)),
                        Flexible(
                          child: Text(
                            vehicle.transmission!,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(fontSize: 13, color: OklaColors.neutral500),
                          ),
                        ),
                      ],
                    ],
                  ),
                  const SizedBox(height: 6),

                  // Price
                  Text(
                    'RD\$ ${_formatPrice(vehicle.price)}',
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                      color: OklaColors.primary500,
                    ),
                  ),

                  // Location
                  if (vehicle.location != null && vehicle.location!.isNotEmpty && !compact) ...[
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        const Icon(Icons.location_on_outlined, size: 14, color: OklaColors.neutral400),
                        const SizedBox(width: 4),
                        Expanded(
                          child: Text(
                            vehicle.location!,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(fontSize: 12, color: OklaColors.neutral400),
                          ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _placeholder({Widget? child}) {
    return Container(
      color: OklaColors.neutral100,
      child: Center(
        child: child ?? const Icon(Icons.directions_car, size: 48, color: OklaColors.neutral300),
      ),
    );
  }

  String _formatPrice(double price) {
    final parts = price.toStringAsFixed(0).split('');
    final buffer = StringBuffer();
    for (var i = 0; i < parts.length; i++) {
      if (i > 0 && (parts.length - i) % 3 == 0) {
        buffer.write(',');
      }
      buffer.write(parts[i]);
    }
    return buffer.toString();
  }

  String _formatMileage(int mileage) {
    if (mileage >= 1000) {
      return '${(mileage / 1000).toStringAsFixed(0)}k';
    }
    return mileage.toString();
  }
}
