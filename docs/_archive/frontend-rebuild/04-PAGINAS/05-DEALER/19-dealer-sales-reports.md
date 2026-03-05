# 📊 Reportes de Ventas del Dealer

> **Tiempo estimado:** 30 minutos  
> **Páginas:** DealerSalesReportsPage, DealerAnalyticsPage

---

## 📋 OBJETIVO

Dashboard de reportes y analytics:

- Métricas de ventas
- Gráficos de rendimiento
- Exportación de datos

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ REPORTES                              [Este mes ▼] [Exportar]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐    │
│ │ $4.5M      │ │ 12         │ │ 2,450      │ │ 4.8%       │    │
│ │ Ventas     │ │ Vendidos   │ │ Vistas     │ │ Conversión │    │
│ │ +15%       │ │ +3         │ │ +320       │ │ +0.5%      │    │
│ └────────────┘ └────────────┘ └────────────┘ └────────────┘    │
│                                                                 │
│ VENTAS POR MES                                                  │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │     📈 [Gráfico de barras]                                  │ │
│ │                                                              │ │
│ │  ██                                                          │ │
│ │  ██  ██      ██                                              │ │
│ │  ██  ██  ██  ██  ██                                          │ │
│ │  Ene Feb Mar Abr May                                         │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ TOP VEHÍCULOS                                                   │
│ ┌──────────────────────────────────┬────────┬─────────────────┐ │
│ │ Vehículo                         │ Vistas │ Consultas       │ │
│ │ Toyota Camry 2024                │ 450    │ 28              │ │
│ │ Honda CR-V 2023                  │ 380    │ 22              │ │
│ │ Hyundai Tucson 2024              │ 310    │ 18              │ │
│ └──────────────────────────────────┴────────┴─────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(dealer)/dealer/reports/page.tsx
'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { dealerService } from '@/services/api/dealerService';
import { formatCurrency } from '@/lib/format';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  LineChart, Line, PieChart, Pie, Cell
} from 'recharts';
import {
  DollarSign, Car, Eye, TrendingUp, Download,
  ArrowUp, ArrowDown
} from 'lucide-react';

const periods = [
  { value: '7d', label: 'Últimos 7 días' },
  { value: '30d', label: 'Este mes' },
  { value: '90d', label: 'Últimos 3 meses' },
  { value: '1y', label: 'Este año' },
];

export default function DealerSalesReportsPage() {
  const [period, setPeriod] = useState('30d');

  const { data: stats } = useQuery({
    queryKey: ['dealer-stats', period],
    queryFn: () => dealerService.getStats(period),
  });

  const { data: salesByMonth } = useQuery({
    queryKey: ['dealer-sales-chart', period],
    queryFn: () => dealerService.getSalesChart(period),
  });

  const { data: topVehicles } = useQuery({
    queryKey: ['dealer-top-vehicles', period],
    queryFn: () => dealerService.getTopVehicles(period),
  });

  const handleExport = async () => {
    const blob = await dealerService.exportReport(period);
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `reporte-${period}.xlsx`;
    a.click();
  };

  const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6'];

  return (
    <div className="container max-w-6xl mx-auto py-8 px-4">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Reportes</h1>
        <div className="flex gap-2">
          <Select value={period} onValueChange={setPeriod}>
            <SelectTrigger className="w-48">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {periods.map(p => (
                <SelectItem key={p.value} value={p.value}>
                  {p.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button variant="outline" onClick={handleExport}>
            <Download className="w-4 h-4 mr-2" />
            Exportar
          </Button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <StatCard
          title="Ventas Totales"
          value={formatCurrency(stats?.totalSales || 0)}
          change={stats?.salesChange}
          icon={DollarSign}
        />
        <StatCard
          title="Vehículos Vendidos"
          value={stats?.vehiclesSold || 0}
          change={stats?.soldChange}
          icon={Car}
        />
        <StatCard
          title="Vistas Totales"
          value={stats?.totalViews?.toLocaleString() || 0}
          change={stats?.viewsChange}
          icon={Eye}
        />
        <StatCard
          title="Tasa de Conversión"
          value={`${stats?.conversionRate || 0}%`}
          change={stats?.conversionChange}
          icon={TrendingUp}
        />
      </div>

      {/* Charts Grid */}
      <div className="grid lg:grid-cols-2 gap-6 mb-8">
        {/* Sales Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Ventas por Período</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={salesByMonth}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" />
                  <YAxis />
                  <Tooltip formatter={(value) => formatCurrency(value as number)} />
                  <Bar dataKey="sales" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* Views Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Vistas por Día</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={salesByMonth}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" />
                  <YAxis />
                  <Tooltip />
                  <Line
                    type="monotone"
                    dataKey="views"
                    stroke="#10b981"
                    strokeWidth={2}
                    dot={false}
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Top Vehicles */}
      <Card>
        <CardHeader>
          <CardTitle>Vehículos con Mejor Rendimiento</CardTitle>
        </CardHeader>
        <CardContent>
          <table className="w-full">
            <thead>
              <tr className="border-b text-left">
                <th className="pb-3 font-medium">Vehículo</th>
                <th className="pb-3 font-medium text-right">Vistas</th>
                <th className="pb-3 font-medium text-right">Consultas</th>
                <th className="pb-3 font-medium text-right">Conversión</th>
              </tr>
            </thead>
            <tbody>
              {topVehicles?.map((v: any, idx: number) => (
                <tr key={v.id} className="border-b">
                  <td className="py-3">
                    <div className="flex items-center gap-3">
                      <span className="text-gray-400 font-medium">#{idx + 1}</span>
                      <div>
                        <div className="font-medium">{v.title}</div>
                        <div className="text-sm text-gray-600">{formatCurrency(v.price)}</div>
                      </div>
                    </div>
                  </td>
                  <td className="py-3 text-right">{v.views.toLocaleString()}</td>
                  <td className="py-3 text-right">{v.inquiries}</td>
                  <td className="py-3 text-right">{v.conversionRate}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  );
}

function StatCard({
  title,
  value,
  change,
  icon: Icon
}: {
  title: string;
  value: string | number;
  change?: number;
  icon: any;
}) {
  const isPositive = (change || 0) >= 0;

  return (
    <Card>
      <CardContent className="pt-6">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-gray-600">{title}</p>
            <p className="text-2xl font-bold mt-1">{value}</p>
            {change !== undefined && (
              <div className={`flex items-center text-sm mt-1 ${isPositive ? 'text-green-600' : 'text-red-600'}`}>
                {isPositive ? <ArrowUp className="w-3 h-3" /> : <ArrowDown className="w-3 h-3" />}
                <span>{Math.abs(change)}%</span>
              </div>
            )}
          </div>
          <div className="w-10 h-10 rounded-full bg-primary-100 flex items-center justify-center">
            <Icon className="w-5 h-5 text-primary-600" />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
```

---

## 📡 ENDPOINTS

| Método | Endpoint                                 | Descripción        |
| ------ | ---------------------------------------- | ------------------ |
| `GET`  | `/api/dealers/stats?period=30d`          | Estadísticas       |
| `GET`  | `/api/dealers/sales-chart?period=30d`    | Datos para gráfico |
| `GET`  | `/api/dealers/top-vehicles?period=30d`   | Top vehículos      |
| `GET`  | `/api/dealers/reports/export?period=30d` | Exportar Excel     |

---

## ✅ CHECKLIST

- [ ] Cards de métricas principales
- [ ] Gráfico de ventas por período
- [ ] Gráfico de vistas
- [ ] Tabla de top vehículos
- [ ] Selector de período
- [ ] Exportación a Excel

---

_Última actualización: Enero 30, 2026_
