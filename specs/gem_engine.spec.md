# Especificación Matemática y Funcional: GemEngine
**Documento Clean-Room:** `specs/gem_engine.spec.md`  
**Estado:** Certificado para Sala Limpia  
**Propósito:** Definir el modelo matemático, contratos de entrada/salida y transformaciones geométricas para la colocación e instanciación de gemas sobre curvas y superficies en Rhino 8.

---

## 1. Modelo Canónico de Gema (Canonical Gem Space)

Toda gema se modela en un espacio de referencia canónico $\mathbb{R}^3$ con las siguientes propiedades:
* **Centro de referencia:** El centro geométrico del plano del filetín (*girdle plane*) se ubica exactamente en el origen $(0, 0, 0)$.
* **Orientación canónica:**
  * El plano del filetín es paralelo al plano $XY$ (normal canónica $\hat{n}_0 = (0, 0, 1)$ apuntando hacia la tabla/corona).
  * La culata (*culet*) se sitúa en el semieje negativo $Z$ ($z < 0$).
  * La tabla (*table*) se sitúa en el semieje positivo $Z$ ($z > 0$).
* **Escala normalizada:** El diámetro exterior del filetín (o ancho en gemas rectangulares) es exactamente $d_0 = 1.0\text{ mm}$.

---

## 2. Parámetros de Entrada para el Comando `JS_GemOnCurve`

| Parámetro | Tipo | Rango / Valores | Unidad | Descripción |
| :--- | :--- | :--- | :--- | :--- |
| `RailCurve` | Guid / Curve | Curva 3D abierta o cerrada | — | Curva guía sobre la cual se dispondrán los centros de las gemas. |
| `TargetSurface` | Guid / Surface (Opcional) | Superficie o BrepFace | — | Si se suministra, la normal de la gema se alinea con la normal de la superficie $N(u,v)$. Si no, se usa el marco de Frenet-Serret de la curva. |
| `GemCut` | Enum | `RoundBrilliant`, `Princess`, `Oval`, `Pear`, `Emerald`, `Marquise` | — | Tipo de corte canónico a instanciar. |
| `GemSize` | Double | $[0.8, 25.0]$ | mm | Diámetro exterior del filetín de la gema. |
| `Spacing` | Double | $[-0.5, 5.0]$ | mm | Distancia entre filetines adyacentes a lo largo de la curva. |
| `StartOffset` | Double | $[0.0, 100.0]$ | mm | Distancia desde el inicio de la curva hasta el centro de la primera gema. |
| `EndOffset` | Double | $[0.0, 100.0]$ | mm | Distancia desde el fin de la curva hasta el centro de la última gema. |
| `FlipNormal` | Boolean | `true` / `false` | — | Invierte la dirección del eje de la tabla ($Z \to -Z$). |
| `RotationAngle` | Double | $[0.0, 360.0]$ | grados | Rotación azimutal de la gema alrededor de su eje normal $Z$. |

---

## 3. Algoritmo Matemático de Posicionamiento

### 3.1. Subdivisión por Longitud de Arco (Arc-Length Parameterization)
Dada la curva paramétrica $C(t)$ con $t \in [t_{start}, t_{end}]$ y longitud total $L$:

1. Se define la longitud utilizable:
   $$L_{usable} = L - (\text{StartOffset} + \text{EndOffset})$$
2. El paso lineal entre centros de gemas es:
   $$S_{step} = \text{GemSize} + \text{Spacing}$$
3. El número máximo de gemas $N$ a colocar es:
   $$N = \left\lfloor \frac{L_{usable} - \text{GemSize}}{S_{step}} \right\rfloor + 1$$
4. Para cada gema $i \in [0, N-1]$, la distancia acumulada a lo largo del arco es:
   $$s_i = \text{StartOffset} + \frac{\text{GemSize}}{2} + i \cdot S_{step}$$
5. Se calcula el parámetro correspondiente $t_i$ mediante la función inversa de longitud de arco de RhinoCommon:
   $$t_i = C.\text{DivideByLength}(s_i)$$

---

### 3.2. Construcción del Marco Ortonormal Local (Orthonormal Basis)

En el punto de evaluación $P_i = C(t_i)$:

1. **Vector Tangente:**
   $$\vec{T}_i = \frac{C'(t_i)}{\|C'(t_i)\|}$$
2. **Vector Normal $\vec{N}_i$:**
   * *Caso con Superficie:* Si existe `TargetSurface`, se proyecta $P_i$ sobre la superficie obteniendo parámetros $(u_i, v_i)$ y la normal unitaria:
     $$\vec{N}_i = \frac{\frac{\partial S}{\partial u} \times \frac{\partial S}{\partial v}}{\left\|\frac{\partial S}{\partial u} \times \frac{\partial S}{\partial v}\right\|}$$
   * *Caso sin Superficie:* Se evalúa el vector de curvatura (normal principal de Frenet):
     $$\vec{N}_i = \frac{C''(t_i)}{\|C''(t_i)\|}$$
   * Si `FlipNormal` es verdadero: $\vec{N}_i \leftarrow -\vec{N}_i$.
3. **Vector Binormal:**
   $$\vec{B}_i = \vec{T}_i \times \vec{N}_i$$
4. **Ortogonalización de Gram-Schmidt:** Se recalcula $\vec{T}_i^* = \vec{N}_i \times \vec{B}_i$ para garantizar ortonormalidad estricta con tolerancia $\epsilon \le 10^{-9}$.

---

### 3.3. Matriz de Transformación Afín $4 \times 4$ ($M_i$)

La matriz que proyecta la gema canónica normalizada desde su espacio local al punto 3D en el viewport es el producto de traslación, orientación y escala:

$$M_i = \begin{bmatrix}
B_{i,x} & T_{i,x}^* & N_{i,x} & P_{i,x} \\
B_{i,y} & T_{i,y}^* & N_{i,y} & P_{i,y} \\
B_{i,z} & T_{i,z}^* & N_{i,z} & P_{i,z} \\
0 & 0 & 0 & 1
\end{bmatrix} \cdot R_z(\theta) \cdot \begin{bmatrix}
d & 0 & 0 & 0 \\
0 & d \cdot \text{AspectRatio} & 0 & 0 \\
0 & 0 & d & 0 \\
0 & 0 & 0 & 1
\end{bmatrix}$$

donde:
* $d = \text{GemSize}$.
* $R_z(\theta)$ es la matriz de rotación alrededor del eje $Z$ por el ángulo `RotationAngle`.
* $\text{AspectRatio} = 1.0$ para cortes redondos y cuadrados, o el ratio definido en `gem_proportions.json` para formas alargadas.

---

## 4. Pipeline de Previsualización y Persistencia

1. **Previsualización en GPU (DisplayConduit):**
   * Las $N$ matrices $M_i$ se calculan en la memoria RAM y se transfieren a la tarjeta gráfica.
   * `DisplayConduit.DrawMesh` o `DisplayConduit.DrawShadedPolyline` renderiza la malla canónica multiplicada por cada matriz $M_i$.
   * **Rendimiento:** 500 gemas renderizadas a $>60\text{ FPS}$ con consumo de memoria $< 5\text{ MB}$.
2. **Persistencia en .3dm (UserData):**
   * Los parámetros (`GemSize`, `Spacing`, `GemCut`, etc.) y el `Guid` del Riel padre se serializan en un `ArchivableDictionary` adjunto al objeto de Rhino.
   * Si el riel padre se modifica, se dispara el evento de actualización y el grafo recalcula las matrices $M_i$ sin alterar el historial de Undo de Rhino.
