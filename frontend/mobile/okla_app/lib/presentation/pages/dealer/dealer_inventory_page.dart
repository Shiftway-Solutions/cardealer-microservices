import 'package:flutter/material.dart';
import 'package:okla_app/core/constants/colors.dart';

class DealerInventoryPage extends StatelessWidget {
  const DealerInventoryPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Inventario'),
      ),
      body: const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.inventory, size: 64, color: OklaColors.primary500),
            SizedBox(height: 16),
            Text('Inventario', style: TextStyle(fontSize: 24, fontWeight: FontWeight.w600)),
            SizedBox(height: 8),
            Text('Próximamente', style: TextStyle(color: OklaColors.neutral400)),
          ],
        ),
      ),
    );
  }
}
