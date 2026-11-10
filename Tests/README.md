# Pruebas automáticas

Pruebas de la lógica del juego, ejecutables **sin abrir Unity**. Usan Mono y unas definiciones simplificadas de Unity, que están en `Stubs/`.

```
Tests/
├── Stubs/     ← definiciones simplificadas de UnityEngine, TMPro y UI (solo para compilar fuera de Unity)
├── Suites/    ← una suite por sistema (*Test.cs)
├── run.sh     ← ejecuta una suite:   bash Tests/run.sh DamageTest
└── run_all.sh ← compila todo (incluidos los constructores de escena) y ejecuta todas las suites
```

## Cómo ejecutarlas

Necesitas **Mono** (`mcs` y `mono`):
- **Linux o macOS:** `sudo apt install mono-devel` o `brew install mono`.
- **Windows:** usa WSL (Ubuntu) y dentro instala `mono-devel`.

Desde la raíz del repositorio:

```bash
bash Tests/run_all.sh
```

Si está instalado el SDK de .NET 8 (`dotnet`), también se compila todo con **Roslyn**, el compilador que usa Unity, que es más estricto que `mcs` (`bash Tests/roslyn_check.sh`). Sin `dotnet`, ese paso se omite.

El resultado esperado es `ROSLYN: compilación OK` y `TOTAL: 492 comprobaciones, 0 fallos`. Los ejecutables y los registros se generan en `Tests/bin/`, que no se sube a git.

> Esta carpeta está **fuera de `Assets/`** a propósito: Unity no la compila, porque las definiciones de `Stubs/` chocarían con las reales de Unity.
