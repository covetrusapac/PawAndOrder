import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Tab,
  Tabs,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { format } from 'date-fns';
import { useMemo, useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { SectionCard } from '../../components/SectionCard';
import { StatusChip } from '../../components/StatusChip';
import { api } from '../../lib/api';
import type { DashboardResponse, InvoiceListResponse, LookupResponse, PatientListItem } from '../../types/api';

export function DashboardPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [openAppointmentDialog, setOpenAppointmentDialog] = useState(false);
  const [invoiceSearch, setInvoiceSearch] = useState('');
  const [view, setView] = useState<'nursing' | 'frontdesk'>('nursing');
  const [form, setForm] = useState({
    patientId: 0,
    clientId: 0,
    clinicianUserId: 0,
    appointmentTypeId: 0,
    startAt: `${format(new Date(), 'yyyy-MM-dd')}T09:00`,
    endAt: `${format(new Date(), 'yyyy-MM-dd')}T09:30`,
    status: 'Booked',
    reason: '',
    notes: '',
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['dashboard'],
    queryFn: () => api.get<DashboardResponse>('/reports/dashboard'),
  });

  const lookupsQuery = useQuery({
    queryKey: ['lookups'],
    queryFn: () => api.get<LookupResponse>('/lookups/bootstrap'),
  });

  const patientsQuery = useQuery({
    queryKey: ['patients', 'lookup'],
    queryFn: () => api.get<{ items: PatientListItem[] }>('/patients'),
  });

  const invoicesQuery = useQuery({
    queryKey: ['dashboard', 'pos', invoiceSearch],
    queryFn: () => api.get<InvoiceListResponse>(`/invoices${invoiceSearch ? `?search=${encodeURIComponent(invoiceSearch)}` : ''}`),
  });

  const createAppointmentMutation = useMutation({
    mutationFn: () => api.post('/appointments', {
      ...form,
      clinicianUserId: Number(form.clinicianUserId),
      appointmentTypeId: Number(form.appointmentTypeId),
      patientId: Number(form.patientId),
      clientId: Number(form.clientId),
      startAt: new Date(form.startAt).toISOString(),
      endAt: new Date(form.endAt).toISOString(),
    }),
    onSuccess: async () => {
      setOpenAppointmentDialog(false);
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      await queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });

  const arriveMutation = useMutation({
    mutationFn: (appointmentId: number) => api.post(`/appointments/${appointmentId}/arrive`),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      await queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });

  const startConsultMutation = useMutation({
    mutationFn: (appointmentId: number) => api.post<{ consultId: number }>(`/appointments/${appointmentId}/start-consult`),
    onSuccess: async (response) => {
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      await queryClient.invalidateQueries({ queryKey: ['appointments'] });
      navigate(`/consults/${response.consultId}`);
    },
  });

  const unpaidInvoices = useMemo(
    () => (invoicesQuery.data?.items ?? []).filter((invoice) => invoice.balanceDue > 0),
    [invoicesQuery.data?.items],
  );

  if (isLoading) {
    return <CircularProgress />;
  }

  if (error || !data) {
    return <Alert severity="error">Unable to load dashboard data.</Alert>;
  }

  return (
    <Stack spacing={3}>
      <div>
        <Typography variant="h4">Command Center</Typography>
        <Typography color="text.secondary">Split between patient-flow nursing work and front-desk booking and checkout.</Typography>
      </div>

      <Tabs value={view} onChange={(_, value) => setView(value)}>
        <Tab label="Nursing" value="nursing" />
        <Tab label="Front Desk" value="frontdesk" />
      </Tabs>

      {view === 'nursing' ? (
        <>
          <Grid container spacing={2.5}>
            {[
              { label: "Today's appointments", value: data.metrics.todaysAppointments },
              { label: 'Arrivals waiting', value: data.metrics.arrivalsWaiting },
              { label: 'In consult', value: data.metrics.inConsult },
            ].map(({ label, value }) => (
              <Grid size={{ xs: 12, md: 4 }} key={label}>
                <SectionCard title={label}>
                  <Typography variant="h3">{value}</Typography>
                </SectionCard>
              </Grid>
            ))}
          </Grid>

          <SectionCard title="Today&apos;s Appointments" subtitle="Keep the day moving from check-in through consult handoff.">
            <List disablePadding>
              {data.todaysAppointments.map((appointment) => (
                <ListItem key={appointment.id} divider>
                  <ListItemText
                    primary={`${format(new Date(appointment.startAt), 'p')} • ${appointment.patient}`}
                    secondary={`${appointment.client} • ${appointment.clinician} • ${appointment.reason}`}
                  />
                  <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 1 }}>
                    <StatusChip value={appointment.status} />
                    <Box sx={{ display: 'flex', gap: 1 }}>
                      {appointment.status === 'Booked' ? (
                        <Button size="small" variant="outlined" onClick={() => arriveMutation.mutate(appointment.id)}>
                          Arrive
                        </Button>
                      ) : null}
                      <Button
                        size="small"
                        variant="contained"
                        onClick={() => {
                          if (appointment.consultId) {
                            navigate(`/consults/${appointment.consultId}`);
                            return;
                          }

                          startConsultMutation.mutate(appointment.id);
                        }}
                      >
                        {appointment.consultId ? 'Open Consult' : 'Start Consult'}
                      </Button>
                    </Box>
                  </Box>
                </ListItem>
              ))}
            </List>
          </SectionCard>
        </>
      ) : (
        <>
          <Grid container spacing={2.5}>
            {[
              { label: "Today's appointments", value: data.metrics.todaysAppointments },
              { label: 'Arrivals waiting', value: data.metrics.arrivalsWaiting },
              { label: 'POS queue', value: data.metrics.openInvoices },
            ].map(({ label, value }) => (
              <Grid size={{ xs: 12, md: 4 }} key={label}>
                <SectionCard title={label}>
                  <Typography variant="h3">{value}</Typography>
                </SectionCard>
              </Grid>
            ))}
          </Grid>

          <Grid container spacing={2.5}>
            <Grid size={{ xs: 12, lg: 5 }}>
              <SectionCard title="Front Desk Actions" subtitle="Same-day booking and direct sales.">
                <Stack spacing={1.25}>
                  <Button variant="contained" onClick={() => setOpenAppointmentDialog(true)}>Book New Appointment</Button>
                  <Button variant="contained" component={RouterLink} to="/pos">Start Walk-In POS</Button>
                  <Button variant="outlined" component={RouterLink} to="/appointments">Open Appointment Board</Button>
                  <Button variant="outlined" component={RouterLink} to="/patients">Find Patient Record</Button>
                </Stack>
              </SectionCard>
            </Grid>
            <Grid size={{ xs: 12, lg: 7 }}>
              <SectionCard title="POS Queue" subtitle="Search open balances and jump straight into checkout.">
                <Stack spacing={1.5}>
                  <TextField
                    label="Search invoice, client, or patient"
                    value={invoiceSearch}
                    onChange={(event) => setInvoiceSearch(event.target.value)}
                  />
                  {unpaidInvoices.slice(0, 6).map((invoice) => (
                    <Box key={invoice.id} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, p: 1.5 }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, alignItems: 'flex-start' }}>
                        <div>
                          <Typography sx={{ fontWeight: 700 }}>{invoice.invoiceNumber}</Typography>
                          <Typography variant="body2" color="text.secondary">
                            {invoice.patient} • {invoice.client}
                          </Typography>
                          <Typography variant="body2">Balance due: ${invoice.balanceDue.toFixed(2)}</Typography>
                        </div>
                        <Button component={RouterLink} to={`/invoices/${invoice.id}`} variant="contained" size="small">
                          Open POS
                        </Button>
                      </Box>
                    </Box>
                  ))}
                  {!unpaidInvoices.length ? (
                    <Typography color="text.secondary">No unpaid invoices matched the current search.</Typography>
                  ) : null}
                </Stack>
              </SectionCard>
            </Grid>
          </Grid>
        </>
      )}

      <Dialog open={openAppointmentDialog} onClose={() => setOpenAppointmentDialog(false)} fullWidth maxWidth="sm">
        <DialogTitle>Book Appointment</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              select
              label="Patient"
              value={form.patientId}
              onChange={(event) => {
                const patient = patientsQuery.data?.items.find((item) => item.id === Number(event.target.value));
                setForm({ ...form, patientId: Number(event.target.value), clientId: patient?.clientId ?? 0 });
              }}
            >
              {patientsQuery.data?.items.map((patient) => (
                <MenuItem key={patient.id} value={patient.id}>{patient.name} • {patient.ownerName}</MenuItem>
              ))}
            </TextField>
            <TextField select label="Clinician" value={form.clinicianUserId} onChange={(event) => setForm({ ...form, clinicianUserId: Number(event.target.value) })}>
              {lookupsQuery.data?.clinicians.map((clinician) => (
                <MenuItem key={clinician.id} value={clinician.id}>{clinician.displayName}</MenuItem>
              ))}
            </TextField>
            <TextField select label="Appointment type" value={form.appointmentTypeId} onChange={(event) => setForm({ ...form, appointmentTypeId: Number(event.target.value) })}>
              {lookupsQuery.data?.appointmentTypes.map((appointmentType) => (
                <MenuItem key={appointmentType.id} value={appointmentType.id}>{appointmentType.name}</MenuItem>
              ))}
            </TextField>
            <TextField label="Start" type="datetime-local" value={form.startAt} onChange={(event) => setForm({ ...form, startAt: event.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
            <TextField label="End" type="datetime-local" value={form.endAt} onChange={(event) => setForm({ ...form, endAt: event.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
            <TextField label="Reason" value={form.reason} onChange={(event) => setForm({ ...form, reason: event.target.value })} />
            <TextField label="Notes" multiline minRows={3} value={form.notes} onChange={(event) => setForm({ ...form, notes: event.target.value })} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenAppointmentDialog(false)}>Cancel</Button>
          <Button variant="contained" onClick={() => createAppointmentMutation.mutate()} disabled={createAppointmentMutation.isPending}>Save</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
