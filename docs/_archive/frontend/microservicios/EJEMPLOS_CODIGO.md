# 💻 Ejemplos de Código: Frontend ↔ Backend

**Versión:** 1.0  
**Actualizado:** Enero 18, 2026

---

## 1️⃣ AUTENTICACIÓN (AuthService)

### Frontend: Login

```typescript
// src/services/authService.ts

import axios from "axios";
import { useAuthStore } from "@/stores/authStore";

interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    role: "Individual" | "Dealer" | "Admin";
  };
}

export const authService = {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    try {
      const response = await axios.post<LoginResponse>(
        "/api/auth/login",
        credentials,
        {
          baseURL: import.meta.env.VITE_API_URL,
          withCredentials: true,
        },
      );

      // Guardar tokens
      localStorage.setItem("accessToken", response.data.accessToken);
      localStorage.setItem("refreshToken", response.data.refreshToken);

      // Guardar usuario en store
      const store = useAuthStore();
      store.setUser(response.data.user);
      store.setIsAuthenticated(true);

      return response.data;
    } catch (error) {
      throw new Error("Login fallido: " + error.message);
    }
  },

  async register(data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    accountType: "Individual" | "Dealer";
  }): Promise<LoginResponse> {
    const response = await axios.post<LoginResponse>(
      "/api/auth/register",
      data,
      {
        baseURL: import.meta.env.VITE_API_URL,
      },
    );

    localStorage.setItem("accessToken", response.data.accessToken);
    localStorage.setItem("refreshToken", response.data.refreshToken);

    const store = useAuthStore();
    store.setUser(response.data.user);
    store.setIsAuthenticated(true);

    return response.data;
  },

  async logout(): Promise<void> {
    const store = useAuthStore();
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    store.setIsAuthenticated(false);
    store.setUser(null);
  },

  async refreshToken(): Promise<string> {
    const refreshToken = localStorage.getItem("refreshToken");
    if (!refreshToken) throw new Error("No refresh token");

    const response = await axios.post<{ accessToken: string }>(
      "/api/auth/refresh-token",
      { refreshToken },
      {
        baseURL: import.meta.env.VITE_API_URL,
      },
    );

    localStorage.setItem("accessToken", response.data.accessToken);
    return response.data.accessToken;
  },

  getAccessToken(): string | null {
    return localStorage.getItem("accessToken");
  },
};
```

### Frontend: Axios Interceptor

```typescript
// src/config/axiosConfig.ts

import axios from "axios";
import { authService } from "@/services/authService";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request Interceptor - Agregar JWT
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response Interceptor - Refresh token si expira
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Si 401 y no es un refresh
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const newToken = await authService.refreshToken();
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        // Refresh falló, redirigir a login
        window.location.href = "/login";
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  },
);

export default apiClient;
```

### Backend: Controller

```csharp
// AuthService.Api/Controllers/AuthController.cs

using Microsoft.AspNetCore.Mvc;
using MediatR;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Application.Features.Auth.Queries;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation("Login attempt for email: {Email}", command.Email);

        var result = await _mediator.Send(command, ct);

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresIn = 3600, // 1 hour
            user = new
            {
                id = result.UserId,
                email = result.Email,
                firstName = result.FirstName,
                lastName = result.LastName,
                role = result.Role
            }
        });
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(
        RegisterCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);

        return CreatedAtAction(nameof(Login), new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresIn = 3600,
            user = new
            {
                id = result.UserId,
                email = result.Email,
                firstName = result.FirstName,
                lastName = result.LastName,
                role = result.Role
            }
        });
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken(
        RefreshTokenCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);

        return Ok(new
        {
            accessToken = result.AccessToken,
            expiresIn = 3600
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult> GetCurrentUser(
        GetCurrentUserQuery query,
        CancellationToken ct)
    {
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }
}
```

---

## 2️⃣ LISTAR VEHÍCULOS (VehiclesSaleService)

### Frontend: Component

```typescript
// src/pages/SearchPage.tsx

import { useEffect, useState } from 'react';
import { vehicleService } from '@/services/vehicleService';
import { Vehicle } from '@/types/vehicle';

export const SearchPage = () => {
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState({
    make: '', // Toyota, Honda, etc.
    minPrice: 0,
    maxPrice: 1000000,
    year: 0,
  });

  const fetchVehicles = async () => {
    setLoading(true);
    try {
      const data = await vehicleService.getVehicles({
        page,
        pageSize: 20,
        make: filters.make,
        minPrice: filters.minPrice,
        maxPrice: filters.maxPrice,
        year: filters.year,
      });
      setVehicles(data.items);
    } catch (error) {
      console.error('Error fetching vehicles:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchVehicles();
  }, [page, filters]);

  const handleFilterChange = (newFilters: typeof filters) => {
    setFilters(newFilters);
    setPage(1);
  };

  return (
    <div className="flex gap-6">
      {/* Filtros */}
      <aside className="w-64">
        <select
          value={filters.make}
          onChange={(e) =>
            handleFilterChange({ ...filters, make: e.target.value })
          }
          className="w-full px-3 py-2 border rounded"
        >
          <option value="">Todas las marcas</option>
          <option value="Toyota">Toyota</option>
          <option value="Honda">Honda</option>
          <option value="Ford">Ford</option>
        </select>

        <div className="mt-4">
          <label className="block text-sm font-medium mb-2">
            Precio: ${filters.minPrice.toLocaleString()} - $
            {filters.maxPrice.toLocaleString()}
          </label>
          <input
            type="range"
            min="0"
            max="1000000"
            value={filters.minPrice}
            onChange={(e) =>
              handleFilterChange({
                ...filters,
                minPrice: Number(e.target.value),
              })
            }
            className="w-full"
          />
        </div>
      </aside>

      {/* Listado */}
      <main className="flex-1">
        {loading && <p>Cargando...</p>}

        <div className="grid grid-cols-3 gap-4">
          {vehicles.map((vehicle) => (
            <VehicleCard key={vehicle.id} vehicle={vehicle} />
          ))}
        </div>

        {/* Paginación */}
        <div className="flex justify-center gap-2 mt-8">
          {Array.from({ length: 5 }, (_, i) => (
            <button
              key={i + 1}
              onClick={() => setPage(i + 1)}
              className={`px-4 py-2 rounded ${
                page === i + 1
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-200 hover:bg-gray-300'
              }`}
            >
              {i + 1}
            </button>
          ))}
        </div>
      </main>
    </div>
  );
};
```

### Frontend: Service

```typescript
// src/services/vehicleService.ts

import apiClient from "@/config/axiosConfig";

interface GetVehiclesParams {
  page: number;
  pageSize: number;
  make?: string;
  minPrice?: number;
  maxPrice?: number;
  year?: number;
}

interface Vehicle {
  id: string;
  title: string;
  description: string;
  price: number;
  make: string;
  model: string;
  year: number;
  mileage: number;
  status: "Active" | "Pending" | "Sold";
  images: { id: string; url: string }[];
  seller: {
    id: string;
    name: string;
    rating: number;
    reviewCount: number;
  };
}

export const vehicleService = {
  async getVehicles(params: GetVehiclesParams) {
    const response = await apiClient.get<{
      items: Vehicle[];
      total: number;
      page: number;
      pageSize: number;
    }>("/api/vehicles", {
      params: {
        page: params.page,
        pageSize: params.pageSize,
        make: params.make,
        "filters.minPrice": params.minPrice,
        "filters.maxPrice": params.maxPrice,
        year: params.year,
      },
    });

    return response.data;
  },

  async getVehicleById(id: string) {
    const response = await apiClient.get<Vehicle>(`/api/vehicles/${id}`);
    return response.data;
  },

  async createVehicle(data: {
    title: string;
    description: string;
    price: number;
    make: string;
    model: string;
    year: number;
    mileage: number;
  }) {
    const response = await apiClient.post<Vehicle>("/api/vehicles", data);
    return response.data;
  },

  async updateVehicle(id: string, data: Partial<Vehicle>) {
    const response = await apiClient.put<Vehicle>(`/api/vehicles/${id}`, data);
    return response.data;
  },

  async deleteVehicle(id: string) {
    await apiClient.delete(`/api/vehicles/${id}`);
  },
};
```

### Backend: Query Handler

```csharp
// VehiclesSaleService.Application/Features/Vehicles/Queries/GetVehiclesQuery.cs

using MediatR;

public class GetVehiclesQuery : IRequest<GetVehiclesResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Make { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public int? Year { get; set; }
}

public class GetVehiclesResponse
{
    public List<VehicleDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, GetVehiclesResponse>
{
    private readonly IVehicleRepository _repository;
    private readonly IMapper _mapper;

    public GetVehiclesQueryHandler(IVehicleRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<GetVehiclesResponse> Handle(
        GetVehiclesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _repository.GetAll()
            .Where(v => v.Status == VehicleStatus.Active);

        // Filtros
        if (!string.IsNullOrEmpty(request.Make))
            query = query.Where(v => v.Make == request.Make);

        if (request.MinPrice.HasValue)
            query = query.Where(v => v.Price >= request.MinPrice);

        if (request.MaxPrice.HasValue)
            query = query.Where(v => v.Price <= request.MaxPrice);

        if (request.Year.HasValue)
            query = query.Where(v => v.Year == request.Year);

        var total = await query.CountAsync(cancellationToken);

        var vehicles = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new GetVehiclesResponse
        {
            Items = _mapper.Map<List<VehicleDto>>(vehicles),
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
```

---

## 3️⃣ SUBIR IMÁGENES (MediaService)

### Frontend: Upload Component

```typescript
// src/components/ImageUpload.tsx

import { useState } from 'react';
import { mediaService } from '@/services/mediaService';

interface ImageUploadProps {
  vehicleId: string;
  onUploadComplete?: (images: UploadedImage[]) => void;
}

export const ImageUpload = ({ vehicleId, onUploadComplete }: ImageUploadProps) => {
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      setSelectedFiles(Array.from(e.target.files));
    }
  };

  const handleUpload = async () => {
    if (selectedFiles.length === 0) return;

    setUploading(true);
    try {
      // Crear FormData
      const formData = new FormData();
      selectedFiles.forEach((file) => {
        formData.append('files', file);
      });
      formData.append('vehicleId', vehicleId);

      // Upload
      const uploadedImages = await mediaService.uploadImages(
        formData,
        (progressEvent) => {
          const percentCompleted = Math.round(
            (progressEvent.loaded * 100) / progressEvent.total
          );
          setProgress(percentCompleted);
        }
      );

      setSelectedFiles([]);
      setProgress(0);

      if (onUploadComplete) {
        onUploadComplete(uploadedImages);
      }
    } catch (error) {
      console.error('Upload error:', error);
      alert('Error al subir imágenes');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="border-2 border-dashed border-blue-300 rounded-lg p-8 text-center">
      <input
        type="file"
        multiple
        accept="image/jpeg,image/png"
        onChange={handleFileSelect}
        disabled={uploading}
        className="hidden"
        id="file-input"
      />

      <label
        htmlFor="file-input"
        className="cursor-pointer block text-blue-600 font-medium"
      >
        {selectedFiles.length > 0
          ? `${selectedFiles.length} archivos seleccionados`
          : 'Haz clic o arrastra imágenes'}
      </label>

      {progress > 0 && (
        <div className="mt-4">
          <div className="bg-gray-200 rounded-full h-2">
            <div
              className="bg-blue-600 h-2 rounded-full transition-all"
              style={{ width: `${progress}%` }}
            />
          </div>
          <p className="text-sm text-gray-600 mt-2">{progress}%</p>
        </div>
      )}

      <button
        onClick={handleUpload}
        disabled={selectedFiles.length === 0 || uploading}
        className="mt-4 px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
      >
        {uploading ? 'Subiendo...' : 'Subir ' + selectedFiles.length + ' imágenes'}
      </button>
    </div>
  );
};
```

### Frontend: Media Service

```typescript
// src/services/mediaService.ts

import apiClient from "@/config/axiosConfig";
import { AxiosProgressEvent } from "axios";

interface UploadedImage {
  id: string;
  url: string;
  thumbnail: string;
  medium: string;
  large: string;
  order: number;
}

export const mediaService = {
  async uploadImages(
    formData: FormData,
    onProgress?: (progressEvent: AxiosProgressEvent) => void,
  ) {
    const response = await apiClient.post<{
      uploadedCount: number;
      images: UploadedImage[];
    }>("/api/media/upload", formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
      onUploadProgress: onProgress,
    });

    return response.data.images;
  },

  async deleteImage(imageId: string) {
    await apiClient.delete(`/api/media/${imageId}`);
  },

  async reorderImages(vehicleId: string, imageIds: string[]) {
    await apiClient.post(`/api/media/reorder`, {
      vehicleId,
      imageIds,
    });
  },
};
```

### Backend: Controller

```csharp
// MediaService.Api/Controllers/MediaController.cs

using Microsoft.AspNetCore.Mvc;
using MediatR;
using MediaService.Application.Features.Upload.Commands;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator) => _mediator = mediator;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        [FromForm] UploadImagesCommand command,
        CancellationToken ct)
    {
        // Files viene del IFormFileCollection
        command.Files = Request.Form.Files;
        command.UserId = GetUserIdFromClaims();

        var result = await _mediator.Send(command, ct);

        return Ok(new
        {
            uploadedCount = result.UploadedCount,
            images = result.Images
        });
    }

    [HttpDelete("{imageId}")]
    public async Task<IActionResult> DeleteImage(
        string imageId,
        CancellationToken ct)
    {
        var command = new DeleteImageCommand { ImageId = imageId };
        await _mediator.Send(command, ct);
        return NoContent();
    }

    private string GetUserIdFromClaims()
    {
        return User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();
    }
}
```

---

## 4️⃣ CONTACTAR VENDEDOR (ContactService)

### Frontend: Send Message

```typescript
// src/components/ContactModal.tsx

import { useState } from 'react';
import { contactService } from '@/services/contactService';
import { useAuthStore } from '@/stores/authStore';

interface ContactModalProps {
  sellerId: string;
  vehicleId: string;
  onClose: () => void;
}

export const ContactModal = ({
  sellerId,
  vehicleId,
  onClose,
}: ContactModalProps) => {
  const user = useAuthStore((s) => s.user);
  const [subject, setSubject] = useState('');
  const [message, setMessage] = useState('');
  const [sending, setSending] = useState(false);

  const handleSend = async () => {
    if (!user) {
      alert('Debes estar autenticado');
      return;
    }

    setSending(true);
    try {
      await contactService.sendMessage({
        recipientId: sellerId,
        vehicleId,
        subject,
        body: message,
      });

      alert('Mensaje enviado');
      onClose();
    } catch (error) {
      alert('Error al enviar mensaje');
    } finally {
      setSending(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
      <div className="bg-white rounded-lg p-6 w-full max-w-md">
        <h2 className="text-xl font-bold mb-4">Contactar Vendedor</h2>

        <input
          type="text"
          placeholder="Asunto"
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          className="w-full px-3 py-2 border rounded mb-3"
        />

        <textarea
          placeholder="Tu mensaje..."
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          rows={4}
          className="w-full px-3 py-2 border rounded mb-3"
        />

        <div className="flex gap-2">
          <button
            onClick={onClose}
            className="flex-1 px-4 py-2 border rounded hover:bg-gray-100"
          >
            Cancelar
          </button>
          <button
            onClick={handleSend}
            disabled={sending || !subject || !message}
            className="flex-1 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
          >
            {sending ? 'Enviando...' : 'Enviar'}
          </button>
        </div>
      </div>
    </div>
  );
};
```

### Frontend: Contact Service

```typescript
// src/services/contactService.ts

import apiClient from "@/config/axiosConfig";

interface SendMessageRequest {
  recipientId: string;
  vehicleId: string;
  subject: string;
  body: string;
}

interface Conversation {
  id: string;
  participantId: string;
  participantName: string;
  lastMessage: string;
  lastMessageTime: string;
  unreadCount: number;
}

interface Message {
  id: string;
  senderId: string;
  senderName: string;
  body: string;
  createdAt: string;
  isRead: boolean;
}

export const contactService = {
  async sendMessage(data: SendMessageRequest) {
    const response = await apiClient.post("/api/contacts/send", data);
    return response.data;
  },

  async getConversations() {
    const response = await apiClient.get<Conversation[]>(
      "/api/contacts/conversations",
    );
    return response.data;
  },

  async getConversationMessages(conversationId: string) {
    const response = await apiClient.get<Message[]>(
      `/api/contacts/conversations/${conversationId}/messages`,
    );
    return response.data;
  },

  async markAsRead(conversationId: string) {
    await apiClient.put(
      `/api/contacts/conversations/${conversationId}/mark-read`,
    );
  },
};
```

### Backend: Controller

```csharp
// ContactService.Api/Controllers/ContactsController.cs

using Microsoft.AspNetCore.Mvc;
using MediatR;
using ContactService.Application.Features.Messages.Commands;
using ContactService.Application.Features.Messages.Queries;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(
        SendMessageCommand command,
        CancellationToken ct)
    {
        command.SenderId = GetUserIdFromClaims();
        var result = await _mediator.Send(command, ct);
        return Created(string.Empty, result);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        var result = await _mediator.Send(
            new GetConversationsQuery { UserId = userId },
            ct
        );
        return Ok(result);
    }

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(
        string conversationId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetMessagesQuery { ConversationId = conversationId },
            ct
        );
        return Ok(result);
    }

    [HttpPut("conversations/{conversationId}/mark-read")]
    public async Task<IActionResult> MarkAsRead(
        string conversationId,
        CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        var result = await _mediator.Send(
            new MarkConversationAsReadCommand
            {
                ConversationId = conversationId,
                UserId = userId
            },
            ct
        );
        return NoContent();
    }

    private string GetUserIdFromClaims()
    {
        return User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();
    }
}
```

---

## 5️⃣ ERROR HANDLING

### Frontend: Error Handling Completo

```typescript
// src/utils/errorHandler.ts

import { AxiosError } from "axios";
import { useNotificationStore } from "@/stores/notificationStore";

interface ApiError {
  statusCode: number;
  message: string;
  errors?: Record<string, string[]>;
  timestamp: string;
}

export const handleError = (error: unknown) => {
  const notificationStore = useNotificationStore();

  if (error instanceof AxiosError<ApiError>) {
    const errorData = error.response?.data;

    switch (error.response?.status) {
      case 400:
        // Validación
        notificationStore.addNotification({
          type: "error",
          title: "Datos inválidos",
          message: errorData?.message || "Por favor revisa los datos",
          duration: 5000,
        });
        break;

      case 401:
        // No autenticado
        notificationStore.addNotification({
          type: "error",
          title: "Sesión expirada",
          message: "Por favor inicia sesión nuevamente",
          duration: 5000,
        });
        window.location.href = "/login";
        break;

      case 403:
        // Forbidden
        notificationStore.addNotification({
          type: "error",
          title: "No autorizado",
          message: "No tienes permisos para esta acción",
          duration: 5000,
        });
        break;

      case 404:
        notificationStore.addNotification({
          type: "error",
          title: "No encontrado",
          message: errorData?.message || "El recurso no existe",
          duration: 5000,
        });
        break;

      case 500:
        notificationStore.addNotification({
          type: "error",
          title: "Error del servidor",
          message: "Intenta más tarde",
          duration: 5000,
        });
        console.error("Server error:", errorData);
        break;

      default:
        notificationStore.addNotification({
          type: "error",
          title: "Error",
          message: error.message,
          duration: 5000,
        });
    }
  } else if (error instanceof Error) {
    notificationStore.addNotification({
      type: "error",
      title: "Error",
      message: error.message,
      duration: 5000,
    });
  }
};
```

### Backend: Global Exception Handler

```csharp
// Shared/Middleware/ExceptionHandlingMiddleware.cs

using System.Text.Json;
using ErrorService.Domain;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred");
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new { message = "", statusCode = 0, errors = (object?)null };

        switch (exception)
        {
            case UnauthorizedAccessException:
                response.StatusCode = StatusCodes.Status401Unauthorized;
                errorResponse = new
                {
                    message = "No autenticado",
                    statusCode = 401,
                    errors = (object?)null
                };
                break;

            case ArgumentException argEx:
                response.StatusCode = StatusCodes.Status400BadRequest;
                errorResponse = new
                {
                    message = argEx.Message,
                    statusCode = 400,
                    errors = (object?)null
                };
                break;

            case KeyNotFoundException:
                response.StatusCode = StatusCodes.Status404NotFound;
                errorResponse = new
                {
                    message = "No encontrado",
                    statusCode = 404,
                    errors = (object?)null
                };
                break;

            default:
                response.StatusCode = StatusCodes.Status500InternalServerError;
                errorResponse = new
                {
                    message = "Error interno del servidor",
                    statusCode = 500,
                    errors = (object?)null
                };
                break;
        }

        return response.WriteAsJsonAsync(errorResponse);
    }
}
```

---

**Ejemplos de Código - OKLA Marketplace**  
Enero 2026
