# Changelog

## [1.2.0] - 2026-10-03

### Agregado
- Pestaña **Resumen** con una fila por app: estado (OK / Error / Omitido), cantidad de errores y advertencias, y duración. Las filas se colorean según el estado y al terminar queda seleccionado el primer app que falló.
- Detalle de errores y advertencias del app seleccionado.
- Botones **Copiar errores** y **Copiar log completo**, que copian un reporte con la solución, la configuración, el orden de compilación y el estado de cada app.
- Selector **Detalle** (Mínimo / Solo errores / Todo) para elegir cuánto se escribe en el log. La elección se recuerda entre sesiones.
- La barra de estado muestra el avance (`Compilando... 2/5`) y, al final, los totales con la duración.

## [1.1.3] - 2026-09-19

Primera versión pública.

### Agregado
- Orden de compilación igual al del *Project Dependency Editor* del IDE: tiene en cuenta los `<ProjectReference>` de cada `.cwproj`, las `ProjectDependencies` del `.sln` y el orden de declaración, sobre toda la solución.
- Si un `.app` no se puede abrir en modo exclusivo (por ejemplo, porque está abierto en el IDE), se omite con un aviso claro en lugar de llamar a ClarionCL.
- Número de versión en el título del pad, tomado de `AssemblyInfo.cs` al compilar.

### Corregido
- `MSB4019: No se encuentra "SoftVelocity.Build.Clarion.targets"`: MSBuild ahora recibe `ClarionBinPath`, como cuando compila el IDE.
- Cada app se compila sola (`NoDependency=true`), sin recompilar sus dependencias.
- Acentos rotos en el log (la salida de ClarionCL y MSBuild se lee con la codepage OEM).

## [1.0.0] - 2026-09-19

Versión inicial (sin release publicado).
