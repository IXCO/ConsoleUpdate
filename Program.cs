using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUpdate
{
    class Program
    {
        static Boolean isExpired(String date)
        {
            date = date.Substring(0, 2);
            int dayValue= int.Parse(date);
            //Checks if by today have pass 7 day of reception
            if ((DateTime.Today.Day - dayValue) >= 7)
            {
                return true;
            }//In case is change of month or new month 
            else if (dayValue > DateTime.Today.Day)
            {
                //Add a month to today's date 
                //so the substraction works
                int today = DateTime.Today.Day + 30;
                if ((today - dayValue) >= 7)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Actualización de estatus para pendientes.");
            //Initializers
            ControladorBD dbAccess = new ControladorBD();
            Email mail = new Email();
            //Get information from db
            Factura[] pendingInvoices = dbAccess.getPendings();
            Console.WriteLine("Iniciando busqueda...");
            //Checks for pending invoices
            foreach (Factura pending in pendingInvoices)
            {
                //If there is information on the actual invoice 
                //proceed else get out of cicle
                if (pending != null)
                {
                    //Gets data for further use, this means the exportation by Exult was successfully done
                    Factura invoice = dbAccess.getInvoiceData(pending.uuid);
                    mail = new Email(pending.email, pending.timeAdded);
                    if (invoice != null)
                    {
                        //Checks time receive is lower than 7 days
                        if (!isExpired(pending.timeAdded))
                        {
                            Console.WriteLine("Procesando revision con SAT...");
                            //Gets status on SAT webservice
                            switch (invoice.statusOnSAT())
                            {
                                case 0: //Mistake at connection
                                case 2://Pending
                                    Console.WriteLine("Actualizando estatus como pendiente...");
                                    dbAccess.updatePending(invoice.uuid);
                                    break;
                                case 1: //Successful

                                    Console.WriteLine("Exito! Todas las validaciones pasaron.");
                                    //Starts authorization section
                                    Solicitud request = dbAccess.getRequestMain(invoice);
                                    //If there is information to relate the invoice to a requester this proceeds
                                    if (request != null)
                                    {
                                        Console.WriteLine("Inicia revisión para generar solictud...");
                                        //Checks for duplicity
                                        if (!dbAccess.existenceRequest(request))
                                        {
                                            //Send email to requester and direct chief, according to society and department.
                                            String emailContent = "La factura No. " + request.invoiceNo + " del proveedor " + request.sender +
                                            "por un total de: $" + request.total + " ha sido recibida y validada, por lo que se generó una solicitud de egresos.";
                                            Console.WriteLine("Agregando solicitud a BD");
                                            //Adds invoice to tables of request, this triggers further functionality 
                                            dbAccess.insertRequest(request);
                                            dbAccess.getRequesterEmail(request);
                                            mail.from = request.requesterEmail;
                                            Console.WriteLine("Enviando correo a solicitante");
                                            mail.sendAuthorizationEmail(emailContent, request.uuid, request.society.ToString());
                                            dbAccess.getChiefEmail(request);
                                            //Checks for consistency in relation with the authorization process
                                            if (request.chiefEmail != null)
                                            {
                                                mail.from = request.chiefEmail;
                                                Console.WriteLine("Enviando correo a jefe directo");
                                                mail.sendAuthorizationEmail(emailContent, request.uuid, request.society.ToString());
                                            }
                                            else
                                            {
                                                Console.WriteLine("Error: falta de asociatividad con jefe directo.");
                                                mail.from = "ana.arellano@ixco.com.mx";
                                                mail.sendErrorEmail("No se encontro asociacion para sociedad: "+ request.society.ToString());
                                            }
                                            Console.WriteLine("Termina con solicitud");
                                            //Deletes from pending
                                            dbAccess.deletePending(request.uuid);
                                        }
                                        else
                                        {//If the requester was already notify then just delete from pending
                                            dbAccess.deletePending(request.uuid);
                                            Console.WriteLine("Factura ya solicitada.");
                                            Console.WriteLine("Borrando de pendientes...");
                                        }
                                    }
                                    else {//Just erase from pending
                                        dbAccess.deletePending(pending.uuid);
                                        Console.WriteLine("No existe información para solicitud.");
                                        Console.WriteLine("Borrando de pendientes...");
                                    }
                                    break;
                                case 3://Canceled
                                    //Reports error on DB and by mail
                                    dbAccess.deletePending(invoice.uuid);
                                    dbAccess.insertCanceled(invoice.uuid, invoice.recepientRFC, invoice.senderRFC, invoice.folio);
                                    Console.WriteLine("Error: Comprobante cancelado");
                                    Console.WriteLine("Enviando correo de error...");
                                    mail.sendErrorEmail("El comprobante que mando NO fue aceptado, debido a que se encuentra cancelado ante el SAT.");

                                    break;
                                case 4://Incorrect
                                    //Reports error on DB and by mail
                                    dbAccess.deletePending(invoice.uuid);
                                    dbAccess.insertCanceled(invoice.uuid, invoice.recepientRFC, invoice.senderRFC, invoice.folio);
                                    Console.WriteLine("Error: Comprobante incorrecto ante el SAT");
                                    Console.WriteLine("Enviando correo de error...");
                                    mail.sendErrorEmail("El comprobante que mando NO fue aceptado, debido a que el SAT lo marca como incorrecto.");

                                    break;
                                default:
                                    break;
                            }
                        }//If the time span that the invoice has been pending is higher than 7 days
                        else
                        {
                            Console.WriteLine("Error: Tiempo limite de vida pasa de 7 dias");
                            Console.WriteLine("Borrando de pendientes y enviando correo a proveedor...");
                            dbAccess.deletePending(invoice.uuid);
                            dbAccess.insertCanceled(invoice.uuid, invoice.recepientRFC, invoice.senderRFC, invoice.folio);
                            
                        }
                    }//If there is no information for the specific invoice
                    else
                    {
                        Console.WriteLine("Error: Información no encontrada");
                        Console.WriteLine("Borrando de pendientes ...");
                        dbAccess.deletePending(pending.uuid);
                        dbAccess.insertCanceled(pending.uuid, "", "","");
                       
                    }
                }//Empty registries
                else
                {
                    Console.WriteLine("Sin más registros a procesar.");
                    break;
                }
                Console.WriteLine("Siguiente registro.");
            }
            //Deletes invalid registries on db
            dbAccess.deleteInvalidRecurrent();
            Console.WriteLine("Terminando tarea.");
           
        }
    }
}
