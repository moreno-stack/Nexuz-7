# Escape Espacial: Nexus-7. Mejoras, Etapa 3 (Fase 3, Sala de energía)

## Cómo reconstruir

1. En Unity, abre el menú **Nexus-7 → Fase 3 → Construir Sala de Energía**. Se enlaza solo con la salida del almacén.
2. Pulsa **Play** en `SalaEnergia`.

## Qué verás

1. **Cinemática «Energía»** (19 s, con subtítulos):
   - la sala con el reactor apagado;
   - un **robot guardia** con su llave inglesa;
   - la barrera eléctrica del ala oeste.

   Se salta manteniendo **Espacio** 1 s.
2. **Dos robots guardia** amarillos y más grandes, con una **llave inglesa gigante**. Su golpe quita 25 en Fácil, con 0,6 s de anticipación y 2 m de alcance. No golpean a través de paredes ni barandillas.
3. **Objetos al azar**, repartidos para que la sala **siempre se pueda terminar**:
   - **zona sur y sala central:** 1 batería, 1 circuito y el **módulo de control del reactor**;
   - **alas y plataforma del reactor:** 1 batería y 1 circuito;
   - el resto de los objetos son chatarra y circuitos.
4. **El reactor ahora necesita el módulo de control.** Es una pieza propia, así que gastar circuitos en hackeos o en el banco ya no puede dejarte sin poder arrancar el reactor.
5. **Banco de trabajo** junto a la entrada:
   - **señuelo sonoro:** 3 chatarra + 1 circuito;
   - **kit de reparación:** 4 chatarra. Restaura 40 de integridad y, con la vida al máximo, no se gasta.

   Selecciónalo con **1–9** y úsalo con **G**.

## Verificación

- **373 comprobaciones automáticas, 0 fallos.** Las 15 nuevas son:

  | Prueba | Comprobaciones | Qué cubre |
  |---|---|---|
  | `EnergyProgressTest` | 5 | Simula la sala con todas las combinaciones del botín garantizado, llegando sin nada; todas se pueden terminar |
  | `Phase3SceneTest` | 7 | Guion de la cinemática |
  | `FixE3Test` | 3 | Correcciones de la revisión |

- Un script comprueba que los 16 puntos de botín quedan fuera del foso, las rampas, los cables, el arco, los transformadores, el reactor, los contenedores, los zócalos, las terminales y el banco, y cada uno en su zona.

## Revisión independiente

Resultado: **«lista con correcciones»**, sin fallos críticos. Se corrigieron estos 3 puntos:

1. **Reactor bloqueado.** Si gastabas todos los circuitos (señuelo y hackeos), el reactor ya no se podía arrancar. Ahora usa su propio **módulo de control**.
2. **Resultados finales.** El kit de reparación contaba como un 4.º tipo de dispositivo, mostraba «4/3» y daba el logro «Arsenal completo» sin haberlo ganado.
3. **Texto de la cámara.** La alarma decía «drones»; ahora dice «robots guardia».

**Detalles menores pendientes:**
- Dos puntos de botín del sur están cerca de la ruta del guardia. Se puede resolver esperando a que pase.
- La guía antigua `GUIA_FASE3.md` está desactualizada.

## Siguiente: Etapa 4 (Fase 4, Laboratorio)

- Robots de investigación con **porra eléctrica**, que aturde.
- Botín al azar (cristal óptico y chatarra para un señuelo garantizados).
- Banco de trabajo.
- Cinemática del laboratorio.
