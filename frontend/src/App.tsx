import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined';
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined';
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined';
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined';
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined';
import MedicalInformationOutlinedIcon from '@mui/icons-material/MedicalInformationOutlined';
import PetsOutlinedIcon from '@mui/icons-material/PetsOutlined';
import PointOfSaleOutlinedIcon from '@mui/icons-material/PointOfSaleOutlined';
import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AppBar, Box, Button, CssBaseline, Drawer, List, ListItemButton, ListItemIcon, ListItemText, ThemeProvider, Toolbar, Typography } from '@mui/material';
import { useMemo } from 'react';
import { BrowserRouter, Link as RouterLink, Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import { AuthProvider, useAuth } from './app/auth';
import { appTheme } from './app/theme';
import logo from './assets/logo.png';
import { LoginPage } from './features/auth/LoginPage';
import { AppointmentsPage } from './features/appointments/AppointmentsPage';
import { ClientDetailPage } from './features/clients/ClientDetailPage';
import { ClientsPage } from './features/clients/ClientsPage';
import { ConsultPage } from './features/consults/ConsultPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { InventoryPage } from './features/inventory/InventoryPage';
import { InvoicePage } from './features/invoices/InvoicePage';
import { PatientDetailPage } from './features/patients/PatientDetailPage';
import { PatientsPage } from './features/patients/PatientsPage';
import { PosPage } from './features/pos/PosPage';
import { PurchasingPage } from './features/purchasing/PurchasingPage';
import { ReportsPage } from './features/reports/ReportsPage';

const queryClient = new QueryClient();
const drawerWidth = 280;

const navigation = [
  { label: 'Dashboard', to: '/dashboard', icon: <AssessmentOutlinedIcon /> },
  { label: 'Clients', to: '/clients', icon: <GroupsOutlinedIcon /> },
  { label: 'Patients', to: '/patients', icon: <PetsOutlinedIcon /> },
  { label: 'Appointments', to: '/appointments', icon: <CalendarMonthOutlinedIcon /> },
  { label: 'Walk-In POS', to: '/pos', icon: <PointOfSaleOutlinedIcon /> },
  { label: 'Inventory', to: '/inventory', icon: <Inventory2OutlinedIcon /> },
  { label: 'Purchasing', to: '/purchasing', icon: <LocalShippingOutlinedIcon /> },
  { label: 'Reports', to: '/reports', icon: <ReceiptLongOutlinedIcon /> },
];

function RequireAuth() {
  const { user } = useAuth();
  const location = useLocation();
  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <Outlet />;
}

function AppLayout() {
  const { user, logout } = useAuth();
  const location = useLocation();
  const title = useMemo(() => {
    const item = navigation.find((entry) => location.pathname.startsWith(entry.to));
    return item?.label ?? 'Paw & Order';
  }, [location.pathname]);

  return (
    <Box sx={{ display: 'flex' }}>
      <CssBaseline />
      <AppBar position="fixed" sx={{ width: `calc(100% - ${drawerWidth}px)`, ml: `${drawerWidth}px`, bgcolor: 'background.paper', color: 'text.primary', boxShadow: 'none', borderBottom: '1px solid', borderColor: 'divider' }}>
        <Toolbar sx={{ justifyContent: 'space-between' }}>
          <div>
            <Typography variant="h6">{title}</Typography>
            <Typography variant="body2" color="text.secondary">Paw &amp; Order veterinary practice management prototype</Typography>
          </div>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Typography variant="body2" color="text.secondary">{user?.displayName}</Typography>
            <Button onClick={logout}>Sign Out</Button>
          </Box>
        </Toolbar>
      </AppBar>

      <Drawer variant="permanent" sx={{ width: drawerWidth, flexShrink: 0, '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box', bgcolor: '#10243c', color: '#fff' } }}>
        <Toolbar />
        <Box sx={{ p: 3 }}>
          <Box component="img" src={logo} alt="Paw & Order logo" sx={{ display: 'block', width: '100%', maxWidth: 180, height: 'auto', mb: 2 }} />
          <Typography variant="h5">Paw &amp; Order</Typography>
          <Typography variant="body2" sx={{ opacity: 0.8, mt: 1 }}>
            In the veterinary world, pets are considered family.
            Dedicated vets and nurses investigate mysterious symptoms, enforce strict treat policies, and bring justice to chewed furniture everywhere.
            These are their cases
          </Typography>
        </Box>
        <List>
          {navigation.map((item) => (
            <ListItemButton key={item.to} component={RouterLink} to={item.to} selected={location.pathname.startsWith(item.to)}>
              <ListItemIcon sx={{ color: 'inherit' }}>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} />
            </ListItemButton>
          ))}
        </List>
        <Box sx={{ mt: 'auto', p: 3 }}>
          <Button component={RouterLink} to="/consults/1" startIcon={<MedicalInformationOutlinedIcon />} fullWidth variant="outlined" sx={{ color: '#fff', borderColor: 'rgba(255,255,255,0.2)' }}>
            Open Demo Consult
          </Button>
        </Box>
      </Drawer>

      <Box component="main" sx={{ flexGrow: 1, p: 3.5, bgcolor: 'background.default', minHeight: '100vh' }}>
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
}

export default function App() {
  return (
    <ThemeProvider theme={appTheme}>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <BrowserRouter>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route element={<RequireAuth />}>
                <Route element={<AppLayout />}>
                  <Route path="/" element={<Navigate to="/dashboard" replace />} />
                  <Route path="/dashboard" element={<DashboardPage />} />
                  <Route path="/clients" element={<ClientsPage />} />
                  <Route path="/clients/:id" element={<ClientDetailPage />} />
                  <Route path="/patients" element={<PatientsPage />} />
                  <Route path="/patients/:id" element={<PatientDetailPage />} />
                  <Route path="/appointments" element={<AppointmentsPage />} />
                  <Route path="/pos" element={<PosPage />} />
                  <Route path="/consults/:id" element={<ConsultPage />} />
                  <Route path="/invoices/:id" element={<InvoicePage />} />
                  <Route path="/inventory" element={<InventoryPage />} />
                  <Route path="/purchasing" element={<PurchasingPage />} />
                  <Route path="/reports" element={<ReportsPage />} />
                </Route>
              </Route>
            </Routes>
          </BrowserRouter>
        </AuthProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}
