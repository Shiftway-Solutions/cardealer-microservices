import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:okla_app/domain/entities/vehicle.dart';
import 'package:okla_app/domain/repositories/vehicle_repository.dart';

// ──── Events ────
abstract class VehiclesEvent {}

class VehiclesSearchRequested extends VehiclesEvent {
  final VehicleFilters filters;
  VehiclesSearchRequested({required this.filters});
}

class VehiclesLoadMore extends VehiclesEvent {}

class VehiclesFeaturedRequested extends VehiclesEvent {}

class VehicleDetailRequested extends VehiclesEvent {
  final String slugOrId;
  final bool isSlug;
  VehicleDetailRequested({required this.slugOrId, this.isSlug = true});
}

class VehicleSimilarRequested extends VehiclesEvent {
  final String vehicleId;
  VehicleSimilarRequested({required this.vehicleId});
}

// ──── States ────
abstract class VehiclesState {}

class VehiclesInitial extends VehiclesState {}

class VehiclesLoading extends VehiclesState {}

class VehiclesLoaded extends VehiclesState {
  final List<Vehicle> vehicles;
  final int totalCount;
  final bool hasMore;
  final VehicleFilters filters;

  VehiclesLoaded({
    required this.vehicles,
    required this.totalCount,
    required this.hasMore,
    required this.filters,
  });
}

class VehiclesLoadingMore extends VehiclesState {
  final List<Vehicle> currentVehicles;
  final VehicleFilters filters;

  VehiclesLoadingMore({required this.currentVehicles, required this.filters});
}

class VehicleFeaturedLoaded extends VehiclesState {
  final List<Vehicle> vehicles;
  VehicleFeaturedLoaded({required this.vehicles});
}

class VehicleDetailLoaded extends VehiclesState {
  final Vehicle vehicle;
  final List<Vehicle> similarVehicles;
  VehicleDetailLoaded({required this.vehicle, this.similarVehicles = const []});
}

class VehiclesError extends VehiclesState {
  final String message;
  VehiclesError({required this.message});
}

// ──── BLoC ────
class VehiclesBloc extends Bloc<VehiclesEvent, VehiclesState> {
  final VehicleRepository _repository;
  VehicleFilters _currentFilters = const VehicleFilters();
  List<Vehicle> _currentVehicles = [];
  int _totalCount = 0;
  bool _hasMore = false;

  VehiclesBloc({required VehicleRepository repository})
      : _repository = repository,
        super(VehiclesInitial()) {
    on<VehiclesSearchRequested>(_onSearch);
    on<VehiclesLoadMore>(_onLoadMore);
    on<VehiclesFeaturedRequested>(_onFeatured);
    on<VehicleDetailRequested>(_onDetail);
    on<VehicleSimilarRequested>(_onSimilar);
  }

  Future<void> _onSearch(VehiclesSearchRequested event, Emitter<VehiclesState> emit) async {
    _currentFilters = event.filters;
    emit(VehiclesLoading());

    final (result, failure) = await _repository.searchVehicles(event.filters);
    if (result != null) {
      _currentVehicles = result.items;
      _totalCount = result.totalCount;
      _hasMore = result.hasMore;
      emit(VehiclesLoaded(
        vehicles: _currentVehicles,
        totalCount: _totalCount,
        hasMore: _hasMore,
        filters: _currentFilters,
      ));
    } else {
      emit(VehiclesError(message: failure?.message ?? 'Error al buscar vehículos'));
    }
  }

  Future<void> _onLoadMore(VehiclesLoadMore event, Emitter<VehiclesState> emit) async {
    if (!_hasMore) return;

    emit(VehiclesLoadingMore(currentVehicles: _currentVehicles, filters: _currentFilters));

    final nextFilters = _currentFilters.copyWith(page: _currentFilters.page + 1);
    final (result, failure) = await _repository.searchVehicles(nextFilters);

    if (result != null) {
      _currentFilters = nextFilters;
      _currentVehicles = [..._currentVehicles, ...result.items];
      _hasMore = result.hasMore;
      emit(VehiclesLoaded(
        vehicles: _currentVehicles,
        totalCount: result.totalCount,
        hasMore: _hasMore,
        filters: _currentFilters,
      ));
    } else {
      emit(VehiclesLoaded(
        vehicles: _currentVehicles,
        totalCount: _totalCount,
        hasMore: _hasMore,
        filters: _currentFilters,
      ));
    }
  }

  Future<void> _onFeatured(VehiclesFeaturedRequested event, Emitter<VehiclesState> emit) async {
    emit(VehiclesLoading());
    final (vehicles, failure) = await _repository.getFeaturedVehicles();
    if (vehicles != null) {
      emit(VehicleFeaturedLoaded(vehicles: vehicles));
    } else {
      emit(VehiclesError(message: failure?.message ?? 'Error al cargar destacados'));
    }
  }

  Future<void> _onDetail(VehicleDetailRequested event, Emitter<VehiclesState> emit) async {
    emit(VehiclesLoading());
    final result = event.isSlug
        ? await _repository.getVehicleBySlug(event.slugOrId)
        : await _repository.getVehicleById(event.slugOrId);

    final (vehicle, failure) = result;
    if (vehicle != null) {
      emit(VehicleDetailLoaded(vehicle: vehicle));
      // Track view
      _repository.trackView(vehicle.id);
    } else {
      emit(VehiclesError(message: failure?.message ?? 'Vehículo no encontrado'));
    }
  }

  Future<void> _onSimilar(VehicleSimilarRequested event, Emitter<VehiclesState> emit) async {
    if (state is VehicleDetailLoaded) {
      final currentState = state as VehicleDetailLoaded;
      final (similar, _) = await _repository.getSimilarVehicles(event.vehicleId);
      emit(VehicleDetailLoaded(
        vehicle: currentState.vehicle,
        similarVehicles: similar ?? [],
      ));
    }
  }
}
