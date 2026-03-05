# 🎨 SECCIÓN 6: Vistas Faltantes en Frontend

**Fecha:** 2 Enero 2026  
**Objetivo:** Identificar páginas y componentes UI completamente ausentes

---

## 📊 RESUMEN EJECUTIVO

| Categoría | Cantidad |
|-----------|----------|
| **Páginas Faltantes** | 15 páginas |
| **Componentes Faltantes** | 32 componentes |
| **Dashboards Faltantes** | 4 dashboards |
| **Esfuerzo Total** | 168-216 horas |
| **Prioridad Alta** | 8 páginas (72-92h) |
| **Prioridad Media** | 5 páginas (58-74h) |
| **Prioridad Baja** | 2 páginas (38-50h) |

---

## 🔴 PRIORIDAD ALTA - Páginas Críticas Faltantes

### 1. Notification Center (CRÍTICO)

**Estado:** ❌ No existe

#### Componentes a Crear

##### A. NotificationBell Component
```tsx
// components/NotificationBell.tsx
interface NotificationBellProps {
  userId: string;
}

export const NotificationBell = ({ userId }: NotificationBellProps) => {
  const [unreadCount, setUnreadCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const { notifications, markAsRead } = useNotifications(userId);
  
  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <PopoverTrigger>
        <Button variant="ghost" size="icon" className="relative">
          <BellIcon className="h-5 w-5" />
          {unreadCount > 0 && (
            <Badge className="absolute -top-1 -right-1 h-5 w-5 p-0">
              {unreadCount > 9 ? '9+' : unreadCount}
            </Badge>
          )}
        </Button>
      </PopoverTrigger>
      
      <PopoverContent className="w-80">
        <NotificationList
          notifications={notifications}
          onMarkAsRead={markAsRead}
          onViewAll={() => navigate('/notifications')}
        />
      </PopoverContent>
    </Popover>
  );
};
```

**Features:**
- Badge con count de no leídos
- Dropdown con últimas 5 notificaciones
- Click para marcar como leída
- Link a página completa de notificaciones
- Real-time updates con SignalR
- Sound/vibration opcional

**Esfuerzo:** 8-10 horas

---

##### B. NotificationsPage (Full Page)
```tsx
// pages/user/NotificationsPage.tsx
export const NotificationsPage = () => {
  const [filter, setFilter] = useState<'all' | 'unread' | 'read'>('all');
  const [category, setCategory] = useState<'all' | 'messages' | 'listings' | 'system'>('all');
  
  return (
    <PageLayout>
      <PageHeader
        title="Notifications"
        actions={
          <Button onClick={handleMarkAllAsRead}>
            Mark All as Read
          </Button>
        }
      />
      
      <NotificationFilters
        filter={filter}
        category={category}
        onFilterChange={setFilter}
        onCategoryChange={setCategory}
      />
      
      <NotificationList
        notifications={filteredNotifications}
        grouped
        onMarkAsRead={handleMarkAsRead}
        onDelete={handleDelete}
      />
      
      <NotificationSettings
        preferences={preferences}
        onUpdatePreferences={handleUpdatePreferences}
      />
    </PageLayout>
  );
};
```

**Secciones:**
- Lista de notificaciones paginada
- Filtros (all, unread, read)
- Categorías (messages, listings, system)
- Notification settings
- Email/SMS preferences

**Esfuerzo:** 10-12 horas

---

**Total Notification Center:** 18-22 horas  
**Impacto:** 🔴 **MUY ALTO** - Core UX feature

---

### 2. Real Estate Management Dashboard

**Estado:** ❌ No existe (páginas browse/detail usan mock)

#### Páginas a Crear

##### A. RealEstateListingsPage (Dealer)
```tsx
// pages/dealer/RealEstateListingsPage.tsx
export const RealEstateListingsPage = () => {
  const { properties, loading } = useProperties();
  
  return (
    <DealerLayout>
      <PageHeader
        title="My Properties"
        actions={
          <Button onClick={() => navigate('/dealer/properties/new')}>
            <PlusIcon /> Add Property
          </Button>
        }
      />
      
      <PropertyStats stats={stats} />
      
      <DataTable
        columns={[
          { key: 'image', label: '', render: PropertyImage },
          { key: 'title', label: 'Property' },
          { key: 'type', label: 'Type' },
          { key: 'price', label: 'Price' },
          { key: 'status', label: 'Status' },
          { key: 'views', label: 'Views' },
          { key: 'inquiries', label: 'Inquiries' },
          { key: 'actions', label: '', render: PropertyActions }
        ]}
        data={properties}
      />
    </DealerLayout>
  );
};
```

**Features:**
- Lista de propiedades del dealer
- Stats dashboard (views, inquiries, favorites)
- Quick actions (edit, delete, feature, sold)
- Filtros por tipo/estado
- Bulk actions

**Esfuerzo:** 10-12 horas

---

##### B. PropertyFormPage (Create/Edit)
```tsx
// pages/dealer/PropertyFormPage.tsx
export const PropertyFormPage = () => {
  const { property } = useProperty(propertyId);
  const form = useForm<PropertyFormData>({
    defaultValues: property,
    validationSchema: propertySchema
  });
  
  return (
    <FormLayout>
      <Form {...form}>
        <FormSection title="Basic Information">
          <FormField name="title" label="Property Title" />
          <FormField name="type" label="Property Type" type="select" />
          <FormField name="price" label="Price" type="currency" />
          <FormField name="description" label="Description" type="textarea" />
        </FormSection>
        
        <FormSection title="Location">
          <FormField name="address" label="Address" />
          <FormField name="city" label="City" />
          <FormField name="state" label="State" />
          <FormField name="zipCode" label="ZIP Code" />
          <LocationPicker
            value={form.values.location}
            onChange={form.setValue}
          />
        </FormSection>
        
        <FormSection title="Details">
          <FormField name="bedrooms" label="Bedrooms" type="number" />
          <FormField name="bathrooms" label="Bathrooms" type="number" />
          <FormField name="sqft" label="Square Feet" type="number" />
          <FormField name="lotSize" label="Lot Size" type="number" />
          <FormField name="yearBuilt" label="Year Built" type="number" />
        </FormSection>
        
        <FormSection title="Amenities">
          <AmenitiesCheckboxGroup
            value={form.values.amenities}
            onChange={form.setValue}
          />
        </FormSection>
        
        <FormSection title="Images">
          <PropertyImageUploader
            images={form.values.images}
            onUpload={handleImageUpload}
            onDelete={handleImageDelete}
            onReorder={handleImageReorder}
          />
        </FormSection>
        
        <FormActions>
          <Button type="button" variant="outline" onClick={handleCancel}>
            Cancel
          </Button>
          <Button type="button" variant="secondary" onClick={form.handleSubmit(handleSaveDraft)}>
            Save Draft
          </Button>
          <Button type="submit" onClick={form.handleSubmit(handlePublish)}>
            Publish
          </Button>
        </FormActions>
      </Form>
    </FormLayout>
  );
};
```

**Features:**
- Multi-step form
- Location picker con mapa
- Amenities checkboxes
- Image uploader con drag & drop
- Save draft functionality
- Preview before publish

**Esfuerzo:** 14-16 horas

---

**Total Real Estate:** 24-28 horas  
**Impacto:** 🔴 **MUY ALTO** - Vertical completo sin UI funcional

---

### 3. Roles & Permissions Management

**Estado:** ❌ No existe

#### Página a Crear

```tsx
// pages/admin/RolesManagementPage.tsx
export const RolesManagementPage = () => {
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const { roles, permissions } = useRolesAndPermissions();
  
  return (
    <AdminLayout>
      <PageHeader title="Roles & Permissions" />
      
      <Grid cols={3} gap={6}>
        <div className="col-span-1">
          <Card>
            <CardHeader>
              <CardTitle>Roles</CardTitle>
              <Button size="sm" onClick={handleAddRole}>
                <PlusIcon /> Add Role
              </Button>
            </CardHeader>
            <CardContent>
              <RolesList
                roles={roles}
                selectedRole={selectedRole}
                onSelect={setSelectedRole}
                onDelete={handleDeleteRole}
              />
            </CardContent>
          </Card>
        </div>
        
        <div className="col-span-2">
          {selectedRole ? (
            <Card>
              <CardHeader>
                <CardTitle>{selectedRole.name}</CardTitle>
                <Badge>{selectedRole.usersCount} users</Badge>
              </CardHeader>
              <CardContent>
                <Tabs>
                  <Tab label="Permissions">
                    <PermissionsGrid
                      permissions={permissions}
                      selectedPermissions={selectedRole.permissions}
                      onToggle={handleTogglePermission}
                    />
                  </Tab>
                  
                  <Tab label="Users">
                    <UsersWithRoleTable
                      roleId={selectedRole.id}
                      onRemoveUser={handleRemoveUser}
                      onAddUser={handleAddUser}
                    />
                  </Tab>
                  
                  <Tab label="Settings">
                    <RoleSettingsForm
                      role={selectedRole}
                      onUpdate={handleUpdateRole}
                    />
                  </Tab>
                </Tabs>
              </CardContent>
            </Card>
          ) : (
            <EmptyState
              icon={<ShieldIcon />}
              title="Select a role"
              description="Choose a role from the list to manage its permissions"
            />
          )}
        </div>
      </Grid>
    </AdminLayout>
  );
};
```

**Componentes:**
- RolesList con CRUD
- PermissionsGrid (grouped por módulo)
- UsersWithRoleTable
- RoleSettingsForm
- Add/Remove user modal

**Esfuerzo:** 14-16 horas  
**Impacto:** 🔴 **ALTO** - Admin necesita gestionar permisos

---

### 4. Jobs Management Dashboard

**Estado:** ❌ No existe

#### Página a Crear

```tsx
// pages/admin/JobsManagementPage.tsx
export const JobsManagementPage = () => {
  const { jobs, failedJobs, recurringJobs } = useScheduledJobs();
  
  return (
    <AdminLayout>
      <PageHeader title="Scheduled Jobs" />
      
      <JobsStats
        total={jobs.length}
        running={runningCount}
        failed={failedCount}
        scheduled={scheduledCount}
      />
      
      <Tabs defaultValue="active">
        <Tab value="active" label="Active Jobs">
          <JobsDataTable
            jobs={jobs.filter(j => j.status === 'active')}
            columns={activeJobsColumns}
            actions={[
              { label: 'Trigger Now', onClick: handleTriggerJob },
              { label: 'Pause', onClick: handlePauseJob },
              { label: 'View Logs', onClick: handleViewLogs }
            ]}
          />
        </Tab>
        
        <Tab value="failed" label="Failed Jobs">
          <FailedJobsTable
            jobs={failedJobs}
            onRetry={handleRetryJob}
            onRetryAll={handleRetryAllJobs}
            onDelete={handleDeleteJob}
          />
        </Tab>
        
        <Tab value="recurring" label="Recurring Jobs">
          <RecurringJobsTable
            jobs={recurringJobs}
            onEdit={handleEditRecurring}
            onDisable={handleDisableRecurring}
          />
        </Tab>
        
        <Tab value="history" label="Job History">
          <JobHistoryTable
            history={jobHistory}
            filters={['success', 'failed', 'cancelled']}
          />
        </Tab>
      </Tabs>
    </AdminLayout>
  );
};
```

**Features:**
- Stats cards (total, running, failed, scheduled)
- Active jobs table
- Failed jobs con retry
- Recurring jobs config
- Job history con filtros
- Job logs viewer
- Manual trigger

**Esfuerzo:** 12-14 horas  
**Impacto:** 🟠 **MEDIO** - Admin tool importante

---

### 5. Finance Dashboard

**Estado:** ❌ No existe (FinanceService sin UI)

#### Páginas a Crear

##### A. FinanceDashboardPage
```tsx
// pages/admin/FinanceDashboardPage.tsx
export const FinanceDashboardPage = () => {
  const { summary, transactions } = useFinanceSummary();
  
  return (
    <AdminLayout>
      <PageHeader title="Finance Dashboard" />
      
      <Grid cols={4} gap={4}>
        <StatCard
          title="Total Revenue"
          value={formatCurrency(summary.totalRevenue)}
          trend={summary.revenueTrend}
          icon={<DollarIcon />}
        />
        <StatCard
          title="Outstanding"
          value={formatCurrency(summary.outstanding)}
          icon={<ClockIcon />}
        />
        <StatCard
          title="This Month"
          value={formatCurrency(summary.thisMonth)}
          trend={summary.monthTrend}
          icon={<CalendarIcon />}
        />
        <StatCard
          title="Profit Margin"
          value={`${summary.profitMargin}%`}
          trend={summary.marginTrend}
          icon={<TrendingUpIcon />}
        />
      </Grid>
      
      <Grid cols={2} gap={6}>
        <Card>
          <CardHeader>
            <CardTitle>Revenue Chart</CardTitle>
          </CardHeader>
          <CardContent>
            <LineChart data={summary.revenueChart} />
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader>
            <CardTitle>Expenses Breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            <PieChart data={summary.expensesBreakdown} />
          </CardContent>
        </Card>
      </Grid>
      
      <Card>
        <CardHeader>
          <CardTitle>Recent Transactions</CardTitle>
          <Button size="sm" onClick={() => navigate('/admin/finance/transactions')}>
            View All
          </Button>
        </CardHeader>
        <CardContent>
          <TransactionsTable transactions={transactions} compact />
        </CardContent>
      </Card>
    </AdminLayout>
  );
};
```

**Esfuerzo:** 12-14 horas

---

##### B. TransactionsPage
```tsx
// pages/admin/TransactionsPage.tsx
export const TransactionsPage = () => {
  return (
    <AdminLayout>
      <PageHeader
        title="Transactions"
        actions={
          <Button onClick={handleExport}>
            <DownloadIcon /> Export
          </Button>
        }
      />
      
      <TransactionFilters
        filters={filters}
        onFilterChange={setFilters}
      />
      
      <DataTable
        columns={transactionColumns}
        data={transactions}
        sortable
        filterable
        exportable
      />
    </AdminLayout>
  );
};
```

**Esfuerzo:** 8-10 horas

---

**Total Finance:** 20-24 horas  
**Impacto:** 🟠 **MEDIO** - Feature avanzada para admin

---

### 6. Appointment Calendar

**Estado:** ❌ No existe (AppointmentService sin UI)

```tsx
// pages/dealer/CalendarPage.tsx
import FullCalendar from '@fullcalendar/react';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';

export const CalendarPage = () => {
  const { appointments } = useAppointments();
  const [selectedSlot, setSelectedSlot] = useState<DateSelectArg | null>(null);
  const [selectedEvent, setSelectedEvent] = useState<EventClickArg | null>(null);
  
  return (
    <DealerLayout>
      <PageHeader title="Calendar" />
      
      <Grid cols={4} gap={4}>
        <StatCard title="Today" value={todayCount} />
        <StatCard title="This Week" value={weekCount} />
        <StatCard title="Pending" value={pendingCount} />
        <StatCard title="Completed" value={completedCount} />
      </Grid>
      
      <Card>
        <CardContent>
          <FullCalendar
            plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
            initialView="timeGridWeek"
            headerToolbar={{
              left: 'prev,next today',
              center: 'title',
              right: 'dayGridMonth,timeGridWeek,timeGridDay'
            }}
            events={appointments.map(a => ({
              id: a.id,
              title: a.title,
              start: a.startTime,
              end: a.endTime,
              backgroundColor: getColorForStatus(a.status)
            }))}
            editable
            selectable
            select={handleDateSelect}
            eventClick={handleEventClick}
            eventDrop={handleEventDrop}
            eventResize={handleEventResize}
          />
        </CardContent>
      </Card>
      
      <AppointmentModal
        isOpen={!!selectedSlot}
        date={selectedSlot?.start}
        onClose={() => setSelectedSlot(null)}
        onSave={handleCreateAppointment}
      />
      
      <AppointmentDetailModal
        isOpen={!!selectedEvent}
        appointment={selectedEvent?.event}
        onClose={() => setSelectedEvent(null)}
        onUpdate={handleUpdateAppointment}
        onDelete={handleDeleteAppointment}
      />
    </DealerLayout>
  );
};
```

**Features:**
- FullCalendar integration
- Month/Week/Day views
- Drag & drop para mover citas
- Click para crear cita
- Click en evento para ver detalles
- Color coding por estado
- Stats cards
- Email reminders

**Esfuerzo:** 16-20 horas  
**Impacto:** 🔴 **ALTO** - Dealers necesitan gestionar citas

---

### 7. Contact Messages Admin View

**Estado:** ❌ No existe (ContactService sin UI admin)

```tsx
// pages/admin/ContactMessagesPage.tsx
export const ContactMessagesPage = () => {
  const { messages, stats } = useContactMessages();
  
  return (
    <AdminLayout>
      <PageHeader title="Contact Messages" />
      
      <Grid cols={4} gap={4}>
        <StatCard title="Total" value={stats.total} />
        <StatCard title="Unread" value={stats.unread} />
        <StatCard title="Pending" value={stats.pending} />
        <StatCard title="Resolved" value={stats.resolved} />
      </Grid>
      
      <Card>
        <CardHeader>
          <CardTitle>Messages</CardTitle>
          <MessageFilters
            status={statusFilter}
            dateRange={dateRangeFilter}
            onChange={setFilters}
          />
        </CardHeader>
        <CardContent>
          <DataTable
            columns={[
              { key: 'name', label: 'Name' },
              { key: 'email', label: 'Email' },
              { key: 'subject', label: 'Subject' },
              { key: 'status', label: 'Status', render: StatusBadge },
              { key: 'createdAt', label: 'Date', render: DateCell },
              { key: 'actions', label: '', render: MessageActions }
            ]}
            data={messages}
            onRowClick={handleViewMessage}
          />
        </CardContent>
      </Card>
      
      <MessageDetailDrawer
        message={selectedMessage}
        isOpen={!!selectedMessage}
        onClose={() => setSelectedMessage(null)}
        onReply={handleReply}
        onMarkAsRead={handleMarkAsRead}
        onUpdateStatus={handleUpdateStatus}
      />
    </AdminLayout>
  );
};
```

**Esfuerzo:** 8-10 horas  
**Impacto:** 🟠 **MEDIO** - Admin tool

---

### 8. System Health Dashboard

**Estado:** ❌ No existe (AdminService tiene endpoint, sin UI)

```tsx
// pages/admin/SystemHealthPage.tsx
export const SystemHealthPage = () => {
  const { health, loading } = useSystemHealth({ refreshInterval: 30000 });
  
  return (
    <AdminLayout>
      <PageHeader title="System Health" />
      
      <OverallHealthCard status={health.overall} />
      
      <Grid cols={3} gap={4}>
        <Card>
          <CardHeader>
            <CardTitle>Services</CardTitle>
          </CardHeader>
          <CardContent>
            <ServiceStatusList services={health.services} />
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader>
            <CardTitle>Databases</CardTitle>
          </CardHeader>
          <CardContent>
            <DatabaseStatusList databases={health.databases} />
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader>
            <CardTitle>Infrastructure</CardTitle>
          </CardHeader>
          <CardContent>
            <InfrastructureStatusList infrastructure={health.infrastructure} />
          </CardContent>
        </Card>
      </Grid>
      
      <Card>
        <CardHeader>
          <CardTitle>Response Times</CardTitle>
        </CardHeader>
        <CardContent>
          <LineChart
            data={health.responseTimesChart}
            series={['AuthService', 'ProductService', 'UserService']}
          />
        </CardContent>
      </Card>
      
      <Card>
        <CardHeader>
          <CardTitle>Recent Alerts</CardTitle>
        </CardHeader>
        <CardContent>
          <AlertsList alerts={health.recentAlerts} />
        </CardContent>
      </Card>
    </AdminLayout>
  );
};
```

**Esfuerzo:** 10-12 horas  
**Impacto:** 🔴 **ALTO** - Operaciones necesita visibilidad

---

## 🟠 PRIORIDAD MEDIA - Páginas Importantes

### 9. Messages Center (Chat UI)

**Estado:** ❌ MessagesPage existe pero usa mock

```tsx
// pages/user/MessagesPage.tsx (rediseño completo)
export const MessagesPage = () => {
  const [selectedConversation, setSelectedConversation] = useState<Conversation | null>(null);
  const { conversations } = useConversations();
  
  return (
    <PageLayout>
      <Grid cols={3} gap={0} className="h-[calc(100vh-64px)]">
        <div className="col-span-1 border-r">
          <ConversationList
            conversations={conversations}
            selectedConversation={selectedConversation}
            onSelect={setSelectedConversation}
          />
        </div>
        
        <div className="col-span-2">
          {selectedConversation ? (
            <ChatWindow
              conversation={selectedConversation}
              onSendMessage={handleSendMessage}
            />
          ) : (
            <EmptyState
              icon={<MessageCircleIcon />}
              title="Select a conversation"
              description="Choose a conversation from the list to start chatting"
            />
          )}
        </div>
      </Grid>
    </PageLayout>
  );
};
```

**Components:**
- ConversationList (sidebar)
- ChatWindow (messages + input)
- MessageBubble component
- TypingIndicator
- OnlineStatus
- File attachment
- SignalR real-time

**Esfuerzo:** 16-18 horas  
**Impacto:** 🔴 **ALTO** - Users esperan mensajería funcional

---

### 10. Audit Logs Viewer

**Estado:** ❌ No existe (AuditService sin UI)

```tsx
// pages/admin/AuditLogsPage.tsx
export const AuditLogsPage = () => {
  const { logs, loading } = useAuditLogs(filters);
  
  return (
    <AdminLayout>
      <PageHeader title="Audit Logs" />
      
      <AuditLogFilters
        filters={filters}
        onFilterChange={setFilters}
      />
      
      <DataTable
        columns={[
          { key: 'timestamp', label: 'Time' },
          { key: 'user', label: 'User' },
          { key: 'action', label: 'Action' },
          { key: 'entity', label: 'Entity' },
          { key: 'changes', label: 'Changes', render: ChangesSummary },
          { key: 'ipAddress', label: 'IP' },
          { key: 'actions', label: '', render: ViewDetailsButton }
        ]}
        data={logs}
      />
      
      <AuditLogDetailModal
        log={selectedLog}
        isOpen={!!selectedLog}
        onClose={() => setSelectedLog(null)}
      />
    </AdminLayout>
  );
};
```

**Esfuerzo:** 8-10 horas  
**Impacto:** 🟡 **BAJO** - Compliance feature

---

### 11-13. Settings Pages (User, Dealer, Admin)

**Estado:** ❌ Configuraciones dispersas, sin centralizar

#### User Settings Page
```tsx
// pages/user/SettingsPage.tsx
export const UserSettingsPage = () => {
  return (
    <SettingsLayout>
      <Tabs>
        <Tab label="Profile">
          <ProfileSettingsForm />
        </Tab>
        <Tab label="Account">
          <AccountSettingsForm />
        </Tab>
        <Tab label="Notifications">
          <NotificationPreferencesForm />
        </Tab>
        <Tab label="Privacy">
          <PrivacySettingsForm />
        </Tab>
        <Tab label="Security">
          <SecuritySettingsForm />
        </Tab>
      </Tabs>
    </SettingsLayout>
  );
};
```

**Esfuerzo:** 12-14 horas por tipo de usuario → **36-42 horas total**  
**Impacto:** 🟠 **MEDIO** - Better UX

---

## 🟡 PRIORIDAD BAJA - Nice to Have

### 14. Reports Builder

**Estado:** ❌ No existe (custom reports manual)

```tsx
// pages/admin/ReportsBuilderPage.tsx
export const ReportsBuilderPage = () => {
  const [reportConfig, setReportConfig] = useState<ReportConfig>({
    dataSource: 'sales',
    fields: [],
    filters: [],
    groupBy: [],
    sortBy: []
  });
  
  return (
    <AdminLayout>
      <PageHeader title="Report Builder" />
      
      <Grid cols={3} gap={6}>
        <div className="col-span-1">
          <ReportConfigPanel
            config={reportConfig}
            onChange={setReportConfig}
          />
        </div>
        
        <div className="col-span-2">
          <ReportPreview
            config={reportConfig}
            data={previewData}
          />
        </div>
      </Grid>
      
      <FormActions>
        <Button onClick={handleSaveReport}>Save Report</Button>
        <Button onClick={handleScheduleReport}>Schedule Report</Button>
        <Button onClick={handleExportReport}>Export</Button>
      </FormActions>
    </AdminLayout>
  );
};
```

**Esfuerzo:** 20-24 horas  
**Impacto:** 🟡 **BAJO** - Power user feature

---

### 15. Marketing Campaigns Manager

**Estado:** ❌ No existe (MarketingService básico sin UI)

```tsx
// pages/admin/MarketingCampaignsPage.tsx
export const MarketingCampaignsPage = () => {
  return (
    <AdminLayout>
      <PageHeader
        title="Marketing Campaigns"
        actions={<Button>Create Campaign</Button>}
      />
      
      <CampaignsDataTable
        campaigns={campaigns}
        onView={handleView}
        onEdit={handleEdit}
        onDuplicate={handleDuplicate}
        onArchive={handleArchive}
      />
    </AdminLayout>
  );
};
```

**Esfuerzo:** 18-20 horas  
**Impacto:** 🟡 **BAJO** - Feature avanzada

---

## 📊 COMPONENTES COMPARTIDOS A CREAR

### Componentes de UI Generales (32 componentes)

| Componente | Descripción | Esfuerzo |
|-----------|-------------|----------|
| **NotificationBell** | Bell icon con badge | 2-3h |
| **NotificationList** | Lista de notificaciones | 3-4h |
| **NotificationCard** | Tarjeta individual | 1-2h |
| **ChatWindow** | Ventana de chat | 4-5h |
| **MessageBubble** | Burbuja de mensaje | 2-3h |
| **ConversationList** | Lista de conversaciones | 3-4h |
| **TypingIndicator** | Indicador de escritura | 1-2h |
| **FileAttachment** | Attachment component | 2-3h |
| **PropertyCard** | Tarjeta de propiedad | 2-3h |
| **PropertyImageGallery** | Galería de imágenes | 3-4h |
| **LocationPicker** | Selector de ubicación | 4-5h |
| **AmenitiesSelector** | Selector de amenidades | 2-3h |
| **RoleCard** | Tarjeta de rol | 1-2h |
| **PermissionsGrid** | Grid de permisos | 3-4h |
| **JobCard** | Tarjeta de job | 1-2h |
| **JobLogsViewer** | Visor de logs | 3-4h |
| **SystemHealthCard** | Tarjeta de health | 2-3h |
| **ServiceStatusBadge** | Badge de estado | 1h |
| **TransactionCard** | Tarjeta de transacción | 2-3h |
| **RevenueChart** | Gráfico de revenue | 3-4h |
| **CalendarWidget** | Widget de calendario | 4-5h |
| **AppointmentCard** | Tarjeta de cita | 2-3h |
| **ContactMessageCard** | Tarjeta de mensaje | 2-3h |
| **AuditLogCard** | Tarjeta de audit log | 2-3h |
| **ReportPreview** | Preview de reporte | 4-5h |
| **DataTable** | Tabla genérica (mejorar) | 6-8h |
| **StatCard** | Tarjeta de estadística | 1-2h |
| **EmptyState** | Estado vacío | 1-2h |
| **LoadingState** | Estado de carga | 1-2h |
| **ErrorState** | Estado de error | 1-2h |
| **ConfirmDialog** | Diálogo de confirmación | 2-3h |
| **Drawer** | Drawer component | 2-3h |

**Total Componentes:** 75-95 horas

---

## 📊 RESUMEN FINAL

### Por Prioridad

| Prioridad | Páginas | Esfuerzo | Impacto |
|-----------|---------|----------|---------|
| 🔴 **Alta** | 8 páginas | 118-140h | MUY ALTO |
| 🟠 **Media** | 5 páginas | 68-82h | ALTO |
| 🟡 **Baja** | 2 páginas | 38-44h | MEDIO |
| **Componentes** | 32 comp. | 75-95h | - |
| **TOTAL** | 47 items | **299-361h** | - |

### Quick Wins (< 10 horas, alto impacto)

1. NotificationBell component (2-3h)
2. SystemHealthCard component (2-3h)
3. ContactMessagesPage (8-10h)

**Total Quick Wins:** 12-16 horas

---

## 🎯 ROADMAP DE UI

### Sprint 1 (2 semanas) - Core UX
- NotificationBell + NotificationCenter (18-22h)
- System Health Dashboard (10-12h)
- Contact Messages Admin (8-10h)

**Total:** 36-44 horas

---

### Sprint 2 (2 semanas) - Real Estate
- RealEstateListingsPage (10-12h)
- PropertyFormPage (14-16h)
- PropertyComponents (8-10h)

**Total:** 32-38 horas

---

### Sprint 3 (2 semanas) - Admin Tools
- Roles & Permissions (14-16h)
- Jobs Management (12-14h)
- Audit Logs (8-10h)

**Total:** 34-40 horas

---

### Sprint 4 (2 semanas) - Communication
- Messages Center rediseño (16-18h)
- Appointment Calendar (16-20h)

**Total:** 32-38 horas

---

### Sprint 5 (2 semanas) - Finance & Settings
- Finance Dashboard (12-14h)
- Transactions Page (8-10h)
- User/Dealer/Admin Settings (36-42h)

**Total:** 56-66 horas

---

### Sprint 6 (opcional) - Advanced Features
- Reports Builder (20-24h)
- Marketing Campaigns (18-20h)

**Total:** 38-44 horas

---

## 🎓 CONCLUSIONES SECCIÓN 6

### Páginas más Críticas

1. **Notification Center** (18-22h) - Core UX, alta visibilidad
2. **Real Estate Forms** (24-28h) - Vertical completo sin UI
3. **Appointment Calendar** (16-20h) - Dealers lo necesitan
4. **System Health Dashboard** (10-12h) - Operaciones críticas
5. **Messages Center** (16-18h) - User expectation

### ROI Analysis

**Quick Wins (<12h, alto impacto):**
- NotificationBell (2-3h)
- Contact Messages (8-10h)
- System Health (10-12h)

**Total Quick Wins:** 20-25 horas → Impacto visible inmediato

### Total Investment

- **Páginas Alta Prioridad:** 118-140 horas
- **Páginas Media Prioridad:** 68-82 horas
- **Páginas Baja Prioridad:** 38-44 horas
- **Componentes Compartidos:** 75-95 horas
- **TOTAL:** 299-361 horas (7.5-9 semanas)

Con **2 developers frontend:** 3.75-4.5 semanas

---

## ➡️ PRÓXIMA SECCIÓN

**[SECCION_7_PLAN_ACCION.md](SECCION_7_PLAN_ACCION.md)**  
Plan de acción y roadmap priorizado

---

**Estado:** ✅ Completo  
**Última actualización:** 2 Enero 2026
