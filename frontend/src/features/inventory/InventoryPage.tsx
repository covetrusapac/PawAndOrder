import { useQuery } from '@tanstack/react-query';
import { Alert, Chip, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { SectionCard } from '../../components/SectionCard';
import { api } from '../../lib/api';
import type { InventoryStockItem } from '../../types/api';

export function InventoryPage() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['inventory'],
    queryFn: () => api.get<{ items: InventoryStockItem[] }>('/inventory/stock'),
  });

  return (
    <SectionCard title="Inventory" subtitle="Lot-tracked medications, vaccines, and consumables with expiry visibility.">
      {error ? <Alert severity="error">Unable to load inventory.</Alert> : null}
      {isLoading ? <Typography>Loading...</Typography> : null}
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Product</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>On hand</TableCell>
            <TableCell>Reorder threshold</TableCell>
            <TableCell>Next expiry</TableCell>
            <TableCell>Status</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {data?.items.map((item) => (
            <TableRow key={item.id}>
              <TableCell>{item.name}</TableCell>
              <TableCell>{item.type}</TableCell>
              <TableCell>{item.quantityOnHand}</TableCell>
              <TableCell>{item.reorderThreshold}</TableCell>
              <TableCell>{item.nextExpiry ?? '-'}</TableCell>
              <TableCell>
                {item.lowStock ? <Chip color="warning" label="Low stock" size="small" /> : <Chip color="success" label="Healthy" size="small" />}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <Stack sx={{ mt: 2 }} spacing={1}>
        <Typography variant="body2" color="text.secondary">
          Stock is depleted against the earliest valid lot by default. Batch and expiry tracking are active in the backend seed data and receiving flow.
        </Typography>
      </Stack>
    </SectionCard>
  );
}
