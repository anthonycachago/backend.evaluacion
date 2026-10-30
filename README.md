# Inventory – Evaluación Full Stack (.NET + Microservicios)

Aplicación de gestión de inventario compuesta por dos microservicios en **.NET 10**, organizados en capas (Core, Infrastructure, Business, Api), con persistencia en **SQL Server** (EF Core) y comunicación **síncrona vía REST**.

| Microservicio | Responsabilidad | URL local | Base de datos |
|---|---|---|---|
| **Products.Api** | CRUD de productos, filtros dinámicos, ajuste de stock | http://localhost:5001 | `InventoryProductsDb` |
| **Transactions.Api** | CRUD de compras/ventas, validación y ajuste de stock, historial por producto | http://localhost:5002 | `InventoryTransactionsDb` |

## Arquitectura

Cada microservicio es independiente y tiene las mismas 4 bibliotecas de clases:

```
backend.evaluacion/
├── database.sql                          # Script de creación de las bases de datos
├── Inventory.slnx
├── src/
│   ├── Products/
│   │   ├── Products.Core/                # Lo común, sin dependencias
│   │   │   ├── Models/                   #   ModelBase, ProductEntity
│   │   │   ├── Dto/Common/               #   PagedResultDto, PagedFilterDto
│   │   │   ├── Dto/Product/              #   ProductDto, ProductCreateDto, ProductUpdateDto, ProductFilterDto...
│   │   │   ├── Repository/               #   Interfaces: IModelBaseRepository<T>, IProductRepository
│   │   │   ├── Mappers/                  #   ProductMapper (Entity <-> DTO)
│   │   │   ├── Enums/  Exceptions/
│   │   ├── Products.Infrastructure/      # Acceso a datos
│   │   │   ├── DataBase/                 #   ProductsContext, ConfigShema/ProductConfig
│   │   │   ├── Repository/               #   BaseRepository<T>, Product/ProductRepository
│   │   │   └── RepositoryExtension.cs    #   AddDataBase(), AddRepository()
│   │   ├── Products.Business/            # Lógica de negocio
│   │   │   ├── Product/Interface/        #   IProductService
│   │   │   ├── Product/Service/          #   ProductService
│   │   │   └── ServiceExtension.cs       #   AddServicesExtension()
│   │   └── Products.Api/                 # API REST
│   │       ├── Controllers/Product/      #   ProductController
│   │       ├── Middleware/               #   Manejo global de errores y validaciones
│   │       └── Program.cs
│   └── Transactions/
│       ├── Transactions.Core/            # (misma estructura) + ExternalServices/IProductApiClient
│       ├── Transactions.Infrastructure/  # (misma estructura) + ExternalServices/ProductApiClient (HTTP)
│       ├── Transactions.Business/        # Transaction/Interface, Transaction/Service
│       └── Transactions.Api/             # Controllers/Transaction
└── tests/
    ├── Products.UnitTests/
    └── Transactions.UnitTests/
```

**Referencias entre proyectos:** `Api → Business → Infrastructure → Core`. Las interfaces (repositorios y cliente HTTP) viven en `Core`, y sus implementaciones en `Infrastructure`.

**Flujo de una petición:** `Controller` → `Service` (Business) → `Repository` (Infrastructure) → `DbContext` → SQL Server. Los DTOs se validan con DataAnnotations antes de llegar al controlador. Los errores de negocio se lanzan como excepciones y el middleware global los convierte en respuestas HTTP, así los controladores no necesitan `try/catch`.

### Decisiones de diseño

- **Una base de datos por microservicio.** `Transactions.ProductId` es una referencia lógica: el servicio de Transacciones valida el producto llamando a la API de Productos.
- **Validación de stock:** antes de registrar una venta, el servicio de Transacciones consulta el stock del producto. Después, el servicio de Productos vuelve a validar al aplicar el ajuste con una sola sentencia atómica (`UPDATE ... SET Stock = Stock + @cantidad WHERE Stock + @cantidad >= 0`), así dos ventas simultáneas no pueden dejar el stock negativo.
- **Ajuste de stock:** cada alta, edición o eliminación de una transacción ajusta el stock (`POST /api/products/{id}/stock-adjustments`):
  - **Alta:** compra `+cantidad`, venta `−cantidad`.
  - **Edición:** se aplica sólo la diferencia respecto a la transacción original. Si cambia el producto, se revierte el anterior y se aplica al nuevo.
  - **Eliminación:** se revierte el efecto. No se permite eliminar una compra si el stock quedaría negativo.
- **Consistencia entre servicios (compensación):** si falla el guardado local después de ajustar el stock remoto, los ajustes se revierten automáticamente. El cliente HTTP no hace reintentos automáticos porque los ajustes de stock no son idempotentes.
- **Eliminación lógica de productos (`IsDeleted`):** el historial de transacciones conserva el nombre de los productos eliminados.
- **El stock no se edita desde el formulario de producto:** sólo se define al crearlo y luego cambia únicamente mediante transacciones, para que el historial sea coherente.
- **Errores uniformes:** todas las respuestas de error usan el formato RFC 7807 (`ProblemDetails`) con mensajes en español:
  - `400` validación (con `errors` por campo)
  - `404` no encontrado
  - `422` regla de negocio (por ejemplo, stock insuficiente)
  - `503` servicio dependiente no disponible

## Requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) o superior
- SQL Server 2019+ (Express, Developer o LocalDB)
- SQL Server Management Studio, Azure Data Studio o `sqlcmd` para ejecutar el script
- Node.js 20+ (para el frontend)

## Ejecución del backend

1. **Crear las bases de datos.** Ejecuta `database.sql` desde la raíz del proyecto. Crea `InventoryProductsDb` e `InventoryTransactionsDb`, sus tablas, índices y productos de ejemplo:

   ```bash
   sqlcmd -S localhost\SQLEXPRESS -E -C -i database.sql
   ```

   También puedes abrir el archivo en SSMS y ejecutarlo con **F5**.

2. **Configurar la conexión, si tu instancia no es `localhost\SQLEXPRESS`.** Edita `ConnectionStrings` en:
   - `src/Products/Products.Api/appsettings.json` (`ProductsConnection`)
   - `src/Transactions/Transactions.Api/appsettings.json` (`TransactionsConnection` y `Services:ProductsApi:BaseUrl`)

3. **Levantar los microservicios**, cada uno en una terminal. Levanta primero Productos, porque Transacciones depende de él:

   ```bash
   dotnet run --project src/Products/Products.Api
   dotnet run --project src/Transactions/Transactions.Api
   ```

   En Visual Studio puedes abrir `Inventory.slnx` y configurar **Varios proyectos de inicio** con `Products.Api` y `Transactions.Api`.

4. **Probar las APIs con Swagger:**
   - Productos: http://localhost:5001/swagger
   - Transacciones: http://localhost:5002/swagger
   - Health checks: `/health` en ambos servicios

5. **Ejecutar las pruebas unitarias:**

   ```bash
   dotnet test
   ```

### Endpoints

**Products.Api** (`/api/products`)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/products` | Listado paginado con filtros: `search`, `category`, `minPrice`, `maxPrice`, `minStock`, `maxStock`, `sortBy` (`name`, `category`, `price`, `stock`, `createdAt`), `sortDirection` (`Asc`, `Desc`), `pageNumber`, `pageSize` |
| GET | `/api/products/{id}` | Detalle de un producto |
| GET | `/api/products/categories` | Categorías existentes, para combos de filtro |
| GET | `/api/products/lookup?ids=..&ids=..` | Consulta de varios productos, incluidos los eliminados (uso interno) |
| POST | `/api/products` | Crear |
| PUT | `/api/products/{id}` | Editar (sin stock) |
| DELETE | `/api/products/{id}` | Eliminar (lógico) |
| POST | `/api/products/{id}/stock-adjustments` | Ajustar stock `{ "quantity": ±n }` (uso interno) |

**Transactions.Api** (`/api/transactions`)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/transactions` | Listado paginado con filtros: `productId`, `type` (`Purchase`, `Sale`), `dateFrom`, `dateTo`, `minTotal`, `maxTotal`, `search` (en el detalle), `sortBy` (`date`, `type`, `quantity`, `unitPrice`, `totalPrice`), `sortDirection`, `pageNumber`, `pageSize`. Cada ítem incluye el nombre y el stock actual del producto |
| GET | `/api/transactions/{id}` | Detalle de una transacción |
| GET | `/api/transactions/products/{productId}/history` | Historial de un producto (nombre y stock) con los mismos filtros |
| POST | `/api/transactions` | Registrar compra o venta. `date` es opcional (por defecto, ahora). `unitPrice` es opcional (por defecto, el precio del producto) |
| PUT | `/api/transactions/{id}` | Editar (reajusta el stock) |
| DELETE | `/api/transactions/{id}` | Eliminar (revierte el stock) |

Ejemplo de creación de una transacción:

```json
POST /api/transactions
{
  "date": "2026-10-29T15:30:00Z",
  "type": "Sale",
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "quantity": 2,
  "unitPrice": 59.90,
  "detail": "Venta mostrador"
}
```

## Ejecución del frontend

El frontend es una aplicación **Angular 22** con **Angular Material**, ubicada en la carpeta `frontend/`.

1. **Levanta antes los dos microservicios** (ver la sección anterior).
2. **Instala las dependencias y arranca el servidor de desarrollo:**

   ```bash
   cd frontend
   npm install
   npm start
   ```

3. **Abre la aplicación** en http://localhost:4200

Las URLs de las APIs se configuran en `frontend/src/environments/environment.ts`.

Para ejecutar las pruebas unitarias del frontend:

```bash
cd frontend
npm test -- --watch=false
```

### Estructura del frontend

```
frontend/src/app/
├── core/                       # Lo transversal a toda la app
│   ├── models/                 #   Interfaces de Product, Transaction, PagedResult...
│   ├── services/               #   ProductService, TransactionService (HTTP), NotificationService
│   ├── http/                   #   Interceptor que normaliza los errores de la API (ApiError)
│   └── i18n/                   #   Paginador en español
├── shared/                     # Reutilizable
│   ├── components/
│   │   ├── dynamic-filter/     #   Panel de filtros dinámicos (agregar / quitar / chips)
│   │   └── confirm-dialog/     #   Diálogo de confirmación
│   ├── forms/                  #   Validadores y mensajes de error de formularios
│   └── utils/                  #   Utilidades de fechas
└── features/
    ├── products/
    │   ├── product-list/       #   Listado con tabla dinámica, paginación, orden y filtros
    │   ├── product-form/       #   Crear / editar producto
    │   ├── product-detail/     #   Consulta de un producto (solo lectura)
    │   └── product-history/    #   Historial de transacciones del producto
    └── transactions/
        ├── transaction-list/   #   Listado con tabla dinámica, paginación, orden y filtros
        ├── transaction-form/   #   Crear / editar transacción (valida el stock)
        ├── transaction-detail/ #   Consulta de una transacción (solo lectura)
        └── transaction-table/  #   Tabla reutilizada por el listado y el historial
```

### Funcionalidades

| Requisito | Dónde se ve |
|---|---|
| Vistas de productos y transacciones con rutas | Menú superior: `/products`, `/transactions` |
| Tabla dinámica con paginación y botones de acción | Listados: paginación, orden por columna y columnas que se pueden mostrar u ocultar (botón "Columnas") |
| Búsqueda con filtros avanzados y dinámicos | "Agregar filtro": se añaden o quitan filtros, y los aplicados se muestran como chips removibles |
| Mensajes de éxito o error | Notificaciones verdes (éxito) o rojas (error, con el mensaje del backend) |
| Validación de campos vacíos o con formato incorrecto | Formularios: obligatorios, longitud, URL, números, decimales y fechas |
| Validación compleja: no vender más stock del disponible | Formulario de transacción: valida la cantidad contra el stock (al editar considera la transacción original) y muestra el stock resultante |
| Historial por producto con nombre y stock | Producto → "Historial", con filtros por fecha y tipo |
| Consulta de información de un formulario (extra) | Botón "Ver detalle" en productos y transacciones |

## Evidencias

_Pendiente: capturas de pantalla de:_

- Listado dinámico de productos y transacciones con paginación
- Pantalla de creación de productos
- Pantalla de edición de productos
- Pantalla de creación de transacciones
- Pantalla de edición de transacciones
- Pantalla de filtros dinámicos
- Pantalla de consulta de información de un formulario (extra)
