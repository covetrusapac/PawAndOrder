import { useQuery } from '@tanstack/react-query';
import { Alert, Grid, List, ListItem, ListItemText, Typography } from '@mui/material';
import { SectionCard } from '../../components/SectionCard';
import { StatusChip } from '../../components/StatusChip';
import { api } from '../../lib/api';

export function ReportsPage() {
  const revenueQuery = useQuery({
    queryKey: ['reports', 'revenue'],
    queryFn: () => api.get<{ items: Array<{ date: string; total: number; invoiceCount: number }> }>('/reports/revenue'),
  });
  const appointmentsQuery = useQuery({
    queryKey: ['reports', 'appointments'],
    queryFn: () => api.get<{ items: Array<{ status: string; count: number }> }>('/reports/appointments'),
  });
  const lowStockQuery = useQuery({
    queryKey: ['reports', 'low-stock'],
    queryFn: () => api.get<{ items: Array<{ id: number; name: string; onHand: number; reorderThreshold: number }> }>('/reports/low-stock'),
  });
  const usageQuery = useQuery({
    queryKey: ['reports', 'usage'],
    queryFn: () => api.get<{ items: Array<{ name: string; quantity: number; revenue: number }> }>('/reports/product-usage'),
  });

  if (revenueQuery.error || appointmentsQuery.error || lowStockQuery.error || usageQuery.error) {
    return <Alert severity="error">Unable to load reports.</Alert>;
  }

  return (
    <Grid container spacing={2.5}>
      <Grid size={{ xs: 12, md: 6 }}>
        <SectionCard title="Revenue Trend">
          <List disablePadding>
            {revenueQuery.data?.items.map((item) => (
              <ListItem key={item.date} divider>
                <ListItemText primary={item.date} secondary={`${item.invoiceCount} invoices`} />
                <Typography>${item.total.toFixed(2)}</Typography>
              </ListItem>
            ))}
          </List>
        </SectionCard>
      </Grid>
      <Grid size={{ xs: 12, md: 6 }}>
        <SectionCard title="Appointment Status Mix">
          <List disablePadding>
            {appointmentsQuery.data?.items.map((item) => (
              <ListItem key={item.status} divider>
                <ListItemText primary={item.status} />
                <StatusChip value={item.status} />
                <Typography sx={{ ml: 2 }}>{item.count}</Typography>
              </ListItem>
            ))}
          </List>
        </SectionCard>
      </Grid>
      <Grid size={{ xs: 12, md: 6 }}>
        <SectionCard title="Low Stock Report">
          <List disablePadding>
            {lowStockQuery.data?.items.map((item) => (
              <ListItem key={item.id} divider>
                <ListItemText primary={item.name} secondary={`Reorder threshold ${item.reorderThreshold}`} />
                <Typography>{item.onHand}</Typography>
              </ListItem>
            ))}
          </List>
        </SectionCard>
      </Grid>
      <Grid size={{ xs: 12, md: 6 }}>
        <SectionCard title="Top Product and Service Usage">
          <List disablePadding>
            {usageQuery.data?.items.map((item) => (
              <ListItem key={item.name} divider>
                <ListItemText primary={item.name} secondary={`Quantity ${item.quantity}`} />
                <Typography>${item.revenue.toFixed(2)}</Typography>
              </ListItem>
            ))}
          </List>
        </SectionCard>
      </Grid>
    </Grid>
  );
}
