import 'package:flutter/material.dart';
import 'package:okla_app/core/constants/colors.dart';

class ChatbotPage extends StatelessWidget {
  const ChatbotPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Asistente OKLA'),
      ),
      body: const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.smart_toy, size: 64, color: OklaColors.primary500),
            SizedBox(height: 16),
            Text('Asistente OKLA', style: TextStyle(fontSize: 24, fontWeight: FontWeight.w600)),
            SizedBox(height: 8),
            Text('Próximamente', style: TextStyle(color: OklaColors.neutral400)),
          ],
        ),
      ),
    );
  }
}
