using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
namespace ConsoleUpdate
{
    class ControladorBD
    {

        private MySqlConnection connection;
        public ControladorBD()
        {
            connection = new MySqlConnection();
            connection.ConnectionString = "server =" + server
                + ";user id=" + user + ";password=" + password
                + ";database=" + database;
        }
        public Factura[] getPendings()
        {
            Factura[] pendingInvoices = new Factura[30];
            int counter = 0;
            String statement = "SELECT SUBSTRING( p.name, 1, (LENGTH( p.name ) -4 )"+
            "), p.time_added, r.nombreProv FROM Pendientes p "+
            "INNER JOIN Recibidas r ON r.idfact = SUBSTRING( p.name, 1, ("+
            "LENGTH( p.name ) -4 )) LIMIT 30 ;";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    pendingInvoices[counter] = new Factura(reader.GetString(1), reader.GetString(0),reader.GetString(2));
                    counter++;
                }
                
            }
            catch (MySqlException)
            {

            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return pendingInvoices;
        }
        public Factura getInvoiceData(String uuid)
        {
            Factura invoice = null;
            String statement = "SELECT EMISOR.RFC,RECEPTOR.RFC,COMPROBANTE.TOTAL,COMPROBANTE.FOLIO FROM TFD_TIMBREFISCALDIGITAL TIMBRE " +
        "INNER JOIN CFDI_COMPLEMENTO COMPLEMENTO ON TIMBRE.CFDI_COMPLEMENTO_FKEY = COMPLEMENTO.CFDI_COMPLEMENTO_PKEY " +
        "INNER JOIN CFDI_COMPROBANTE COMPROBANTE ON COMPLEMENTO.CFDI_COMPROBANTE_FKEY = COMPROBANTE.CFDI_COMPROBANTE_PKEY " +
        "INNER JOIN CFDI_EMISOR EMISOR ON COMPROBANTE.CFDI_COMPROBANTE_PKEY = EMISOR.CFDI_COMPROBANTE_FKEY " +
        "INNER JOIN CFDI_RECEPTOR RECEPTOR ON RECEPTOR.CFDI_COMPROBANTE_FKEY = COMPROBANTE.CFDI_COMPROBANTE_PKEY " +
        "INNER JOIN CFDI_DOMICILIO DOMICILIO ON DOMICILIO.CFDI_RECEPTOR_FKEY = RECEPTOR.CFDI_RECEPTOR_PKEY " +
        "WHERE TIMBRE.UUID = '" + uuid + "' ;";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    if (!(reader.IsDBNull(3)))
                    {
                        invoice = new Factura(reader.GetString(1), reader.GetString(0), uuid, reader.GetString(2), reader.GetString(3));
                    }
                    else
                    {
                        invoice = new Factura(reader.GetString(1), reader.GetString(0), uuid, reader.GetString(2),"");
                    }
                }
                
            }
            catch (MySqlException)
            {            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return invoice;
        }

        
        public Boolean deletePending(String idInvoice)
        {
            connection.Open();
            Boolean success = true;
            String statement = "DELETE FROM Pendientes WHERE name LIKE '%" + idInvoice + "%';";
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }
        public void deleteInvalidRecurrent()
        {
            connection.Open();
            
            String statement = "DELETE FROM gasto_recurrente WHERE cc_fkey = 0 AND trans_fkey = 0;";
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            
        }
        public Solicitud getRequestMain(Factura invoice)
        {
            Solicitud request = null;
            String statement = "SELECT soc_fkey,dep_fkey,EMISOR.NOMBRE,COMPROBANTE.FOLIO,COMPROBANTE.TOTAL FROM provxsociedad " +
                "INNER JOIN CFDI_EMISOR EMISOR ON EMISOR.RFC = '" + invoice.senderRFC + "' " +
                "INNER JOIN CFDI_COMPROBANTE COMPROBANTE ON EMISOR.CFDI_COMPROBANTE_FKEY = COMPROBANTE.CFDI_COMPROBANTE_PKEY " +
                "INNER JOIN CFDI_COMPLEMENTO COMPLEMENTO ON COMPLEMENTO.CFDI_COMPROBANTE_FKEY = COMPROBANTE.CFDI_COMPROBANTE_PKEY " +
                "INNER JOIN TFD_TIMBREFISCALDIGITAL TIMBRE ON TIMBRE.CFDI_COMPLEMENTO_FKEY = COMPLEMENTO.CFDI_COMPLEMENTO_PKEY " +
                "INNER JOIN sociedades ON sociedades.id_soc = soc_fkey WHERE sociedades.rfc = '" +
                invoice.recepientRFC + "' AND rfcE='" + invoice.senderRFC + "' AND TIMBRE.UUID= '" + invoice.uuid + "' group by EMISOR.RFC;";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    if (reader.IsDBNull(3))
                    {
                        request = new Solicitud(reader.GetInt32(0), reader.GetInt32(1).ToString(), reader.GetString(2), reader.GetString(4));
                    }
                    else
                    {
                        request = new Solicitud(reader.GetInt32(0), reader.GetInt32(1).ToString(), reader.GetString(2), reader.GetString(4), reader.GetString(3));
                    }
                    request.uuid = invoice.uuid;
                }
                
            }
            catch (MySqlException)
            {

            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();

            return request;
        }

        public void getChiefEmail(Solicitud request)
        {
            String statement = "SELECT us.correo FROM sociedad soc " +
                "INNER JOIN departamento dep ON dep.dep_pkey = soc.dep_fkey " +
                "INNER JOIN usuario_permisos USER ON USER.us_fkey = dep.us_fkey " +
                "INNER JOIN usuario us ON us.us_pkey = USER.us_fkey " +
                "WHERE soc.soc_fkey = " + request.society.ToString() + " And USER.valor_fkey = 2 And dep.nombre = " + request.department + ";";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        request.chiefEmail = reader.GetString(0);
                    }
                    else
                    {
                        request.chiefEmail = null;
                    }

                }
            }
            catch (MySqlException)
            {

            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
        }
        public void getRequesterEmail(Solicitud request)
        {
            String statement = "SELECT DISTINCT user.correo,dep.nombre FROM usuario user " +
                "INNER JOIN departamento dep ON dep.us_fkey= user.us_pkey " +
                "INNER JOIN usuario_permisos per ON per.us_fkey = user.us_pkey " +
                "WHERE per.valor_fkey = 3 And dep.dep_pkey = " + request.department + " ;";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    request.requesterEmail = reader.GetString(0);
                    request.department = reader.GetString(1);
                }
            }
            catch (MySqlException)
            {

            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
        }
       

        public Boolean existenceRequest(Solicitud request)
        {
            Boolean exist = false;
            String statement = "SELECT * FROM factura WHERE uuid_fkey='" + request.uuid + "' ;";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    exist = true;
                }

            }
            catch (MySqlException)
            {

            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return exist;
        }

        public void updatePending(String uuid)
        {
            String statement ="UPDATE Pendientes SET error='Not found', last_check='" + DateTime.Today.ToShortDateString() + "' WHERE name LIKE '%" +uuid+ "%';";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            { }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
        }

        public void insertRequest(Solicitud request)
        {
            connection.Open();
            String statement = "INSERT INTO factura (uuid_fkey, soc_fkey) VALUES('" + request.uuid + "'," + request.society.ToString() + ");";
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            { }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
        }
        public Boolean insertCanceled(String name, String receiverRFC, String senderRFC, String serial)
        {
            bool success = true;
            connection.Open();
            String statement = "INSERT INTO Canceladas (name,rfcR,rfcE,folio,time_added) VALUES('" + name + "','" + receiverRFC + "','" + senderRFC + "'," +
                "'" + serial + "','" + DateTime.Today.ToShortDateString() + "');";
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }
       

       
    }
}
