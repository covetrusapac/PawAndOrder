import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { SectionCard } from '../../components/SectionCard';
import { api } from '../../lib/api';
import type { ClientListItem } from '../../types/api';

const blankClient = {
  firstName: '',
  lastName: '',
  phone: '',
  email: '',
  addressLine1: '',
  addressLine2: '',
  suburb: '',
  state: '',
  postcode: '',
  notes: '',
};

type ClientForm = typeof blankClient;

const australianStates = ['NSW', 'VIC', 'QLD', 'SA', 'WA', 'TAS', 'NT', 'ACT'];

// Mirrors ClientUpsertRequestValidator in backend Contracts.cs; the API remains the source of truth.
function validate(form: ClientForm): Partial<Record<keyof ClientForm, string>> {
  const errors: Partial<Record<keyof ClientForm, string>> = {};
  const required: (keyof ClientForm)[] = ['firstName', 'lastName', 'phone', 'email', 'addressLine1', 'suburb', 'state', 'postcode'];
  for (const key of required) {
    if (!form[key].trim()) errors[key] = 'Required';
  }
  if (!errors.phone && !/^(?:\+61|0)[2-478](?:\s?\d){8}$/.test(form.phone.trim())) errors.phone = 'Valid Australian number, e.g. 0412 345 678';
  if (!errors.email && !/^[^@\s]+@[^@\s]+$/.test(form.email.trim())) errors.email = 'Valid email address';
  if (!errors.postcode && !/^\d{4}$/.test(form.postcode.trim())) errors.postcode = '4 digits';
  return errors;
}

function errorMessage(error: Error): string {
  try {
    const problem = JSON.parse(error.message) as { errors?: Record<string, string[]> };
    if (problem.errors) return Object.values(problem.errors).flat().join(' ');
  } catch {
    // not JSON, fall through
  }
  return error.message;
}

export function ClientsPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState(blankClient);
  const [submitted, setSubmitted] = useState(false);
  const errors = submitted ? validate(form) : {};

  const fieldProps = (key: keyof ClientForm) => ({
    value: form[key],
    onChange: (event: React.ChangeEvent<HTMLInputElement>) => setForm({ ...form, [key]: event.target.value }),
    error: Boolean(errors[key]),
    helperText: errors[key],
  });

  const close = () => {
    setOpen(false);
    setSubmitted(false);
    mutation.reset();
  };

  const save = () => {
    setSubmitted(true);
    if (Object.keys(validate(form)).length === 0) mutation.mutate();
  };

  const { data, isLoading, error } = useQuery({
    queryKey: ['clients', search],
    queryFn: () => api.get<{ items: ClientListItem[] }>(`/clients${search ? `?search=${encodeURIComponent(search)}` : ''}`),
  });

  const mutation = useMutation({
    mutationFn: () => api.post('/clients', form),
    onSuccess: async () => {
      close();
      setForm(blankClient);
      await queryClient.invalidateQueries({ queryKey: ['clients'] });
    },
  });

  return (
    <SectionCard
      title="Clients"
      subtitle="Owners, contacts, and household-level billing context."
      action={<Button variant="contained" onClick={() => setOpen(true)}>New Client</Button>}
    >
      <Stack spacing={2}>
        <TextField label="Search clients" value={search} onChange={(event) => setSearch(event.target.value)} />

        {error ? <Alert severity="error">Unable to load clients.</Alert> : null}
        {isLoading ? <Typography>Loading...</Typography> : null}

        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Phone</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>Patients</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.items.map((client) => (
              <TableRow key={client.id} hover>
                <TableCell>
                  <Button component={RouterLink} to={`/clients/${client.id}`}>{client.displayName}</Button>
                </TableCell>
                <TableCell>{client.phone}</TableCell>
                <TableCell>{client.email ?? '-'}</TableCell>
                <TableCell>{client.patientCount}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Stack>

      <Dialog open={open} onClose={close} fullWidth maxWidth="sm">
        <DialogTitle>New Client</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {mutation.isError ? <Alert severity="error">{errorMessage(mutation.error)}</Alert> : null}
            <TextField label="First name" required {...fieldProps('firstName')} />
            <TextField label="Last name" required {...fieldProps('lastName')} />
            <TextField label="Phone" required {...fieldProps('phone')} />
            <TextField label="Email" type="email" required {...fieldProps('email')} />
            <TextField label="Address line 1" required {...fieldProps('addressLine1')} />
            <TextField label="Suburb" required {...fieldProps('suburb')} />
            <TextField label="State" select required {...fieldProps('state')}>
              {australianStates.map((state) => <MenuItem key={state} value={state}>{state}</MenuItem>)}
            </TextField>
            <TextField label="Postcode" required {...fieldProps('postcode')} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={close}>Cancel</Button>
          <Button variant="contained" onClick={save} disabled={mutation.isPending}>Save</Button>
        </DialogActions>
      </Dialog>
    </SectionCard>
  );
}
