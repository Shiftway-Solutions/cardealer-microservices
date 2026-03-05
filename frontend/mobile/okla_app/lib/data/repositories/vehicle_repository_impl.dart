import 'package:okla_app/core/errors/failures.dart';
import 'package:okla_app/core/network/api_response_handler.dart';
import 'package:okla_app/data/datasources/remote/vehicle_remote_datasource.dart';
import 'package:okla_app/domain/entities/vehicle.dart';
import 'package:okla_app/domain/repositories/vehicle_repository.dart';

class VehicleRepositoryImpl implements VehicleRepository {
  final VehicleRemoteDataSource _remote;

  VehicleRepositoryImpl({required VehicleRemoteDataSource remote}) : _remote = remote;

  @override
  Future<(PaginatedResponse<Vehicle>?, Failure?)> searchVehicles(VehicleFilters filters) async {
    try {
      final result = await _remote.searchVehicles(filters);
      return (
        PaginatedResponse<Vehicle>(
          items: result.items,
          totalCount: result.totalCount,
          page: result.page,
          pageSize: result.pageSize,
          hasMore: result.hasMore,
        ),
        null
      );
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(Vehicle?, Failure?)> getVehicleBySlug(String slug) async {
    try {
      final vehicle = await _remote.getVehicleBySlug(slug);
      return (vehicle as Vehicle, null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(Vehicle?, Failure?)> getVehicleById(String id) async {
    try {
      final vehicle = await _remote.getVehicleById(id);
      return (vehicle as Vehicle, null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(List<Vehicle>?, Failure?)> getFeaturedVehicles({int limit = 10}) async {
    try {
      final vehicles = await _remote.getFeaturedVehicles(limit: limit);
      return (vehicles.cast<Vehicle>(), null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(List<Vehicle>?, Failure?)> getSimilarVehicles(String vehicleId, {int limit = 6}) async {
    try {
      final vehicles = await _remote.getSimilarVehicles(vehicleId, limit: limit);
      return (vehicles.cast<Vehicle>(), null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(bool, Failure?)> trackView(String vehicleId) async {
    try {
      await _remote.trackView(vehicleId);
      return (true, null);
    } catch (e) {
      return (false, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(Vehicle?, Failure?)> createVehicle(Map<String, dynamic> vehicleData) async {
    try {
      final vehicle = await _remote.createVehicle(vehicleData);
      return (vehicle as Vehicle, null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(Vehicle?, Failure?)> updateVehicle(String id, Map<String, dynamic> vehicleData) async {
    try {
      final vehicle = await _remote.updateVehicle(id, vehicleData);
      return (vehicle as Vehicle, null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(bool, Failure?)> publishVehicle(String id) async {
    try {
      await _remote.updateVehicle(id, {'status': 'active'});
      return (true, null);
    } catch (e) {
      return (false, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(bool, Failure?)> unpublishVehicle(String id) async {
    try {
      await _remote.updateVehicle(id, {'status': 'paused'});
      return (true, null);
    } catch (e) {
      return (false, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(bool, Failure?)> markAsSold(String id) async {
    try {
      await _remote.updateVehicle(id, {'status': 'sold'});
      return (true, null);
    } catch (e) {
      return (false, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(List<Vehicle>?, Failure?)> getMyVehicles() async {
    try {
      final vehicles = await _remote.getMyVehicles();
      return (vehicles.cast<Vehicle>(), null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }

  @override
  Future<(List<Vehicle>?, Failure?)> getDealerVehicles(String dealerId) async {
    try {
      final vehicles = await _remote.getDealerVehicles(dealerId);
      return (vehicles.cast<Vehicle>(), null);
    } catch (e) {
      return (null, ApiResponseHandler.handleError(e));
    }
  }
}
