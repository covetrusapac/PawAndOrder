export type UserRole = 'Admin' | 'Veterinarian' | 'Nurse' | 'Receptionist' | 'InventoryManager';
export type AppointmentStatus = 'Booked' | 'Arrived' | 'InConsult' | 'Completed' | 'Cancelled' | 'NoShow';
export type ConsultStatus = 'Draft' | 'Finalized';
export type InvoiceStatus = 'Draft' | 'Issued' | 'Paid' | 'PartiallyPaid' | 'Void';
export type PaymentMethod = 'Cash' | 'Card';
export type ProductType = 'Service' | 'Medication' | 'RetailItem' | 'Consumable';

export interface SessionUser {
  id: number;
  username: string;
  displayName: string;
  role: UserRole;
}

export interface DashboardResponse {
  metrics: {
    todaysAppointments: number;
    arrivalsWaiting: number;
    inConsult: number;
    openInvoices: number;
  };
  todaysAppointments: Array<{
    id: number;
    startAt: string;
    endAt: string;
    status: AppointmentStatus;
    reason: string;
    notes?: string | null;
    patient: string;
    client: string;
    clinician: string;
    consultId?: number | null;
  }>;
}

export interface InvoiceListItem {
  id: number;
  invoiceNumber: string;
  status: InvoiceStatus;
  totalIncGst: number;
  balanceDue: number;
  issuedAt?: string | null;
  patient: string;
  client: string;
}

export interface InvoiceListResponse {
  items: InvoiceListItem[];
}

export interface ProductListItem {
  id: number;
  sku: string;
  name: string;
  type: ProductType;
  trackInventory: boolean;
  defaultSalePriceExGst: number;
  reorderThreshold: number;
  quantityOnHand: number;
}

export interface InventoryStockItem {
  id: number;
  sku: string;
  name: string;
  type: ProductType;
  reorderThreshold: number;
  quantityOnHand: number;
  nextExpiry?: string | null;
  lowStock: boolean;
}

export interface PurchaseOrderListItem {
  id: number;
  orderNumber: string;
  vendor: string;
  status: string;
  orderedAt: string;
  expectedAt?: string | null;
  totalLines: number;
  totalOrdered: number;
  totalReceived: number;
}

export interface VendorListItem {
  id: number;
  name: string;
  phone?: string | null;
  email?: string | null;
  contactName?: string | null;
  openOrders: number;
}

export interface LookupResponse {
  clinicians: Array<{ id: number; displayName: string }>;
  appointmentTypes: Array<{ id: number; name: string; durationMinutes: number; colorHex: string }>;
  diagnoses: Array<{ id: number; name: string; code?: string | null }>;
  procedures: Array<{ id: number; name: string; code?: string | null; defaultProductId?: number | null }>;
  vaccinationTypes: Array<{ id: number; name: string; species: string; defaultIntervalMonths: number }>;
}

export interface ClientListItem {
  id: number;
  firstName: string;
  lastName: string;
  displayName: string;
  phone: string;
  email?: string | null;
  patientCount: number;
}

export interface ClientDetail {
  id: number;
  firstName: string;
  lastName: string;
  displayName: string;
  phone: string;
  email?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  suburb?: string | null;
  state?: string | null;
  postcode?: string | null;
  notes?: string | null;
  patients: Array<{ id: number; name: string; species: string; breed?: string | null; weightKg: number }>;
  invoices: Array<{ id: number; invoiceNumber: string; status: InvoiceStatus; totalIncGst: number; balanceDue: number; issuedAt?: string | null }>;
}

export interface PatientListItem {
  id: number;
  clientId: number;
  name: string;
  species: string;
  breed?: string | null;
  weightKg: number;
  ownerName: string;
  nextVaccinationDue?: string | null;
}

export interface PatientDetail {
  id: number;
  name: string;
  species: string;
  breed?: string | null;
  sex?: string | null;
  dateOfBirth?: string | null;
  estimatedAgeYears?: number | null;
  color?: string | null;
  microchipNumber?: string | null;
  weightKg: number;
  isDesexed: boolean;
  alertsSummary?: string | null;
  allergiesSummary?: string | null;
  notes?: string | null;
  owner?: { id: number; displayName: string; phone: string } | null;
  appointments: Array<{ id: number; startAt: string; status: AppointmentStatus; reason: string; clinician: string }>;
  consults: Array<{ id: number; visitReason: string; status: ConsultStatus; startedAt: string; completedAt?: string | null }>;
  invoices: Array<{ id: number; invoiceNumber: string; status: InvoiceStatus; totalIncGst: number; balanceDue: number; issuedAt?: string | null }>;
  vaccinations: Array<{ id: number; name: string; dateAdministered: string; dueDate: string; notes?: string | null }>;
}

export interface AppointmentListItem {
  id: number;
  startAt: string;
  endAt: string;
  status: AppointmentStatus;
  reason: string;
  notes?: string | null;
  patient: string;
  client: string;
  clinician: string;
  appointmentType: string;
  appointmentTypeColor: string;
  consultId?: number | null;
}

export interface ConsultDetail {
  id: number;
  appointmentId: number;
  patientId: number;
  clinicianUserId: number;
  visitReason: string;
  presentingComplaint?: string | null;
  subjectiveNotes?: string | null;
  objectiveNotes?: string | null;
  assessmentNotes?: string | null;
  planNotes?: string | null;
  followUpNotes?: string | null;
  weightKg?: number | null;
  temperatureC?: number | null;
  heartRateBpm?: number | null;
  respiratoryRateBpm?: number | null;
  status: ConsultStatus;
  startedAt: string;
  completedAt?: string | null;
  patient?: {
    id: number;
    name: string;
    species: string;
    breed?: string | null;
    weightKg: number;
    owner?: string | null;
  } | null;
  clinician?: string | null;
  diagnoses: Array<{ id: number; diagnosisCatalogItemId?: number | null; diagnosisText: string }>;
  procedures: Array<{ id: number; procedureCatalogItemId?: number | null; procedureText: string; quantity: number; notes?: string | null }>;
  medications: Array<{ id: number; productId: number; productName: string; inventoryLotId?: number | null; lot?: string | null; quantity: number; doseInstructions?: string | null; notes?: string | null }>;
  vaccinations: Array<{ id: number; vaccinationTypeId: number; name: string; dateAdministered: string; dueDate: string; inventoryLotId?: number | null; notes?: string | null }>;
  invoice?: { id: number; invoiceNumber: string; status: InvoiceStatus; totalIncGst: number } | null;
}

export interface InvoiceDetail {
  id: number;
  invoiceNumber: string;
  status: InvoiceStatus;
  issuedAt?: string | null;
  subtotalExGst: number;
  totalGst: number;
  totalIncGst: number;
  amountPaid: number;
  balanceDue: number;
  client?: string | null;
  patient?: string | null;
  lines: Array<{ id: number; description: string; lineType: string; quantity: number; unitPriceExGst: number; lineSubtotalExGst: number; gstAmount: number; lineTotalIncGst: number; lot?: string | null }>;
  payments: Array<{ id: number; paymentMethod: PaymentMethod; amount: number; paidAt: string; reference?: string | null; notes?: string | null }>;
}
