import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Button, Checkbox, FormControlLabel, Grid, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { Link as RouterLink, useParams } from 'react-router-dom';
import { SectionCard } from '../../components/SectionCard';
import { StatusChip } from '../../components/StatusChip';
import { api } from '../../lib/api';
import type { ConsultDetail, LookupResponse, ProductListItem } from '../../types/api';

export function ConsultPage() {
  const { id } = useParams();
  const queryClient = useQueryClient();
  const [form, setForm] = useState({
    appointmentId: 0,
    patientId: 0,
    clinicianUserId: 0,
    visitReason: '',
    presentingComplaint: '',
    subjectiveNotes: '',
    objectiveNotes: '',
    assessmentNotes: '',
    planNotes: '',
    followUpNotes: '',
    weightKg: 0,
    temperatureC: 0,
    heartRateBpm: 0,
    respiratoryRateBpm: 0,
  });
  const [diagnosisText, setDiagnosisText] = useState('');
  const [diagnosisId, setDiagnosisId] = useState<number | ''>('');
  const [saveDiagnosis, setSaveDiagnosis] = useState(false);
  const [procedureText, setProcedureText] = useState('');
  const [procedureId, setProcedureId] = useState<number | ''>('');
  const [medProductId, setMedProductId] = useState<number | ''>('');
  const [medQuantity, setMedQuantity] = useState(1);
  const [medInstructions, setMedInstructions] = useState('');
  const [vaxTypeId, setVaxTypeId] = useState<number | ''>('');
  const [vaxProductId, setVaxProductId] = useState<number | ''>('');

  const consultQuery = useQuery({
    queryKey: ['consult', id],
    queryFn: () => api.get<ConsultDetail>(`/consults/${id}`),
    enabled: Boolean(id),
  });

  const lookupsQuery = useQuery({
    queryKey: ['lookups'],
    queryFn: () => api.get<LookupResponse>('/lookups/bootstrap'),
  });

  const productsQuery = useQuery({
    queryKey: ['products'],
    queryFn: () => api.get<{ items: ProductListItem[] }>('/products'),
  });

  useEffect(() => {
    if (!consultQuery.data) {
      return;
    }

    setForm({
      appointmentId: consultQuery.data.appointmentId,
      patientId: consultQuery.data.patientId,
      clinicianUserId: consultQuery.data.clinicianUserId,
      visitReason: consultQuery.data.visitReason,
      presentingComplaint: consultQuery.data.presentingComplaint ?? '',
      subjectiveNotes: consultQuery.data.subjectiveNotes ?? '',
      objectiveNotes: consultQuery.data.objectiveNotes ?? '',
      assessmentNotes: consultQuery.data.assessmentNotes ?? '',
      planNotes: consultQuery.data.planNotes ?? '',
      followUpNotes: consultQuery.data.followUpNotes ?? '',
      weightKg: consultQuery.data.weightKg ?? consultQuery.data.patient?.weightKg ?? 0,
      temperatureC: consultQuery.data.temperatureC ?? 0,
      heartRateBpm: consultQuery.data.heartRateBpm ?? 0,
      respiratoryRateBpm: consultQuery.data.respiratoryRateBpm ?? 0,
    });
  }, [consultQuery.data]);

  const saveMutation = useMutation({
    mutationFn: () => api.put(`/consults/${id}`, form),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['consult', id] }),
  });

  const diagnosisMutation = useMutation({
    mutationFn: () => api.post(`/consults/${id}/diagnoses`, { diagnosisCatalogItemId: diagnosisId || null, diagnosisText, saveToCatalog: saveDiagnosis }),
    onSuccess: async () => {
      setDiagnosisText('');
      setDiagnosisId('');
      setSaveDiagnosis(false);
      await queryClient.invalidateQueries({ queryKey: ['consult', id] });
      await queryClient.invalidateQueries({ queryKey: ['lookups'] });
    },
  });

  const procedureMutation = useMutation({
    mutationFn: () => api.post(`/consults/${id}/procedures`, { procedureCatalogItemId: procedureId || null, procedureText, quantity: 1, notes: '', saveToCatalog: false, defaultProductId: null }),
    onSuccess: async () => {
      setProcedureText('');
      setProcedureId('');
      await queryClient.invalidateQueries({ queryKey: ['consult', id] });
    },
  });

  const medicationMutation = useMutation({
    mutationFn: () => api.post(`/consults/${id}/medications`, { productId: medProductId, inventoryLotId: null, quantity: medQuantity, doseInstructions: medInstructions, notes: '' }),
    onSuccess: async () => {
      setMedProductId('');
      setMedQuantity(1);
      setMedInstructions('');
      await queryClient.invalidateQueries({ queryKey: ['consult', id] });
      await queryClient.invalidateQueries({ queryKey: ['inventory'] });
    },
  });

  const vaccinationMutation = useMutation({
    mutationFn: () => api.post(`/consults/${id}/vaccinations`, {
      vaccinationTypeId: vaxTypeId,
      productId: vaxProductId || null,
      inventoryLotId: null,
      dateAdministered: new Date().toISOString().slice(0, 10),
      dueDate: new Date(new Date().setFullYear(new Date().getFullYear() + 1)).toISOString().slice(0, 10),
      notes: '',
    }),
    onSuccess: async () => {
      setVaxTypeId('');
      setVaxProductId('');
      await queryClient.invalidateQueries({ queryKey: ['consult', id] });
    },
  });

  const finalizeMutation = useMutation({
    mutationFn: () => api.post(`/consults/${id}/finalize`),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['consult', id] }),
  });

  const generateInvoiceMutation = useMutation({
    mutationFn: () => api.post<{ id: number; invoiceNumber: string }>(`/consults/${id}/generate-invoice`),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['consult', id] }),
  });

  if (consultQuery.isLoading) return <Typography>Loading...</Typography>;
  if (consultQuery.error || !consultQuery.data) return <Alert severity="error">Unable to load consult.</Alert>;

  const consult = consultQuery.data;
  const medicationProducts = (productsQuery.data?.items ?? []).filter((product) => product.trackInventory);

  return (
    <Stack spacing={2.5}>
      <SectionCard
        title={`${consult.patient?.name ?? 'Patient'} Consult`}
        subtitle={`${consult.patient?.species ?? ''}${consult.patient?.breed ? ` • ${consult.patient.breed}` : ''} • ${consult.patient?.owner ?? ''}`}
        action={<StatusChip value={consult.status} />}
      >
        <Stack direction="row" spacing={1}>
          <Button variant="contained" onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}>Save Consult</Button>
          <Button variant="outlined" onClick={() => finalizeMutation.mutate()} disabled={finalizeMutation.isPending}>Finalize</Button>
          <Button variant="outlined" onClick={() => generateInvoiceMutation.mutate()} disabled={generateInvoiceMutation.isPending}>Generate Invoice</Button>
          {consult.invoice ? <Button component={RouterLink} to={`/invoices/${consult.invoice.id}`}>Open {consult.invoice.invoiceNumber}</Button> : null}
        </Stack>
      </SectionCard>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <SectionCard title="Structured Consult">
            <Stack spacing={2}>
              <TextField label="Visit reason" value={form.visitReason} onChange={(event) => setForm({ ...form, visitReason: event.target.value })} />
              <TextField label="Presenting complaint" value={form.presentingComplaint} onChange={(event) => setForm({ ...form, presentingComplaint: event.target.value })} />
              <TextField label="Subjective" multiline minRows={3} value={form.subjectiveNotes} onChange={(event) => setForm({ ...form, subjectiveNotes: event.target.value })} />
              <TextField label="Objective" multiline minRows={3} value={form.objectiveNotes} onChange={(event) => setForm({ ...form, objectiveNotes: event.target.value })} />
              <TextField label="Assessment" multiline minRows={3} value={form.assessmentNotes} onChange={(event) => setForm({ ...form, assessmentNotes: event.target.value })} />
              <TextField label="Plan" multiline minRows={3} value={form.planNotes} onChange={(event) => setForm({ ...form, planNotes: event.target.value })} />
              <TextField label="Follow-up" multiline minRows={3} value={form.followUpNotes} onChange={(event) => setForm({ ...form, followUpNotes: event.target.value })} />
            </Stack>
          </SectionCard>
        </Grid>
        <Grid size={{ xs: 12, lg: 4 }}>
          <SectionCard title="Vitals">
            <Stack spacing={2}>
              <TextField label="Weight (kg)" type="number" value={form.weightKg} onChange={(event) => setForm({ ...form, weightKg: Number(event.target.value) })} />
              <TextField label="Temperature (C)" type="number" value={form.temperatureC} onChange={(event) => setForm({ ...form, temperatureC: Number(event.target.value) })} />
              <TextField label="Heart rate (bpm)" type="number" value={form.heartRateBpm} onChange={(event) => setForm({ ...form, heartRateBpm: Number(event.target.value) })} />
              <TextField label="Respiratory rate (bpm)" type="number" value={form.respiratoryRateBpm} onChange={(event) => setForm({ ...form, respiratoryRateBpm: Number(event.target.value) })} />
            </Stack>
          </SectionCard>
        </Grid>
      </Grid>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 6 }}>
          <SectionCard title="Diagnoses" subtitle="Pick from the starter library or create a new clinic-specific entry.">
            <Stack spacing={1.5}>
              <TextField select label="Existing diagnosis" value={diagnosisId} onChange={(event) => setDiagnosisId(Number(event.target.value))}>
                <MenuItem value="">None</MenuItem>
                {lookupsQuery.data?.diagnoses.map((diagnosis) => (
                  <MenuItem key={diagnosis.id} value={diagnosis.id}>{diagnosis.name}</MenuItem>
                ))}
              </TextField>
              <TextField label="Diagnosis text" value={diagnosisText} onChange={(event) => setDiagnosisText(event.target.value)} />
              <FormControlLabel control={<Checkbox checked={saveDiagnosis} onChange={(event) => setSaveDiagnosis(event.target.checked)} />} label="Save as reusable clinic diagnosis" />
              <Button variant="outlined" onClick={() => diagnosisMutation.mutate()}>Add Diagnosis</Button>
              {consult.diagnoses.map((diagnosis) => <Typography key={diagnosis.id}>• {diagnosis.diagnosisText}</Typography>)}
            </Stack>
          </SectionCard>
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <SectionCard title="Procedures">
            <Stack spacing={1.5}>
              <TextField select label="Existing procedure" value={procedureId} onChange={(event) => setProcedureId(Number(event.target.value))}>
                <MenuItem value="">None</MenuItem>
                {lookupsQuery.data?.procedures.map((procedure) => (
                  <MenuItem key={procedure.id} value={procedure.id}>{procedure.name}</MenuItem>
                ))}
              </TextField>
              <TextField label="Procedure text" value={procedureText} onChange={(event) => setProcedureText(event.target.value)} />
              <Button variant="outlined" onClick={() => procedureMutation.mutate()}>Add Procedure</Button>
              {consult.procedures.map((procedure) => <Typography key={procedure.id}>• {procedure.procedureText}</Typography>)}
            </Stack>
          </SectionCard>
        </Grid>
      </Grid>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 6 }}>
          <SectionCard title="Dispensed Medications">
            <Stack spacing={1.5}>
              <TextField select label="Product" value={medProductId} onChange={(event) => setMedProductId(Number(event.target.value))}>
                <MenuItem value="">Select a product</MenuItem>
                {medicationProducts.map((product) => (
                  <MenuItem key={product.id} value={product.id}>{product.name}</MenuItem>
                ))}
              </TextField>
              <TextField label="Quantity" type="number" value={medQuantity} onChange={(event) => setMedQuantity(Number(event.target.value))} />
              <TextField label="Dose instructions" value={medInstructions} onChange={(event) => setMedInstructions(event.target.value)} />
              <Button variant="outlined" onClick={() => medicationMutation.mutate()}>Dispense</Button>
              {consult.medications.map((medication) => (
                <Typography key={medication.id}>• {medication.productName} x {medication.quantity} {medication.lot ? `(${medication.lot})` : ''}</Typography>
              ))}
            </Stack>
          </SectionCard>
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <SectionCard title="Vaccinations">
            <Stack spacing={1.5}>
              <TextField select label="Vaccination type" value={vaxTypeId} onChange={(event) => setVaxTypeId(Number(event.target.value))}>
                <MenuItem value="">Select a vaccination</MenuItem>
                {lookupsQuery.data?.vaccinationTypes.map((vaccination) => (
                  <MenuItem key={vaccination.id} value={vaccination.id}>{vaccination.name}</MenuItem>
                ))}
              </TextField>
              <TextField select label="Vaccine stock product" value={vaxProductId} onChange={(event) => setVaxProductId(Number(event.target.value))}>
                <MenuItem value="">Optional stock item</MenuItem>
                {medicationProducts.map((product) => (
                  <MenuItem key={product.id} value={product.id}>{product.name}</MenuItem>
                ))}
              </TextField>
              <Button variant="outlined" onClick={() => vaccinationMutation.mutate()}>Record Vaccination</Button>
              {consult.vaccinations.map((vaccination) => (
                <Typography key={vaccination.id}>• {vaccination.name} due {vaccination.dueDate}</Typography>
              ))}
            </Stack>
          </SectionCard>
        </Grid>
      </Grid>
    </Stack>
  );
}
