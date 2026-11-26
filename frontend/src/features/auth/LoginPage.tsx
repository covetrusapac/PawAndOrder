import { Alert, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../app/auth';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('PawAndOrder!');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await login(username, password);
      navigate('/dashboard');
    } catch {
      setError('Unable to log in with those demo credentials.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', background: 'linear-gradient(135deg, #16324f 0%, #2d6a4f 100%)', p: 3 }}>
      <Paper sx={{ width: 420, p: 4 }}>
        <Stack spacing={3} component="form" onSubmit={handleSubmit}>
          <div>
            <Typography variant="h4">Paw &amp; Order</Typography>
            <Typography color="text.secondary">
              In the veterinary system, these are their stories.
            </Typography>
          </div>

          {error ? <Alert severity="error">{error}</Alert> : null}

          <TextField label="Username" value={username} onChange={(event) => setUsername(event.target.value)} fullWidth />
          <TextField label="Password" type="password" value={password} onChange={(event) => setPassword(event.target.value)} fullWidth />

          <Button type="submit" variant="contained" size="large" disabled={submitting}>
            Sign In
          </Button>

          <Typography variant="body2" color="text.secondary">
            Demo default: `admin` / `PawAndOrder!`
          </Typography>
        </Stack>
      </Paper>
    </Box>
  );
}
