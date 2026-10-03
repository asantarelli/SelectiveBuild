# SelectiveBuild

Addin para el IDE de **Clarion 11** que compila solo los apps que elijas de una solución multi-DLL, en el mismo orden que usaría el IDE, en lugar de compilar la solución completa.

*English:* Clarion IDE addin that builds only the apps you pick from a multi-DLL solution, in the same order the IDE's Project Dependency Editor uses, instead of rebuilding the whole solution.

---

## Características

- Pad acoplable **Selective Build** con la lista de apps (`.cwproj` de Clarion) de la solución abierta
- Botones **Refrescar**, **Todos** y **Ninguno**, y selector de configuración (**Debug** / **Release**)
- **Compilar seleccionados**: para cada app corre el mismo proceso de dos pasos que el IDE
  1. `ClarionCL.exe /ag "<app>.app" /au` — genera el código fuente
  2. `MSBuild.exe <app>.cwproj` — compila y linkea
- **Orden de compilación igual al del IDE**: el que muestra *Project Dependency Editor → "Projects build in this order"*
- Log en tiempo real de la salida de ClarionCL y MSBuild, con **nivel de detalle** configurable (Mínimo / Solo errores / Todo)
- **Resumen** al terminar: una fila por app con estado (OK / Error / Omitido), cantidad de errores y advertencias, y duración; los errores del app seleccionado se ven en detalle
- Botones **Copiar errores** y **Copiar log completo**, para pegar el resultado en un mail o en un issue
- Recuerda la última selección de cada solución y el nivel de detalle elegido

---

## Requisitos

- Clarion 11 (probado con 11.0.13505)
- .NET Framework 4.0 o superior (el mismo que usa el IDE)

---

## Instalación

**Con Addin Finder:** buscá *SelectiveBuild* e instalalo.

**Manual:** descargá `SelectiveBuild.dll` y `SelectiveBuild.addin` del [último release](https://github.com/asantarelli/SelectiveBuild/releases/latest) y copialos a una carpeta propia dentro de `<Clarion>\accessory\addins\SelectiveBuild\`. Reiniciá el IDE.

Para abrir el pad: menú **Tools → Selective Build**.

---

## Uso

1. Abrí la solución (`.sln`) en el IDE.
2. En el pad, tildá los apps a compilar y elegí la configuración.
3. **Cerrá en el IDE los apps que vayas a compilar.** ClarionCL necesita acceso exclusivo al `.app`; si alguno está abierto, se omite con un aviso en el log y se sigue con el resto.
4. Presioná **Compilar seleccionados**.

Al terminar se abre la pestaña **Resumen**, con una fila por app y el primer app que falló ya seleccionado. En rojo los que fallaron, en naranja los omitidos, en amarillo los que compilaron con advertencias y en verde los que quedaron limpios.

El selector **Detalle** controla solo cuánto se escribe en el log; el Resumen y los botones de copiar siempre tienen la información completa:

| Nivel | Log |
|---|---|
| Mínimo | encabezado de cada app y su resultado |
| Solo errores | encabezados, errores y advertencias |
| Todo | toda la salida de ClarionCL y MSBuild |

---

## Cómo se decide el orden

SelectiveBuild calcula el orden igual que el diálogo *Project Dependency Editor* del IDE, a partir de:

1. los `<ProjectReference>` de cada `.cwproj`;
2. las secciones `ProjectSection(ProjectDependencies)` del `.sln`, que son las que modifican los botones ↑/↓ de ese diálogo;
3. el orden en que los proyectos están declarados en el `.sln`, para desempatar.

El orden se calcula sobre toda la solución y después se filtra a los apps tildados. Si reordenás proyectos en ese diálogo, SelectiveBuild toma el nuevo orden la próxima vez que presiones **Refrescar**.

Cada app se compila **solo** (`NoDependency=true`, igual que "compilar solo el proyecto" en el IDE): sus dependencias no se recompilan salvo que también estén tildadas.

---

## Qué hace en tu máquina

- Ejecuta `ClarionCL.exe` y `MSBuild.exe` (.NET Framework 4.0, 32 bits) sobre los apps que elegiste. Al generar, ClarionCL escribe el código fuente del app, igual que al generar desde el IDE; MSBuild escribe los `.obj`, `.lib` y `.dll`/`.exe` donde lo indique tu proyecto.
- Guarda la última selección en `%APPDATA%\SelectiveBuild\settings.txt`.
- **No se conecta a internet** ni envía datos a ningún lado.

---

## Compilar desde el código

```
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" SelectiveBuild.sln /p:Configuration=Release
```

El proyecto referencia `ICSharpCode.Core.dll` y `ICSharpCode.SharpDevelop.dll` desde `D:\Clarion11\bin`; ajustá los `HintPath` de `SelectiveBuild.csproj` a tu instalación. La versión se define solo en `Properties/AssemblyInfo.cs`: el build la copia al `SelectiveBuild.addin` de `bin\Release`, que es el que hay que distribuir.

---

## Licencia

[MIT](LICENSE) — Copyright (c) 2026 asantarelli
