import { Chip } from '@mui/material';

const colorMap: Record<string, 'default' | 'primary' | 'success' | 'warning' | 'error'> = {
  Booked: 'primary',
  Arrived: 'warning',
  InConsult: 'warning',
  Completed: 'success',
  Cancelled: 'error',
  NoShow: 'error',
  Draft: 'default',
  Issued: 'primary',
  Paid: 'success',
  PartiallyPaid: 'warning',
  Void: 'error',
  Submitted: 'primary',
  Received: 'success',
  PartiallyReceived: 'warning',
};

export function StatusChip({ value }: { value: string }) {
  return <Chip size="small" label={value} color={colorMap[value] ?? 'default'} variant="outlined" />;
}
