using System; using System.IO; using System.Linq; using System.Collections.Generic; using System.Text.RegularExpressions;
using UnityEngine; using Nexus7.Controls;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static int frame = 1;
  static void Next(){ frame++; }
  static RawTouch Tch(int id, float x, float y, TouchPhaseLite p){ return new RawTouch(id, x, y, p); }
  static bool Near(float a, float b, float eps = 0.02f){ return Math.Abs(a-b) < eps; }
  static void Main(){
    TouchInput.Clock = () => frame;
    TouchInputTests(); LayoutTests(); RouterTests(); GameInputTests();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }

  // ---------------------------------------------------------------- Tarea 1
  static void TouchInputTests(){
    TouchInput.ReleaseAll(); Next();
    TouchInput.Press(KeyCode.Space, 1);
    Check(TouchInput.GetKey(KeyCode.Space) && TouchInput.GetKeyDown(KeyCode.Space), "Pulsar: mantenida y 'Down' en ese fotograma");
    Next();
    Check(TouchInput.GetKey(KeyCode.Space) && !TouchInput.GetKeyDown(KeyCode.Space), "Al fotograma siguiente sigue mantenida pero ya no es 'Down'");
    TouchInput.Press(KeyCode.Space, 2); TouchInput.Release(KeyCode.Space, 1);
    Check(TouchInput.GetKey(KeyCode.Space) && !TouchInput.GetKeyUp(KeyCode.Space) && !TouchInput.GetKeyDown(KeyCode.Space), "Dos dedos en la misma tecla: soltar uno no la suelta");
    Next(); TouchInput.Release(KeyCode.Space, 2);
    Check(!TouchInput.GetKey(KeyCode.Space) && TouchInput.GetKeyUp(KeyCode.Space), "Soltar el último dedo: 'Up' en ese fotograma");
    Next();
    Check(!TouchInput.GetKeyUp(KeyCode.Space), "'Up' dura un solo fotograma");
    TouchInput.Release(KeyCode.E, 5);
    Check(!TouchInput.GetKeyUp(KeyCode.E), "Soltar algo no pulsado no hace nada");
    TouchInput.AddAxis("Horizontal", 0.7f); TouchInput.AddAxis("Horizontal", 0.6f); TouchInput.AddAxis("Mouse X", 3f); TouchInput.AddAxis("Mouse X", 2f);
    Check(Near(TouchInput.Axis("Horizontal"), 1f) && Near(TouchInput.Axis("Mouse X"), 5f), "Ejes: el movimiento se acota a 1, la mirada no");
    TouchInput.AddAxis("Vertical", -3f);
    Check(Near(TouchInput.Axis("Vertical"), -1f), "Ejes: acotado también por abajo");
    Next();
    Check(TouchInput.Axis("Horizontal") == 0f && TouchInput.Axis("Mouse X") == 0f, "Los ejes se ponen a cero en cada fotograma");
    TouchInput.Press(KeyCode.Mouse0, 1); TouchInput.Press(KeyCode.C, 2); Next();
    TouchInput.ReleaseAll();
    Check(!TouchInput.GetKey(KeyCode.Mouse0) && TouchInput.GetKeyUp(KeyCode.Mouse0) && TouchInput.GetKeyUp(KeyCode.C), "Soltar todo da 'Up' de cada tecla pulsada");
    Next();
  }

  // ---------------------------------------------------------------- Tarea 2: distribución
  static void LayoutTests(){
    var sizes = new[]{ new[]{1920f,1080f}, new[]{2400f,1080f}, new[]{1440f,1080f}, new[]{1280f,720f} };
    foreach (TouchContext ctx in Enum.GetValues(typeof(TouchContext))){
      if (ctx == TouchContext.None) { Check(TouchLayout.For(ctx, 1920, 1080).Length == 0, "Sin contexto no hay botones"); continue; }
      foreach (var s in sizes){
        var b = TouchLayout.For(ctx, s[0], s[1]);
        bool inside = b.All(x => x.X - x.Radius >= 0 && x.X + x.Radius <= s[0] && x.Y - x.Radius >= 0 && x.Y + x.Radius <= s[1]);
        bool apart = true;
        for (int i = 0; i < b.Length; i++) for (int j = i + 1; j < b.Length; j++){
          float dx = b[i].X - b[j].X, dy = b[i].Y - b[j].Y;
          if (Math.Sqrt(dx*dx + dy*dy) < b[i].Radius + b[j].Radius) apart = false;
        }
        Check(b.Length > 0 && inside && apart, ctx + " en " + s[0] + "×" + s[1] + ": " + b.Length + " botones dentro de la pantalla y sin solaparse");
      }
    }
    var g = TouchLayout.For(TouchContext.Gameplay, 1920, 1080);
    var keys = new[]{ KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Space, KeyCode.C, KeyCode.E, KeyCode.R, KeyCode.Q, KeyCode.G, KeyCode.Tab, KeyCode.V, KeyCode.JoystickButton4, KeyCode.JoystickButton5 };
    Check(keys.All(k => g.Any(x => x.Key == k)), "Juego: disparar, apuntar, saltar, agacharse, usar, recargar, arma, dispositivo, inventario, cámara y objeto ←/→");
    Check(g.Where(x => x.LooksWhileHeld).Select(x => x.Key).OrderBy(k => k).SequenceEqual(new[]{ KeyCode.Mouse0, KeyCode.Mouse1 }.OrderBy(k => k)), "Disparar y apuntar dejan mirar mientras se mantienen");
    var fire = g.First(x => x.Key == KeyCode.Mouse0);
    Check(fire.Radius >= g.Max(x => x.Radius) && fire.X > 960 && fire.Y < 540, "Disparar es el más grande, abajo a la derecha");
    Check(TouchLayout.HasStick(TouchContext.Gameplay) && TouchLayout.HasLook(TouchContext.Gameplay) && !TouchLayout.HasStick(TouchContext.Hack) && !TouchLayout.HasLook(TouchContext.Ending), "Solo el juego tiene joystick y mirada");
    var hack = TouchLayout.For(TouchContext.Hack, 1920, 1080).Select(x => x.Key).ToList();
    Check(new[]{ KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Space, KeyCode.Q }.All(hack.Contains), "Hackeo: 4 direcciones, interferencia y cancelar");
    var menu = TouchLayout.For(TouchContext.Menu, 1920, 1080).Select(x => x.Key).ToList();
    Check(new[]{ KeyCode.W, KeyCode.S, KeyCode.E, KeyCode.Q }.All(menu.Contains), "Consola y banco: arriba, abajo, aceptar y volver");
    var end = TouchLayout.For(TouchContext.Ending, 1920, 1080).Select(x => x.Key).ToList();
    Check(end.Contains(KeyCode.R) && end.Contains(KeyCode.M), "Pantalla final: reintentar y menú");
    Check(TouchLayout.For(TouchContext.Cinematic, 1920, 1080).Single().Key == KeyCode.Space, "Cinemática: un botón para saltarla (Espacio mantenido)");
    Check(TouchLayout.For(TouchContext.Inventory, 1920, 1080).Any(x => x.Key == KeyCode.Tab), "Inventario abierto: botón para cerrarlo");
    Check(TouchLayout.Pick(true, true, true, true, true, true) == TouchContext.Cinematic, "Prioridad: cinemática");
    Check(TouchLayout.Pick(false, true, true, true, true, true) == TouchContext.Ending, "Prioridad: pantalla final");
    Check(TouchLayout.Pick(false, false, true, true, true, true) == TouchContext.Hack, "Prioridad: hackeo");
    Check(TouchLayout.Pick(false, false, false, true, true, true) == TouchContext.Menu, "Prioridad: consola o banco");
    Check(TouchLayout.Pick(false, false, false, false, true, true) == TouchContext.Inventory, "Prioridad: inventario");
    Check(TouchLayout.Pick(false, false, false, false, false, true) == TouchContext.Gameplay, "Con jugador: juego");
    Check(TouchLayout.Pick(false, false, false, false, false, false) == TouchContext.None, "Sin jugador (menú principal): sin controles táctiles");
  }

  // ---------------------------------------------------------------- Tarea 2: router
  static void RouterTests(){
    const float W = 1920, H = 1080;
    TouchInput.ReleaseAll(); Next();
    var r = new TouchRouter();
    var fire = TouchLayout.For(TouchContext.Gameplay, W, H).First(x => x.Key == KeyCode.Mouse0);
    var jump = TouchLayout.For(TouchContext.Gameplay, W, H).First(x => x.Key == KeyCode.Space);

    // Joystick
    r.Update(TouchContext.Gameplay, new[]{ Tch(1, 300, 300, TouchPhaseLite.Began) }, W, H);
    Check(r.StickActive && Near(r.StickOrigin.x, 300) && TouchInput.Axis("Horizontal") == 0f, "El joystick aparece donde se apoya el dedo (mitad izquierda)");
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(1, 300 + 110 * 0.1f, 300, TouchPhaseLite.Moved) }, W, H);
    Check(TouchInput.Axis("Horizontal") == 0f, "Zona muerta: un 10 % no mueve");
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(1, 300 + 55, 300, TouchPhaseLite.Moved) }, W, H);
    float half = TouchInput.Axis("Horizontal");
    Check(half > 0.35f && half < 0.55f && !TouchInput.GetKey(KeyCode.LeftShift), "A media distancia: algo menos de la mitad y sin correr (" + half.ToString("0.00") + ")");
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(1, 300, 300 + 400, TouchPhaseLite.Moved) }, W, H);
    Check(Near(TouchInput.Axis("Vertical"), 1f) && Near(TouchInput.Axis("Horizontal"), 0f) && TouchInput.GetKey(KeyCode.LeftShift), "A fondo hacia arriba: avanzar al máximo y correr");
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(1, 1500, 700, TouchPhaseLite.Moved) }, W, H);
    Check(r.StickActive && TouchInput.Axis("Mouse X") == 0f, "El dedo del joystick sigue siéndolo aunque cruce a la mitad derecha");
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(1, 1500, 700, TouchPhaseLite.Ended) }, W, H);
    Check(!r.StickActive && !TouchInput.GetKey(KeyCode.LeftShift) && TouchInput.GetKeyUp(KeyCode.LeftShift), "Al levantar el dedo el joystick desaparece y se deja de correr");
    Next(); r.Update(TouchContext.Gameplay, new RawTouch[0], W, H);
    Check(TouchInput.Axis("Horizontal") == 0f && TouchInput.Axis("Vertical") == 0f, "Sin dedos no hay movimiento");

    // Mirar
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(2, 1200, 800, TouchPhaseLite.Began) }, W, H);
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(2, 1300, 780, TouchPhaseLite.Moved) }, W, H);
    float mx = TouchInput.Axis("Mouse X"), my = TouchInput.Axis("Mouse Y");
    Check(Near(mx, 100 * TouchRouter.LookScale, 0.001f) && Near(my, -20 * TouchRouter.LookScale, 0.001f), "Mirar: arrastrar en la mitad derecha mueve la cámara (" + mx.ToString("0.00") + ", " + my.ToString("0.00") + ")");
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(2, 1300, 780, TouchPhaseLite.Stationary) }, W, H);
    Check(TouchInput.Axis("Mouse X") == 0f, "Dedo quieto: la cámara no se mueve");
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(2, 1300, 780, TouchPhaseLite.Ended) }, W, H);
    // a 720p el mismo gesto proporcional da la misma rotación
    var r720 = new TouchRouter();
    Next(); r720.Update(TouchContext.Gameplay, new[]{ Tch(3, 800, 500, TouchPhaseLite.Began) }, 1280, 720);
    Next(); r720.Update(TouchContext.Gameplay, new[]{ Tch(3, 800 + 100 * 720f / 1080f, 500, TouchPhaseLite.Moved) }, 1280, 720);
    Check(Near(TouchInput.Axis("Mouse X"), 100 * TouchRouter.LookScale, 0.01f), "Mirar se escala con la resolución (mismo giro a 720p)");
    Next(); r720.Update(TouchContext.Gameplay, new[]{ Tch(3, 0, 0, TouchPhaseLite.Canceled) }, 1280, 720);

    // Botones
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(4, jump.X, jump.Y, TouchPhaseLite.Began) }, W, H);
    Check(TouchInput.GetKeyDown(KeyCode.Space) && r.PressedButtons.Contains(jump.Id), "Tocar Saltar pulsa Espacio");
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(4, jump.X + 300, jump.Y + 300, TouchPhaseLite.Moved) }, W, H);
    Check(TouchInput.GetKey(KeyCode.Space) && TouchInput.Axis("Mouse X") == 0f, "Saltar sigue pulsado aunque el dedo se salga, y no mira");
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(4, jump.X, jump.Y, TouchPhaseLite.Ended) }, W, H);
    Check(TouchInput.GetKeyUp(KeyCode.Space), "Levantar el dedo suelta Espacio");
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(5, fire.X, fire.Y, TouchPhaseLite.Began) }, W, H);
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(5, fire.X - 40, fire.Y, TouchPhaseLite.Moved) }, W, H);
    Check(TouchInput.GetKey(KeyCode.Mouse0) && Near(TouchInput.Axis("Mouse X"), -40 * TouchRouter.LookScale, 0.001f), "Mantener Disparar y arrastrar: dispara y apunta a la vez");

    // Tres dedos a la vez: mover, mirar y disparar
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(5, fire.X - 40, fire.Y, TouchPhaseLite.Stationary), Tch(6, 300, 300, TouchPhaseLite.Began), Tch(7, 1200, 900, TouchPhaseLite.Began) }, W, H);
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(5, fire.X - 40, fire.Y, TouchPhaseLite.Stationary), Tch(6, 300, 400, TouchPhaseLite.Moved), Tch(7, 1230, 900, TouchPhaseLite.Moved) }, W, H);
    Check(TouchInput.GetKey(KeyCode.Mouse0) && TouchInput.Axis("Vertical") > 0.5f && Near(TouchInput.Axis("Mouse X"), 30 * TouchRouter.LookScale, 0.001f), "Tres dedos: disparar, avanzar y mirar a la vez");

    // Cambio de contexto: todo se suelta y los dedos puestos se ignoran hasta levantarlos
    Next(); r.Update(TouchContext.Hack, new[]{ Tch(5, fire.X, fire.Y, TouchPhaseLite.Stationary), Tch(6, 300, 400, TouchPhaseLite.Moved), Tch(7, 1240, 900, TouchPhaseLite.Moved) }, W, H);
    Check(!TouchInput.GetKey(KeyCode.Mouse0) && TouchInput.GetKeyUp(KeyCode.Mouse0) && !r.StickActive && TouchInput.Axis("Mouse X") == 0f, "Al cambiar de contexto se suelta todo (nada se queda pegado)");
    var up = TouchLayout.For(TouchContext.Hack, W, H).First(x => x.Key == KeyCode.W);
    Next(); r.Update(TouchContext.Hack, new[]{ Tch(5, up.X, up.Y, TouchPhaseLite.Moved) }, W, H);
    Check(!TouchInput.GetKey(KeyCode.W), "Un dedo que ya estaba puesto no pulsa los botones nuevos");
    Next(); r.Update(TouchContext.Hack, new[]{ Tch(8, up.X, up.Y, TouchPhaseLite.Began) }, W, H);
    Check(TouchInput.GetKeyDown(KeyCode.W), "Un dedo nuevo sí: ↑ del hackeo pulsa W");
    Next(); r.Update(TouchContext.Hack, new[]{ Tch(8, up.X, up.Y, TouchPhaseLite.Ended), Tch(5, 0, 0, TouchPhaseLite.Ended), Tch(6, 0, 0, TouchPhaseLite.Ended), Tch(7, 0, 0, TouchPhaseLite.Ended) }, W, H);
    Next(); r.Update(TouchContext.Hack, new[]{ Tch(9, 300, 300, TouchPhaseLite.Began) }, W, H);
    Check(!r.StickActive && TouchInput.Axis("Horizontal") == 0f, "En el hackeo no hay joystick");
    Next(); r.Update(TouchContext.Hack, new[]{ Tch(9, 0, 0, TouchPhaseLite.Ended) }, W, H);

    // Dedos que desaparecen sin 'Ended' (la app pierde el foco) y más de 10 dedos
    Next(); r.Update(TouchContext.Gameplay, new[]{ Tch(10, jump.X, jump.Y, TouchPhaseLite.Began) }, W, H);
    Next(); r.Update(TouchContext.Gameplay, new RawTouch[0], W, H);
    Check(!TouchInput.GetKey(KeyCode.Space), "Un dedo que desaparece sin terminar se suelta igual");
    var many = Enumerable.Range(20, 12).Select(i => Tch(i, 1000 + i * 30, 700, TouchPhaseLite.Began)).ToArray();
    Next(); r.Update(TouchContext.Gameplay, many, W, H);
    Check(r.TrackedFingers == TouchRouter.MaxFingers, "Más de 10 dedos: los sobrantes se ignoran (" + r.TrackedFingers + ")");
    Next(); r.Update(TouchContext.Gameplay, new RawTouch[0], W, H);
    r.Reset();
    Check(r.TrackedFingers == 0, "Reset olvida todos los dedos");
  }

  // ---------------------------------------------------------------- Tarea 3: GameInput
  static void GameInputTests(){
    TouchInput.ReleaseAll(); Next();
    TouchInput.Enabled = true;
    Check(!GameInput.GetKey(KeyCode.Space) && !GameInput.GetKeyDown(KeyCode.Space), "Sin teclado ni toques: nada pulsado");
    TouchInput.Press(KeyCode.Space, 1);
    Check(GameInput.GetKey(KeyCode.Space) && GameInput.GetKeyDown(KeyCode.Space), "GameInput ve la tecla que pulsa lo táctil");
    Next(); TouchInput.Release(KeyCode.Space, 1);
    Check(GameInput.GetKeyUp(KeyCode.Space), "GameInput ve cuándo se suelta");
    TouchInput.Press(KeyCode.Mouse0, 2);
    Check(GameInput.GetMouseButtonDown(0) && !GameInput.GetMouseButtonDown(1), "El botón de disparar cuenta como clic izquierdo");
    TouchInput.AddAxis("Horizontal", 0.6f); TouchInput.AddAxis("Mouse Y", 2.5f);
    Check(Near(GameInput.GetAxisRaw("Horizontal"), 0.6f) && Near(GameInput.GetAxis("Mouse Y"), 2.5f), "Los ejes táctiles se suman a los del Input Manager");
    Check(GameInput.PointerCaptured, "Con controles táctiles la mirada y el disparo no dependen del cursor");
    TouchInput.Enabled = false; Cursor.lockState = CursorLockMode.None;
    Check(!GameInput.PointerCaptured, "En PC dependen del cursor bloqueado, como antes");
    Cursor.lockState = CursorLockMode.Locked;
    Check(GameInput.PointerCaptured, "En PC con el cursor bloqueado: sí");
    Next(); TouchInput.ReleaseAll(); Next();

    // Ningún script del juego lee Input directamente (todo pasa por GameInput)
    var bad = new List<string>();
    var rx = new Regex(@"(?<![A-Za-z])Input\.(GetKey|GetKeyDown|GetKeyUp|GetAxis|GetAxisRaw|GetMouseButton|GetMouseButtonDown|GetMouseButtonUp)\(");
    foreach (var f in Directory.GetFiles("Assets/_Nexus7/Scripts", "*.cs", SearchOption.AllDirectories)){
      string n = f.Replace('\\','/');
      if (n.Contains("/Editor/") || n.Contains("/Controls/")) continue;
      if (rx.IsMatch(File.ReadAllText(f))) bad.Add(Path.GetFileName(f));
    }
    Check(bad.Count == 0, "Todos los scripts leen los controles con GameInput" + (bad.Count > 0 ? " (faltan: " + string.Join(", ", bad) + ")" : ""));
    foreach (var f in new[]{ "Player/PlayerLook.cs", "Combat/PlayerWeapon.cs", "Devices/DeviceQuickbar.cs" })
      Check(File.ReadAllText("Assets/_Nexus7/Scripts/" + f).Contains("GameInput.PointerCaptured") && !File.ReadAllText("Assets/_Nexus7/Scripts/" + f).Contains("Cursor.lockState == CursorLockMode.Locked"), f + ": usa GameInput.PointerCaptured en lugar del cursor");
  }
}
