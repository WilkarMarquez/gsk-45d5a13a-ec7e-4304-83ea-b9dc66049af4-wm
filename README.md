# Estado de la implementación

## Prototipo funcional

La solución entregada corresponde a un **prototipo funcional** que implementa los requerimientos principales del ejercicio:

- API REST desarrollada en ASP.NET Core 10.
- Arquitectura basada en Clean Architecture, CQRS y MediatR.
- Persistencia utilizando PostgreSQL y Entity Framework Core.
- Endpoints completamente implementados y ejecutables localmente.
- Manejo global de excepciones mediante middleware.
- Documentación técnica de la solución, decisiones de diseño, trade-offs, escalabilidad, seguridad y observabilidad.

## Estado de las pruebas

Durante el tiempo disponible para el desarrollo se priorizó la implementación funcional de la solución, la arquitectura, la consistencia de los datos y la documentación técnica.

Se implementó principalmente una suite de **pruebas unitarias**, enfocadas en validar:

- Casos de uso (Handlers).
- Lógica de agregación.
- Validaciones.
- Reglas de negocio.
- Escenarios de error.

Adicionalmente, se preparó la estructura de los proyectos para **Integration Tests** y **End-to-End Tests**, de acuerdo con la arquitectura propuesta.

Sin embargo, debido a la restricción de tiempo establecida para la prueba, **no fue posible completar ni validar exhaustivamente la suite de pruebas de integración y End-to-End**, por lo que no puedo asegurar una cobertura completa sobre estos niveles de prueba.

De haber contado con más tiempo, la siguiente prioridad habría sido:

1. Completar los Integration Tests sobre repositorios, Entity Framework Core y transacciones.
2. Implementar y validar los End-to-End Tests utilizando `WebApplicationFactory`, cubriendo:
   - Registro de puntuaciones.
   - Consulta de puntuación por usuario.
   - Consulta del leaderboard.
   - Escenarios de concurrencia.
   - Validación de respuestas HTTP y manejo de errores.

Esta habría sido la siguiente etapa natural del desarrollo una vez finalizada la implementación funcional del servicio.

---

# Ejecución del proyecto

## Requisitos

- .NET SDK 10.0
- PostgreSQL 16 o superior

Configurar previamente la cadena de conexión en `appsettings.json` o `appsettings.Development.json`.

### Restaurar dependencias

```bash
dotnet restore
```

### Compilar la solución

```bash
dotnet build
```

# Ejecución de la aplicación

## Restaurar dependencias

```bash
dotnet restore
```

## Compilar la solución

```bash
dotnet build
```

## Ejecutar la API

```bash
dotnet run --project src/Leaderboard.Api
```

---

# Ejecución de pruebas

## Ejecutar todas las pruebas

```bash
dotnet test
```

## Ejecutar únicamente las pruebas unitarias

```bash
dotnet test tests/Leaderboard.UnitTests
```

o

```bash
dotnet test tests/Leaderboard.UnitTests/Leaderboard.UnitTests.csproj
```

## Ejecutar las pruebas de integración

```bash
dotnet test tests/Leaderboard.IntegrationTests
```

o

```bash
dotnet test tests/Leaderboard.IntegrationTests/Leaderboard.IntegrationTests.csproj
```

## Ejecutar las pruebas End-to-End

```bash
dotnet test tests/Leaderboard.E2E
```

o

```bash
dotnet test tests/Leaderboard.E2E/Leaderboard.E2E.csproj
```

> **Nota:** La suite de **Unit Tests** fue implementada y validada completamente. Los proyectos **Leaderboard.IntegrationTests** y **Leaderboard.E2E** fueron creados como parte de la estrategia de pruebas propuesta; sin embargo, debido a la restricción de tiempo de la prueba técnica, no fue posible completar e implementar todos los escenarios de integración y End-to-End.


# Prompts utilizados

A continuación se presentan ejemplos representativos de las interacciones realizadas con la IA durante el desarrollo. Los prompts fueron utilizados para validar decisiones de diseño, contrastar alternativas y documentar la solución; posteriormente las propuestas fueron adaptadas y verificadas manualmente.

### 1. Validación de arquitectura

**Objetivo**

Evaluar si la arquitectura propuesta cumplía con los principios de Clean Architecture y si existían mejoras respecto a la separación de responsabilidades.

**Prompt**

> Revisa la arquitectura propuesta para un servicio de Leaderboard desarrollado en .NET 10 utilizando Clean Architecture, CQRS y MediatR. Analiza la ubicación de las interfaces, responsabilidades de cada capa, dependencias entre proyectos y posibles mejoras para mantener el principio de inversión de dependencias.

**Uso del resultado**

La respuesta fue utilizada para contrastar la organización inicial del proyecto y realizar ajustes en la ubicación de algunas abstracciones, evitando dependencias innecesarias entre Infrastructure y Application.

---

### 2. Estrategia de persistencia

**Objetivo**

Analizar diferentes alternativas para almacenar los eventos de puntuación y responder consultas del leaderboard.

**Prompt**

> Evalúa diferentes estrategias de persistencia para un leaderboard que debe soportar consultas sobre una ventana configurable de tiempo. Compara el uso de agregados persistidos, almacenamiento de eventos y cálculos en tiempo de consulta, indicando ventajas, desventajas y posibles estrategias de evolución hacia producción.

**Uso del resultado**

Se utilizó como insumo para justificar la decisión de almacenar todos los eventos (`ScoreEvents`) como fuente de verdad y mantener un agregado (`UserAggregates`) únicamente para consultas individuales.

---

### 3. Escalabilidad

**Objetivo**

Definir una estrategia de evolución para soportar un volumen elevado de solicitudes.

**Prompt**

> Propón una arquitectura para evolucionar un servicio de Leaderboard desde un prototipo hacia una solución capaz de soportar aproximadamente 100.000 solicitudes por segundo, identificando los componentes que deberían escalar horizontalmente, posibles cuellos de botella y mecanismos para mantener el leaderboard actualizado en near-real-time.

**Uso del resultado**

La información obtenida fue utilizada para elaborar la propuesta de evolución incluida en `SOLUTION.md`, incorporando Azure Service Bus, procesamiento asíncrono y Redis como mecanismo de aceleración de consultas.

---

### 4. Consistencia y concurrencia

**Objetivo**

Revisar mecanismos para garantizar consistencia durante escrituras concurrentes.

**Prompt**

> Analiza estrategias para garantizar consistencia durante escrituras concurrentes en un servicio REST que registra puntuaciones de usuarios utilizando PostgreSQL y Entity Framework Core. Explica las ventajas del uso de transacciones, Unit of Work y las implicaciones sobre la consistencia de los datos.

**Uso del resultado**

Sirvió para validar el uso de transacciones mediante Unit of Work y documentar las garantías de consistencia ofrecidas por la solución.

---

### 5. Observabilidad y operación

**Objetivo**

Identificar buenas prácticas para una solución desplegada en producción.

**Prompt**

> Propón un conjunto de recomendaciones de observabilidad para una API REST desarrollada en ASP.NET Core, incluyendo métricas, trazabilidad distribuida, logging estructurado, monitoreo y alertamiento.

**Uso del resultado**

La respuesta fue utilizada como referencia para la sección de observabilidad y monitoreo del documento técnico.

---

### 6. Documentación técnica

**Objetivo**

Mejorar la documentación de la solución y facilitar su comprensión.

**Prompt**

> Ayúdame a estructurar un documento técnico que describa la arquitectura, decisiones de diseño, trade-offs, estrategia de despliegue, rollback, seguridad, observabilidad y evolución de la solución, incluyendo diagramas Mermaid cuando sean apropiados.

**Uso del resultado**

Se utilizó como apoyo para estructurar el archivo `SOLUTION.md`. Los diagramas y el contenido fueron revisados y adaptados para reflejar la implementación final del proyecto.

---

## Aclaraciones

La inteligencia artificial fue utilizada como una herramienta de apoyo para el análisis técnico, validación de alternativas y documentación. Ninguna respuesta fue incorporada de manera automática; todas las propuestas fueron evaluadas, ajustadas y adaptadas a los requerimientos de la prueba antes de ser implementadas.
