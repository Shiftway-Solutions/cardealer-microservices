import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:okla_app/domain/entities/user.dart';
import 'package:okla_app/domain/repositories/auth_repository.dart';

// ──── Events ────
abstract class AuthEvent {}

class AuthCheckRequested extends AuthEvent {}

class AuthLoginRequested extends AuthEvent {
  final String email;
  final String password;
  AuthLoginRequested({required this.email, required this.password});
}

class AuthRegisterRequested extends AuthEvent {
  final String email;
  final String password;
  final String firstName;
  final String lastName;
  AuthRegisterRequested({
    required this.email,
    required this.password,
    required this.firstName,
    required this.lastName,
  });
}

class AuthLogoutRequested extends AuthEvent {}

class Auth2FARequested extends AuthEvent {
  final String code;
  Auth2FARequested({required this.code});
}

class AuthForgotPasswordRequested extends AuthEvent {
  final String email;
  AuthForgotPasswordRequested({required this.email});
}

// ──── States ────
abstract class AuthState {}

class AuthInitial extends AuthState {}

class AuthLoading extends AuthState {}

class AuthAuthenticated extends AuthState {
  final User user;
  AuthAuthenticated({required this.user});
}

class AuthUnauthenticated extends AuthState {}

class Auth2FARequired extends AuthState {}

class AuthEmailVerificationRequired extends AuthState {
  final User user;
  AuthEmailVerificationRequired({required this.user});
}

class AuthError extends AuthState {
  final String message;
  AuthError({required this.message});
}

class AuthPasswordResetSent extends AuthState {
  final String email;
  AuthPasswordResetSent({required this.email});
}

// ──── BLoC ────
class AuthBloc extends Bloc<AuthEvent, AuthState> {
  final AuthRepository _repository;

  AuthBloc({required AuthRepository repository})
      : _repository = repository,
        super(AuthInitial()) {
    on<AuthCheckRequested>(_onCheckAuth);
    on<AuthLoginRequested>(_onLogin);
    on<AuthRegisterRequested>(_onRegister);
    on<AuthLogoutRequested>(_onLogout);
    on<Auth2FARequested>(_on2FA);
    on<AuthForgotPasswordRequested>(_onForgotPassword);
  }

  Future<void> _onCheckAuth(AuthCheckRequested event, Emitter<AuthState> emit) async {
    emit(AuthLoading());
    final isLoggedIn = await _repository.isLoggedIn();
    if (!isLoggedIn) {
      emit(AuthUnauthenticated());
      return;
    }
    final (user, failure) = await _repository.getCurrentUser();
    if (user != null) {
      if (!user.isEmailVerified) {
        emit(AuthEmailVerificationRequired(user: user));
      } else {
        emit(AuthAuthenticated(user: user));
      }
    } else {
      await _repository.clearTokens();
      emit(AuthUnauthenticated());
    }
  }

  Future<void> _onLogin(AuthLoginRequested event, Emitter<AuthState> emit) async {
    emit(AuthLoading());
    final (user, failure) = await _repository.login(
      email: event.email,
      password: event.password,
    );
    if (user != null) {
      if (user.isTwoFactorEnabled) {
        emit(Auth2FARequired());
      } else if (!user.isEmailVerified) {
        emit(AuthEmailVerificationRequired(user: user));
      } else {
        emit(AuthAuthenticated(user: user));
      }
    } else {
      emit(AuthError(message: failure?.message ?? 'Error al iniciar sesión'));
    }
  }

  Future<void> _onRegister(AuthRegisterRequested event, Emitter<AuthState> emit) async {
    emit(AuthLoading());
    final (user, failure) = await _repository.register(
      email: event.email,
      password: event.password,
      firstName: event.firstName,
      lastName: event.lastName,
    );
    if (user != null) {
      emit(AuthEmailVerificationRequired(user: user));
    } else {
      emit(AuthError(message: failure?.message ?? 'Error al registrarse'));
    }
  }

  Future<void> _onLogout(AuthLogoutRequested event, Emitter<AuthState> emit) async {
    await _repository.logout();
    emit(AuthUnauthenticated());
  }

  Future<void> _on2FA(Auth2FARequested event, Emitter<AuthState> emit) async {
    emit(AuthLoading());
    final (success, failure) = await _repository.verify2FA(event.code);
    if (success) {
      final (user, _) = await _repository.getCurrentUser();
      if (user != null) {
        emit(AuthAuthenticated(user: user));
      } else {
        emit(AuthError(message: 'Error al verificar 2FA'));
      }
    } else {
      emit(AuthError(message: failure?.message ?? 'Código 2FA inválido'));
    }
  }

  Future<void> _onForgotPassword(AuthForgotPasswordRequested event, Emitter<AuthState> emit) async {
    emit(AuthLoading());
    final (success, failure) = await _repository.forgotPassword(event.email);
    if (success) {
      emit(AuthPasswordResetSent(email: event.email));
    } else {
      emit(AuthError(message: failure?.message ?? 'Error al enviar correo'));
    }
  }
}
