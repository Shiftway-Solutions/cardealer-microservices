# 🤖 Admin ML/AI Tools

> **Tiempo estimado:** 25 minutos  
> **Página:** AdminMLDashboardPage

---

## 📋 OBJETIVO

Panel de herramientas ML/IA para administradores:

- Detección de fraude
- Pricing automático
- Análisis de imágenes

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ ML/AI TOOLS                                      [Refrescar]    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ MODELOS ACTIVOS                                                 │
│ ┌────────────────┐ ┌────────────────┐ ┌────────────────┐        │
│ │ 🔍 Fraud       │ │ 💰 Pricing     │ │ 📷 Image       │        │
│ │ Detection     │ │ Prediction    │ │ Analysis      │        │
│ │ v2.3 ✅ Active│ │ v1.8 ✅ Active│ │ v3.1 ✅ Active│        │
│ │ 99.2% acc     │ │ 94.5% acc     │ │ 97.8% acc     │        │
│ └────────────────┘ └────────────────┘ └────────────────┘        │
│                                                                 │
│ ALERTAS DE FRAUDE (últimas 24h)                                 │
│ ┌───┬────────────────────────┬──────────┬─────────┬───────────┐ │
│ │   │ Vehículo               │ Score    │ Razón   │ Acción    │ │
│ │ ⚠️│ BMW X5 2024 - $800K   │ 0.92     │ Precio  │ [Revisar] │ │
│ │ ⚠️│ Mercedes C300 - $500K │ 0.87     │ Imágenes│ [Revisar] │ │
│ └───┴────────────────────────┴──────────┴─────────┴───────────┘ │
│                                                                 │
│ SUGERENCIAS DE PRECIO                                           │
│ ┌────────────────────────┬───────────┬───────────┬────────────┐ │
│ │ Vehículo               │ Actual    │ Sugerido  │ Diff       │ │
│ │ Toyota Camry 2024      │ $1.85M    │ $1.92M    │ +$70K      │ │
│ │ Honda Civic 2023       │ $1.20M    │ $1.15M    │ -$50K      │ │
│ └────────────────────────┴───────────┴───────────┴────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(admin)/admin/ml/page.tsx
'use client';

import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { adminService } from '@/services/api/adminService';
import { formatCurrency } from '@/lib/format';
import { Brain, AlertTriangle, DollarSign, Image, RefreshCw, Eye } from 'lucide-react';
import Link from 'next/link';

const models = [
  { id: 'fraud', name: 'Fraud Detection', version: 'v2.3', accuracy: 99.2, icon: AlertTriangle },
  { id: 'pricing', name: 'Pricing Prediction', version: 'v1.8', accuracy: 94.5, icon: DollarSign },
  { id: 'image', name: 'Image Analysis', version: 'v3.1', accuracy: 97.8, icon: Image },
];

export default function AdminMLDashboardPage() {
  const { data: fraudAlerts } = useQuery({
    queryKey: ['admin-fraud-alerts'],
    queryFn: () => adminService.getFraudAlerts(),
  });

  const { data: priceSuggestions } = useQuery({
    queryKey: ['admin-price-suggestions'],
    queryFn: () => adminService.getPriceSuggestions(),
  });

  return (
    <div className="container max-w-6xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold flex items-center gap-2">
          <Brain className="w-6 h-6" />
          ML/AI Tools
        </h1>
        <Button variant="outline">
          <RefreshCw className="w-4 h-4 mr-2" />
          Refrescar
        </Button>
      </div>

      {/* Active Models */}
      <div className="grid md:grid-cols-3 gap-4 mb-8">
        {models.map((model) => (
          <Card key={model.id}>
            <CardContent className="pt-6">
              <div className="flex items-center gap-3 mb-3">
                <div className="w-10 h-10 rounded-full bg-primary-100 flex items-center justify-center">
                  <model.icon className="w-5 h-5 text-primary-600" />
                </div>
                <div>
                  <div className="font-semibold">{model.name}</div>
                  <div className="text-sm text-gray-600">{model.version}</div>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <Badge variant="success">✅ Activo</Badge>
                <span className="text-sm text-gray-600">{model.accuracy}% precisión</span>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Fraud Alerts */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <AlertTriangle className="w-5 h-5 text-red-500" />
            Alertas de Fraude (24h)
          </CardTitle>
        </CardHeader>
        <CardContent>
          <table className="w-full">
            <thead>
              <tr className="border-b text-left">
                <th className="pb-3">Vehículo</th>
                <th className="pb-3">Score</th>
                <th className="pb-3">Razón</th>
                <th className="pb-3">Acción</th>
              </tr>
            </thead>
            <tbody>
              {fraudAlerts?.map((alert: any) => (
                <tr key={alert.id} className="border-b">
                  <td className="py-3">
                    <div className="font-medium">{alert.vehicleTitle}</div>
                    <div className="text-sm text-gray-600">{formatCurrency(alert.price)}</div>
                  </td>
                  <td className="py-3">
                    <Badge variant={alert.score > 0.9 ? 'destructive' : 'warning'}>
                      {alert.score.toFixed(2)}
                    </Badge>
                  </td>
                  <td className="py-3 text-gray-600">{alert.reason}</td>
                  <td className="py-3">
                    <Button size="sm" variant="outline" asChild>
                      <Link href={`/admin/vehicles/${alert.vehicleId}`}>
                        <Eye className="w-4 h-4 mr-1" />
                        Revisar
                      </Link>
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>

      {/* Price Suggestions */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <DollarSign className="w-5 h-5 text-green-500" />
            Sugerencias de Precio
          </CardTitle>
        </CardHeader>
        <CardContent>
          <table className="w-full">
            <thead>
              <tr className="border-b text-left">
                <th className="pb-3">Vehículo</th>
                <th className="pb-3 text-right">Precio Actual</th>
                <th className="pb-3 text-right">Sugerido</th>
                <th className="pb-3 text-right">Diferencia</th>
              </tr>
            </thead>
            <tbody>
              {priceSuggestions?.map((s: any) => {
                const diff = s.suggestedPrice - s.currentPrice;
                return (
                  <tr key={s.id} className="border-b">
                    <td className="py-3 font-medium">{s.vehicleTitle}</td>
                    <td className="py-3 text-right">{formatCurrency(s.currentPrice)}</td>
                    <td className="py-3 text-right font-medium">{formatCurrency(s.suggestedPrice)}</td>
                    <td className={`py-3 text-right ${diff > 0 ? 'text-green-600' : 'text-red-600'}`}>
                      {diff > 0 ? '+' : ''}{formatCurrency(diff)}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método | Endpoint                          | Descripción           |
| ------ | --------------------------------- | --------------------- |
| `GET`  | `/api/admin/ml/models`            | Estado de modelos     |
| `GET`  | `/api/admin/ml/fraud-alerts`      | Alertas de fraude     |
| `GET`  | `/api/admin/ml/price-suggestions` | Sugerencias de precio |
| `POST` | `/api/admin/ml/retrain/{model}`   | Re-entrenar modelo    |

---

## ✅ CHECKLIST

- [ ] Cards de modelos activos
- [ ] Tabla de alertas de fraude
- [ ] Tabla de sugerencias de precio
- [ ] Links a revisión de vehículos
- [ ] Indicadores de precisión

---

_Última actualización: Enero 31, 2026_
