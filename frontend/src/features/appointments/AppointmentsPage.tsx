import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Grid, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { format } from 'date-fns';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { SectionCard } from '../../components/SectionCard';
import { StatusChip } from '../../components/StatusChip';
import { api } from '../../lib/api';
import type { AppointmentListItem, LookupResponse, PatientListItem } from '../../types/api';

export function AppointmentsPage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [day, setDay] = useState(format(new Date(), 'yyyy-MM-dd'));
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

  const appointmentsQuery = useQuery({
    queryKey: ['appointments', day],
    queryFn: () => api.get<{ items: AppointmentListItem[] }>(`/appointments?from=${encodeURIComponent(`${day}T00:00:00Z`)}&to=${encodeURIComponent(`${day}T23:59:59Z`)}`),
  });

  const lookupsQuery = useQuery({
    queryKey: ['lookups'],
    queryFn: () => api.get<LookupResponse>('/lookups/bootstrap'),
  });

  const patientsQuery = useQuery({
    queryKey: ['patients', 'lookup'],
    queryFn: () => api.get<{ items: PatientListItem[] }>('/patients'),
  });

  const createMutation = useMutation({
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
      setOpen(false);
      await queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });

  const arriveMutation = useMutation({
    mutationFn: (appointmentId: number) => api.post(`/appointments/${appointmentId}/arrive`),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['appointments'] }),
  });

  const startConsultMutation = useMutation({
    mutationFn: (appointmentId: number) => api.post<{ consultId: number }>(`/appointments/${appointmentId}/start-consult`),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: ['appointments'] });
      navigate(`/consults/${data.consultId}`);
    },
  });

  const groupedByClinician = useMemo(() => {
    const items = appointmentsQuery.data?.items ?? [];
    return (lookupsQuery.data?.clinicians ?? []).map((clinician) => ({
      clinician,
      appointments: items.filter((item) => item.clinician === clinician.displayName),
    }));
  }, [appointmentsQuery.data?.items, lookupsQuery.data?.clinicians]);

  return (
    <Stack spacing={2.5}>
      <SectionCard
        title="Clinician Schedule"
        subtitle="Desktop day view grouped by veterinarian."
        action={<Button variant="contained" onClick={() => setOpen(true)}>Book Appointment</Button>}
      >
        <TextField type="date" label="Day" value={day} onChange={(event) => setDay(event.target.value)} sx={{ maxWidth: 220 }} slotProps={{ inputLabel: { shrink: true } }} />
      </SectionCard>

      {appointmentsQuery.error ? <Alert severity="error">Unable to load appointments.</Alert> : null}

      <Grid container spacing={2.5}>
        {groupedByClinician.map(({ clinician, appointments }) => (
          <Grid key={clinician.id} size={{ xs: 12, md: 6 }}>
            <SectionCard title={clinician.displayName}>
              <Stack spacing={1.25}>
                {appointments.map((appointment) => (
                  <Box key={appointment.id} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, p: 1.5, borderLeft: `6px solid ${appointment.appointmentTypeColor}` }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2 }}>
                      <div>
                        <Typography sx={{ fontWeight: 700 }}>{appointment.patient}</Typography>
                        <Typography variant="body2" color="text.secondary">
                          {format(new Date(appointment.startAt), 'p')} to {format(new Date(appointment.endAt), 'p')} • {appointment.appointmentType}
                        </Typography>
                        <Typography variant="body2">{appointment.reason}</Typography>
                      </div>
                      <StatusChip value={appointment.status} />
                    </Box>
                    <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
                      <Button size="small" variant="outlined" onClick={() => arriveMutation.mutate(appointment.id)}>Arrive</Button>
                      <Button size="small" variant="contained" onClick={() => startConsultMutation.mutate(appointment.id)}>
                        {appointment.consultId ? 'Open Consult' : 'Start Consult'}
                      </Button>
                    </Stack>
                  </Box>
                ))}
              </Stack>
            </SectionCard>
          </Grid>
        ))}
      </Grid>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
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
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={() => createMutation.mutate()} disabled={createMutation.isPending}>Save</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
