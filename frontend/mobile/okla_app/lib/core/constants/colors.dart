import 'package:flutter/material.dart';

/// OKLA Brand Colors — Verde Esmeralda
class OklaColors {
  OklaColors._();

  // Primary — Verde Esmeralda OKLA
  static const Color primary50 = Color(0xFFE6F7F0);
  static const Color primary100 = Color(0xFFB3E8D4);
  static const Color primary200 = Color(0xFF80D9B8);
  static const Color primary300 = Color(0xFF4DCA9C);
  static const Color primary400 = Color(0xFF26BE85);
  static const Color primary500 = Color(0xFF00A870); // Main brand
  static const Color primary600 = Color(0xFF009663);
  static const Color primary700 = Color(0xFF008456);
  static const Color primary800 = Color(0xFF006B46);
  static const Color primary900 = Color(0xFF005236);

  // Secondary — Azul Marino
  static const Color secondary50 = Color(0xFFE8EFF5);
  static const Color secondary100 = Color(0xFFB8CCE0);
  static const Color secondary200 = Color(0xFF88AACB);
  static const Color secondary300 = Color(0xFF5887B6);
  static const Color secondary400 = Color(0xFF3B73A4);
  static const Color secondary500 = Color(0xFF3B73A4);
  static const Color secondary600 = Color(0xFF2D5A82);
  static const Color secondary700 = Color(0xFF1F4161);
  static const Color secondary800 = Color(0xFF152D44);
  static const Color secondary900 = Color(0xFF0B1720);

  // Semantic
  static const Color success = Color(0xFF10B981);
  static const Color warning = Color(0xFFF59E0B);
  static const Color error = Color(0xFFEF4444);
  static const Color info = Color(0xFF3B82F6);

  // Deal Rating
  static const Color dealGreat = Color(0xFF00A870);
  static const Color dealGood = Color(0xFF22C55E);
  static const Color dealFair = Color(0xFFF59E0B);
  static const Color dealHigh = Color(0xFFEF4444);
  static const Color dealUncertain = Color(0xFF9CA3AF);

  // Neutral
  static const Color neutral50 = Color(0xFFF9FAFB);
  static const Color neutral100 = Color(0xFFF3F4F6);
  static const Color neutral200 = Color(0xFFE5E7EB);
  static const Color neutral300 = Color(0xFFD1D5DB);
  static const Color neutral400 = Color(0xFF9CA3AF);
  static const Color neutral500 = Color(0xFF6B7280);
  static const Color neutral600 = Color(0xFF4B5563);
  static const Color neutral700 = Color(0xFF374151);
  static const Color neutral800 = Color(0xFF1F2937);
  static const Color neutral900 = Color(0xFF111827);

  // Background
  static const Color backgroundLight = Color(0xFFFFFFFF);
  static const Color backgroundSecondaryLight = Color(0xFFF9FAFB);
  static const Color backgroundDark = Color(0xFF0F172A);
  static const Color backgroundSecondaryDark = Color(0xFF1E293B);

  // Surface
  static const Color surfaceLight = Colors.white;
  static const Color surfaceDark = Color(0xFF1E293B);

  // Text
  static const Color textPrimaryLight = Color(0xFF111827);
  static const Color textSecondaryLight = Color(0xFF4B5563);
  static const Color textTertiaryLight = Color(0xFF9CA3AF);
  static const Color textPrimaryDark = Color(0xFFF1F5F9);
  static const Color textSecondaryDark = Color(0xFFCBD5E1);
  static const Color textTertiaryDark = Color(0xFF94A3B8);

  // Material Swatch
  static const MaterialColor primarySwatch = MaterialColor(
    0xFF00A870,
    <int, Color>{
      50: primary50,
      100: primary100,
      200: primary200,
      300: primary300,
      400: primary400,
      500: primary500,
      600: primary600,
      700: primary700,
      800: primary800,
      900: primary900,
    },
  );
}
