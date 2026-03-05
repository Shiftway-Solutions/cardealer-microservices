import 'package:flutter/material.dart';
import 'package:okla_app/core/constants/colors.dart';

/// OKLA App Theme — Verde Esmeralda brand identity
class OklaTheme {
  OklaTheme._();

  // ──── Light Theme ────
  static ThemeData get light {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.light,
      colorScheme: const ColorScheme.light(
        primary: OklaColors.primary500,
        onPrimary: Colors.white,
        primaryContainer: OklaColors.primary50,
        onPrimaryContainer: OklaColors.primary900,
        secondary: OklaColors.secondary500,
        onSecondary: Colors.white,
        secondaryContainer: OklaColors.secondary50,
        onSecondaryContainer: OklaColors.secondary900,
        surface: OklaColors.surfaceLight,
        onSurface: OklaColors.textPrimaryLight,
        error: OklaColors.error,
        onError: Colors.white,
        outline: OklaColors.neutral300,
        surfaceContainerHighest: OklaColors.neutral100,
      ),
      scaffoldBackgroundColor: OklaColors.backgroundLight,
      fontFamily: 'Inter',
      textTheme: _textTheme(Brightness.light),
      appBarTheme: const AppBarTheme(
        backgroundColor: Colors.white,
        foregroundColor: OklaColors.textPrimaryLight,
        elevation: 0,
        centerTitle: false,
        scrolledUnderElevation: 1,
      ),
      cardTheme: CardThemeData(
        color: Colors.white,
        elevation: 1,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        clipBehavior: Clip.antiAlias,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: OklaColors.primary500,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          textStyle: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w600,
            fontFamily: 'Inter',
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: OklaColors.primary500,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          side: const BorderSide(color: OklaColors.primary500),
          textStyle: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w600,
            fontFamily: 'Inter',
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: OklaColors.primary500,
          textStyle: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w600,
            fontFamily: 'Inter',
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: OklaColors.neutral50,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: OklaColors.neutral200),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: OklaColors.neutral200),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: OklaColors.primary500, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: OklaColors.error),
        ),
        hintStyle: const TextStyle(color: OklaColors.neutral400, fontSize: 14),
        labelStyle: const TextStyle(color: OklaColors.neutral600, fontSize: 14),
      ),
      chipTheme: ChipThemeData(
        backgroundColor: OklaColors.neutral100,
        selectedColor: OklaColors.primary50,
        labelStyle: const TextStyle(fontSize: 13),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        side: BorderSide.none,
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      ),
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: Colors.white,
        selectedItemColor: OklaColors.primary500,
        unselectedItemColor: OklaColors.neutral400,
        type: BottomNavigationBarType.fixed,
        elevation: 8,
        selectedLabelStyle: TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
        unselectedLabelStyle: TextStyle(fontSize: 12),
      ),
      tabBarTheme: const TabBarThemeData(
        labelColor: OklaColors.primary500,
        unselectedLabelColor: OklaColors.neutral500,
        indicatorColor: OklaColors.primary500,
        labelStyle: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
        unselectedLabelStyle: TextStyle(fontSize: 14),
      ),
      dividerTheme: const DividerThemeData(
        color: OklaColors.neutral200,
        thickness: 1,
        space: 0,
      ),
      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: Colors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
      dialogTheme: DialogThemeData(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
    );
  }

  // ──── Dark Theme ────
  static ThemeData get dark {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      colorScheme: const ColorScheme.dark(
        primary: OklaColors.primary400,
        onPrimary: OklaColors.primary900,
        primaryContainer: OklaColors.primary900,
        onPrimaryContainer: OklaColors.primary100,
        secondary: OklaColors.secondary400,
        onSecondary: OklaColors.secondary900,
        secondaryContainer: OklaColors.secondary900,
        onSecondaryContainer: OklaColors.secondary100,
        surface: OklaColors.surfaceDark,
        onSurface: OklaColors.textPrimaryDark,
        error: Color(0xFFF87171),
        onError: Color(0xFF7F1D1D),
        outline: Color(0xFF475569),
        surfaceContainerHighest: Color(0xFF334155),
      ),
      scaffoldBackgroundColor: OklaColors.backgroundDark,
      fontFamily: 'Inter',
      textTheme: _textTheme(Brightness.dark),
      appBarTheme: const AppBarTheme(
        backgroundColor: OklaColors.backgroundDark,
        foregroundColor: OklaColors.textPrimaryDark,
        elevation: 0,
        centerTitle: false,
        scrolledUnderElevation: 1,
      ),
      cardTheme: CardThemeData(
        color: OklaColors.surfaceDark,
        elevation: 1,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        clipBehavior: Clip.antiAlias,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: OklaColors.primary500,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: OklaColors.primary400,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          side: const BorderSide(color: OklaColors.primary400),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: const Color(0xFF1E293B),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: Color(0xFF475569)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: Color(0xFF475569)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: OklaColors.primary400, width: 2),
        ),
        hintStyle: const TextStyle(color: Color(0xFF94A3B8), fontSize: 14),
      ),
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: OklaColors.backgroundDark,
        selectedItemColor: OklaColors.primary400,
        unselectedItemColor: Color(0xFF64748B),
        type: BottomNavigationBarType.fixed,
        elevation: 8,
      ),
      dividerTheme: const DividerThemeData(
        color: Color(0xFF334155),
        thickness: 1,
        space: 0,
      ),
      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: OklaColors.surfaceDark,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: OklaColors.surfaceDark,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
    );
  }

  // ──── Text Theme ────
  static TextTheme _textTheme(Brightness brightness) {
    final color = brightness == Brightness.light
        ? OklaColors.textPrimaryLight
        : OklaColors.textPrimaryDark;

    return TextTheme(
      displayLarge: TextStyle(fontSize: 48, fontWeight: FontWeight.w700, color: color, height: 1.1),
      displayMedium: TextStyle(fontSize: 36, fontWeight: FontWeight.w700, color: color, height: 1.2),
      displaySmall: TextStyle(fontSize: 30, fontWeight: FontWeight.w600, color: color, height: 1.2),
      headlineLarge: TextStyle(fontSize: 24, fontWeight: FontWeight.w700, color: color, height: 1.3),
      headlineMedium: TextStyle(fontSize: 20, fontWeight: FontWeight.w600, color: color, height: 1.3),
      headlineSmall: TextStyle(fontSize: 18, fontWeight: FontWeight.w600, color: color, height: 1.4),
      titleLarge: TextStyle(fontSize: 18, fontWeight: FontWeight.w600, color: color, height: 1.4),
      titleMedium: TextStyle(fontSize: 16, fontWeight: FontWeight.w500, color: color, height: 1.4),
      titleSmall: TextStyle(fontSize: 14, fontWeight: FontWeight.w500, color: color, height: 1.4),
      bodyLarge: TextStyle(fontSize: 16, fontWeight: FontWeight.w400, color: color, height: 1.5),
      bodyMedium: TextStyle(fontSize: 14, fontWeight: FontWeight.w400, color: color, height: 1.5),
      bodySmall: TextStyle(fontSize: 12, fontWeight: FontWeight.w400, color: color, height: 1.5),
      labelLarge: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: color),
      labelMedium: TextStyle(fontSize: 12, fontWeight: FontWeight.w500, color: color),
      labelSmall: TextStyle(fontSize: 11, fontWeight: FontWeight.w500, color: color),
    );
  }
}
