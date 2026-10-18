import LegalLayout, { Field } from '../components/LegalLayout'
import { legal } from '../legal/config'

export default function PrivacyPage() {
  return (
    <LegalLayout title="Política de privacidad">
      <p>
        En cumplimiento del Reglamento (UE) 2016/679 (RGPD) y de la Ley Orgánica 3/2018 (LOPDGDD), te informamos de
        cómo tratamos los datos personales en {legal.siteName}.
      </p>

      <h2>1. Responsable del tratamiento</h2>
      <ul>
        <li>Titular: <Field value={legal.ownerName} /></li>
        <li>NIF/CIF: <Field value={legal.ownerTaxId} /></li>
        <li>Domicilio: <Field value={legal.ownerAddress} /></li>
        <li>Correo de contacto: <Field value={legal.contactEmail} /></li>
      </ul>

      <h2>2. Qué datos tratamos y para qué</h2>
      <table className="legal-table">
        <caption className="sr-only">Tratamientos de datos personales</caption>
        <thead>
          <tr><th scope="col">Personas</th><th scope="col">Datos</th><th scope="col">Finalidad</th><th scope="col">Base jurídica</th></tr>
        </thead>
        <tbody>
          <tr>
            <td>Jugadores y participantes</td>
            <td>Nombre, posición de juego (delantero/zaguero), grupo, resultados y clasificaciones</td>
            <td>Organizar ligas y torneos: inscripciones, sorteos, calendarios, resultados y clasificaciones, que se publican en esta web</td>
            <td>Ejecución de la participación en la competición (art. 6.1.b RGPD) y, en su caso, consentimiento (art. 6.1.a)</td>
          </tr>
          <tr>
            <td>Administradores</td>
            <td>Nombre de usuario y contraseña (almacenada cifrada, nunca en claro)</td>
            <td>Permitir el acceso a las funciones de gestión y proteger la web</td>
            <td>Interés legítimo en la seguridad y gestión de la web (art. 6.1.f RGPD)</td>
          </tr>
          <tr>
            <td>Visitantes</td>
            <td>Dirección IP y datos técnicos del navegador que los servidores registran al atender la petición</td>
            <td>Prestar el servicio, garantizar su seguridad y prevenir abusos</td>
            <td>Interés legítimo (art. 6.1.f RGPD)</td>
          </tr>
          <tr>
            <td>Registro de actividad</td>
            <td>Qué pantalla se abre y qué acción se hace (apuntarse, guardar un resultado, iniciar sesión…), con la fecha, el resultado y quién la hace: el nombre del administrador, el del jugador cuyo dispositivo está vinculado, o «visitante». Al vincular o desvincular un móvil con su jugador se anota además el navegador, el sistema y una huella corta del dispositivo</td>
            <td>Seguridad, detección de abusos y poder aclarar quién cambió qué. Las acciones que modifican datos se guardan 30 días y después se borran solas</td>
            <td>Interés legítimo en la seguridad y gestión de la web (art. 6.1.f RGPD)</td>
          </tr>
        </tbody>
      </table>
      <p>
        No elaboramos perfiles ni tomamos decisiones automatizadas con efectos jurídicos. Los sorteos de parejas y
        grupos son aleatorios y sirven solo para organizar la competición. No usamos analítica, publicidad ni
        herramientas de seguimiento.
      </p>

      <h2>3. Publicidad de los datos</h2>
      <p>
        Los nombres de los participantes y los resultados de las competiciones son visibles para cualquier visitante de
        la web, ya que es su finalidad. Si no quieres aparecer, o quieres hacerlo con un alias, solicítalo en el correo
        de contacto.
      </p>

      <h2>4. Menores de edad</h2>
      <p>
        Si un participante es menor de 14 años, su nombre solo se publicará con el consentimiento de sus padres o
        tutores (art. 7 LOPDGDD). Los mayores de 14 años pueden consentir por sí mismos.
      </p>

      <h2>5. Destinatarios y encargados del tratamiento</h2>
      <p>No vendemos ni cedemos tus datos. Para funcionar usamos proveedores tecnológicos que actúan como encargados del tratamiento:</p>
      <ul>
        <li>Cloudflare, Inc. — alojamiento y distribución de la parte visible de la web.</li>
        <li>Render Services, Inc. — alojamiento del servidor (API).</li>
        <li>Microsoft Corporation (Azure SQL) — base de datos.</li>
      </ul>
      <p>
        Estos proveedores pueden tratar datos fuera del Espacio Económico Europeo (por ejemplo, en EE. UU.). Lo hacen
        con las garantías del RGPD, como las cláusulas contractuales tipo de la Comisión Europea o el Marco de
        Privacidad de Datos UE-EE. UU. cuando el proveedor está adherido. Consulta sus políticas para más detalle.
      </p>

      <h2>6. Plazo de conservación</h2>
      <p>
        Los datos de jugadores y competiciones se conservan mientras sean necesarios para la actividad del club y
        hasta que solicites su supresión. Podemos mantenerlos bloqueados el tiempo exigido por las obligaciones
        legales. Los datos de acceso de administradores se conservan mientras tengan esa condición.
      </p>

      <h2>7. Tus derechos</h2>
      <p>
        Puedes ejercer los derechos de acceso, rectificación, supresión, oposición, limitación del tratamiento y
        portabilidad escribiendo a <Field value={legal.contactEmail} />, indicando tu nombre y el derecho que ejercitas.
        Responderemos en el plazo máximo de un mes. Si consideras que no hemos tratado bien tus datos, puedes
        reclamar ante la Agencia Española de Protección de Datos (<a href="https://www.aepd.es" rel="noopener noreferrer">www.aepd.es</a>).
        Si el tratamiento se basa en tu consentimiento, puedes retirarlo en cualquier momento sin que afecte a lo
        anterior.
      </p>

      <h2>8. Seguridad</h2>
      <p>
        Aplicamos medidas técnicas y organizativas razonables: conexión cifrada (HTTPS), contraseñas almacenadas con
        hash, acceso de gestión restringido a administradores y copias gestionadas por el proveedor de la base de datos.
      </p>

      <h2>9. Cambios</h2>
      <p>Podemos actualizar esta política; la fecha de la última versión figura al principio de la página.</p>
    </LegalLayout>
  )
}
