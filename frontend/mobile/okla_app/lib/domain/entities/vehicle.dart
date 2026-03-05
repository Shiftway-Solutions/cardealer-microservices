/// Vehicle entity — core domain model
class Vehicle {
  final String id;
  final String? slug;
  final String make;
  final String model;
  final int year;
  final double price;
  final String currency; // DOP or USD
  final int? mileage;
  final String? transmission; // Automática, Manual, CVT
  final String? fuelType; // Gasolina, Diesel, Híbrido, Eléctrico
  final String? bodyType; // Sedán, SUV, Pickup, etc.
  final String condition; // Nuevo, Usado, Certificado
  final String? color;
  final String? description;
  final String? location;
  final String? province;
  final List<String> features;
  final List<String> imageUrls;
  final String? mainImageUrl;
  final String? engineSize;
  final int? doors;
  final int? seats;
  final String? driveType; // 4x4, 4x2, AWD
  final String? vin;
  final String? plateNumber;
  final String status; // active, sold, paused, pending
  final bool isFeatured;
  final bool isPremium;
  final int viewCount;
  final int favoriteCount;
  final int contactCount;
  final String? sellerId;
  final String? dealerId;
  final String? sellerName;
  final String? sellerAvatarUrl;
  final double? oklaScore;
  final String? dealRating; // great, good, fair, high, uncertain
  final DateTime? createdAt;
  final DateTime? updatedAt;

  const Vehicle({
    required this.id,
    this.slug,
    required this.make,
    required this.model,
    required this.year,
    required this.price,
    this.currency = 'DOP',
    this.mileage,
    this.transmission,
    this.fuelType,
    this.bodyType,
    this.condition = 'Usado',
    this.color,
    this.description,
    this.location,
    this.province,
    this.features = const [],
    this.imageUrls = const [],
    this.mainImageUrl,
    this.engineSize,
    this.doors,
    this.seats,
    this.driveType,
    this.vin,
    this.plateNumber,
    this.status = 'active',
    this.isFeatured = false,
    this.isPremium = false,
    this.viewCount = 0,
    this.favoriteCount = 0,
    this.contactCount = 0,
    this.sellerId,
    this.dealerId,
    this.sellerName,
    this.sellerAvatarUrl,
    this.oklaScore,
    this.dealRating,
    this.createdAt,
    this.updatedAt,
  });

  String get title => '$make $model $year';

  String get formattedPrice {
    final symbol = currency == 'USD' ? 'US\$' : 'RD\$';
    return '$symbol${_formatNumber(price)}';
  }

  String? get formattedMileage =>
      mileage != null ? '${_formatNumber(mileage!.toDouble())} km' : null;

  static String _formatNumber(double number) {
    if (number >= 1000000) {
      return '${(number / 1000000).toStringAsFixed(1)}M';
    }
    if (number >= 1000) {
      final parts = number.toStringAsFixed(0).split('');
      final buffer = StringBuffer();
      for (var i = 0; i < parts.length; i++) {
        if (i > 0 && (parts.length - i) % 3 == 0) buffer.write(',');
        buffer.write(parts[i]);
      }
      return buffer.toString();
    }
    return number.toStringAsFixed(0);
  }
}

/// Search filters for vehicle queries
class VehicleFilters {
  final String? query;
  final String? make;
  final String? model;
  final int? yearMin;
  final int? yearMax;
  final double? priceMin;
  final double? priceMax;
  final String? currency;
  final String? transmission;
  final String? fuelType;
  final String? bodyType;
  final String? condition;
  final String? province;
  final String? color;
  final String? driveType;
  final int? mileageMax;
  final String? sortBy; // price_asc, price_desc, date_desc, mileage_asc
  final int page;
  final int pageSize;

  const VehicleFilters({
    this.query,
    this.make,
    this.model,
    this.yearMin,
    this.yearMax,
    this.priceMin,
    this.priceMax,
    this.currency,
    this.transmission,
    this.fuelType,
    this.bodyType,
    this.condition,
    this.province,
    this.color,
    this.driveType,
    this.mileageMax,
    this.sortBy,
    this.page = 1,
    this.pageSize = 20,
  });

  VehicleFilters copyWith({
    String? query,
    String? make,
    String? model,
    int? yearMin,
    int? yearMax,
    double? priceMin,
    double? priceMax,
    String? currency,
    String? transmission,
    String? fuelType,
    String? bodyType,
    String? condition,
    String? province,
    String? color,
    String? driveType,
    int? mileageMax,
    String? sortBy,
    int? page,
    int? pageSize,
  }) {
    return VehicleFilters(
      query: query ?? this.query,
      make: make ?? this.make,
      model: model ?? this.model,
      yearMin: yearMin ?? this.yearMin,
      yearMax: yearMax ?? this.yearMax,
      priceMin: priceMin ?? this.priceMin,
      priceMax: priceMax ?? this.priceMax,
      currency: currency ?? this.currency,
      transmission: transmission ?? this.transmission,
      fuelType: fuelType ?? this.fuelType,
      bodyType: bodyType ?? this.bodyType,
      condition: condition ?? this.condition,
      province: province ?? this.province,
      color: color ?? this.color,
      driveType: driveType ?? this.driveType,
      mileageMax: mileageMax ?? this.mileageMax,
      sortBy: sortBy ?? this.sortBy,
      page: page ?? this.page,
      pageSize: pageSize ?? this.pageSize,
    );
  }

  Map<String, dynamic> toQueryParameters() {
    final params = <String, dynamic>{};
    if (query != null) params['q'] = query;
    if (make != null) params['make'] = make;
    if (model != null) params['model'] = model;
    if (yearMin != null) params['yearMin'] = yearMin;
    if (yearMax != null) params['yearMax'] = yearMax;
    if (priceMin != null) params['priceMin'] = priceMin;
    if (priceMax != null) params['priceMax'] = priceMax;
    if (currency != null) params['currency'] = currency;
    if (transmission != null) params['transmission'] = transmission;
    if (fuelType != null) params['fuelType'] = fuelType;
    if (bodyType != null) params['bodyType'] = bodyType;
    if (condition != null) params['condition'] = condition;
    if (province != null) params['province'] = province;
    if (color != null) params['color'] = color;
    if (driveType != null) params['driveType'] = driveType;
    if (mileageMax != null) params['mileageMax'] = mileageMax;
    if (sortBy != null) params['sortBy'] = sortBy;
    params['page'] = page;
    params['pageSize'] = pageSize;
    return params;
  }

  bool get hasActiveFilters =>
      make != null ||
      model != null ||
      yearMin != null ||
      yearMax != null ||
      priceMin != null ||
      priceMax != null ||
      transmission != null ||
      fuelType != null ||
      bodyType != null ||
      condition != null ||
      province != null ||
      color != null;

  VehicleFilters clearAll() => VehicleFilters(
        query: query,
        page: 1,
        pageSize: pageSize,
        sortBy: sortBy,
      );
}
