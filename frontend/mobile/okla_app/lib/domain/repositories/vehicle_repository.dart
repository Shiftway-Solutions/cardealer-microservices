import 'package:okla_app/core/errors/failures.dart';
import 'package:okla_app/core/network/api_response_handler.dart';
import 'package:okla_app/domain/entities/vehicle.dart';

/// Vehicle repository interface
abstract class VehicleRepository {
  Future<(PaginatedResponse<Vehicle>?, Failure?)> searchVehicles(VehicleFilters filters);
  Future<(Vehicle?, Failure?)> getVehicleBySlug(String slug);
  Future<(Vehicle?, Failure?)> getVehicleById(String id);
  Future<(List<Vehicle>?, Failure?)> getFeaturedVehicles({int limit = 10});
  Future<(List<Vehicle>?, Failure?)> getSimilarVehicles(String vehicleId, {int limit = 6});
  Future<(bool, Failure?)> trackView(String vehicleId);
  Future<(Vehicle?, Failure?)> createVehicle(Map<String, dynamic> vehicleData);
  Future<(Vehicle?, Failure?)> updateVehicle(String id, Map<String, dynamic> vehicleData);
  Future<(bool, Failure?)> publishVehicle(String id);
  Future<(bool, Failure?)> unpublishVehicle(String id);
  Future<(bool, Failure?)> markAsSold(String id);
  Future<(List<Vehicle>?, Failure?)> getMyVehicles();
  Future<(List<Vehicle>?, Failure?)> getDealerVehicles(String dealerId);
}
