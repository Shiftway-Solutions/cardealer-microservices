import 'package:okla_app/domain/entities/vehicle.dart';

/// Vehicle JSON model
class VehicleModel extends Vehicle {
  const VehicleModel({
    required super.id,
    super.slug,
    required super.make,
    required super.model,
    required super.year,
    required super.price,
    super.currency,
    super.mileage,
    super.transmission,
    super.fuelType,
    super.bodyType,
    super.condition,
    super.color,
    super.description,
    super.location,
    super.province,
    super.features,
    super.imageUrls,
    super.mainImageUrl,
    super.engineSize,
    super.doors,
    super.seats,
    super.driveType,
    super.vin,
    super.plateNumber,
    super.status,
    super.isFeatured,
    super.isPremium,
    super.viewCount,
    super.favoriteCount,
    super.contactCount,
    super.sellerId,
    super.dealerId,
    super.sellerName,
    super.sellerAvatarUrl,
    super.oklaScore,
    super.dealRating,
    super.createdAt,
    super.updatedAt,
  });

  factory VehicleModel.fromJson(Map<String, dynamic> json) {
    return VehicleModel(
      id: json['id']?.toString() ?? '',
      slug: json['slug']?.toString(),
      make: json['make']?.toString() ?? json['brand']?.toString() ?? '',
      model: json['model']?.toString() ?? '',
      year: (json['year'] as num?)?.toInt() ?? 0,
      price: (json['price'] as num?)?.toDouble() ?? 0,
      currency: json['currency']?.toString() ?? 'DOP',
      mileage: (json['mileage'] as num?)?.toInt() ?? (json['kilometraje'] as num?)?.toInt(),
      transmission: json['transmission']?.toString() ?? json['transmision']?.toString(),
      fuelType: json['fuelType']?.toString() ?? json['combustible']?.toString(),
      bodyType: json['bodyType']?.toString() ?? json['carroceria']?.toString(),
      condition: json['condition']?.toString() ?? json['condicion']?.toString() ?? 'Usado',
      color: json['color']?.toString(),
      description: json['description']?.toString() ?? json['descripcion']?.toString(),
      location: json['location']?.toString() ?? json['ubicacion']?.toString(),
      province: json['province']?.toString() ?? json['provincia']?.toString(),
      features: _parseStringList(json['features'] ?? json['caracteristicas']),
      imageUrls: _parseStringList(json['imageUrls'] ?? json['images'] ?? json['fotos']),
      mainImageUrl: json['mainImageUrl']?.toString() ??
          json['mainImage']?.toString() ??
          json['thumbnailUrl']?.toString(),
      engineSize: json['engineSize']?.toString() ?? json['motor']?.toString(),
      doors: (json['doors'] as num?)?.toInt() ?? (json['puertas'] as num?)?.toInt(),
      seats: (json['seats'] as num?)?.toInt() ?? (json['asientos'] as num?)?.toInt(),
      driveType: json['driveType']?.toString() ?? json['traccion']?.toString(),
      vin: json['vin']?.toString(),
      plateNumber: json['plateNumber']?.toString() ?? json['placa']?.toString(),
      status: json['status']?.toString() ?? json['estado']?.toString() ?? 'active',
      isFeatured: json['isFeatured'] == true || json['destacado'] == true,
      isPremium: json['isPremium'] == true || json['premium'] == true,
      viewCount: (json['viewCount'] as num?)?.toInt() ?? (json['vistas'] as num?)?.toInt() ?? 0,
      favoriteCount:
          (json['favoriteCount'] as num?)?.toInt() ?? (json['favoritos'] as num?)?.toInt() ?? 0,
      contactCount:
          (json['contactCount'] as num?)?.toInt() ?? (json['contactos'] as num?)?.toInt() ?? 0,
      sellerId: json['sellerId']?.toString() ?? json['userId']?.toString(),
      dealerId: json['dealerId']?.toString(),
      sellerName: json['sellerName']?.toString(),
      sellerAvatarUrl: json['sellerAvatarUrl']?.toString(),
      oklaScore: (json['oklaScore'] as num?)?.toDouble(),
      dealRating: json['dealRating']?.toString(),
      createdAt:
          json['createdAt'] != null ? DateTime.tryParse(json['createdAt'].toString()) : null,
      updatedAt:
          json['updatedAt'] != null ? DateTime.tryParse(json['updatedAt'].toString()) : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'slug': slug,
      'make': make,
      'model': model,
      'year': year,
      'price': price,
      'currency': currency,
      'mileage': mileage,
      'transmission': transmission,
      'fuelType': fuelType,
      'bodyType': bodyType,
      'condition': condition,
      'color': color,
      'description': description,
      'location': location,
      'province': province,
      'features': features,
      'imageUrls': imageUrls,
      'mainImageUrl': mainImageUrl,
      'engineSize': engineSize,
      'doors': doors,
      'seats': seats,
      'driveType': driveType,
      'vin': vin,
      'status': status,
      'isFeatured': isFeatured,
      'isPremium': isPremium,
    };
  }

  static List<String> _parseStringList(dynamic value) {
    if (value == null) return [];
    if (value is List) return value.map((e) => e.toString()).toList();
    return [];
  }
}
