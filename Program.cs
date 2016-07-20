using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using NLog;
namespace ConsoleUpdate
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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
            Logger.Info("Actualización de estatus para pendientes.");
            //Initializers
            ControladorBD dbAccess = new ControladorBD();
            Email mail = new Email();
            //Get information from db
            Factura[] pendingInvoices = dbAccess.getPendings();
            Logger.Debug("Iniciando busqueda...");
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
                    //If there was a successful exportation from Exult to DB, this is not going to be null
                    if (invoice != null)
                    {
                        //Checks time receive is lower than 7 days
                        if (!isExpired(pending.timeAdded))
                        {
                            Logger.Debug("Procesando revision con SAT...");
                            //Gets status on SAT webservice
                            switch (invoice.statusOnSAT())
                            {
                                case 0: //Mistake at connection
                                case 2://Pending
                                    Logger.Info("Actualizando estatus como pendiente...");
                                    dbAccess.updatePending(invoice.uuid);
                                    break;
                                case 1: //Successful

                                    Logger.Info("Exito! Todas las validaciones pasaron.");
                                    //Starts authorization section
                                    Solicitud request = dbAccess.getRequestMain(invoice);
                                    //If there is information to relate the invoice to a requester this proceeds
                                    if (request != null)
                                    {
                                        Logger.Debug("Inicia revisión para generar solictud...");
                                        //Checks for duplicity
                                        if (!dbAccess.existenceRequest(request))
                                        {
                                            //Send email to requester and direct chief, according to society and department.
                                            String emailContent = "La factura No. " + request.invoiceNo + " del proveedor " + request.sender +
                                            "por un total de: $" + request.total + " ha sido recibida y validada, por lo que se generó una solicitud de egresos.";
                                            Logger.Debug("Agregando solicitud a BD");
                                            //Adds invoice to tables of request, this triggers further functionality 
                                            dbAccess.insertRequest(request);
                                            dbAccess.getRequesterEmail(request);
                                            mail.from = request.requesterEmail;
                                            //Sending authorization email for direct responsable
                                            Logger.Debug("Enviando correo a solicitante");
                                            mail.subject = "Solicitud de aprobación";
                                            mail.sendComposeMail(emailContent, request.uuid, request.society.ToString());
                                            dbAccess.getChiefEmail(request);
                                            //Checks for consistency in relation with the authorization process
                                            if (request.chiefEmail != null)
                                            {
                                                //Send authorization email to the chief of the responsable user
                                                mail.from = request.chiefEmail;
                                                Logger.Debug("Enviando correo a jefe directo");
                                                mail.subject = "Solicitud de aprobación";
                                                mail.sendComposeMail(emailContent, request.uuid, request.society.ToString());
                                                
                                            }
                                            else
                                            {
                                                //Sending error notification email to admin
                                                Logger.Warn("Error: falta de asociatividad con jefe directo.");
                                                //NOTE: This email has to be updated accordingly
                                                mail.from = mail.adminAccount;
                                                mail.subject = "Error de recepción";
                                                mail.sendComposeMail("No se encontro asociacion para sociedad: " + request.society.ToString(),"","");
                                                
                                            }
                                            Logger.Debug("Termina con solicitud");
                                            //Deletes from pending
                                            dbAccess.deletePending(request.uuid);
                                        }
                                        else
                                        {//If the requester was already notify then just delete invoice from pending
                                            dbAccess.deletePending(request.uuid);
                                            Logger.Info("Factura ya solicitada.");
                                            Logger.Debug("Borrando de pendientes...");
                                        }
                                    }
                                    else {//Just erase from pending
                                        dbAccess.deletePending(pending.uuid);
                                        Logger.Info("No existe información para solicitud.");
                                        Logger.Debug("Borrando de pendientes...");
                                    }
                                    break;
                                case 3://Canceled
                                    //Reports error on DB and by mail
                                    dbAccess.deletePending(invoice.uuid);
                                    dbAccess.insertCanceled(invoice.uuid, invoice.recepientRFC, invoice.senderRFC, invoice.folio);
                                    Logger.Info("Error: Comprobante cancelado");
                                    Logger.Debug("Enviando correo de error...");
                                    mail.subject = "Error de recepción";
                                    mail.sendComposeMail("El comprobante que mando NO fue aceptado, debido a que se encuentra cancelado ante el SAT.", "", "");
                                    //mail.sendErrorEmail("El comprobante que mando NO fue aceptado, debido a que se encuentra cancelado ante el SAT.");

                                    break;
                                case 4://Incorrect
                                    //Reports error on DB and by mail
                                    dbAccess.deletePending(invoice.uuid);
                                    dbAccess.insertCanceled(invoice.uuid, invoice.recepientRFC, invoice.senderRFC, invoice.folio);
                                    Logger.Info("Error: Comprobante incorrecto ante el SAT");
                                    Logger.Debug("Enviando correo de error...");
                                    mail.subject = "Error de recepción";
                                    mail.sendComposeMail("El comprobante que mando NO fue aceptado, debido a que el SAT lo marca como incorrecto.", "", "");
                                    //mail.sendErrorEmail("El comprobante que mando NO fue aceptado, debido a que el SAT lo marca como incorrecto.");
                                    break;
                                default:
                                    break;
                            }
                        }//If the time span that the invoice has been pending is higher than 7 days, it is deleted from pending.
                        else
                        {
                            Logger.Info("Error: Tiempo limite de vida pasa de 7 dias");
                            Logger.Debug("Borrando de pendientes y enviando correo a proveedor...");
                            dbAccess.deletePending(invoice.uuid);
                            dbAccess.insertCanceled(invoice.uuid, invoice.recepientRFC, invoice.senderRFC, invoice.folio);
                            
                        }
                    }//If there is no information for the specific invoice (Possible incorrect Exult exportation)
                    else
                    {
                        Logger.Info("Error: Información no encontrada");
                        Logger.Debug("Borrando de pendientes ...");
                        dbAccess.deletePending(pending.uuid);
                        dbAccess.insertCanceled(pending.uuid, "", "","");
                       
                    }
                }//Empty registries so it need to get out of loop
                else
                {
                    Logger.Info("Sin más registros a procesar.");
                    break;
                }
                Logger.Debug("Siguiente registro.");
            }
            //Aditional consistency check on DB
            Logger.Info("Haciendo tareas adicionales.");
            //Deletes invalid registries on db
            dbAccess.deleteInvalidRecurrent();
            //Update correct information of finance's users
            dbAccess.updateUsers();
            Logger.Info("Terminando tarea.");
            
           
        }
    }
}
