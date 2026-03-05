import 'package:okla_app/core/network/api_client.dart';
import 'package:okla_app/core/network/api_response_handler.dart';
import 'package:okla_app/data/models/vehicle_model.dart';
import 'package:okla_app/domain/entities/vehicle.dart';

/// Remote data source for vehicle operations
class VehicleRemoteDataSource {
  final ApiClient _client;

  VehicleRemoteDataSource({required ApiClient client}) : _client = client;

  Future<PaginatedResponse<VehicleModel>> searchVehicles(VehicleFilters filters) async {
    final response = await _client.get(
      '/vehicles/search',
      queryParameters: filters.toQueryParameters(),
    );
    return ApiResponseHandler.handlePaginatedResponse<VehicleModel>(
      response,
      VehicleModel.fromJson,
    );
  }

  Future<VehicleModel> getVehicleBySlug(String slug) async {
    final response = await _client.get('/vehicles/slug/$slug');
    return ApiResponseHandler.handleResponse<VehicleModel>(
      response,
      (json) => VehicleModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<VehicleModel> getVehicleById(String id) async {
    final response = await _client.get('/vehicles/$id');
    return ApiResponseHandler.handleResponse<VehicleModel>(
      response,
      (json) => VehicleModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<List<VehicleModel>> getFeaturedVehicles({int limit = 10}) async {
    final response = await _client.get(
      '/vehicles/featured',
      queryParameters: {'limit': limit},
    );
    return ApiResponseHandler.handleListResponse<VehicleModel>(
      response,
      VehicleModel.fromJson,
    );
  }

  Future<List<VehicleModel>> getSimilarVehicles(String vehicleId, {int limit = 6}) async {
    final response = await _client.get(
      '/vehicles/$vehicleId/similar',
      queryParameters: {'limit': limit},
    );
    return ApiResponseHandler.handleListResponse<VehicleModel>(
      response,
      VehicleModel.fromJson,
    );
  }

  Future<void> trackView(String vehicleId) async {
    await _client.post('/vehicles/$vehicleId/view');
  }

  Future<VehicleModel> createVehicle(Map<String, dynamic> data) async {
    final response = await _client.post('/vehicles', data: data);
    return ApiResponseHandler.handleResponse<VehicleModel>(
      response,
      (json) => VehicleModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<VehicleModel> updateVehicle(String id, Map<String, dynamic> data) async {
    final response = await _client.put('/vehicles/$id', data: data);
    return ApiResponseHandler.handleResponse<VehicleModel>(
      response,
      (json) => VehicleModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<List<VehicleModel>> getMyVehicles() async {
    final response = await _client.get('/vehicles/my');
    return ApiResponseHandler.handleListResponse<VehicleModel>(
      response,
      VehicleModel.fromJson,
    );
  }

  Future<List<VehicleModel>> getDealerVehicles(String dealerId) async {
    final response = await _client.get('/vehicles/dealer/$dealerId');
    return ApiResponseHandler.handleListResponse<VehicleModel>(
      response,
      VehicleModel.fromJson,
    );
  }
}
