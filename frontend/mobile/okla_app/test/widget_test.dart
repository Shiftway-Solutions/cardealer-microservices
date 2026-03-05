// Basic Flutter widget smoke test for OKLA app.

import 'package:flutter_test/flutter_test.dart';

import 'package:okla_app/main.dart';

void main() {
  testWidgets('App smoke test - OklaApp builds', (WidgetTester tester) async {
    // Verify the app widget can be instantiated.
    expect(const OklaApp(), isA<OklaApp>());
  });
}
