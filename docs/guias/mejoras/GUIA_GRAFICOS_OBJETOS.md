# Escape Espacial: Nexus-7. Gráficos de armas, objetos, dispositivos e iconos

Las armas y los objetos ya no son cubos, cilindros y cápsulas de un color: cada uno tiene **su propio modelo**, hecho por código igual que el resto del juego (sin descargar nada). El inventario y la barra rápida muestran además **el icono** de cada objeto.

## Cómo aplicarlo

1. Abre el proyecto y espera a que Unity compile.
2. Ejecuta **Nexus-7 → Construir todo el juego**. Hace falta por el arreglo de las puertas (abajo); los modelos y los iconos se crean solos al jugar.
3. Pulsa **Play**.

## Arreglo: puertas y generadores que no se podían usar

Al reconstruir las fases, las baterías, la llave, las tarjetas, el módulo del reactor y las recetas se guardaban **vacíos** en la escena. La consola decía «objeto imprescindible sin asignar», los objetos no aparecían en el suelo y las puertas y el generador pedían algo que no existía.

**Causa:** cada fase cargaba los objetos y *después* creaba la escena nueva, y crear una escena descarga de la memoria los objetos que ya se habían guardado. **Arreglo:** ahora cada fase crea primero la escena y luego carga sus objetos. La prueba `BuilderItemRefsTest` impide que vuelva a pasar.

## Armas en la mano

| Arma | Cómo se ve |
|---|---|
| Pistola de plasma | Corta, con corredera, empuñadura inclinada, mira y una célula de energía en la culata |
| Escopeta de pulsos | Gruesa, con **dos cañones**, guardamanos de bombeo y culata corta |
| Rifle de asalto | Larga, con cargador curvo, culata, guardamanos, riel y **mira telescópica** con lente que brilla |
| Cañón de riel | La más larga, con **dos raíles**, **3 bobinas** que brillan, batería trasera y pantalla de carga |

Todas llevan una franja del color del arma. **Los accesorios se ven montados**:
- **supresor**: tubo en la boca del cañón;
- **cargador ampliado**: el cargador baja más;
- **estabilizador**: empuñadura vertical bajo el cañón;
- **disipador**: 3 aletas encima.

El arma se vuelve a montar al cambiar de arma o al recoger un accesorio.

## Objetos

| Objeto | Modelo |
|---|---|
| Batería de iones | Celda con tapa, borne y 3 franjas de carga que brillan |
| Chatarra metálica | Placas dobladas y un tornillo |
| Circuito de control | Placa verde con chips, pistas que brillan y conector |
| Cristal óptico | Prisma que brilla sobre una base metálica |
| Llave hidráulica | Mango de goma, cuerpo y boca abierta de acero, rueda de ajuste |
| Tarjeta del director | Tarjeta blanca con banda magnética, chip y holograma |
| Módulo de control del reactor | Caja con asas y un núcleo que brilla |
| Señuelo sonoro | Cilindro con altavoz y antena |
| Pulso PEM | Esfera con dos anillos de energía |
| Camuflaje óptico | Disco con una lente |
| Kit de reparación | Maletín blanco con cruz |
| Munición (la sueltan los robots) | Caja con 3 balas de plasma |
| Accesorio suelto | Maletín morado con la forma del accesorio encima |

- **Imprescindibles:** se siguen viendo 2,2 veces más grandes y conservan el brillo, el haz de luz y el marcador del traje.
- **En la mano:** el objeto de la barra rápida se ve con su modelo.
- **Objetos sueltos de la Fase 1** (las chatarras): también usan su modelo; lo montan al empezar la partida.
- **Lanzados:** el señuelo y el PEM se ven con su modelo. Al apagarse, el altavoz del señuelo se vuelve gris.

## Iconos

Cada icono se dibuja una sola vez a partir del modelo del objeto, en vista 3/4, con luz y contorno.
- **Barra rápida:** una fila de iconos encima del texto. La casilla elegida tiene marco blanco y se ve algo mayor.
- **Inventario [Tab]:** una rejilla de 4 columnas a la derecha, con la cantidad de cada objeto. Los objetos clave llevan marco naranja. El panel es 180 px más ancho para que quepan el texto y la rejilla.
- Para hacer sitio a la fila de iconos, el aviso del banco de trabajo sube a 150 px y los mensajes a 190 px (hace falta reconstruir).
- Un icono tarda poco más de 1 ms en dibujarse: no hay tirón al empezar una fase.

## Añadir el modelo de un objeto nuevo

1. En `Visual/Models/ModelLibrary.Items.cs`, añade su Id a `HasOwnModel`.
2. Escribe un método con sus piezas. Cada pieza lleva:
   - forma: `Box`, `Cylinder`, `Sphere` o `Capsule`;
   - posición y tamaño en metros;
   - giro;
   - papel del material: `Body`, `Metal`, `Grip`, `Accent` o `Glow`.
3. Llámalo desde el `switch` de `Item`.

Si no añades nada, el objeto usa la forma genérica de su categoría.

## Pruebas

`bash Tests/run_all.sh` (1366 comprobaciones, también con Roslyn):

| Suite | Qué comprueba |
|---|---|
| `ModelTest` | Armas distintas y en orden de tamaño; accesorios a la vista; modelos propios de los 7 objetos y los 4 dispositivos; tamaños; formas genéricas |
| `IconTest` | Esquinas transparentes, encuadre, contorno, color del objeto e iconos distintos |
| `ModelFactoryTest` | La escala de las primitivas de Unity y que nada se cree con primitivas sueltas |
| `ItemIconTest` | La caché de iconos y la interfaz |
| `BuilderItemRefsTest` | El orden escena → objetos en los constructores |
| `ReviewFixGfxTest` | Arreglos de la revisión: orden de dibujo y posición de los iconos, chatarras de la Fase 1, velocidad de los iconos y ancho del inventario |
