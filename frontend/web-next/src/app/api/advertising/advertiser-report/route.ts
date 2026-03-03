import { NextRequest, NextResponse } from 'next/server';
import type {
  AdvertiserReport,
  AdvertiserReportSummary,
  CampaignReportDetail,
  DailyDataPoint,
} from '@/types/advertising';

// =============================================================================
// BFF: Advertiser Stats Report
// =============================================================================
// Aggregates campaign stats for a seller/dealer for email reporting.
// Tries backend first, falls back to demo data for development.
// =============================================================================

const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

export async function GET(request: NextRequest) {
  const ownerId = request.nextUrl.searchParams.get('ownerId');
  const period = request.nextUrl.searchParams.get('period') || '7d'; // 7d, 30d, 90d

  if (!ownerId) {
    return NextResponse.json({ success: false, error: 'ownerId is required' }, { status: 400 });
  }

  try {
    // Try fetching from backend
    const [ownerRes, campaignsRes] = await Promise.all([
      fetch(`${API_URL}/api/advertising/reports/owner/${ownerId}`, {
        headers: { 'Content-Type': 'application/json' },
        next: { revalidate: 300 }, // 5 min cache
      }),
      fetch(`${API_URL}/api/advertising/campaigns?ownerId=${ownerId}`, {
        headers: { 'Content-Type': 'application/json' },
        next: { revalidate: 300 },
      }),
    ]);

    if (ownerRes.ok && campaignsRes.ok) {
      const ownerData = await ownerRes.json();
      const campaignsData = await campaignsRes.json();

      const owner = ownerData.data || ownerData;
      const campaigns =
        campaignsData.data?.items || campaignsData.data || campaignsData.items || [];

      const report = buildReport(ownerId, owner, campaigns, period);
      return NextResponse.json({ success: true, data: report });
    }
  } catch (error) {
    console.error('[Advertiser Report] Backend fetch failed:', error);
  }

  // Fallback: generate demo report
  const demoReport = generateDemoReport(ownerId, period);
  return NextResponse.json({ success: true, data: demoReport, source: 'demo' });
}

// =============================================================================
// BUILD REPORT FROM BACKEND DATA
// =============================================================================

function buildReport(
  ownerId: string,
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  owner: any,
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  campaigns: any[],
  period: string
): AdvertiserReport {
  const now = new Date();
  const days = period === '90d' ? 90 : period === '30d' ? 30 : 7;
  const from = new Date(now);
  from.setDate(from.getDate() - days);

  const periodLabel =
    days === 7 ? 'Últimos 7 días' : days === 30 ? 'Último mes' : 'Últimos 3 meses';

  const totalImpressions = owner.totalImpressions || 0;
  const totalClicks = owner.totalClicks || 0;
  const totalSpent = owner.totalSpent || 0;
  const totalBudget = campaigns.reduce(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (sum: number, c: any) => sum + (c.totalBudget || 0),
    0
  );
  const activeCampaigns = owner.activeCampaigns || 0;
  const totalCampaigns = owner.totalCampaigns || campaigns.length;

  const summary: AdvertiserReportSummary = {
    totalCampaigns,
    activeCampaigns,
    completedCampaigns: totalCampaigns - activeCampaigns,
    totalImpressions,
    totalClicks,
    overallCtr:
      owner.overallCtr || (totalImpressions > 0 ? (totalClicks / totalImpressions) * 100 : 0),
    totalSpent,
    totalBudget,
    budgetUtilization: totalBudget > 0 ? (totalSpent / totalBudget) * 100 : 0,
    costPerClick: totalClicks > 0 ? totalSpent / totalClicks : 0,
    costPerMil: totalImpressions > 0 ? (totalSpent / totalImpressions) * 1000 : 0,
    estimatedLeads: Math.round(totalClicks * 0.08),
    costPerLead: totalClicks > 0 ? totalSpent / Math.max(1, Math.round(totalClicks * 0.08)) : 0,
    dailyTrend:
      owner.dailyImpressions?.map(
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (d: any, i: number) => ({
          date: d.date,
          views: d.views || d.count || 0,
          clicks: owner.dailyClicks?.[i]?.count || owner.dailyClicks?.[i]?.clicks || 0,
          spent: 0,
        })
      ) || [],
    impressionsChange: 0,
    clicksChange: 0,
    ctrChange: 0,
    spendChange: 0,
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const campaignDetails: CampaignReportDetail[] = campaigns.map((c: any) => {
    const impressions = c.totalViews || c.totalImpressions || 0;
    const clicks = c.totalClicks || 0;
    const spent = (c.totalBudget || 0) - (c.remainingBudget || 0);
    return {
      campaignId: c.id,
      vehicleId: c.vehicleId || '',
      vehicleTitle: c.vehicleTitle || `Vehículo ${c.vehicleId?.slice(0, 8) || 'N/A'}`,
      placementType: c.placementType || 'FeaturedSpot',
      pricingModel: c.pricingModel || 'FlatFee',
      status: c.status || 'Active',
      startDate: c.startDate || now.toISOString(),
      endDate: c.endDate || now.toISOString(),
      totalBudget: c.totalBudget || 0,
      spent,
      remainingBudget: c.remainingBudget || 0,
      impressions,
      clicks,
      ctr: impressions > 0 ? (clicks / impressions) * 100 : 0,
      costPerClick: clicks > 0 ? spent / clicks : 0,
      costPerMil: impressions > 0 ? (spent / impressions) * 1000 : 0,
      qualityScore: c.qualityScore || 0,
      dailyData: [],
      avgDailyImpressions: days > 0 ? Math.round(impressions / days) : 0,
      avgDailyClicks: days > 0 ? Math.round(clicks / days) : 0,
    };
  });

  return {
    ownerId,
    ownerType: owner.ownerType || 'dealer',
    ownerName: owner.ownerName || `Advertiser ${ownerId.slice(0, 8)}`,
    period: { from: from.toISOString(), to: now.toISOString(), label: periodLabel },
    summary,
    campaigns: campaignDetails,
    generatedAt: now.toISOString(),
  };
}

// =============================================================================
// DEMO REPORT GENERATOR
// =============================================================================

function generateDemoReport(ownerId: string, period: string): AdvertiserReport {
  const now = new Date();
  const days = period === '90d' ? 90 : period === '30d' ? 30 : 7;
  const from = new Date(now);
  from.setDate(from.getDate() - days);
  const periodLabel =
    days === 7 ? 'Últimos 7 días' : days === 30 ? 'Último mes' : 'Últimos 3 meses';

  // Generate daily trend
  const dailyTrend: DailyDataPoint[] = [];
  for (let i = days - 1; i >= 0; i--) {
    const d = new Date(now);
    d.setDate(d.getDate() - i);
    dailyTrend.push({
      date: d.toISOString().split('T')[0],
      views: Math.round(80 + Math.random() * 120),
      clicks: Math.round(3 + Math.random() * 12),
      spent: Math.round(50 + Math.random() * 150),
    });
  }

  const totalImpressions = dailyTrend.reduce((s, d) => s + d.views, 0);
  const totalClicks = dailyTrend.reduce((s, d) => s + d.clicks, 0);
  const totalSpent = dailyTrend.reduce((s, d) => s + d.spent, 0);

  const campaigns: CampaignReportDetail[] = [
    {
      campaignId: 'demo-camp-1',
      vehicleId: 'v-001',
      vehicleTitle: '2023 Toyota RAV4 XLE',
      vehicleImage: '/demo/rav4.jpg',
      placementType: 'FeaturedSpot',
      pricingModel: 'FlatFee',
      status: 'Active',
      startDate: from.toISOString(),
      endDate: now.toISOString(),
      totalBudget: 3990,
      spent: 2800,
      remainingBudget: 1190,
      impressions: Math.round(totalImpressions * 0.6),
      clicks: Math.round(totalClicks * 0.55),
      ctr: 4.2,
      costPerClick: 28.5,
      costPerMil: 3.2,
      qualityScore: 82,
      dailyData: dailyTrend.slice(-7),
      avgDailyImpressions: Math.round((totalImpressions * 0.6) / days),
      avgDailyClicks: Math.round((totalClicks * 0.55) / days),
    },
    {
      campaignId: 'demo-camp-2',
      vehicleId: 'v-002',
      vehicleTitle: '2022 Honda CR-V Touring',
      placementType: 'PremiumSpot',
      pricingModel: 'FlatFee',
      status: 'Active',
      startDate: from.toISOString(),
      endDate: now.toISOString(),
      totalBudget: 7990,
      spent: 5200,
      remainingBudget: 2790,
      impressions: Math.round(totalImpressions * 0.4),
      clicks: Math.round(totalClicks * 0.45),
      ctr: 5.1,
      costPerClick: 22.3,
      costPerMil: 4.8,
      qualityScore: 91,
      dailyData: dailyTrend.slice(-7),
      avgDailyImpressions: Math.round((totalImpressions * 0.4) / days),
      avgDailyClicks: Math.round((totalClicks * 0.45) / days),
    },
  ];

  return {
    ownerId,
    ownerType: 'dealer',
    ownerName: 'Auto Premium RD',
    period: { from: from.toISOString(), to: now.toISOString(), label: periodLabel },
    summary: {
      totalCampaigns: 2,
      activeCampaigns: 2,
      completedCampaigns: 0,
      totalImpressions,
      totalClicks,
      overallCtr: totalImpressions > 0 ? (totalClicks / totalImpressions) * 100 : 0,
      totalSpent,
      totalBudget: 11980,
      budgetUtilization: (totalSpent / 11980) * 100,
      costPerClick: totalClicks > 0 ? totalSpent / totalClicks : 0,
      costPerMil: totalImpressions > 0 ? (totalSpent / totalImpressions) * 1000 : 0,
      estimatedLeads: Math.round(totalClicks * 0.08),
      costPerLead: totalClicks > 0 ? totalSpent / Math.max(1, Math.round(totalClicks * 0.08)) : 0,
      dailyTrend,
      impressionsChange: 12.5,
      clicksChange: 8.3,
      ctrChange: -1.2,
      spendChange: 15.0,
    },
    campaigns,
    generatedAt: now.toISOString(),
  };
}
