import { Box, Card, CardContent, Stack, Typography } from '@mui/material';

export function SectionCard({
  title,
  subtitle,
  action,
  children,
}: {
  title: string;
  subtitle?: string;
  action?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <Card>
      <CardContent>
        <Stack spacing={2.5}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 2 }}>
            <div>
              <Typography variant="h6">{title}</Typography>
              {subtitle ? <Typography color="text.secondary">{subtitle}</Typography> : null}
            </div>
            {action}
          </Box>
          {children}
        </Stack>
      </CardContent>
    </Card>
  );
}
