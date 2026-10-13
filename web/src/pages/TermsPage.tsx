import LegalLayout, { Field } from '../components/LegalLayout'
import { legal } from '../legal/config'

export default function TermsPage() {
  return (
    <LegalLayout title="Términos y condiciones">
      <p>
        Estos términos regulan el acceso y uso de {legal.siteName} (<Field value={legal.siteUrl} />). Al usar la web
        aceptas estas condiciones; si no estás de acuerdo, no la utilices.
      </p>

      <h2>1. Titular (información legal)</h2>
      <p>
        De acuerdo con el art. 10 de la Ley 34/2002 (LSSI-CE): titular <Field value={legal.ownerName} />, NIF/CIF{' '}
        <Field value={legal.ownerTaxId} />, domicilio <Field value={legal.ownerAddress} />, correo{' '}
        <Field value={legal.contactEmail} />.
      </p>

      <h2>2. Objeto del servicio</h2>
      <p>
        La web permite consultar y gestionar ligas y torneos de frontenis: jugadores, parejas, grupos, calendarios,
        resultados y clasificaciones, además de un cronómetro. La consulta es libre y gratuita. Las funciones de
        gestión están reservadas a administradores con credenciales.
      </p>

      <h2>3. Uso adecuado</h2>
      <ul>
        <li>Debes usar la web de forma lícita, sin dañarla, sobrecargarla ni intentar acceder a zonas restringidas.</li>
        <li>Está prohibido introducir datos falsos, ofensivos o de terceros sin su permiso o el de sus tutores legales.</li>
        <li>Los administradores son responsables de custodiar sus credenciales y de los datos que introducen, y deben
          informar a los participantes y obtener los consentimientos necesarios (véase la política de privacidad).</li>
      </ul>

      <h2>4. Cuentas de administrador</h2>
      <p>
        El acceso de administrador lo concede el titular. Podemos suspenderlo o retirarlo si se hace un uso indebido de
        la web. No compartas tu contraseña.
      </p>

      <h2>5. Exactitud de la información</h2>
      <p>
        Los resultados, clasificaciones y calendarios los introducen los organizadores. Hacemos lo posible por que
        sean correctos, pero pueden contener errores o retrasos. En caso de discrepancia prevalece el criterio del
        comité organizador de cada competición, cuyas bases pueden establecer reglas propias.
      </p>

      <h2>6. Propiedad intelectual</h2>
      <p>
        El diseño, el código y los contenidos de la web, así como el logotipo del club, pertenecen a sus titulares y
        están protegidos por la normativa de propiedad intelectual e industrial. No se permite su reproducción o
        explotación comercial sin autorización, salvo lo permitido por la ley. Los nombres de los participantes
        pertenecen a sus titulares y se tratan según la política de privacidad.
      </p>

      <h2>7. Disponibilidad y responsabilidad</h2>
      <p>
        El servicio se presta «tal cual». Puede haber interrupciones o lentitud (por ejemplo, el servidor puede tardar
        unos segundos en responder tras un periodo sin uso). No garantizamos disponibilidad continua ni ausencia de
        errores. En la medida permitida por la ley, el titular no responde de los daños derivados del uso o la
        imposibilidad de uso de la web, ni de los datos introducidos por terceros. Esto no limita los derechos
        irrenunciables que la normativa reconozca a los consumidores ni la responsabilidad por dolo o negligencia grave.
      </p>

      <h2>8. Enlaces externos</h2>
      <p>La web puede enlazar a sitios de terceros (por ejemplo, la AEPD). No controlamos su contenido ni respondemos de él.</p>

      <h2>9. Modificaciones</h2>
      <p>
        Podemos modificar estos términos o el servicio en cualquier momento. La versión vigente es la publicada aquí,
        con la fecha de actualización al inicio.
      </p>

      <h2>10. Legislación y jurisdicción</h2>
      <p>
        Estos términos se rigen por la ley española. Para cualquier controversia, las partes se someten a los juzgados y
        tribunales que correspondan conforme a la normativa aplicable; si eres consumidor, a los de tu domicilio.
      </p>

      <h2>11. Contacto</h2>
      <p>Para cualquier duda o reclamación escribe a <Field value={legal.contactEmail} />.</p>
    </LegalLayout>
  )
}
