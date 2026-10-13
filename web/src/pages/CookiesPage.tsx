import { Link } from 'react-router-dom'
import LegalLayout, { Field } from '../components/LegalLayout'
import { legal } from '../legal/config'

export default function CookiesPage() {
  return (
    <LegalLayout title="Política de cookies">
      <p>
        Esta web no utiliza cookies propias ni de terceros. Sí usa almacenamiento local del navegador
        (<em>localStorage</em> y caché del navegador), en todos los casos estrictamente necesario para que el servicio
        funcione. Por eso, de acuerdo con el art. 22.2 de la LSSI-CE, no es necesario pedir tu consentimiento ni
        mostrar un banner de cookies.
      </p>

      <h2>1. Qué se almacena en tu dispositivo</h2>
      <table className="legal-table">
        <caption className="sr-only">Elementos almacenados en el navegador</caption>
        <thead>
          <tr><th scope="col">Nombre</th><th scope="col">Tipo</th><th scope="col">Finalidad</th><th scope="col">Duración</th><th scope="col">Titular</th></tr>
        </thead>
        <tbody>
          <tr>
            <td><code>frontenis.token</code></td><td>localStorage</td>
            <td>Mantener la sesión de administrador (solo si inicias sesión)</td>
            <td>Hasta que cierres sesión o la borres</td><td>Propia</td>
          </tr>
          <tr>
            <td><code>frontenis.user</code></td><td>localStorage</td>
            <td>Mostrar el nombre del administrador conectado (solo si inicias sesión)</td>
            <td>Hasta que cierres sesión o la borres</td><td>Propia</td>
          </tr>
          <tr>
            <td>Caché de la aplicación (service worker)</td><td>Caché del navegador</td>
            <td>Cargar la web más rápido y permitir su instalación como aplicación</td>
            <td>Hasta que se actualice la web o la borres</td><td>Propia</td>
          </tr>
        </tbody>
      </table>
      <p>
        Los visitantes que no son administradores no guardan datos personales en su dispositivo. No usamos analítica,
        publicidad ni redes sociales integradas.
      </p>

      <h2>2. Proveedores</h2>
      <p>
        Los servidores que alojan la web (Cloudflare, Render) pueden registrar tu dirección IP al atender las
        peticiones, tal como se explica en la <Link to="/privacidad">política de privacidad</Link>.
      </p>

      <h2>3. Cómo eliminarlos</h2>
      <p>
        Puedes borrar el almacenamiento local y la caché desde los ajustes de tu navegador (opción «Borrar datos de
        navegación» o «Datos de sitios»), o cerrando sesión en la web, lo que elimina los datos de sesión. Si lo haces,
        un administrador deberá volver a iniciar sesión.
      </p>

      <h2>4. Cambios</h2>
      <p>
        Si en el futuro se añade analítica, publicidad u otras tecnologías no necesarias, se pedirá antes tu
        consentimiento y se actualizará esta política. Dudas: <Field value={legal.contactEmail} />.
      </p>
    </LegalLayout>
  )
}
