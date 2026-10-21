# Escape Espacial: Nexus-7. Mejoras, Etapa 2 (Fase 2, Almacén de carga)

## Cómo reconstruir

1. En Unity, abre el menú **Nexus-7 → Fase 2 → Construir Almacén de Carga**.
2. Para jugar seguido desde la Fase 1, reconstruye también la Fase 1. El constructor de la Fase 2 vuelve a enlazar la salida de la Fase 1 con el almacén.
3. Pulsa **Play** en `AlmacenCarga`.

## Qué verás

1. **Cinemática «Almacén»** (19 s, con subtítulos). Se salta manteniendo **Espacio** 1 s. Tiene tres planos:
   - un recorrido aéreo por el almacén;
   - el robot de seguridad con su bate;
   - el montacargas.

   Al terminar aparecen la pista y el objetivo.
2. **Dos robots de seguridad**, redondeados, con **bate** en la mano.
   - Si te alcanzan, levantan el bate y su ojo parpadea en rojo. **Apártate a tiempo para esquivar.**
   - El golpe base quita 25 en Fácil, 37,5 en Normal y 50 en Difícil.
   - Primero lo absorbe el escudo y después la vida.
   - No te golpean a través de paredes, estanterías ni del techo del conducto.
3. **Fuera de combate.** Con la vida a 0, reapareces en el último punto de control (o en la entrada) con la vida y el escudo llenos. Cuenta como una captura. Al reaparecer no haces ruido, y los objetos que ya recogiste no vuelven a aparecer.
4. **Objetos al azar en cada partida:**
   - **siempre** hay 3 baterías y la llave hidráulica, que brillan;
   - el resto es chatarra y circuitos.

   Los contenedores tienen chatarra y circuitos. Ningún objeto cae dentro del conducto.
5. **Banco de trabajo** en el rincón suroeste, con un cartel luminoso «BANCO DE TRABAJO».
   - Al acercarte, el HUD avisa «BANCO DE TRABAJO cerca».
   - Fabrica el **señuelo sonoro** (3 chatarra + 1 circuito). Selecciónalo con **1–9** y **mantén G** (o clic) para lanzarlo.
6. **HUD:** barras de **escudo** (azul) e **integridad**, barra rápida y avisos.

## Verificación

- Compilan todos los scripts, incluidos los 6 constructores y el nuevo `RobotBuilder`.
- **358 comprobaciones automáticas, 0 fallos.** Las 24 nuevas son:

  | Prueba | Comprobaciones | Qué cubre |
  |---|---|---|
  | `WeaponCatalogTest` | 5 | Valores de las 4 armas del diseño |
  | `WorkbenchTest` | 5 | Banco más cercano |
  | `Phase2SceneTest` | 8 | Guion de la cinemática; la intro funciona aunque saltes la cinemática |
  | `FixE2Test` | 6 | Correcciones de la revisión |

- Un script comprueba el plano: los 12 puntos de botín quedan fuera del conducto, de las estanterías, las cajas, el derrumbe, el banco y los contenedores.

## Revisión independiente

Resultado: **«lista con correcciones»**, sin fallos críticos. Se corrigieron estos 4 puntos:

1. **Golpes a través de paredes.** Los robots podían golpearte a través de una pared, del techo del conducto o de la esquina de una caja. Ahora necesitan línea directa; si algo se interpone, el golpe cuenta como esquivado.
2. **Bate escondido.** El bate apuntaba hacia arriba, dentro del brazo, y casi no se veía. Ahora apunta hacia delante y hacia abajo.
3. **Ruido al reaparecer.** Al reaparecer en un punto algo elevado se hacía ruido de aterrizaje cerca de los robots. Ya no ocurre.
4. **Textos.** Los mensajes decían «dron» y ahora dicen «robot de seguridad».

**Detalles menores pendientes** (no impiden jugar):
- El texto del cartel podría partirse en dos líneas; hay que comprobarlo en Unity.
- El inventario no se cierra al recibir un golpe.
- Recoger chatarra ya no hace ruido.
- La guía antigua `GUIA_FASE2.md` aún describe el botín fijo.

## Siguiente: Etapa 3 (Fase 3, Sala de energía)

- Guardias amarillos con llave inglesa gigante.
- Botín al azar (2 baterías y 2 circuitos garantizados).
- Banco con señuelo y kit de reparación.
- Cinemática del reactor.
