import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Button, Grid, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { SectionCard } from '../../components/SectionCard';
import { StatusChip } from '../../components/StatusChip';
import { api } from '../../lib/api';
import type { InvoiceDetail, PaymentMethod } from '../../types/api';

export function InvoicePage() {
  const { id } = useParams();
  const queryClient = useQueryClient();
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('Card');
  const [amount, setAmount] = useState(0);
  const [reference, setReference] = useState('');

  const { data, isLoading, error } = useQuery({
    queryKey: ['invoice', id],
    queryFn: () => api.get<InvoiceDetail>(`/invoices/${id}`),
    enabled: Boolean(id),
  });

  const paymentMutation = useMutation({
    mutationFn: () => api.post(`/invoices/${id}/payments`, { paymentMethod, amount, reference, notes: '' }),
    onSuccess: async () => {
      setReference('');
      await queryClient.invalidateQueries({ queryKey: ['invoice', id] });
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });

  const issueMutation = useMutation({
    mutationFn: () => api.post(`/invoices/${id}/issue`),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['invoice', id] }),
  });

  if (isLoading) return <Typography>Loading...</Typography>;
  if (error || !data) return <Alert severity="error">Unable to load invoice.</Alert>;

  return (
    <Grid container spacing={2.5}>
      <Grid size={{ xs: 12, lg: 8 }}>
        <SectionCard title={data.invoiceNumber} subtitle={`${data.client ?? ''} • ${data.patient ?? ''}`} action={<StatusChip value={data.status} />}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Description</TableCell>
                <TableCell>Qty</TableCell>
                <TableCell>Unit ex GST</TableCell>
                <TableCell>GST</TableCell>
                <TableCell>Total inc GST</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data.lines.map((line) => (
                <TableRow key={line.id}>
                  <TableCell>{line.description}{line.lot ? ` (${line.lot})` : ''}</TableCell>
                  <TableCell>{line.quantity}</TableCell>
                  <TableCell>${line.unitPriceExGst.toFixed(2)}</TableCell>
                  <TableCell>${line.gstAmount.toFixed(2)}</TableCell>
                  <TableCell>${line.lineTotalIncGst.toFixed(2)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </SectionCard>
      </Grid>
      <Grid size={{ xs: 12, lg: 4 }}>
        <Stack spacing={2.5}>
          <SectionCard title="Totals">
            <Typography>Subtotal ex GST: ${data.subtotalExGst.toFixed(2)}</Typography>
            <Typography>Total GST: ${data.totalGst.toFixed(2)}</Typography>
            <Typography variant="h6">Total inc GST: ${data.totalIncGst.toFixed(2)}</Typography>
            <Typography>Paid: ${data.amountPaid.toFixed(2)}</Typography>
            <Typography color={data.balanceDue > 0 ? 'warning.main' : 'success.main'}>
              Balance: ${data.balanceDue.toFixed(2)}
            </Typography>
            <Button variant="outlined" onClick={() => issueMutation.mutate()} disabled={issueMutation.isPending}>Issue Invoice</Button>
          </SectionCard>

          <SectionCard title="Manual Payment Entry">
            <Stack spacing={2}>
              <TextField select label="Method" value={paymentMethod} onChange={(event) => setPaymentMethod(event.target.value as PaymentMethod)}>
                <MenuItem value="Cash">Cash</MenuItem>
                <MenuItem value="Card">Card</MenuItem>
              </TextField>
              <TextField label="Amount" type="number" value={amount} onChange={(event) => setAmount(Number(event.target.value))} />
              <TextField label="Reference" value={reference} onChange={(event) => setReference(event.target.value)} />
              <Button variant="contained" onClick={() => paymentMutation.mutate()} disabled={paymentMutation.isPending}>Record Payment</Button>
            </Stack>
          </SectionCard>

          <SectionCard title="Payment History">
            <Stack spacing={1.25}>
              {data.payments.map((payment) => (
                <Typography key={payment.id}>• {payment.paymentMethod} ${payment.amount.toFixed(2)} on {payment.paidAt}</Typography>
              ))}
            </Stack>
          </SectionCard>
        </Stack>
      </Grid>
    </Grid>
  );
}
