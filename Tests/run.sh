#!/bin/bash
# Ejecuta una suite de pruebas: Tests/run.sh NombreTest
# Compila la suite + las definiciones simplificadas de Unity + los scripts del juego (sin Editor) con Mono.
set -e
cd "$(dirname "$0")/.."
T="$1"
mkdir -p Tests/bin
rm -f "Tests/bin/$T.exe"
mcs -nowarn:414,169,649,219,618,67 -out:"Tests/bin/$T.exe" Tests/Stubs/*.cs "Tests/Suites/$T.cs" \
    $(find Assets -name '*.cs' -not -path '*/Editor/*') > "Tests/bin/$T.build.log" 2>&1 || { grep -E "error CS" "Tests/bin/$T.build.log" | head -20; exit 1; }
mono "Tests/bin/$T.exe"
