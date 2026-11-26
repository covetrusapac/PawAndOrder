import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Divider,
  Grid,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { SectionCard } from '../../components/SectionCard';
import { StatusChip } from '../../components/StatusChip';
import { api } from '../../lib/api';
import type { InvoiceDetail, PatientListItem, PaymentMethod, ProductListItem } from '../../types/api';

export function PosPage() {
  const queryClient = useQueryClient();
  const [selectedPatientId, setSelectedPatientId] = useState<number | ''>('');
  const [selectedClientId, setSelectedClientId] = useState<number>(0);
  const [currentInvoiceId, setCurrentInvoiceId] = useState<number | null>(null);
  const [productSearch, setProductSearch] = useState('');
  const [quantities, setQuantities] = useState<Record<number, number>>({});
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('Card');
  const [paymentAmount, setPaymentAmount] = useState(0);
  const [paymentReference, setPaymentReference] = useState('');

  const patientsQuery = useQuery({
    queryKey: ['patients', 'lookup'],
    queryFn: () => api.get<{ items: PatientListItem[] }>('/patients'),
  });

  const productsQuery = useQuery({
    queryKey: ['products', 'pos'],
    queryFn: () => api.get<{ items: ProductListItem[] }>('/products'),
  });

  const invoiceQuery = useQuery({
    queryKey: ['pos-invoice', currentInvoiceId],
    queryFn: () => api.get<InvoiceDetail>(`/invoices/${currentInvoiceId}`),
    enabled: currentInvoiceId !== null,
  });

  useEffect(() => {
    if (invoiceQuery.data) {
      setPaymentAmount(invoiceQuery.data.balanceDue);
    }
  }, [invoiceQuery.data]);

  const createSaleMutation = useMutation({
    mutationFn: () => api.post<{ id: number; invoiceNumber: string }>('/invoices', { clientId: selectedClientId, patientId: selectedPatientId }),
    onSuccess: async (response) => {
      setCurrentInvoiceId(response.id);
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      await queryClient.invalidateQueries({ queryKey: ['dashboard', 'pos'] });
    },
  });

  const addLineMutation = useMutation({
    mutationFn: (product: ProductListItem) => api.post(`/invoices/${currentInvoiceId}/lines`, {
      productId: product.id,
      description: product.name,
      lineType: product.trackInventory ? 'Product' : 'Service',
      quantity: quantities[product.id] ?? 1,
      unitPriceExGst: product.defaultSalePriceExGst,
      isTaxable: true,
      inventoryLotId: null,
    }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['pos-invoice', currentInvoiceId] });
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      await queryClient.invalidateQueries({ queryKey: ['inventory'] });
    },
  });

  const removeLineMutation = useMutation({
    mutationFn: (lineId: number) => api.delete(`/invoices/${currentInvoiceId}/lines/${lineId}`),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['pos-invoice', currentInvoiceId] });
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      await queryClient.invalidateQueries({ queryKey: ['inventory'] });
    },
  });

  const issueMutation = useMutation({
    mutationFn: () => api.post(`/invoices/${currentInvoiceId}/issue`),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['pos-invoice', currentInvoiceId] }),
  });

  const paymentMutation = useMutation({
    mutationFn: () => api.post(`/invoices/${currentInvoiceId}/payments`, {
      paymentMethod,
      amount: paymentAmount,
      reference: paymentReference,
      notes: '',
    }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['pos-invoice', currentInvoiceId] });
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      setPaymentReference('');
    },
  });

  const filteredProducts = useMemo(() => {
    const items = productsQuery.data?.items ?? [];
    if (!productSearch) {
      return items;
    }

    const search = productSearch.toLowerCase();
    return items.filter((product) =>
      product.name.toLowerCase().includes(search) || product.sku.toLowerCase().includes(search),
    );
  }, [productSearch, productsQuery.data?.items]);

  const selectedPatient = patientsQuery.data?.items.find((patient) => patient.id === selectedPatientId);

  return (
    <Stack spacing={3}>
      <div>
        <Typography variant="h4">Walk-In POS</Typography>
        <Typography color="text.secondary">Create and complete a sale outside the consult workflow.</Typography>
      </div>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, lg: 5 }}>
          <Stack spacing={2.5}>
            <SectionCard title="Start New Sale" subtitle="Select the patient at the counter, then begin a direct invoice.">
              <Stack spacing={2}>
                <TextField
                  select
                  label="Patient"
                  value={selectedPatientId}
                  onChange={(event) => {
                    const patient = patientsQuery.data?.items.find((item) => item.id === Number(event.target.value));
                    setSelectedPatientId(Number(event.target.value));
                    setSelectedClientId(patient?.clientId ?? 0);
                  }}
                >
                  {patientsQuery.data?.items.map((patient) => (
                    <MenuItem key={patient.id} value={patient.id}>{patient.name} • {patient.ownerName}</MenuItem>
                  ))}
                </TextField>
                <TextField label="Owner" value={selectedPatient?.ownerName ?? ''} disabled />
                <Button variant="contained" onClick={() => createSaleMutation.mutate()} disabled={!selectedPatientId || createSaleMutation.isPending}>
                  Start Sale
                </Button>
                {currentInvoiceId ? (
                  <Button variant="outlined" onClick={() => setCurrentInvoiceId(null)}>Clear Current Sale</Button>
                ) : null}
              </Stack>
            </SectionCard>

            <SectionCard title="Catalog" subtitle="Add services, medications, and retail items into the active sale.">
              <Stack spacing={1.5}>
                <TextField label="Search products or services" value={productSearch} onChange={(event) => setProductSearch(event.target.value)} />
                <List disablePadding sx={{ maxHeight: 520, overflow: 'auto' }}>
                  {filteredProducts.slice(0, 20).map((product) => (
                    <ListItem key={product.id} divider sx={{ alignItems: 'flex-start', flexDirection: 'column' }}>
                      <Box sx={{ display: 'flex', width: '100%', justifyContent: 'space-between', gap: 2 }}>
                        <ListItemText
                          primary={product.name}
                          secondary={`${product.sku} • ${product.type} • $${product.defaultSalePriceExGst.toFixed(2)} ex GST`}
                        />
                        <StatusChip value={product.trackInventory ? 'Product' : 'Service'} />
                      </Box>
                      <Box sx={{ display: 'flex', width: '100%', gap: 1, mt: 1 }}>
                        <TextField
                          label="Qty"
                          type="number"
                          size="small"
                          value={quantities[product.id] ?? 1}
                          onChange={(event) => setQuantities({ ...quantities, [product.id]: Number(event.target.value) })}
                          sx={{ maxWidth: 100 }}
                        />
                        <Button variant="outlined" onClick={() => addLineMutation.mutate(product)} disabled={!currentInvoiceId || addLineMutation.isPending}>
                          Add To Sale
                        </Button>
                      </Box>
                    </ListItem>
                  ))}
                </List>
              </Stack>
            </SectionCard>
          </Stack>
        </Grid>

        <Grid size={{ xs: 12, lg: 7 }}>
          <Stack spacing={2.5}>
            <SectionCard
              title={invoiceQuery.data?.invoiceNumber ?? 'Current Sale'}
              subtitle={invoiceQuery.data ? `${invoiceQuery.data.client ?? ''} • ${invoiceQuery.data.patient ?? ''}` : 'Start a sale to begin adding line items.'}
              action={invoiceQuery.data ? <StatusChip value={invoiceQuery.data.status} /> : null}
            >
              {!currentInvoiceId ? (
                <Typography color="text.secondary">No sale in progress.</Typography>
              ) : invoiceQuery.error ? (
                <Alert severity="error">Unable to load the current sale.</Alert>
              ) : invoiceQuery.isLoading ? (
                <Typography>Loading sale...</Typography>
              ) : (
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Description</TableCell>
                      <TableCell>Qty</TableCell>
                      <TableCell>Total inc GST</TableCell>
                      <TableCell></TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {invoiceQuery.data?.lines.map((line) => (
                      <TableRow key={line.id}>
                        <TableCell>{line.description}{line.lot ? ` (${line.lot})` : ''}</TableCell>
                        <TableCell>{line.quantity}</TableCell>
                        <TableCell>${line.lineTotalIncGst.toFixed(2)}</TableCell>
                        <TableCell>
                          <Button size="small" color="error" onClick={() => removeLineMutation.mutate(line.id)}>Remove</Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </SectionCard>

            {invoiceQuery.data ? (
              <Grid container spacing={2.5}>
                <Grid size={{ xs: 12, md: 6 }}>
                  <SectionCard title="Checkout Totals">
                    <Stack spacing={1}>
                      <Typography>Subtotal ex GST: ${invoiceQuery.data.subtotalExGst.toFixed(2)}</Typography>
                      <Typography>GST: ${invoiceQuery.data.totalGst.toFixed(2)}</Typography>
                      <Typography variant="h6">Total inc GST: ${invoiceQuery.data.totalIncGst.toFixed(2)}</Typography>
                      <Typography>Paid: ${invoiceQuery.data.amountPaid.toFixed(2)}</Typography>
                      <Typography color={invoiceQuery.data.balanceDue > 0 ? 'warning.main' : 'success.main'}>
                        Balance due: ${invoiceQuery.data.balanceDue.toFixed(2)}
                      </Typography>
                      <Divider sx={{ my: 1 }} />
                      <Button variant="outlined" onClick={() => issueMutation.mutate()} disabled={issueMutation.isPending}>Issue Sale</Button>
                      <Button component={RouterLink} to={`/invoices/${invoiceQuery.data.id}`}>Open Full Invoice View</Button>
                    </Stack>
                  </SectionCard>
                </Grid>
                <Grid size={{ xs: 12, md: 6 }}>
                  <SectionCard title="Take Payment">
                    <Stack spacing={2}>
                      <TextField select label="Method" value={paymentMethod} onChange={(event) => setPaymentMethod(event.target.value as PaymentMethod)}>
                        <MenuItem value="Cash">Cash</MenuItem>
                        <MenuItem value="Card">Card</MenuItem>
                      </TextField>
                      <TextField label="Amount" type="number" value={paymentAmount} onChange={(event) => setPaymentAmount(Number(event.target.value))} />
                      <TextField label="Reference" value={paymentReference} onChange={(event) => setPaymentReference(event.target.value)} />
                      <Button variant="contained" onClick={() => paymentMutation.mutate()} disabled={paymentMutation.isPending || invoiceQuery.data.balanceDue <= 0}>
                        Record Payment
                      </Button>
                    </Stack>
                  </SectionCard>
                </Grid>
              </Grid>
            ) : null}
          </Stack>
        </Grid>
      </Grid>
    </Stack>
  );
}
