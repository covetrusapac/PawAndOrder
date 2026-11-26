import { useQuery } from '@tanstack/react-query';
import { Alert, Grid, List, ListItem, ListItemText, Typography } from '@mui/material';
import { useParams } from 'react-router-dom';
import { SectionCard } from '../../components/SectionCard';
import { StatusChip } from '../../components/StatusChip';
import { api } from '../../lib/api';
import type { ClientDetail } from '../../types/api';

export function ClientDetailPage() {
  const { id } = useParams();
  const { data, isLoading, error } = useQuery({
    queryKey: ['client', id],
    queryFn: () => api.get<ClientDetail>(`/clients/${id}`),
    enabled: Boolean(id),
  });

  if (isLoading) return <Typography>Loading...</Typography>;
  if (error || !data) return <Alert severity="error">Unable to load client record.</Alert>;

  return (
    <Grid container spacing={2.5}>
      <Grid size={{ xs: 12, md: 5 }}>
        <SectionCard title={data.displayName} subtitle={data.phone}>
          <List disablePadding>
            <ListItem><ListItemText primary="Email" secondary={data.email ?? '-'} /></ListItem>
            <ListItem><ListItemText primary="Address" secondary={[data.addressLine1, data.suburb, data.state, data.postcode].filter(Boolean).join(', ') || '-'} /></ListItem>
            <ListItem><ListItemText primary="Notes" secondary={data.notes ?? '-'} /></ListItem>
          </List>
        </SectionCard>
      </Grid>
      <Grid size={{ xs: 12, md: 7 }}>
        <SectionCard title="Patients">
          <List disablePadding>
            {data.patients.map((patient) => (
              <ListItem key={patient.id} divider>
                <ListItemText primary={patient.name} secondary={`${patient.species}${patient.breed ? ` • ${patient.breed}` : ''}`} />
                <Typography>{patient.weightKg} kg</Typography>
              </ListItem>
            ))}
          </List>
        </SectionCard>
      </Grid>
      <Grid size={12}>
        <SectionCard title="Recent Invoices">
          <List disablePadding>
            {data.invoices.map((invoice) => (
              <ListItem key={invoice.id} divider>
                <ListItemText primary={invoice.invoiceNumber} secondary={`Total inc GST: $${invoice.totalIncGst.toFixed(2)}`} />
                <StatusChip value={invoice.status} />
              </ListItem>
            ))}
          </List>
        </SectionCard>
      </Grid>
    </Grid>
  );
}
