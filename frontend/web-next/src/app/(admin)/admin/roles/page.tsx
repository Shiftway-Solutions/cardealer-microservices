/**
 * Admin Staff Roles Page
 *
 * Gestión de roles y permisos del equipo interno (staff) de OKLA.
 * NO aplica a compradores/vendedores (se asignan automáticamente)
 * ni a equipos de dealers (gestionados desde el portal dealer).
 */

'use client';

import { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import {
  Shield,
  Plus,
  Edit,
  Trash2,
  Check,
  X,
  ChevronDown,
  ChevronRight,
  Loader2,
  AlertCircle,
  Info,
  Save,
} from 'lucide-react';
import {
  getRoles,
  getRoleById,
  getPermissions,
  createRole,
  updateRole,
  deleteRole,
  assignPermission,
  removePermission,
  groupPermissionsByModule,
  MODULE_LABELS,
  type RoleListItem,
  type RoleDetails,
  type PermissionListItem,
} from '@/services/roles';

// ============================================================
// ROLE COLOR HELPER
// ============================================================

const ROLE_COLORS: Record<string, string> = {
  SuperAdmin: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300',
  Admin: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
  Moderador: 'bg-primary/10 text-primary dark:bg-primary/95/30 dark:text-primary/60',
  Soporte: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300',
  Analista: 'bg-muted text-foreground',
};

function getRoleColor(roleName: string): string {
  return ROLE_COLORS[roleName] || 'bg-muted text-foreground';
}

// ============================================================
// CREATE ROLE MODAL
// ============================================================

function CreateRoleModal({
  allPermissions,
  onClose,
  onCreated,
}: {
  allPermissions: PermissionListItem[];
  onClose: () => void;
  onCreated: () => void;
}) {
  const [name, setName] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [description, setDescription] = useState('');
  const [selectedPermissionIds, setSelectedPermissionIds] = useState<Set<string>>(new Set());
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const permissionsByModule = groupPermissionsByModule(allPermissions);

  const togglePermission = (id: string) => {
    setSelectedPermissionIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const handleSave = async () => {
    if (!name.trim() || !displayName.trim()) {
      setError('Nombre técnico y nombre visible son requeridos');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await createRole({
        name: name.trim(),
        displayName: displayName.trim(),
        description: description.trim() || undefined,
        permissionIds: Array.from(selectedPermissionIds),
      });
      onCreated();
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } }; message?: string };
      setError(e.response?.data?.message || e.message || 'Error al crear el rol');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="max-h-[90vh] w-full max-w-2xl overflow-y-auto">
        <CardHeader>
          <CardTitle>Nuevo Rol de Staff</CardTitle>
          <CardDescription>
            Crea un nuevo rol para miembros del equipo interno de OKLA
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {error && (
            <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              <AlertCircle className="h-4 w-4" />
              {error}
            </div>
          )}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="mb-1 block text-sm font-medium">Nombre técnico</label>
              <Input
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder="ej: ContentEditor"
                disabled={saving}
              />
              <p className="text-muted-foreground mt-1 text-xs">Solo letras, números y guiones</p>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Nombre visible</label>
              <Input
                value={displayName}
                onChange={e => setDisplayName(e.target.value)}
                placeholder="ej: Editor de Contenido"
                disabled={saving}
              />
            </div>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Descripción</label>
            <Textarea
              value={description}
              onChange={e => setDescription(e.target.value)}
              placeholder="Describe las responsabilidades de este rol dentro del equipo..."
              rows={2}
              disabled={saving}
            />
          </div>
          <div>
            <label className="mb-2 block text-sm font-medium">
              Permisos del staff ({selectedPermissionIds.size} seleccionados)
            </label>
            <div className="max-h-60 space-y-3 overflow-y-auto rounded-lg border p-3">
              {Object.entries(permissionsByModule).map(([module, perms]) => (
                <div key={module}>
                  <p className="text-muted-foreground mb-1 text-xs font-semibold uppercase">
                    {MODULE_LABELS[module] || module}
                  </p>
                  <div className="space-y-1">
                    {perms.map(perm => (
                      <label
                        key={perm.id}
                        className="hover:bg-muted/50 flex cursor-pointer items-center gap-2 rounded p-1.5"
                      >
                        <input
                          type="checkbox"
                          checked={selectedPermissionIds.has(perm.id)}
                          onChange={() => togglePermission(perm.id)}
                          className="rounded"
                          disabled={saving}
                        />
                        <span className="text-sm">{perm.displayName}</span>
                      </label>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={onClose} disabled={saving}>
              Cancelar
            </Button>
            <Button
              onClick={handleSave}
              disabled={saving}
              className="bg-slate-900 hover:bg-slate-800"
            >
              {saving ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Save className="mr-2 h-4 w-4" />
              )}
              Crear Rol
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// ============================================================
// EDIT ROLE MODAL
// ============================================================

function EditRoleModal({
  role,
  allPermissions,
  onClose,
  onUpdated,
}: {
  role: RoleDetails;
  allPermissions: PermissionListItem[];
  onClose: () => void;
  onUpdated: () => void;
}) {
  const [displayName, setDisplayName] = useState(role.displayName || role.name);
  const [description, setDescription] = useState(role.description || '');
  const [selectedPermissionIds, setSelectedPermissionIds] = useState<Set<string>>(
    new Set(role.permissions.map(p => p.id))
  );
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const permissionsByModule = groupPermissionsByModule(allPermissions);

  const togglePermission = (id: string) => {
    setSelectedPermissionIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const handleSave = async () => {
    setSaving(true);
    setError(null);
    try {
      await updateRole(role.id, {
        displayName: displayName.trim(),
        description: description.trim() || undefined,
        permissionIds: Array.from(selectedPermissionIds),
      });
      onUpdated();
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } }; message?: string };
      setError(e.response?.data?.message || e.message || 'Error al actualizar el rol');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="max-h-[90vh] w-full max-w-2xl overflow-y-auto">
        <CardHeader>
          <CardTitle>Editar Rol: {role.displayName || role.name}</CardTitle>
          <CardDescription>Modifica los permisos y detalles de este rol de staff</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {error && (
            <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              <AlertCircle className="h-4 w-4" />
              {error}
            </div>
          )}
          <div>
            <label className="mb-1 block text-sm font-medium">Nombre visible</label>
            <Input
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              disabled={saving}
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Descripción</label>
            <Textarea
              value={description}
              onChange={e => setDescription(e.target.value)}
              rows={2}
              disabled={saving}
            />
          </div>
          <div>
            <label className="mb-2 block text-sm font-medium">
              Permisos ({selectedPermissionIds.size} seleccionados)
            </label>
            <div className="max-h-60 space-y-3 overflow-y-auto rounded-lg border p-3">
              {Object.entries(permissionsByModule).map(([module, perms]) => (
                <div key={module}>
                  <p className="text-muted-foreground mb-1 text-xs font-semibold uppercase">
                    {MODULE_LABELS[module] || module}
                  </p>
                  <div className="space-y-1">
                    {perms.map(perm => (
                      <label
                        key={perm.id}
                        className="hover:bg-muted/50 flex cursor-pointer items-center gap-2 rounded p-1.5"
                      >
                        <input
                          type="checkbox"
                          checked={selectedPermissionIds.has(perm.id)}
                          onChange={() => togglePermission(perm.id)}
                          className="rounded"
                          disabled={saving}
                        />
                        <span className="text-sm">{perm.displayName}</span>
                      </label>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={onClose} disabled={saving}>
              Cancelar
            </Button>
            <Button
              onClick={handleSave}
              disabled={saving}
              className="bg-slate-900 hover:bg-slate-800"
            >
              {saving ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Save className="mr-2 h-4 w-4" />
              )}
              Guardar Cambios
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// ============================================================
// MAIN PAGE
// ============================================================

export default function AdminRolesPage() {
  // Data state
  const [roles, setRoles] = useState<RoleListItem[]>([]);
  const [allPermissions, setAllPermissions] = useState<PermissionListItem[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
  const [selectedRoleDetails, setSelectedRoleDetails] = useState<RoleDetails | null>(null);

  // UI state
  const [loading, setLoading] = useState(true);
  const [loadingDetails, setLoadingDetails] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [expandedGroups, setExpandedGroups] = useState<string[]>([]);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [deleting, setDeleting] = useState(false);

  // --- Data Fetching ---

  const fetchRoles = useCallback(async (silent = false) => {
    try {
      if (!silent) {
        setLoading(true);
        setError(null);
      }
      const [rolesResult, permsResult] = await Promise.all([
        getRoles({ pageSize: 50 }),
        getPermissions(),
      ]);
      setRoles(rolesResult.items || []);
      setAllPermissions(permsResult || []);
    } catch (err: unknown) {
      if (!silent) {
        const e = err as { message?: string };
        setError(e.message || 'Error al cargar los roles');
      }
    } finally {
      if (!silent) setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchRoles();
  }, [fetchRoles]);

  // Silent background polling
  useEffect(() => {
    const interval = setInterval(() => {
      if (!document.hidden) fetchRoles(true);
    }, 30_000);
    return () => clearInterval(interval);
  }, [fetchRoles]);

  const fetchRoleDetails = useCallback(async (roleId: string) => {
    try {
      setLoadingDetails(true);
      const details = await getRoleById(roleId);
      setSelectedRoleDetails(details);
      // Auto-expand first two modules
      const modules = [...new Set(details.permissions.map(p => p.module))];
      setExpandedGroups(modules.slice(0, 2));
    } catch (err: unknown) {
      const e = err as { message?: string };
      setError(e.message || 'Error al cargar detalles del rol');
    } finally {
      setLoadingDetails(false);
    }
  }, []);

  useEffect(() => {
    if (selectedRoleId) {
      fetchRoleDetails(selectedRoleId);
    } else {
      setSelectedRoleDetails(null);
    }
  }, [selectedRoleId, fetchRoleDetails]);

  // --- Actions ---

  const toggleGroup = (groupName: string) => {
    setExpandedGroups(prev =>
      prev.includes(groupName) ? prev.filter(g => g !== groupName) : [...prev, groupName]
    );
  };

  const handleDelete = async (role: RoleDetails) => {
    if (role.isSystemRole) return;
    const confirmed = window.confirm(
      `¿Estás seguro de que deseas eliminar el rol "${role.displayName || role.name}"? Esta acción no se puede deshacer.`
    );
    if (!confirmed) return;

    try {
      setDeleting(true);
      await deleteRole(role.id);
      setSelectedRoleId(null);
      setSelectedRoleDetails(null);
      await fetchRoles(true);
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } }; message?: string };
      alert(e.response?.data?.message || e.message || 'Error al eliminar el rol');
    } finally {
      setDeleting(false);
    }
  };

  const handleTogglePermission = async (
    roleId: string,
    permissionId: string,
    hasPermission: boolean
  ) => {
    try {
      if (hasPermission) {
        await removePermission({ roleId, permissionId });
      } else {
        await assignPermission({ roleId, permissionId });
      }
      // Refresh role details
      await fetchRoleDetails(roleId);
      await fetchRoles(true);
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } }; message?: string };
      alert(e.response?.data?.message || e.message || 'Error al modificar permiso');
    }
  };

  const handleCreated = async () => {
    setShowCreateModal(false);
    await fetchRoles(true);
  };

  const handleUpdated = async () => {
    setShowEditModal(false);
    if (selectedRoleId) {
      await fetchRoleDetails(selectedRoleId);
    }
    await fetchRoles(true);
  };

  // --- Permission grouping for detail view ---

  const permissionGroups = selectedRoleDetails
    ? (() => {
        const grouped = groupPermissionsByModule(allPermissions);
        return Object.entries(grouped).map(([module, modulePerms]) => ({
          module,
          label: MODULE_LABELS[module] || module,
          permissions: modulePerms.map(p => ({
            ...p,
            hasPermission: selectedRoleDetails.permissions.some(rp => rp.id === p.id),
          })),
        }));
      })()
    : [];

  // Check if role has ALL permissions (Super Admin)
  const hasAllPermissions =
    selectedRoleDetails &&
    allPermissions.length > 0 &&
    selectedRoleDetails.permissions.length >= allPermissions.length;

  // ============================================================
  // RENDER
  // ============================================================

  if (loading) {
    return (
      <div className="flex flex-col items-center justify-center py-20">
        <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
        <p className="text-muted-foreground mt-4">Cargando roles y permisos...</p>
      </div>
    );
  }

  if (error && roles.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-20">
        <AlertCircle className="h-8 w-8 text-red-500" />
        <p className="mt-4 text-red-600">{error}</p>
        <Button variant="outline" onClick={() => fetchRoles()} className="mt-4">
          Reintentar
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Roles del Staff</h1>
          <p className="text-muted-foreground">
            Gestión de roles y permisos del equipo interno de OKLA
          </p>
        </div>
        <Button
          className="bg-slate-900 hover:bg-slate-800"
          onClick={() => setShowCreateModal(true)}
        >
          <Plus className="mr-2 h-4 w-4" />
          Nuevo Rol
        </Button>
      </div>

      {/* Staff context info */}
      <div className="flex items-start gap-3 rounded-lg border border-blue-200 bg-blue-50 p-3 text-sm text-blue-800 dark:border-blue-800 dark:bg-blue-900/20 dark:text-blue-300">
        <Info className="mt-0.5 h-4 w-4 flex-shrink-0" />
        <div>
          <p className="font-medium">Estos roles aplican solo al equipo interno (staff) de OKLA.</p>
          <p className="text-blue-600 dark:text-blue-400">
            Los compradores y vendedores individuales tienen permisos asignados automáticamente. Los
            dealers gestionan los roles de su propio equipo desde su portal.
          </p>
        </div>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-700">
          <AlertCircle className="h-4 w-4" />
          {error}
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Roles List */}
        <div className="space-y-4 lg:col-span-1">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <Shield className="h-5 w-5" />
                Roles de Staff ({roles.length})
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {roles.map(role => (
                <button
                  key={role.id}
                  onClick={() => setSelectedRoleId(role.id)}
                  className={`w-full rounded-lg p-3 text-left transition-colors ${
                    selectedRoleId === role.id ? 'bg-slate-900 text-white' : 'hover:bg-muted/50'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">{role.displayName || role.name}</p>
                      <p
                        className={`text-xs ${selectedRoleId === role.id ? 'text-gray-300' : 'text-muted-foreground'}`}
                      >
                        {role.permissionCount} permisos
                      </p>
                    </div>
                    <div className="flex items-center gap-1.5">
                      {role.isSystemRole && (
                        <Badge
                          variant={selectedRoleId === role.id ? 'secondary' : 'outline'}
                          className="text-xs"
                        >
                          Sistema
                        </Badge>
                      )}
                      {!role.isActive && (
                        <Badge variant="danger" className="text-xs">
                          Inactivo
                        </Badge>
                      )}
                    </div>
                  </div>
                </button>
              ))}
              {roles.length === 0 && (
                <p className="text-muted-foreground py-4 text-center text-sm">
                  No hay roles de staff configurados
                </p>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Role Details */}
        <div className="lg:col-span-2">
          {loadingDetails ? (
            <Card>
              <CardContent className="flex flex-col items-center justify-center py-12">
                <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
                <p className="text-muted-foreground mt-4">Cargando detalles del rol...</p>
              </CardContent>
            </Card>
          ) : selectedRoleDetails ? (
            <Card>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div>
                    <CardTitle className="flex items-center gap-2">
                      <Badge className={getRoleColor(selectedRoleDetails.name)}>
                        {selectedRoleDetails.displayName || selectedRoleDetails.name}
                      </Badge>
                      {selectedRoleDetails.isSystemRole && (
                        <Badge variant="outline" className="text-xs">
                          Sistema
                        </Badge>
                      )}
                    </CardTitle>
                    <CardDescription className="mt-1">
                      {selectedRoleDetails.description || 'Sin descripción'}
                    </CardDescription>
                  </div>
                  {!selectedRoleDetails.isSystemRole && (
                    <div className="flex gap-2">
                      <Button variant="outline" size="sm" onClick={() => setShowEditModal(true)}>
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        className="text-red-600 hover:text-red-700"
                        onClick={() => handleDelete(selectedRoleDetails)}
                        disabled={deleting}
                      >
                        {deleting ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Trash2 className="h-4 w-4" />
                        )}
                      </Button>
                    </div>
                  )}
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="text-muted-foreground flex items-center gap-2 text-sm">
                    <Shield className="h-4 w-4" />
                    {selectedRoleDetails.permissions.length} permisos asignados
                  </div>

                  <div className="border-border border-t pt-4">
                    <h4 className="mb-4 font-medium">Permisos</h4>

                    {hasAllPermissions ? (
                      <div className="rounded-lg border border-purple-200 bg-purple-50 p-4 dark:border-purple-800 dark:bg-purple-900/20">
                        <p className="flex items-center gap-2 font-medium text-purple-800 dark:text-purple-300">
                          <Shield className="h-5 w-5" />
                          Acceso Completo
                        </p>
                        <p className="mt-1 text-sm text-purple-600 dark:text-purple-400">
                          Este rol de staff tiene acceso total a la plataforma (
                          {selectedRoleDetails.permissions.length} permisos)
                        </p>
                      </div>
                    ) : (
                      <div className="space-y-2">
                        {permissionGroups.map(group => {
                          const assignedCount = group.permissions.filter(
                            p => p.hasPermission
                          ).length;
                          const isExpanded = expandedGroups.includes(group.module);

                          if (assignedCount === 0 && !isExpanded) return null;

                          return (
                            <div key={group.module} className="rounded-lg border">
                              <button
                                onClick={() => toggleGroup(group.module)}
                                className="hover:bg-muted/50 flex w-full items-center justify-between p-3"
                              >
                                <span className="font-medium">{group.label}</span>
                                <div className="flex items-center gap-2">
                                  <Badge variant="secondary">
                                    {assignedCount}/{group.permissions.length}
                                  </Badge>
                                  {isExpanded ? (
                                    <ChevronDown className="h-4 w-4" />
                                  ) : (
                                    <ChevronRight className="h-4 w-4" />
                                  )}
                                </div>
                              </button>
                              {isExpanded && (
                                <div className="border-border space-y-2 border-t px-3 py-2">
                                  {group.permissions.map(permission => (
                                    <div
                                      key={permission.id}
                                      className={`flex items-center gap-3 rounded p-2 ${
                                        permission.hasPermission
                                          ? 'bg-primary/10 dark:bg-primary/95/20'
                                          : 'bg-muted/50 opacity-50'
                                      }`}
                                    >
                                      {!selectedRoleDetails.isSystemRole ? (
                                        <button
                                          onClick={() =>
                                            handleTogglePermission(
                                              selectedRoleDetails.id,
                                              permission.id,
                                              permission.hasPermission
                                            )
                                          }
                                          className="flex-shrink-0"
                                          title={
                                            permission.hasPermission
                                              ? 'Quitar permiso'
                                              : 'Asignar permiso'
                                          }
                                        >
                                          {permission.hasPermission ? (
                                            <Check className="h-4 w-4 text-primary" />
                                          ) : (
                                            <X className="text-muted-foreground h-4 w-4" />
                                          )}
                                        </button>
                                      ) : permission.hasPermission ? (
                                        <Check className="h-4 w-4 flex-shrink-0 text-primary" />
                                      ) : (
                                        <X className="text-muted-foreground h-4 w-4 flex-shrink-0" />
                                      )}
                                      <div>
                                        <p className="text-sm font-medium">
                                          {permission.displayName}
                                        </p>
                                        <p className="text-muted-foreground text-xs">
                                          {permission.name}
                                        </p>
                                      </div>
                                    </div>
                                  ))}
                                </div>
                              )}
                            </div>
                          );
                        })}
                      </div>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          ) : (
            <Card>
              <CardContent className="flex flex-col items-center justify-center py-12 text-center">
                <Shield className="mb-4 h-12 w-12 text-gray-300" />
                <p className="text-muted-foreground">
                  Selecciona un rol de staff para ver sus permisos
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Modals */}
      {showCreateModal && (
        <CreateRoleModal
          allPermissions={allPermissions}
          onClose={() => setShowCreateModal(false)}
          onCreated={handleCreated}
        />
      )}
      {showEditModal && selectedRoleDetails && (
        <EditRoleModal
          role={selectedRoleDetails}
          allPermissions={allPermissions}
          onClose={() => setShowEditModal(false)}
          onUpdated={handleUpdated}
        />
      )}
    </div>
  );
}
