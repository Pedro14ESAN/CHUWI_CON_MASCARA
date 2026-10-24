# Chuwi — Blender y Unity

Preparado a partir de `CHUWII.blend`, usando `imagetostl_mesh1` (personaje sin gafas). La paleta naranja, crema y roja y la ropa se inspiran en las referencias proporcionadas de ESAN. Se conserva la cabeza del modelo original; no es una reconstrucción exacta de la mascota de las fotografías.

## Archivos

- `CHUWII_Unity.blend`: escena editable, materiales, rig y acciones. Textura empacada.
- `Chuwi_Unity.fbx`: personaje seleccionado, skin y las tres animaciones. Sin cámaras ni luces de estudio.
- `Textures/Chuwi_BaseColor.png`: textura de color de 2048 × 2048.
- `Chuwi_Unity.unitypackage`: modelo, textura, materiales, prefab, Animator y utilidad de configuración.
- `Chuwi_Animations.gif`: vista rápida del movimiento.
- `unity_validation.json`: resultados de la importación y muestreo de animaciones en Unity.

## Uso en Unity

1. Importa `Chuwi_Unity.unitypackage` mediante Assets > Import Package > Custom Package.
2. Arrastra `Assets/Chuwi/Chuwi.prefab` a la escena.
3. El Animator comienza con Walk. Los triggers `Greeting` y `Pointing` activan los gestos y vuelven a Walk al terminar.
4. Para URP, ejecuta `Chuwi > Preparar materiales y Animator` después de importar. La utilidad elige URP/Lit cuando el proyecto usa un render pipeline y ese shader está disponible. La comprobación realizada corresponde al render pipeline integrado de Unity; HDRP requiere adaptar sus materiales.

Si importas solo el FBX, copia también la carpeta `Textures`. Usa Rig = Generic, Avatar Definition = Create From This Model, raíz de movimiento = Root y Scale Factor = 1. Asigna la textura de color al material `Chuwi_ESAN_Palette`. El paquete ya contiene estos ajustes.

## Animaciones

| Clip | Fotogramas de Blender | Duración | Uso |
|---|---|---|---|
| Walk | 1–31 | 1 s a 30 fps | Ciclo en el sitio; Loop Time activado |
| Greeting | 1–76 | 2,5 s a 30 fps | Saludo con la mano derecha y regreso al reposo |
| Pointing | 1–91 | 3 s a 30 fps | Señalar con el brazo/mano derecha, mantener y regresar |

La raíz no se desplaza. Mueve el objeto desde tu controlador o sistema de navegación; `Apply Root Motion` está desactivado. Pointing es un gesto con la mano completa: no tiene un índice articulado independiente.

## Edición en Blender

Selecciona `Chuwi_Rig`, abre Dope Sheet > Action Editor y elige Walk, Greeting o Pointing. Ajusta el final de la línea de tiempo al clip. Las tres pistas NLA permanecen silenciadas para evitar que se superpongan.

Rig FK de 27 huesos: raíz, pelvis, columna, pecho, cuello, cabeza, clavículas, brazos, antebrazos, manos, muslos, piernas, pies, dedos del pie, orejas y cola. No incluye controles IK ni rig facial. Los pesos de skin usan como máximo cuatro influencias por vértice.

Se añadieron torso y extremidades redondeadas para evitar las deformaciones de la malla fusionada original. La geometría fuente de esas zonas sigue en la malla de trabajo detrás del modificador Mask, que puede desactivarse. Los objetos originales de ambas mascotas, el cubo, las cámaras y las luces siguen conservados y ocultos. También hay parches auxiliares ocultos que no se exportan.

El archivo `C:/Proyecto_Chuwi_AR/CHUWII.blend` original no se ha modificado. Su SHA-256 se registra en `build_report.json`.

## Comprobación y alcance

Escala de aproximadamente 1,20 m, eje vertical Y al importar en Unity. Se comprueban tres clips con movimiento, extremos coincidentes, skinning con coordenadas finitas y materiales asignados. El FBX usa Generic por las proporciones del personaje.

Es una malla detallada: consulta el recuento de triángulos y vértices importados en `unity_validation.json`. No se ha probado el rendimiento en un teléfono ni se han generado LOD; para AR móvil conviene medir el rendimiento en el dispositivo de destino.
