import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { SectionCard } from '../../components/SectionCard';
import { api } from '../../lib/api';
import type { ClientListItem, PatientListItem } from '../../types/api';

export function PatientsPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({
    clientId: 0,
    name: '',
    species: 'Dog',
    breed: '',
    sex: '',
    dateOfBirth: null,
    estimatedAgeYears: null,
    color: '',
    microchipNumber: '',
    weightKg: 0,
    isDesexed: false,
    alertsSummary: '',
    allergiesSummary: '',
    notes: '',
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['patients', search],
    queryFn: () => api.get<{ items: PatientListItem[] }>(`/patients${search ? `?search=${encodeURIComponent(search)}` : ''}`),
  });

  const clientsQuery = useQuery({
    queryKey: ['clients', 'lookup'],
    queryFn: () => api.get<{ items: ClientListItem[] }>('/clients'),
  });

  const mutation = useMutation({
    mutationFn: () => api.post('/patients', form),
    onSuccess: async () => {
      setOpen(false);
      await queryClient.invalidateQueries({ queryKey: ['patients'] });
    },
  });

  return (
    <SectionCard
      title="Patients"
      subtitle="Species-specific medical records, due vaccinations, and visit history."
      action={<Button variant="contained" onClick={() => setOpen(true)}>New Patient</Button>}
    >
      <Stack spacing={2}>
        <TextField label="Search patients" value={search} onChange={(event) => setSearch(event.target.value)} />

        {error ? <Alert severity="error">Unable to load patients.</Alert> : null}
        {isLoading ? <Typography>Loading...</Typography> : null}

        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Species</TableCell>
              <TableCell>Owner</TableCell>
              <TableCell>Weight</TableCell>
              <TableCell>Next vaccine due</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.items.map((patient) => (
              <TableRow key={patient.id} hover>
                <TableCell>
                  <Button component={RouterLink} to={`/patients/${patient.id}`}>{patient.name}</Button>
                </TableCell>
                <TableCell>{patient.species}{patient.breed ? ` • ${patient.breed}` : ''}</TableCell>
                <TableCell>{patient.ownerName}</TableCell>
                <TableCell>{patient.weightKg} kg</TableCell>
                <TableCell>{patient.nextVaccinationDue ?? '-'}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Stack>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>New Patient</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField select label="Owner" value={form.clientId} onChange={(event) => setForm({ ...form, clientId: Number(event.target.value) })}>
              {clientsQuery.data?.items.map((client) => (
                <MenuItem key={client.id} value={client.id}>{client.displayName}</MenuItem>
              ))}
            </TextField>
            <TextField label="Patient name" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} />
            <TextField label="Species" value={form.species} onChange={(event) => setForm({ ...form, species: event.target.value })} />
            <TextField label="Breed" value={form.breed} onChange={(event) => setForm({ ...form, breed: event.target.value })} />
            <TextField label="Sex" value={form.sex} onChange={(event) => setForm({ ...form, sex: event.target.value })} />
            <TextField label="Weight (kg)" type="number" value={form.weightKg} onChange={(event) => setForm({ ...form, weightKg: Number(event.target.value) })} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending}>Save</Button>
        </DialogActions>
      </Dialog>
    </SectionCard>
  );
}
