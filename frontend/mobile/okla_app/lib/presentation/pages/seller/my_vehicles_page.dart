import 'package:flutter/material.dart';
import 'package:okla_app/core/constants/colors.dart';

class MyVehiclesPage extends StatelessWidget {
  const MyVehiclesPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Mis Vehículos'),
      ),
      body: const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.directions_car, size: 64, color: OklaColors.primary500),
            SizedBox(height: 16),
            Text('Mis Vehículos', style: TextStyle(fontSize: 24, fontWeight: FontWeight.w600)),
            SizedBox(height: 8),
            Text('Próximamente', style: TextStyle(color: OklaColors.neutral400)),
          ],
        ),
      ),
    );
  }
}
