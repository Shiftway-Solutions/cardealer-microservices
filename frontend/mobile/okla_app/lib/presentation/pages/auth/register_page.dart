import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:okla_app/core/constants/colors.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;
  bool _acceptTerms = false;

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Crear cuenta', style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 8),
                Text(
                  'Regístrate gratis y encuentra tu vehículo ideal',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: OklaColors.neutral500),
                ),
                const SizedBox(height: 32),
                Row(
                  children: [
                    Expanded(
                      child: TextFormField(
                        controller: _firstNameController,
                        decoration: const InputDecoration(
                          labelText: 'Nombre',
                          prefixIcon: Icon(Icons.person_outline),
                        ),
                        validator: (v) => v?.isEmpty == true ? 'Requerido' : null,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: TextFormField(
                        controller: _lastNameController,
                        decoration: const InputDecoration(
                          labelText: 'Apellido',
                          prefixIcon: Icon(Icons.person_outline),
                        ),
                        validator: (v) => v?.isEmpty == true ? 'Requerido' : null,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _emailController,
                  keyboardType: TextInputType.emailAddress,
                  decoration: const InputDecoration(
                    labelText: 'Correo electrónico',
                    prefixIcon: Icon(Icons.email_outlined),
                  ),
                  validator: (v) {
                    if (v?.isEmpty == true) return 'Requerido';
                    if (!RegExp(r'^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$').hasMatch(v!)) return 'Email inválido';
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _passwordController,
                  obscureText: _obscurePassword,
                  decoration: InputDecoration(
                    labelText: 'Contraseña',
                    prefixIcon: const Icon(Icons.lock_outline),
                    suffixIcon: IconButton(
                      icon: Icon(_obscurePassword ? Icons.visibility_off : Icons.visibility),
                      onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                    ),
                    helperText: 'Mínimo 8 caracteres, 1 mayúscula, 1 número, 1 especial',
                  ),
                  validator: (v) {
                    if (v?.isEmpty == true) return 'Requerido';
                    if (v!.length < 8) return 'Mínimo 8 caracteres';
                    if (!RegExp(r'[A-Z]').hasMatch(v)) return 'Incluye una mayúscula';
                    if (!RegExp(r'[0-9]').hasMatch(v)) return 'Incluye un número';
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                CheckboxListTile(
                  value: _acceptTerms,
                  onChanged: (v) => setState(() => _acceptTerms = v ?? false),
                  title: Text.rich(
                    TextSpan(
                      text: 'Acepto los ',
                      style: Theme.of(context).textTheme.bodySmall,
                      children: const [
                        TextSpan(
                          text: 'Términos de Servicio',
                          style: TextStyle(color: OklaColors.primary500, fontWeight: FontWeight.w600),
                        ),
                        TextSpan(text: ' y '),
                        TextSpan(
                          text: 'Política de Privacidad',
                          style: TextStyle(color: OklaColors.primary500, fontWeight: FontWeight.w600),
                        ),
                      ],
                    ),
                  ),
                  controlAffinity: ListTileControlAffinity.leading,
                  contentPadding: EdgeInsets.zero,
                ),
                const SizedBox(height: 24),
                SizedBox(
                  height: 52,
                  child: ElevatedButton(
                    onPressed: _acceptTerms ? () {
                      if (_formKey.currentState!.validate()) {
                        // Submit registration
                      }
                    } : null,
                    child: const Text('Crear cuenta'),
                  ),
                ),
                const SizedBox(height: 24),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text('¿Ya tienes cuenta? ', style: Theme.of(context).textTheme.bodyMedium),
                    TextButton(
                      onPressed: () => context.go('/login'),
                      child: const Text('Inicia sesión'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
