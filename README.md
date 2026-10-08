# PAQTERIA

PAQTERIA conecta una interfaz React/Vite con SQL Server mediante su propio servidor ASP.NET Core. La aplicación no se conecta directamente desde el navegador a SQL Server ni usa una API externa: el servidor propio es la capa interna necesaria para consultar y guardar datos de forma segura.

## Instalación en un servidor nuevo

Esta sección describe el orden completo para ejecutar PAQTERIA desde el proyecto en otra computadora Windows. No necesitas tener la misma instancia SQL Server que el equipo de desarrollo. Copia el proyecto completo a la computadora donde correrá la aplicación y abre PowerShell en la carpeta raíz, donde están `README.md` y `PAQTERIA.slnx`.

Estos pasos arrancan la aplicación con el perfil de desarrollo (`dotnet run` y Vite); no configuran un despliegue productivo.

Antes de empezar, prepara:

- SQL Server Database Engine, iniciado y accesible desde la computadora de PAQTERIA.
- .NET 10 SDK, Node.js con npm y la herramienta `sqlcmd`. Si no tienes `sqlcmd`, puedes ejecutar los mismos archivos desde SSMS.
- Una cuenta de Windows con permisos para crear la base de datos y los objetos SQL. Para asignar el acceso final, también necesitarás una cuenta administradora de SQL Server.
- Una base `Paqteria` nueva o vacía. El script no borra nada y no es una migración para bases existentes; si ya hay tablas con esos nombres, los comandos de creación darán error.

### Pasos de instalación

1. Define el nombre de la instancia SQL Server destino. Cámbialo por el que aparece al conectarte desde SSMS; por ejemplo, `localhost\SQLEXPRESS` o `SERVIDOR\INSTANCIA`:

   ```powershell
   $instancia = 'SERVIDOR\INSTANCIA'
   ```

2. Instala las dependencias de la interfaz web desde la raíz del proyecto:

   ```powershell
   cd .\paqteria.client
   npm ci
   cd ..
   ```

3. Instala el esquema y las mejoras requeridas por la web, en este orden. `-E` usa la cuenta de Windows actual y `-b` detiene `sqlcmd` si una instrucción falla. Esa cuenta debe poder crear la base y sus objetos:

   ```powershell
   sqlcmd -S $instancia -E -b -i .\database\paqteria-schema-portable.sql
   sqlcmd -S $instancia -E -b -i .\database\paqteria-app-setup.sql
   sqlcmd -S $instancia -E -b -i .\database\paqteria-package-sender-migration.sql
   ```

   El primer archivo crea `Paqteria` usando las carpetas predeterminadas de la instancia; no depende de la ruta física del servidor original. El complemento prepara los campos, roles y disparadores que necesita la aplicación. La última migración habilita el registro del remitente como texto. Ninguno de estos pasos carga paquetes, centros ni cuentas web.

   Si prefieres SSMS, conéctate a `$instancia` y ejecuta los mismos tres archivos, uno por uno y en ese orden. Si uno muestra un error, detente y corrígelo antes de seguir. Si `sqlcmd` rechaza el certificado local de SQL Server, puedes agregar `-C` a los comandos solo en desarrollo; para un servidor productivo instala un certificado confiable.

4. Da permisos a la identidad de Windows con la que se ejecutará el servidor web. Abre `database/sqlserver-windows-access.sql` y reemplaza **todas** las apariciones de `DESKTOP-NJOK690\DELL` por esa identidad. Luego ejecuta el archivo con una cuenta administradora de SQL Server:

   ```powershell
   sqlcmd -S $instancia -E -b -i .\database\sqlserver-windows-access.sql
   ```

   Si ejecutas `dotnet run` desde tu propia consola, normalmente se usa tu cuenta de Windows. Si PAQTERIA corre como servicio o en IIS, usa la cuenta del servicio o del grupo de aplicaciones, no la cuenta de quien abre la página. En una conexión a un SQL Server remoto, usa una identidad que ese servidor pueda reconocer, como una cuenta de dominio. El permiso concede lectura, inserción, actualización y eliminación dentro de `dbo`; no da administración del servidor.

5. Confirma que las tablas estén listas:

   ```powershell
   sqlcmd -S $instancia -E -d Paqteria -b -Q "SELECT name FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') ORDER BY name;"
   ```

6. Configura la cadena de conexión para la sesión de PowerShell actual. Mantén esta terminal abierta para los pasos siguientes:

   ```powershell
   $env:ConnectionStrings__PAQTERIA = "Server=$instancia;Database=Paqteria;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Application Name=PAQTERIA.Web"
   ```

   La variable se pierde al cerrar la terminal. Para un servicio, configúrala en el entorno de ese servicio. `TrustServerCertificate=True` es para desarrollo con certificados locales; en producción usa un certificado confiable y cambia a `TrustServerCertificate=False`.

7. Crea la primera cuenta administradora de la aplicación. Hazlo después de configurar la cadena de conexión y los permisos; la contraseña se solicita de forma interactiva:

   ```powershell
   dotnet run --project .\PAQTERIA.Server\PAQTERIA.Server.csproj --launch-profile http -- --create-user
   ```

   La sección **Primera cuenta** explica qué datos te pedirá este comando. Usa la misma terminal para conservar la variable `ConnectionStrings__PAQTERIA`.

8. Inicia PAQTERIA:

   ```powershell
   dotnet run --project .\PAQTERIA.Server\PAQTERIA.Server.csproj --launch-profile http
   ```

   Abre `http://localhost:57290`. Para comprobar el estado de la conexión, visita `http://localhost:57290/api/health`. Este perfil usa HTTP local y no debe exponerse a Internet ni a una red compartida sin configurar HTTPS y revisar la cuenta de servicio.

## Base de datos

La instalación en otra computadora está descrita primero en **Instalación en un servidor nuevo**. Esta sección resume la configuración de desarrollo de este proyecto y explica para qué sirve cada archivo SQL.

La instancia local usada durante el desarrollo es:

- Servidor: `DESKTOP-NJOK690\SQLSERVERPAQUETE`
- Base: `Paqteria`
- Identidad de desarrollo: la cuenta de Windows que ejecuta `dotnet run` (en este equipo, `DESKTOP-NJOK690\DELL`).

`Integrated Security=True` hace que el servidor .NET se conecte con la identidad de Windows que ejecuta el proceso. No se escribe una contraseña de Windows en la cadena de conexión. Esta identidad de SQL Server es independiente de las cuentas que inician sesión en la página; esas cuentas se guardan en `dbo.USUARIOS` y `dbo.ROLES`.

Los archivos SQL tienen propósitos distintos:

- `database/paqteria-schema-portable.sql`: esquema base portable para una instancia nueva. Usa las carpetas predeterminadas del servidor y no incluye filas de datos.
- `database/paqteria-schema.sql`: esquema del entorno de desarrollo; contiene rutas y opciones de la instancia local. No lo uses para instalar en otra computadora.
- `database/paqteria-app-setup.sql`: complemento aditivo e idempotente. Prepara campos de concurrencia y seguimiento, `dbo.APP_CAMBIOS`, los disparadores de avisos y los roles `Administrator`, `Warehouse Manager`, `Customer` y `Driver`. No borra tablas ni datos.
- `database/paqteria-package-sender-migration.sql`: permite capturar el remitente como texto libre; hace nullable `id_cliente` y conserva registros existentes.
- `database/sqlserver-windows-access.sql`: concede a la cuenta de Windows del servidor web acceso de lectura y escritura sobre `dbo`. El nombre de la cuenta debe corresponder a cada instalación.

En la instancia local de desarrollo ya comprobé que el complemento y sus nueve disparadores están instalados. Si necesitas instalar esta base local desde cero, ejecuta los comandos en este orden y solo si la base no existe o está vacía:

```powershell
sqlcmd -S 'DESKTOP-NJOK690\SQLSERVERPAQUETE' -E -C -b -i .\database\paqteria-schema.sql
sqlcmd -S 'DESKTOP-NJOK690\SQLSERVERPAQUETE' -E -C -b -i .\database\paqteria-app-setup.sql
sqlcmd -S 'DESKTOP-NJOK690\SQLSERVERPAQUETE' -E -C -b -i .\database\paqteria-package-sender-migration.sql
sqlcmd -S 'DESKTOP-NJOK690\SQLSERVERPAQUETE' -E -C -b -i .\database\sqlserver-windows-access.sql
```

`-E` usa autenticación de Windows y `-C` acepta el certificado local de SQL Server en este entorno de desarrollo. El último archivo está preparado para `DESKTOP-NJOK690\DELL`; en otro servidor reemplaza esa identidad siguiendo los pasos de instalación de arriba. En esta instancia local, esa cuenta tiene `sysadmin`; para un servidor compartido usa una cuenta de servicio sin privilegios de administrador. En otro servidor, usa siempre el script portable y sigue la sección **Instalación en un servidor nuevo**.

## Primera cuenta

La base no incluye cuentas de acceso a la web. Después de instalar los scripts de base de datos y configurar la conexión del servidor .NET, ejecuta este comando desde la raíz del proyecto y con la identidad de Windows que ya tenga acceso a la base `Paqteria`:

```powershell
dotnet run --project .\PAQTERIA.Server\PAQTERIA.Server.csproj --launch-profile http -- --create-user
```

El programa solicita nombre, correo y contraseña en la consola; la contraseña no aparece en pantalla ni en el historial del shell. La primera cuenta debe ser `Administrator`. Las contraseñas se guardan con el hasher de ASP.NET Core Identity; no se admiten contraseñas en texto plano.

Después inicia la aplicación normalmente e inicia sesión con el correo y la contraseña que acabas de registrar. Un Administrador puede crear cuentas de Encargado de almacén y Repartidor desde **Usuarios**. Los remitentes se escriben directamente al registrar el paquete y no requieren una cuenta web.

## Acceso por rol

- **Administrador:** Resumen, Rutas, Paquetes, Incidencias, Repartidores, Reportes, Centros y Usuarios. Solo este rol puede administrar usuarios y centros; el servidor también protege los endpoints de esas funciones.
- **Encargado de almacén:** Resumen, Rutas, Paquetes, Incidencias y Repartidores.

El servidor comprueba el usuario y el rol desde SQL Server en cada solicitud y el menú refleja los permisos. Los registros Cliente existentes se conservan para compatibilidad con paquetes previos, pero ya no se pueden crear desde **Usuarios**; los remitentes nuevos se capturan como texto. El rol Repartidor sigue usándose en las operaciones de envío. Cliente y Repartidor no pueden iniciar sesión en la consola operativa.

## Lectura, guardado y actualización en vivo

- Paquetes, incidencias, usuarios, centros y repartidores se leen desde `dbo.PAQUETES`, `dbo.INCIDENCIAS_ENTREGA`, `dbo.USUARIOS`, `dbo.ROLES`, `dbo.CENTROS_DISTRIBUCION`, `dbo.TURNOS_REPARTIDOR` y `dbo.UNIDADES`.
- Se pueden registrar paquetes e importar lotes de paquetes CSV UTF-8 o Excel XLSX. La plantilla incluye folio, nombre del remitente, centro de origen, direcciones, peso y atributos opcionales. Solo el centro debe existir previamente. Los archivos anteriores que incluyan correo del cliente siguen aceptándose si ese cliente ya existe.
- En el registro web, **Escanear QR** acepta el folio como texto plano o un objeto JSON de paquete con `version: 1`. El texto plano completa solo el folio; el JSON puede completar los campos del formulario. `originCenterId` debe existir en la base de datos actual. La cámara requiere permiso del navegador y una dirección segura (`localhost` o HTTPS).

## Ejemplo de estructura del QR
Ejemplo del contenido JSON para un QR de paquete:

```json
{
  "version": 1,
  "trackingNumber": "PQ-2026-0001",
  "senderName": "Transportes del Norte",
  "originCenterId": 2,
  "originAddress": "Calle de origen 123",
  "destinationAddress": "Avenida de destino 456",
  "destinationCoordinates": null,
  "weightKg": 2.5,
  "labelSize": "10x15 cm",
  "isPriority": false,
  "isFragile": true
}
```
- Las incidencias guardan paquete, repartidor, severidad, estado y detalle. El complemento de SQL agrega `estado`, `severidad`, `fecha_actualizacion` y `version_fila` a la tabla original.
- Los cambios hechos por la aplicación o directamente en las tablas cubiertas por los disparadores se registran en `dbo.APP_CAMBIOS`. El servidor los consulta cada segundo y publica avisos con SignalR; la interfaz también vuelve a consultar cada 15 segundos para recuperarse de una desconexión.
- `version_fila` evita que una sesión sobrescriba cambios recientes en paquetes o incidencias.

## Ejecutar en desarrollo

Requisitos: .NET 10 SDK, Node.js y acceso a SQL Server con la cuenta de Windows que ejecutará el servidor.

```powershell
cd .\paqteria.client
npm ci
cd ..
dotnet run --project .\PAQTERIA.Server\PAQTERIA.Server.csproj --launch-profile http
```

Abre `http://localhost:57290`. La comprobación `GET /api/health` informa si SQL Server y el esquema web están listos. Para otra instancia local, cambia `ConnectionStrings:PAQTERIA` en `PAQTERIA.Server/appsettings.Development.json`; para otros entornos, configura `ConnectionStrings__PAQTERIA` como variable de entorno o User Secrets.

Este perfil usa HTTP local deliberadamente. No lo expongas a Internet ni a una red compartida hasta configurar HTTPS y revisar las credenciales de servicio.

## Comprobaciones locales

```powershell
dotnet build .\PAQTERIA.Server\PAQTERIA.Server.csproj
cd .\paqteria.client
npm run build
npm run lint
```
