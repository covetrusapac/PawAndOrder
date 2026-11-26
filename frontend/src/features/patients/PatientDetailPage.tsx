import { useQuery } from '@tanstack/react-query';
import { Alert, Grid, List, ListItem, ListItemText, Typography } from '@mui/material';
import { useParams } from 'react-router-dom';
import { SectionCard } from '../../components/SectionCard';
import { StatusChip } from '../../components/StatusChip';
import { api } from '../../lib/api';
import type { PatientDetail } from '../../types/api';

export function PatientDetailPage() {
  const { id } = useParams();
  const { data, isLoading, error } = useQuery({
    queryKey: ['patient', id],
    queryFn: () => api.get<PatientDetail>(`/patients/${id}`),
    enabled: Boolean(id),
  });

  if (isLoading) return <Typography>Loading...</Typography>;
  if (error || !data) return <Alert severity="error">Unable to load patient record.</Alert>;

  return (
    <Grid container spacing={2.5}>
      <Grid size={{ xs: 12, md: 4 }}>
        <SectionCard title={data.name} subtitle={`${data.species}${data.breed ? ` • ${data.breed}` : ''}`}>
          <List disablePadding>
            <ListItem><ListItemText primary="Owner" secondary={data.owner?.displayName ?? '-'} /></ListItem>
            <ListItem><ListItemText primary="Weight" secondary={`${data.weightKg} kg`} /></ListItem>
            <ListItem><ListItemText primary="Alerts" secondary={data.alertsSummary ?? '-'} /></ListItem>
            <ListItem><ListItemText primary="Allergies" secondary={data.allergiesSummary ?? '-'} /></ListItem>
          </List>
        </SectionCard>
      </Grid>
      <Grid size={{ xs: 12, md: 8 }}>
        <SectionCard title="Vaccination Due Dates">
          <List disablePadding>
            {data.vaccinations.map((vaccination) => (
              <ListItem key={vaccination.id} divider>
                <ListItemText primary={vaccination.name} secondary={`Administered ${vaccination.dateAdministered} • Due ${vaccination.dueDate}`} />
              </ListItem>
            ))}
          </List>
        </SectionCard>
      </Grid>
      <Grid size={{ xs: 12, md: 6 }}>
        <SectionCard title="Recent Appointments">
          <List disablePadding>
            {data.appointments.map((appointment) => (
              <ListItem key={appointment.id} divider>
                <ListItemText primary={appointment.reason} secondary={`${appointment.startAt} • ${appointment.clinician}`} />
                <StatusChip value={appointment.status} />
              </ListItem>
            ))}
          </List>
        </SectionCard>
      </Grid>
      <Grid size={{ xs: 12, md: 6 }}>
        <SectionCard title="Invoice History">
          <List disablePadding>
            {data.invoices.map((invoice) => (
              <ListItem key={invoice.id} divider>
                <ListItemText primary={invoice.invoiceNumber} secondary={`$${invoice.totalIncGst.toFixed(2)} inc GST`} />
                <StatusChip value={invoice.status} />
              </ListItem>
            ))}
          </List>
        </SectionCard>
      </Grid>
    </Grid>
  );
}
