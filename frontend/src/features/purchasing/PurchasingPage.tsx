import { useQuery } from '@tanstack/react-query';
import { Alert, Grid, List, ListItem, ListItemText, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { SectionCard } from '../../components/SectionCard';
import { StatusChip } from '../../components/StatusChip';
import { api } from '../../lib/api';
import type { PurchaseOrderListItem, VendorListItem } from '../../types/api';

export function PurchasingPage() {
  const vendorsQuery = useQuery({
    queryKey: ['vendors'],
    queryFn: () => api.get<{ items: VendorListItem[] }>('/vendors'),
  });

  const ordersQuery = useQuery({
    queryKey: ['purchase-orders'],
    queryFn: () => api.get<{ items: PurchaseOrderListItem[] }>('/purchase-orders'),
  });

  return (
    <Grid container spacing={2.5}>
      <Grid size={{ xs: 12, lg: 4 }}>
        <SectionCard title="Vendors">
          {vendorsQuery.error ? <Alert severity="error">Unable to load vendors.</Alert> : null}
          <List disablePadding>
            {vendorsQuery.data?.items.map((vendor) => (
              <ListItem key={vendor.id} divider>
                <ListItemText primary={vendor.name} secondary={`${vendor.contactName ?? 'No primary contact'} • ${vendor.phone ?? 'No phone'}`} />
                <Typography>{vendor.openOrders}</Typography>
              </ListItem>
            ))}
          </List>
        </SectionCard>
      </Grid>
      <Grid size={{ xs: 12, lg: 8 }}>
        <SectionCard title="Purchase Orders">
          {ordersQuery.error ? <Alert severity="error">Unable to load purchase orders.</Alert> : null}
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Order</TableCell>
                <TableCell>Vendor</TableCell>
                <TableCell>Ordered</TableCell>
                <TableCell>Expected</TableCell>
                <TableCell>Received / Ordered</TableCell>
                <TableCell>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {ordersQuery.data?.items.map((order) => (
                <TableRow key={order.id}>
                  <TableCell>{order.orderNumber}</TableCell>
                  <TableCell>{order.vendor}</TableCell>
                  <TableCell>{order.orderedAt.slice(0, 10)}</TableCell>
                  <TableCell>{order.expectedAt?.slice(0, 10) ?? '-'}</TableCell>
                  <TableCell>{order.totalReceived} / {order.totalOrdered}</TableCell>
                  <TableCell><StatusChip value={order.status} /></TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </SectionCard>
      </Grid>
    </Grid>
  );
}
