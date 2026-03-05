# 📊 Benchmarks de Mercado

> **Tiempo estimado:** 20 minutos  
> **Página:** DealerMarketBenchmarksPage

---

## 📋 OBJETIVO

Comparar precios del dealer vs mercado:

- Análisis de competitividad
- Sugerencias de precio
- Tendencias del mercado

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ ANÁLISIS DE MERCADO                                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ TU POSICIÓN EN EL MERCADO                                       │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │  Más bajo    ◀━━━━━━━━━●━━━━━━━━━▶    Más alto             │ │
│ │              Tus precios: 5% bajo el mercado                │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ COMPARACIÓN POR VEHÍCULO                                        │
│ ┌────────────────────────┬───────────┬───────────┬────────────┐ │
│ │ Vehículo               │ Tu Precio │ Mercado   │ Diferencia │ │
│ ├────────────────────────┼───────────┼───────────┼────────────┤ │
│ │ Toyota Camry 2024      │ $1.85M    │ $1.92M    │ -3.6% ✅   │ │
│ │ Honda CR-V 2023        │ $2.10M    │ $1.95M    │ +7.7% ⚠️   │ │
│ │ Hyundai Tucson 2024    │ $1.75M    │ $1.78M    │ -1.7% ✅   │ │
│ └────────────────────────┴───────────┴───────────┴────────────┘ │
│                                                                 │
│ TENDENCIAS DE PRECIOS (Últimos 6 meses)                         │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 📈 [Gráfico de líneas - Tu precio vs Mercado]               │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ 💡 SUGERENCIAS                                                  │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ • Honda CR-V 2023: Considera reducir $150,000 (7.7% alto)   │ │
│ │ • Tu Toyota Camry tiene 450 vistas - precio competitivo     │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(dealer)/dealer/market/page.tsx
'use client';

import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { dealerService } from '@/services/api/dealerService';
import { formatCurrency } from '@/lib/format';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import { TrendingUp, TrendingDown, Minus, Lightbulb, RefreshCw } from 'lucide-react';

export default function DealerMarketBenchmarksPage() {
  const { data: benchmark, isLoading, refetch } = useQuery({
    queryKey: ['dealer-market-benchmark'],
    queryFn: () => dealerService.getMarketBenchmark(),
  });

  const { data: trends } = useQuery({
    queryKey: ['dealer-price-trends'],
    queryFn: () => dealerService.getPriceTrends(),
  });

  if (isLoading) {
    return <div className="p-8 text-center">Cargando análisis...</div>;
  }

  const position = benchmark?.marketPosition || 0; // -100 to +100

  return (
    <div className="container max-w-5xl mx-auto py-8 px-4">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Análisis de Mercado</h1>
        <Button variant="outline" onClick={() => refetch()}>
          <RefreshCw className="w-4 h-4 mr-2" />
          Actualizar
        </Button>
      </div>

      {/* Market Position */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Tu Posición en el Mercado</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="relative pt-4 pb-8">
            {/* Scale */}
            <div className="flex justify-between text-sm text-gray-600 mb-2">
              <span>Más bajo</span>
              <span>Promedio</span>
              <span>Más alto</span>
            </div>

            {/* Bar */}
            <div className="h-3 bg-gradient-to-r from-green-400 via-yellow-400 to-red-400 rounded-full relative">
              {/* Marker */}
              <div
                className="absolute w-4 h-4 bg-white border-2 border-gray-800 rounded-full -top-0.5 transform -translate-x-1/2"
                style={{ left: `${50 + position / 2}%` }}
              />
            </div>

            {/* Label */}
            <p className="text-center mt-4 font-medium">
              {position < 0 ? (
                <span className="text-green-600">
                  Tus precios: {Math.abs(position)}% bajo el mercado
                </span>
              ) : position > 0 ? (
                <span className="text-red-600">
                  Tus precios: {position}% sobre el mercado
                </span>
              ) : (
                <span className="text-yellow-600">
                  Tus precios están en el promedio del mercado
                </span>
              )}
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Vehicle Comparison */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Comparación por Vehículo</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b text-left">
                  <th className="pb-3 font-medium">Vehículo</th>
                  <th className="pb-3 font-medium text-right">Tu Precio</th>
                  <th className="pb-3 font-medium text-right">Mercado</th>
                  <th className="pb-3 font-medium text-right">Diferencia</th>
                  <th className="pb-3 font-medium text-center">Estado</th>
                </tr>
              </thead>
              <tbody>
                {benchmark?.vehicles?.map((v: any) => {
                  const diff = ((v.yourPrice - v.marketPrice) / v.marketPrice) * 100;
                  return (
                    <tr key={v.id} className="border-b">
                      <td className="py-3">
                        <div className="font-medium">{v.title}</div>
                        <div className="text-sm text-gray-600">{v.year} • {v.mileage?.toLocaleString()} km</div>
                      </td>
                      <td className="py-3 text-right font-medium">
                        {formatCurrency(v.yourPrice)}
                      </td>
                      <td className="py-3 text-right text-gray-600">
                        {formatCurrency(v.marketPrice)}
                      </td>
                      <td className="py-3 text-right">
                        <span className={diff > 5 ? 'text-red-600' : diff < -5 ? 'text-green-600' : 'text-gray-600'}>
                          {diff > 0 ? '+' : ''}{diff.toFixed(1)}%
                        </span>
                      </td>
                      <td className="py-3 text-center">
                        {diff > 5 ? (
                          <Badge variant="destructive">Alto</Badge>
                        ) : diff < -5 ? (
                          <Badge variant="success">Competitivo</Badge>
                        ) : (
                          <Badge variant="secondary">OK</Badge>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      {/* Price Trends Chart */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Tendencias de Precios (6 meses)</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={trends}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="month" />
                <YAxis tickFormatter={(v) => `$${(v/1000000).toFixed(1)}M`} />
                <Tooltip formatter={(v) => formatCurrency(v as number)} />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="yourAvg"
                  stroke="#3b82f6"
                  name="Tu promedio"
                  strokeWidth={2}
                />
                <Line
                  type="monotone"
                  dataKey="marketAvg"
                  stroke="#9ca3af"
                  name="Mercado"
                  strokeWidth={2}
                  strokeDasharray="5 5"
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </CardContent>
      </Card>

      {/* Suggestions */}
      {benchmark?.suggestions?.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Lightbulb className="w-5 h-5 text-yellow-500" />
              Sugerencias
            </CardTitle>
          </CardHeader>
          <CardContent>
            <ul className="space-y-3">
              {benchmark.suggestions.map((s: any, i: number) => (
                <li key={i} className="flex items-start gap-3 p-3 bg-yellow-50 rounded-lg">
                  <span className="text-yellow-600">•</span>
                  <div>
                    <p className="font-medium">{s.vehicle}</p>
                    <p className="text-sm text-gray-600">{s.recommendation}</p>
                  </div>
                  {s.action && (
                    <Button size="sm" variant="outline" className="ml-auto">
                      {s.action}
                    </Button>
                  )}
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método | Endpoint                          | Descripción                     |
| ------ | --------------------------------- | ------------------------------- |
| `GET`  | `/api/dealers/market/benchmark`   | Análisis de posición vs mercado |
| `GET`  | `/api/dealers/market/trends`      | Tendencias de precios           |
| `GET`  | `/api/dealers/market/suggestions` | Sugerencias de pricing          |

---

## ✅ CHECKLIST

- [ ] Indicador visual de posición de mercado
- [ ] Tabla comparativa por vehículo
- [ ] Gráfico de tendencias
- [ ] Sugerencias automáticas
- [ ] Badges de estado (Alto/OK/Competitivo)

---

_Última actualización: Enero 31, 2026_
