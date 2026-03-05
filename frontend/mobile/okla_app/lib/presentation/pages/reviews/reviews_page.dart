import 'package:flutter/material.dart';
import 'package:okla_app/core/constants/colors.dart';

class ReviewsPage extends StatelessWidget {
  final String targetId;
  const ReviewsPage({super.key, required this.targetId});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Reseñas')),
      body: const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.star, size: 64, color: OklaColors.primary500),
            SizedBox(height: 16),
            Text('Reseñas', style: TextStyle(fontSize: 24, fontWeight: FontWeight.w600)),
          ],
        ),
      ),
    );
  }
}
