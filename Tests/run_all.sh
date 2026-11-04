#!/bin/bash
# Ejecuta todas las suites y comprueba que también compilan los constructores de escena (carpeta Editor).
cd "$(dirname "$0")/.."
mkdir -p Tests/bin
echo -n "Compilación completa (incluye Editor): "
if mcs -target:library -nowarn:414,169,649,219,618,67 -out:Tests/bin/check.dll Tests/Stubs/UnityStubs.cs Tests/Stubs/Phase6Stubs.cs $(find Assets -name '*.cs') > Tests/bin/check.log 2>&1
then echo "OK"; else echo "ERROR"; grep -E "error" Tests/bin/check.log | head -20; exit 1; fi
TOTAL=0; FAILS=0
for f in Tests/Suites/*Test.cs; do
  t=$(basename "$f" .cs)
  out=$(bash Tests/run.sh "$t" 2>&1); code=$?
  ok=$(echo "$out" | grep -c '^OK'); bad=$(echo "$out" | grep -c '^FAIL\|error CS\|Unhandled Exception')
  # Una suite que termina con error o no imprime su resumen también cuenta como fallo
  if [ "$code" != "0" ] || ! echo "$out" | grep -q "TODAS LAS PRUEBAS PASAN\|FALLOS:"; then [ "$bad" = "0" ] && bad=1; fi
  printf "%-22s OK=%-4s FAIL=%s\n" "$t" "$ok" "$bad"
  [ "$bad" != "0" ] && echo "$out" | grep -E "^FAIL|error CS|Unhandled Exception" | head -5
  TOTAL=$((TOTAL+ok)); FAILS=$((FAILS+bad))
done
echo "TOTAL: $TOTAL comprobaciones, $FAILS fallos"
[ "$FAILS" = "0" ]
