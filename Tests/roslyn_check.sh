#!/bin/bash
# Compila todo el código con Roslyn, el mismo compilador que usa Unity (más estricto que mcs).
# Necesita el SDK de .NET 8 (dotnet). Uso, desde la raíz del repositorio: bash Tests/roslyn_check.sh
cd "$(dirname "$0")/.." || exit 1
CSC=$(ls -d "$(dirname "$(readlink -f "$(which dotnet)")")"/sdk/*/Roslyn/bincore/csc.dll 2>/dev/null | tail -1)
REF=$(ls -d "$(dirname "$(readlink -f "$(which dotnet)")")"/packs/Microsoft.NETCore.App.Ref/*/ref/net8.0 2>/dev/null | tail -1)
if [ -z "$CSC" ] || [ -z "$REF" ]; then echo "ROSLYN: dotnet no disponible, se omite"; exit 0; fi
mkdir -p Tests/bin
REFS=$(for f in "$REF"/*.dll; do printf -- '-r:%s ' "$f"; done)
out=$(dotnet "$CSC" -nologo -t:library -langversion:9.0 -nostdlib -nowarn:0618,0414,0169,0649,0219,0067,8632,0108,0114 \
      -out:Tests/bin/roslyn_check.dll $REFS Tests/Stubs/*.cs $(find Assets -name '*.cs') 2>&1)
errs=$(echo "$out" | grep -c "error CS")
echo "$out" | grep "error CS"
if [ "$errs" -eq 0 ]; then echo "ROSLYN: compilación OK (como en Unity)"; else echo "ROSLYN: $errs errores"; exit 1; fi
